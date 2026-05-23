using Microsoft.EntityFrameworkCore;
using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Services;
using GEPCP_Ferreteria_El_Pana.Models;
using System.Diagnostics;
using System.Threading;

using var instanciaUnica = new Mutex(true, "Global\\GEPCP_Ferreteria_El_Pana_UnicaInstancia", out var esPrimeraInstancia);
if (!esPrimeraInstancia)
{
	try
	{
		var psi = new ProcessStartInfo
		{
			FileName = "cmd.exe",
			Arguments = "/c start \"\" \"http://localhost:5002\"",
			CreateNoWindow = true,
			UseShellExecute = false,
			WindowStyle = ProcessWindowStyle.Hidden
		};
		Process.Start(psi);
	}
	catch
	{
	}

	return;
}

const int PUERTO = 5002;
var URL = $"http://localhost:{PUERTO}";

// Crear builder sin argumentos para evitar conflictos con launchSettings
var builder = WebApplication.CreateBuilder(new string[] { });

// Configuración ÚNICA del puerto
builder.WebHost.UseUrls(URL);

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

builder.Services.AddControllersWithViews();
builder.Services.Configure<ReglasNegocioConfig>(
	builder.Configuration.GetSection("ReglasNegocio"));

builder.Services.AddSession(options =>
{
	options.IdleTimeout = TimeSpan.FromMinutes(30);
	options.Cookie.HttpOnly = true;
	options.Cookie.IsEssential = true;
	options.Cookie.SameSite = SameSiteMode.Lax;
	options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
connectionString = Environment.ExpandEnvironmentVariables(connectionString!);

// Si la ruta es relativa, usar AppData para publish
if (!Path.IsPathRooted(connectionString.Replace("Data Source=", "")))
{
	var appDataPath = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		"GEPCP_FerreteriaElPana"
	);
	Directory.CreateDirectory(appDataPath);
	connectionString = $"Data Source={Path.Combine(appDataPath, "GEPCP_Ferreteria_El_Pana.db")}";
}

var dbPath = connectionString.Replace("Data Source=", "");
var dbDirectory = Path.GetDirectoryName(dbPath);
if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
{
	Directory.CreateDirectory(dbDirectory);
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
   options.UseSqlite(connectionString));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ComprobantePlanillaService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AuditoriaService>();
builder.Services.AddHttpClient();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
	try
	{
		dbContext.Database.Migrate();

		if (!dbContext.Usuarios.Any())
		{
			var usuarios = new[]
			{
				new Usuario
				{
					NombreUsuario = "admin.rrhh",
					PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pana2024"),
					Rol = "RRHH"
				},
				new Usuario
				{
					NombreUsuario = "jefatura",
					PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pana2024"),
					Rol = "Jefatura"
				}
			};
			dbContext.Usuarios.AddRange(usuarios);
			dbContext.SaveChanges();
		}

		var puestoRrhh = dbContext.Puestos.FirstOrDefault(p => p.PuestoId == 1);
		if (puestoRrhh != null)
		{
			puestoRrhh.Codigo = "TOCG";
			puestoRrhh.Departamento = "Recursos Humanos";
		}

		var puestoVendedor = dbContext.Puestos.FirstOrDefault(p => p.PuestoId == 2);
		if (puestoVendedor != null)
		{
			puestoVendedor.Codigo = "TOCG";
			puestoVendedor.Departamento = "Ventas";
		}

		dbContext.SaveChanges();
	}
	catch
	{
	}
}

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
	app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

app.Use(async (context, next) =>
{
	using var scope = app.Services.CreateScope();
	var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
	var empleadosVencidos = await dbContext.Empleados
		.Where(e => e.Activo &&
			e.TipoContrato == TipoContrato.PlazoFijo &&
			e.FechaVencimientoContrato.HasValue &&
			e.FechaVencimientoContrato.Value.Date <= DateTime.Today)
		.ToListAsync();

	if (empleadosVencidos.Count > 0)
	{
		var empleadosADesactivar = new List<Empleado>();

		foreach (var empleado in empleadosVencidos)
		{
			var tienePrestamosPendientes = await dbContext.Prestamos.AnyAsync(p =>
				p.EmpleadoId == empleado.EmpleadoId && p.Activo && p.Monto > 0);
			var tieneCreditosPendientes = await dbContext.CreditosFerreteria.AnyAsync(c =>
				c.EmpleadoId == empleado.EmpleadoId && c.Activo && c.Saldo > 0);

			if (!tienePrestamosPendientes && !tieneCreditosPendientes)
			{
				empleadosADesactivar.Add(empleado);
			}
		}

		if (empleadosADesactivar.Count > 0)
		{
			foreach (var empleado in empleadosADesactivar)
			{
				empleado.Activo = false;
			}

			await dbContext.SaveChangesAsync();
		}
	}

	await next();
});

app.UseSession();
app.UseAuthorization();

// Registrar controladores API
app.MapControllers();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Splash}/{action=Index}/{id?}");

var desactivarAperturaBrowser =
	string.Equals(Environment.GetEnvironmentVariable("GEPCP_NO_AUTO_BROWSER"), "1", StringComparison.OrdinalIgnoreCase);

// Abrir navegador cuando el servidor esté listo (solo si no se desactiva por lanzador)
if (!desactivarAperturaBrowser)
{
	var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
	lifetime.ApplicationStarted.Register(() =>
	{
		Task.Run(async () =>
		{
			await Task.Delay(2000); // Esperar 2 segundos para que el servidor esté completamente listo

			try
			{
				// Método más confiable para Windows: usar cmd /c start
				var psi = new ProcessStartInfo
				{
					FileName = "cmd.exe",
					Arguments = $"/c start \"\" \"{URL}\"",
					CreateNoWindow = true,
					UseShellExecute = false,
					WindowStyle = ProcessWindowStyle.Hidden
				};
				Process.Start(psi);
			}
			catch
			{
				// Fallback: intentar con UseShellExecute
				try
				{
					var psi2 = new ProcessStartInfo
					{
						FileName = URL,
						UseShellExecute = true
					};
					Process.Start(psi2);
				}
				catch
				{
					// No se pudo abrir el navegador
				}
			}
		});
	});
}

app.Run();
