using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace GEPCP_Ferreteria_El_Pana.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        private static string LimpiarCredencial(string? valor, bool quitarEspaciosInternos = false)
        {
            if (string.IsNullOrWhiteSpace(valor)) return string.Empty;

            var limpio = valor.Trim();
            return quitarEspaciosInternos
                ? new string(limpio.Where(c => !char.IsWhiteSpace(c)).ToArray())
                : limpio;
        }

        private static void ValidarCredenciales(string usuario, string password)
        {
            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("El correo no está configurado correctamente (Email:Usuario / Email:Password).");
        }

        private (string host, int port, string usuario, string password, string nombre) LeerConfiguracion()
        {
            var host = (_config["Email:Host"] ?? "smtp.gmail.com").Trim();
            var portStr = (_config["Email:Port"] ?? "587").Trim();
            var usuario = LimpiarCredencial(_config["Email:Usuario"]);
            var password = LimpiarCredencial(_config["Email:Password"], quitarEspaciosInternos: true);
            var nombre = (_config["Email:Nombre"] ?? "GEPCP Ferretería El Pana").Trim();

            if (!int.TryParse(portStr, out int port)) port = 587;

            return (host, port, usuario, password, nombre);
        }

        private async Task EnviarConMailKitAsync(MimeMessage mensaje, string host, int port, string usuario, string password)
        {
            ValidarCredenciales(usuario, password);

            var secureOptions = port switch
            {
                465 => SecureSocketOptions.SslOnConnect,
                587 => SecureSocketOptions.StartTls,
                _ => SecureSocketOptions.StartTlsWhenAvailable
            };

            for (var intento = 1; intento <= 2; intento++)
            {
                using var smtp = new SmtpClient();

                try
                {
                    smtp.Timeout = 20000;
                    smtp.CheckCertificateRevocation = false;

                    _logger.LogInformation("SMTP conectando a {Host}:{Port} con {SecureOption} como {Usuario} (intento {Intento})",
                        host, port, secureOptions, usuario, intento);

                    await smtp.ConnectAsync(host, port, secureOptions);
                    await smtp.AuthenticateAsync(usuario, password);
                    await smtp.SendAsync(mensaje);
                    await smtp.DisconnectAsync(true);
                    return;
                }
                catch (AuthenticationException ex)
                {
                    throw new InvalidOperationException(
                        "Gmail rechazó la autenticación SMTP (535). Verificá Email:Usuario y que Email:Password sea una App Password vigente de 16 caracteres.", ex);
                }
                catch (Exception ex) when (intento == 1)
                {
                    _logger.LogWarning(ex, "Primer intento SMTP falló. Reintentando en 800ms...");
                    await Task.Delay(800);
                }
                catch (SslHandshakeException ex)
                {
                    throw new InvalidOperationException(
                        $"Error TLS/SSL SMTP al conectar con {host}:{port}. Verificá puerto y seguridad (587=STARTTLS, 465=SSL). Detalle: {ex.Message}", ex);
                }
                catch (SmtpCommandException ex)
                {
                    throw new InvalidOperationException($"Error SMTP: {ex.StatusCode} - {ex.Message}", ex);
                }
                catch (SmtpProtocolException ex)
                {
                    throw new InvalidOperationException($"Error de protocolo SMTP: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"No se pudo enviar correo por SMTP ({host}:{port}). Detalle: {ex.Message}", ex);
                }
            }
        }

        public async Task EnviarCodigoRecuperacionAsync(string destino, string codigo)
        {
            var (host, port, usuario, password, nombre) = LeerConfiguracion();

            _logger.LogInformation("Enviando código de recuperación a {Destino}", destino);

            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("El correo no está configurado en appsettings.json (Email:Usuario / Email:Password).");

            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress(nombre, usuario));
            mensaje.To.Add(MailboxAddress.Parse(destino));
            mensaje.Subject = "Código de recuperación — GEPCP Ferretería El Pana";

            mensaje.Body = new TextPart("html")
            {
                Text = $@"
                    <div style='font-family:sans-serif;max-width:480px;margin:auto;
                                border:1px solid #ddd;border-radius:12px;overflow:hidden;'>
                        <div style='background:#FF7A00;padding:24px;text-align:center;'>
                            <h2 style='color:#fff;margin:0;'>🔐 Recuperación de contraseña</h2>
                            <p style='color:rgba(255,255,255,0.85);margin:6px 0 0;font-size:0.85rem;'>
                                GEPCP — Ferretería El Pana
                            </p>
                        </div>
                        <div style='padding:32px;'>
                            <p style='color:#444;'>Tu código de verificación es:</p>
                            <div style='background:#f4f4f4;border-radius:10px;padding:20px;
                                        text-align:center;letter-spacing:8px;
                                        font-size:2.2rem;font-weight:800;color:#111;'>
                                {codigo}
                            </div>
                            <p style='color:#888;font-size:0.82rem;margin-top:16px;'>
                                Este código expira en <strong>15 minutos</strong>.<br/>
                                Si no solicitaste esto, ignorá este mensaje.
                            </p>
                        </div>
                    </div>"
            };

            try
            {
                await EnviarConMailKitAsync(mensaje, host, port, usuario, password);
                _logger.LogInformation("✓ Código de recuperación enviado a: {Correo}", destino);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar código de recuperación a {Correo}", destino);
                throw new Exception(
                    $"No se pudo enviar el correo. Verificá la contraseña de aplicación Gmail en appsettings.json.\nDetalle: {ex.Message}", ex);
            }
        }

        public async Task<bool> EnviarPDFAsync(
            string destinatario,
            string nombreDestinatario,
            string asunto,
            string cuerpo,
            byte[] pdfBytes,
            string nombreArchivo)
        {
            try
            {
                var (host, port, usuario, password, nombreRem) = LeerConfiguracion();

                var mensaje = new MimeMessage();
                mensaje.From.Add(new MailboxAddress(nombreRem, usuario));
                mensaje.To.Add(new MailboxAddress(nombreDestinatario, destinatario));
                mensaje.Subject = asunto;

                var builder = new BodyBuilder { HtmlBody = cuerpo };
                builder.Attachments.Add(nombreArchivo, pdfBytes, new MimeKit.ContentType("application", "pdf"));
                mensaje.Body = builder.ToMessageBody();

                await EnviarConMailKitAsync(mensaje, host, port, usuario, password);
                _logger.LogInformation("PDF enviado a {Email}: {Asunto}", destinatario, asunto);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar PDF a {Email}", destinatario);
                return false;
            }
        }

        public async Task<bool> EnviarAsync(string destinatario, string asunto, string cuerpo)
        {
            try
            {
                var (host, port, usuario, password, nombreRem) = LeerConfiguracion();

                var mensaje = new MimeMessage();
                mensaje.From.Add(new MailboxAddress(nombreRem, usuario));
                mensaje.To.Add(MailboxAddress.Parse(destinatario));
                mensaje.Subject = asunto;
                mensaje.Body = new TextPart("html") { Text = cuerpo };

                await EnviarConMailKitAsync(mensaje, host, port, usuario, password);
                _logger.LogInformation("Correo enviado a: {Correo}", destinatario);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correo a {Correo}", destinatario);
                return false;
            }
        }
    }
}
