using Microsoft.EntityFrameworkCore;
using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Services;
using GEPCP_Ferreteria_El_Pana.Models;
using System.Diagnostics;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

// Ocultar consola después de 3 segundos
Task.Run(async () =>
{
    await Task.Delay(3000);
    var handle = GetConsoleWindow();
    ShowWindow(handle, SW_HIDE);
});

const int PUERTO_FIJO = 5002;
builder.WebHost.UseUrls($`http://localhost:{PUERTO_FIJO}`);

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

builder.Services.AddControllersWithViews();
builder.Services.Configure<ReglasNegocioConfig>(
    builder.Configuration.GetSection(`ReglasNegocio`));

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

var connectionString = builder.Configuration.GetConnectionString(`DefaultConnection`);
connectionString = Environment.ExpandEnvironmentVariables(connectionString!);

var dbPath = connectionString.Replace(`Data Source=`, `);
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
                    NombreUsuario = `admin.rrhh`,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(`Pana2024`),
                    Rol = `RRHH`
                },
                new Usuario
                {
                    NombreUsuario = `jefatura`,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(`Pana2024`),
                    Rol = `Jefatura`
                }
            };
            dbContext.Usuarios.AddRange(usuarios);
            dbContext.SaveChanges();
        }
    }
    catch
    {
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(`/Home/Error`);
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: `default`,
    pattern: `{controller=Splash}/{action=Index}/{id?}`);

// Abrir navegador automáticamente
var url = $`http://localhost:{PUERTO_FIJO}`;
Process.Start(new ProcessStartInfo
{
    FileName = url,
    UseShellExecute = true
});

app.Run();

[DllImport(`kernel32.dll`)]
static extern IntPtr GetConsoleWindow();

[DllImport(`user32.dll`)]
static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

const int SW_HIDE = 0;