using GEPCP_Ferreteria_El_Pana.Data;
using GEPCP_Ferreteria_El_Pana.Services;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// ── Configuración de la base de datos ── SQLite (desarrollo y producción)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Si ya tienes Identity o autenticación, mantenla aquí
// builder.Services.AddDefaultIdentity<IdentityUser>(...)

// ... resto del código (AddRazorPages si usas, etc.)

var app = builder.Build();

// ... Configure the HTTP request pipeline (middleware)

var builder = WebApplication.CreateBuilder(args);

// ==================== SERVICIOS ====================
builder.Services.AddControllersWithViews();

// === SESIÓN (OBLIGATORIO para login y roles) ===
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// === SERVICIOS DE SEGURIDAD ===
builder.Services.AddScoped<IAuthService, AuthService>();

// ==================== PIPELINE ====================
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// ←←← ESTA LÍNEA ES LA QUE FALTABA ←←←
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");




app.Run();