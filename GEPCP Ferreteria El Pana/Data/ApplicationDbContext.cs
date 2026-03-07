using Microsoft.EntityFrameworkCore;
using GEPCP_Ferreteria_El_Pana.Models;

namespace GEPCP_Ferreteria_El_Pana.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Empleado> Empleados { get; set; } = null!;
        public DbSet<Puesto> Puestos { get; set; } = null!;
        public DbSet<Comision> Comisiones { get; set; } = null!;
        public DbSet<Planilla> Planillas { get; set; } = null!;
        public DbSet<Prestamo> Prestamos { get; set; } = null!;
        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Rol> Roles { get; set; } = null!;

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

            // Precisión decimal
            modelBuilder.Entity<Empleado>()
                .Property(e => e.SalarioBase)
                .HasPrecision(18, 2);

            // Cédula única
            modelBuilder.Entity<Empleado>()
                .HasIndex(e => e.Cedula)
                .IsUnique();

            // Seed: Puestos
            modelBuilder.Entity<Puesto>().HasData(
                new Puesto { PuestoId = 1, Nombre = "Encargada de RR.H.H.", SalarioBase = 450000, Activo = true },
                new Puesto { PuestoId = 2, Nombre = "Vendedor", SalarioBase = 380000, Activo = true }
            );

            // Seed: Roles
            modelBuilder.Entity<Rol>().HasData(
                new Rol { RolId = 1, Nombre = "RRHH" },
                new Rol { RolId = 2, Nombre = "Jefatura" }
            );

            // Seed: Usuarios con hashes fijos (NO llamar BCrypt aquí)
            modelBuilder.Entity<Usuario>().HasData(
                new Usuario
                {
                    UsuarioId = 1,
                    NombreUsuario = "admin.rrhh",
                    NombreCompleto = "Administrador RRHH",
                    PasswordHash = "$2a$11$/mJGbQrxHo3bDUtdY6MWoeaJc/6aYPE7EG9ukr6ln9mNupX3Y8Wz.",
                    Rol = "RRHH"
                },
                new Usuario
                {
                    UsuarioId = 2,
                    NombreUsuario = "jefatura",
                    NombreCompleto = "Usuario Jefatura",
                    PasswordHash = "$2a$11$T72F0Mu8ocYejSTck6bprueMSoi5WgVtSD.hIraw5PvhnjDde6rD6",
                    Rol = "Jefatura"
                }
            );
        }
    }
}