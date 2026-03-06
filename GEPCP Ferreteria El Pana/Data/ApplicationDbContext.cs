using Microsoft.EntityFrameworkCore;
using GEPCP_Ferreteria_El_Pana.Models;

namespace GEPCP_Ferreteria_El_Pana.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Empleado> Empleados { get; set; } = null!;  // ← Cambia a Empleado (entidad)

        // Si tienes otros DbSet, agrégalos aquí

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Opcional: configuraciones extras
            modelBuilder.Entity<Empleado>()
                .Property(e => e.SalarioBase)
                .HasPrecision(18, 2);
        }
    }
}