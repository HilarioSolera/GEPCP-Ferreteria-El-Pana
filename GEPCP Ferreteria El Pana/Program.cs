using Microsoft.EntityFrameworkCore;                    // ← Necesario para UseSqlite
using GEPCP_Ferreteria_El_Pana.Data;                   // Para ApplicationDbContext
using GEPCP_Ferreteria_El_Pana.Services;               // Para IAuthService, AuthService

var builder = WebApplication.CreateBuilder(args);

// ── SERVICIOS ────────────────────────────────────────────────

// Controladores + Vistas (MVC)
builder.Services.AddControllersWithViews();

// Sesión (necesaria para login/roles)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Base de datos - SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Servicios personalizados
builder.Services.AddScoped<IAuthService, AuthService>();

// Si usas Identity en el futuro, agrégalo aquí:
// builder.Services.AddDefaultIdentity<IdentityUser>(options => { ... })
//     .AddEntityFrameworkStores<ApplicationDbContext>();

// ── CONSTRUCCIÓN DE LA APLICACIÓN ─────────────────────────────
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

app.UseSession();          // ← Importante: después de UseRouting, antes de UseAuthorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();