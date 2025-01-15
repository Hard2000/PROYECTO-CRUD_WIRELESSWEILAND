using DinkToPdf;
using DinkToPdf.Contracts;

var builder = WebApplication.CreateBuilder(args);

// Configuración de licencia para QuestPDF
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// Configuración y servicios de MongoDB
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDB")); // 
builder.Services.AddSingleton<MongoDbService>();

// Configuración y registro de EmailSettings y EmailService
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddSingleton<EmailService>();

// Registrar DinkToPdf para generación de PDFs
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

// Configurar servicios de MVC, sesiones y autenticación
builder.Services.AddControllersWithViews()
       .AddSessionStateTempDataProvider(); // Necesario para usar TempData con sesiones

builder.Services.AddSession(); // Activar sesiones

builder.Services.AddAuthentication("CookieAuth")
       .AddCookie("CookieAuth", options =>
       {
           options.LoginPath = "/Account/Login"; // Ruta de inicio de sesión
           options.LogoutPath = "/Account/Logout"; // Ruta de cierre de sesión
           options.Cookie.Name = "UserAuthCookie"; // Nombre de la cookie
           options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Tiempo de expiración de la sesión
       });

// Construcción de la aplicación
var app = builder.Build();

// Configuración del middleware en orden
app.UseStaticFiles();    // Permitir archivos estáticos (CSS, JS, imágenes, etc.)
app.UseRouting();        // Habilitar el enrutamiento de las solicitudes
app.UseAuthentication(); // Manejo de autenticación
app.UseAuthorization();  // Manejo de autorización
app.UseSession();        // Activar soporte para sesiones

// Configuración de rutas
app.MapControllerRoute(
    name: "account",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.MapControllerRoute(
    name: "clientes",
    pattern: "Clientes/{action=Index}/{id?}",
    defaults: new { controller = "Clientes", action = "Index" });

app.MapControllerRoute(
    name: "planes",
    pattern: "Planes/{action=Index}/{id?}",
    defaults: new { controller = "Planes", action = "Index" });

app.MapControllerRoute(
    name: "recibos",
    pattern: "Recibos/{action=GenerarRecibo}/{id?}",
    defaults: new { controller = "Recibos", action = "GenerarRecibo" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Ejecución de la aplicación
app.Run();
