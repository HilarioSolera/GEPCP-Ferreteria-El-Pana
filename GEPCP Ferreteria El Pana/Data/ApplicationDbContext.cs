using Microsoft.EntityFrameworkCore;
using GEPCP_Ferreteria_El_Pana.Models;

namespace GEPCP_Ferreteria_El_Pana.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Empleado> Empleados { get; set; } = null!;
        public DbSet<Puesto> Puestos { get; set; } = null!;
        public DbSet<Comision> Comisiones { get; set; } = null!;
        public DbSet<Planilla> Planillas { get; set; } = null!;
        public DbSet<Prestamo> Prestamos { get; set; } = null!;
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Rol> Roles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Relaciones
            modelBuilder.Entity<Comision>()
                .HasOne(c => c.Empleado)
                .WithMany(e => e.Comisiones)
                .HasForeignKey(c => c.EmpleadoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Planilla>()
                .HasOne(p => p.Empleado)
                .WithMany(e => e.Planillas)
                .HasForeignKey(p => p.EmpleadoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Prestamo>()
                .HasOne(pr => pr.Empleado)
                .WithMany(e => e.Prestamos)
                .HasForeignKey(pr => pr.EmpleadoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configuraciones de precision
            modelBuilder.Entity<Empleado>()
                .Property(e => e.SalarioBase)
                .HasPrecision(18, 2);

            // Agrega datos de prueba (seed) si quieres
            modelBuilder.Entity<Puesto>().HasData(
                new Puesto { PuestoId = 1, Nombre = "Encargada de RR.H.H.", SalarioBase = 450000, Activo = true },
                new Puesto { PuestoId = 2, Nombre = "Vendedor", SalarioBase = 380000, Activo = true }
            );
        }
    }
}