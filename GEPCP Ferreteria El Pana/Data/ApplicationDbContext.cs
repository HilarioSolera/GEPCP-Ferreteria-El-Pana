using GEPCP_Ferreteria_El_Pana.Models;
using Microsoft.EntityFrameworkCore;

namespace GEPCP_Ferreteria_El_Pana.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // DbSets existentes
        public DbSet<HistorialSalario> HistorialSalarios { get; set; } = null!;
        public DbSet<Empleado> Empleados { get; set; } = null!;
        public DbSet<Puesto> Puestos { get; set; } = null!;
        public DbSet<Comision> Comisiones { get; set; } = null!;
        public DbSet<Planilla> Planillas { get; set; } = null!;
        public DbSet<Prestamo> Prestamos { get; set; } = null!;
        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Rol> Roles { get; set; } = null!;
        public DbSet<Aguinaldo> Aguinaldos { get; set; }
        public DbSet<RegistroAuditoria> RegistrosAuditoria { get; set; } = null!;
        public DbSet<AbonoCreditoFerreteria> AbonosCreditoFerreteria { get; set; } = null!;
        public DbSet<Vacacion> Vacaciones { get; set; }


        // DbSets nuevos
        public DbSet<PeriodoPago> PeriodosPago { get; set; } = null!;
        public DbSet<PlanillaEmpleado> PlanillasEmpleado { get; set; } = null!;
        public DbSet<CreditoFerreteria> CreditosFerreteria { get; set; } = null!;
        public DbSet<Incapacidad> Incapacidades { get; set; } = null!;
        public DbSet<HorasExtras> HorasExtras { get; set; } = null!;
        public DbSet<Feriado> Feriados { get; set; } = null!;
        public DbSet<PagoFeriado> PagosFeriado { get; set; } = null!;
        public DbSet<AbonoPrestamo> AbonosPrestamo { get; set; }
     

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed: Puestos predeterminados de la empresa
            modelBuilder.Entity<Puesto>().HasData(
                new Puesto { PuestoId = 100, Departamento = "Administrativo", Nombre = "Asistente", Codigo = "TOCG", SalarioBase = 410855.00m, Activo = true },
                new Puesto { PuestoId = 101, Departamento = "Administrativo", Nombre = "Proveeduría", Codigo = "TOCG", SalarioBase = 492556.00m, Activo = true },
                new Puesto { PuestoId = 102, Departamento = "Caja", Nombre = "Cajero", Codigo = "TOCG", SalarioBase = 477778.00m, Activo = true },
                new Puesto { PuestoId = 103, Departamento = "Ventas", Nombre = "Demostrador-vendedor", Codigo = "TOCG", SalarioBase = 447778.00m, Activo = true },
                new Puesto { PuestoId = 104, Departamento = "Bodega", Nombre = "Bodeguero", Codigo = "TOCG", SalarioBase = 447778.00m, Activo = true },
                new Puesto { PuestoId = 105, Departamento = "Conductores", Nombre = "Conductor", Codigo = "TOCG", SalarioBase = 436585.00m, Activo = true }
            );

            // Relaciones entre entidades

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

            modelBuilder.Entity<PlanillaEmpleado>()
                .HasOne(pe => pe.Empleado)
                .WithMany(e => e.PlanillasEmpleado)
                .HasForeignKey(pe => pe.EmpleadoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlanillaEmpleado>()
                .HasOne(pe => pe.PeriodoPago)
                .WithMany(pp => pp.PlanillasEmpleado)
                .HasForeignKey(pe => pe.PeriodoPagoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indice unico: un empleado no puede tener dos planillas en el mismo periodo
            modelBuilder.Entity<PlanillaEmpleado>()
                .HasIndex(pe => new { pe.EmpleadoId, pe.PeriodoPagoId })
                .IsUnique();

            modelBuilder.Entity<CreditoFerreteria>()
                .HasOne(cf => cf.Empleado)
                .WithMany(e => e.CreditosFerreteria)
                .HasForeignKey(cf => cf.EmpleadoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Incapacidad>()
                .HasOne(i => i.Empleado)
                .WithMany(e => e.Incapacidades)
                .HasForeignKey(i => i.EmpleadoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HorasExtras>()
                .HasOne(he => he.Empleado)
                .WithMany(e => e.HorasExtras)
                .HasForeignKey(he => he.EmpleadoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HorasExtras>()
                .HasOne(he => he.PeriodoPago)
                .WithMany(pp => pp.HorasExtras)
                .HasForeignKey(he => he.PeriodoPagoId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PagoFeriado>()
                .HasOne(pf => pf.Empleado)
                .WithMany(e => e.PagosFeriado)
                .HasForeignKey(pf => pf.EmpleadoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PagoFeriado>()
                .HasOne(pf => pf.Feriado)
                .WithMany(f => f.PagosFeriado)
                .HasForeignKey(pf => pf.FeriadoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PagoFeriado>()
                .HasOne(pf => pf.PeriodoPago)
                .WithMany(pp => pp.PagosFeriado)
                .HasForeignKey(pf => pf.PeriodoPagoId)
                .OnDelete(DeleteBehavior.NoAction);


            modelBuilder.Entity<Aguinaldo>()
    .HasIndex(a => new { a.EmpleadoId, a.Anio })
    .IsUnique();
            modelBuilder.Entity<HistorialSalario>()
    .HasOne(h => h.Empleado)
    .WithMany(e => e.HistorialSalarios)
    .HasForeignKey(h => h.EmpleadoId)
    .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HistorialSalario>()
                .Property(h => h.SalarioAnterior).HasPrecision(18, 2);

            modelBuilder.Entity<HistorialSalario>()
                .Property(h => h.SalarioNuevo).HasPrecision(18, 2);

            modelBuilder.Entity<AbonoPrestamo>()
    .HasOne(a => a.Prestamo)
    .WithMany(p => p.AbonosPrestamo)
    .HasForeignKey(a => a.PrestamoId)
    .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AbonoPrestamo>()
                .Property(a => a.Monto).HasPrecision(18, 2);

            modelBuilder.Entity<AbonoCreditoFerreteria>()
                .HasOne(a => a.CreditoFerreteria)
                .WithMany(c => c.AbonosCreditoFerreteria)
                .HasForeignKey(a => a.CreditoFerreteriaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AbonoCreditoFerreteria>()
                .Property(a => a.Monto).HasPrecision(18, 2);

            // PRECISIÓN DECIMAL

            modelBuilder.Entity<Empleado>()
                .Property(e => e.SalarioBase).HasPrecision(18, 2);

            modelBuilder.Entity<PlanillaEmpleado>()
                .Property(pe => pe.HorasOrdinarias).HasPrecision(18, 2);
            modelBuilder.Entity<PlanillaEmpleado>()
                .Property(pe => pe.HorasExtras).HasPrecision(18, 2);
            modelBuilder.Entity<PlanillaEmpleado>()
                .Property(pe => pe.HorasNoLaboradas).HasPrecision(18, 2);
            modelBuilder.Entity<PlanillaEmpleado>()
                .Property(pe => pe.ValorHora).HasPrecision(18, 2);
            modelBuilder.Entity<PlanillaEmpleado>()
                .Property(pe => pe.ValorHoraExtra).HasPrecision(18, 2);
            modelBuilder.Entity<PlanillaEmpleado>()
                .Property(pe => pe.SalarioOrdinario).HasPrecision(18, 2);
            modelBuilder.Entity<PlanillaEmpleado>()
                .Property(pe => pe.AumentoAplicado).HasPrecision(18, 2);
            modelBuilder.Entity<PlanillaEmpleado>()
                .Property(pe => pe.MontoHorasExtras).HasPrecision(18, 2);
            modelBuilder.Entity<PlanillaEmpleado>()
                .Property(pe => pe.MontoFeriados).HasPrecision(18, 2);
            modelBuilder.Entity<PlanillaEmpleado>()
                .Property(pe => pe.TotalDevengado).HasPrecision(18, 2);
            modelBuilder.Entity<PlanillaEmpleado>()
                .Property(pe => pe.PorcentajeCCSS).HasPrecision(5, 2);
            modelBuilder.Entity<PlanillaEmpleado>()
                .Property(pe => pe.DeduccionCCSS).HasPrecision(18, 2);
            modelBuilder.Entity<PlanillaEmpleado>()
                .Property(pe => pe.DeduccionPrestamos).HasPrecision(18, 2);
            modelBuilder.Entity<PlanillaEmpleado>()
                .Property(pe => pe.DeduccionCreditoFerreteria).HasPrecision(18, 2);
            modelBuilder.Entity<PlanillaEmpleado>()
                .Property(pe => pe.DeduccionIncapacidad).HasPrecision(18, 2);
            modelBuilder.Entity<PlanillaEmpleado>()
                .Property(pe => pe.DeduccionHorasNoLaboradas).HasPrecision(18, 2);
            modelBuilder.Entity<PlanillaEmpleado>()
                .Property(pe => pe.OtrasDeducciones).HasPrecision(18, 2);
            modelBuilder.Entity<PlanillaEmpleado>()
                .Property(pe => pe.TotalDeducciones).HasPrecision(18, 2);
            modelBuilder.Entity<PlanillaEmpleado>()
                .Property(pe => pe.NetoAPagar).HasPrecision(18, 2);

            modelBuilder.Entity<CreditoFerreteria>()
                .Property(cf => cf.MontoTotal).HasPrecision(18, 2);
            modelBuilder.Entity<CreditoFerreteria>()
                .Property(cf => cf.Saldo).HasPrecision(18, 2);
            modelBuilder.Entity<CreditoFerreteria>()
                .Property(cf => cf.CuotaQuincenal).HasPrecision(18, 2);

            modelBuilder.Entity<Incapacidad>()
                .Property(i => i.PorcentajePago).HasPrecision(5, 2);
            modelBuilder.Entity<Incapacidad>()
                .Property(i => i.MontoPorDia).HasPrecision(18, 2);
            modelBuilder.Entity<Incapacidad>()
                .Property(i => i.MontoTotal).HasPrecision(18, 2);

            modelBuilder.Entity<HorasExtras>()
                .Property(he => he.TotalHoras).HasPrecision(10, 2);
            modelBuilder.Entity<HorasExtras>()
                .Property(he => he.ValorHora).HasPrecision(18, 2);
            modelBuilder.Entity<HorasExtras>()
                .Property(he => he.Porcentaje).HasPrecision(5, 2);
            modelBuilder.Entity<HorasExtras>()
                .Property(he => he.MontoTotal).HasPrecision(18, 2);

            modelBuilder.Entity<PagoFeriado>()
                .Property(pf => pf.MontoTotal).HasPrecision(18, 2);

            // ÍNDICE ÚNICO CÉDULA
            modelBuilder.Entity<Empleado>()
                .HasIndex(e => e.Cedula)
                .IsUnique();

            // SEED: Puestos
            modelBuilder.Entity<Puesto>().HasData(new Puesto { PuestoId = 1, Nombre = "Encargada de RR.H.H.", SalarioBase = 450000, Activo = true },
                                                  new Puesto { PuestoId = 2, Nombre = "Vendedor", SalarioBase = 380000, Activo = true });
            // SEED: Roles
            modelBuilder.Entity<Rol>().HasData(
                new Rol { RolId = 1, Nombre = "RRHH" },
                new Rol { RolId = 2, Nombre = "Jefatura" }
            );

            // SEED: Feriados CR 2025-2026
            modelBuilder.Entity<Feriado>().HasData(
                new Feriado { FeriadoId = 1, Fecha = new DateTime(2026, 1, 1), Nombre = "Año Nuevo", Tipo = TipoFeriado.Obligatorio },
                new Feriado { FeriadoId = 2, Fecha = new DateTime(2026, 4, 2), Nombre = "Jueves Santo", Tipo = TipoFeriado.Obligatorio },
                new Feriado { FeriadoId = 3, Fecha = new DateTime(2026, 4, 3), Nombre = "Viernes Santo", Tipo = TipoFeriado.Obligatorio },
                new Feriado { FeriadoId = 4, Fecha = new DateTime(2026, 4, 11), Nombre = "Dia de Juan Santamaria", Tipo = TipoFeriado.NoObligatorio },
                new Feriado { FeriadoId = 5, Fecha = new DateTime(2026, 5, 1), Nombre = "Dia del Trabajador", Tipo = TipoFeriado.Obligatorio },
                new Feriado { FeriadoId = 6, Fecha = new DateTime(2026, 7, 25), Nombre = "Anexion Guanacaste", Tipo = TipoFeriado.NoObligatorio },
                new Feriado { FeriadoId = 7, Fecha = new DateTime(2026, 8, 2), Nombre = "Virgen de los Angeles", Tipo = TipoFeriado.NoObligatorio },
                new Feriado { FeriadoId = 8, Fecha = new DateTime(2026, 8, 15), Nombre = "Dia de la Madre", Tipo = TipoFeriado.NoObligatorio },
                new Feriado { FeriadoId = 9, Fecha = new DateTime(2026, 9, 15), Nombre = "Dia de la Independencia", Tipo = TipoFeriado.Obligatorio },
                new Feriado { FeriadoId = 10, Fecha = new DateTime(2026, 12, 25), Nombre = "Navidad", Tipo = TipoFeriado.Obligatorio }
            );

            // SEED: Usuarios
            modelBuilder.Entity<Usuario>().HasData(
    new Usuario
    {
        UsuarioId = 1,
        NombreUsuario = "admin.rrhh",
        NombreCompleto = "Administrador RRHH",
        PasswordHash = "$2a$11$/mJGbQrxHo3bDUtdY6MWoeaJc/6aYPE7EG9ukr6ln9mNupX3Y8Wz.",
        Rol = "RRHH",
        CorreoElectronico = "solerahilario207@gmail.com"
    },
    new Usuario
    {
        UsuarioId = 2,
        NombreUsuario = "jefatura",
        NombreCompleto = "Usuario Jefatura",
        PasswordHash = "$2a$11$T72F0Mu8ocYejSTck6bprueMSoi5WgVtSD.hIraw5PvhnjDde6rD6",
        Rol = "Jefatura",
        CorreoElectronico = "solerahilario207@gmail.com"
    }
);

        }
    }
}
