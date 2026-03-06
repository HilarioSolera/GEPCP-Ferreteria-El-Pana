using Microsoft.EntityFrameworkCore;
using GEPCP_Ferreteria_El_Pana.Models;   // ajusta el namespace si tu modelo está en otro lugar

namespace GEPCP_Ferreteria_El_Pana.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<EmpleadoViewModel> Empleados { get; set; } = null!;

        // Agrega después cuando los implementes:
        // public DbSet<Comision> Comisiones { get; set; }
        // public DbSet<Planilla> Planillas { get; set; }
        // public DbSet<Prestamo> Prestamos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Opcional: configuraciones adicionales
            modelBuilder.Entity<EmpleadoViewModel>()
                .Property(e => e.SalarioBase)
                .HasColumnType("decimal(18,2)");
        }
    }
}