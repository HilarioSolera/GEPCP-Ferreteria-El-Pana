using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Models;
using Microsoft.EntityFrameworkCore;

namespace GEPCP_Ferreteria_El_Pana.Services
{
    public class AuditoriaService
    {
        private readonly ApplicationDbContext _context;

        public AuditoriaService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task RegistrarAsync(
            string usuario,
            string accion,
            string modulo,
            string? detalle = null,
            string? ip = null)
        {
            try
            {
                _context.RegistrosAuditoria.Add(new RegistroAuditoria
                {
                    Usuario = usuario,
                    Accion = accion,
                    Modulo = modulo,
                    Detalle = detalle,
                    IpAddress = ip,
                    FechaHora = DateTime.Now
                });

                // Limpiar registros con más de 90 días
                var limite = DateTime.Now.AddDays(-90);
                var viejos = _context.RegistrosAuditoria
                    .Where(r => r.FechaHora < limite);
                _context.RegistrosAuditoria.RemoveRange(viejos);

                await _context.SaveChangesAsync();
            }
            catch { /* No interrumpir el flujo si falla la auditoría */ }
        }
    }
}