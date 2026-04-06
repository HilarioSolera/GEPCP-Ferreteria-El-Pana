using Microsoft.EntityFrameworkCore;
using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Services;
using GEPCP_Ferreteria_El_Pana.Models;


var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
builder.Services.AddScoped<GEPCP_Ferreteria_El_Pana.Services.ComprobantePlanillaService>();

// ── SERVICIOS ────────────────────────────────────────────────
builder.Services.AddControllersWithViews();
// Reglas de negocio configurables
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

builder.Services.AddDbContext<ApplicationDbContext>(options =>
   options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ComprobantePlanillaService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AuditoriaService>();
builder.Services.AddHttpClient();

// ── CONSTRUCCIÓN DE LA APLICACIÓN ────────────────────────────
var app = builder.Build();

// ── PIPELINE DE MIDDLEWARE ────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}


app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();        // ← DEBE ir aquí, antes de UseAuthorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
