using System.Net;
using System.Net.Mail;

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

        public async Task EnviarCodigoRecuperacionAsync(string destino, string codigo)
        {
            try
            {
                var host = _config["Email:Host"]!;
                var port = int.Parse(_config["Email:Port"]!);
                var usuario = _config["Email:Usuario"]!;
                var password = _config["Email:Password"]!;
                var nombre = _config["Email:Nombre"]!;

                var mensaje = new MailMessage
                {
                    From = new MailAddress(usuario, nombre),
                    Subject = "Código de recuperación — GEPCP Ferretería El Pana",
                    IsBodyHtml = true,
                    Body = $@"
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

                mensaje.To.Add(destino);

                using var smtp = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(usuario, password),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                await smtp.SendMailAsync(mensaje);
                _logger.LogInformation("Código enviado a: {Correo}", destino);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correo a: {Correo}", destino);
                throw;
            }
        }
    }
}