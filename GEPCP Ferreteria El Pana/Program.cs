using GEPCP_Ferreteria_El_Pana.Services;

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