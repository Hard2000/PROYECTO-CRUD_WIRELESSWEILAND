using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using WirelessWeilandCRUD.Models;
using System.Threading.Tasks;

public class AccountController : Controller
{
    private readonly MongoDbService _mongoService;
    private readonly EmailService _emailService;

    public AccountController(MongoDbService mongoService, EmailService emailService)
    {
        _mongoService = mongoService;
        _emailService = emailService;
    }

    // Acción para mostrar la vista de Login
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    // Acción para procesar el formulario de Login
    [HttpPost]
    public IActionResult Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            TempData["Error"] = "Por favor ingrese usuario y contraseña.";
            return RedirectToAction("Login");
        }

        var collection = _mongoService.GetCollection<Usuario>("usuarios");
        var usuario = collection.Find(u => u.Username == username && u.Password == password).FirstOrDefault();

        if (usuario != null)
        {
            HttpContext.Session.SetString("UserRole", usuario.Role);
            HttpContext.Session.SetString("Username", usuario.Username);

            TempData["Message"] = $"Bienvenido, {usuario.Username}.";
            return RedirectToAction("Index", "Home");
        }

        TempData["Error"] = "Usuario o contraseña incorrectos.";
        return RedirectToAction("Login");
    }

    // Acción para cerrar sesión
    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        TempData["Message"] = "Has cerrado sesión exitosamente.";
        return RedirectToAction("Login");
    }

    // Acción para mostrar la vista de Registro
    [HttpGet]
    public IActionResult Register()
    {
        var collection = _mongoService.GetCollection<Usuario>("usuarios");
        var administradorExistente = collection.Find(u => u.Role == "administrador").FirstOrDefault();

        if (administradorExistente != null)
        {
            TempData["Error"] = "Ya existe un administrador registrado. No se permite más de uno.";
            return RedirectToAction("Login");
        }

        return View();
    }

    // Acción para procesar el formulario de Registro
    [HttpPost]
    public IActionResult Register(string username, string password, string confirmPassword, string email)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || 
            string.IsNullOrWhiteSpace(confirmPassword) || string.IsNullOrWhiteSpace(email))
        {
            TempData["Error"] = "Por favor complete todos los campos.";
            return RedirectToAction("Register");
        }

        if (password != confirmPassword)
        {
            TempData["Error"] = "Las contraseñas no coinciden.";
            return RedirectToAction("Register");
        }

        var collection = _mongoService.GetCollection<Usuario>("usuarios");
        var administradorExistente = collection.Find(u => u.Role == "administrador").FirstOrDefault();

        if (administradorExistente != null)
        {
            TempData["Error"] = "Ya existe un administrador registrado. No se permite más de uno.";
            return RedirectToAction("Login");
        }

        var nuevoUsuario = new Usuario
        {
            Username = username,
            Password = password,
            Email = email,
            Role = "administrador"
        };

        collection.InsertOne(nuevoUsuario);

        TempData["Message"] = "Registro exitoso. Por favor inicie sesión.";
        return RedirectToAction("Login");
    }

    // Acción para mostrar la vista de Recuperación de Contraseña
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    // Acción para procesar el formulario de Recuperación de Contraseña
    [HttpPost]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["Error"] = "Por favor, ingresa un correo electrónico.";
            return RedirectToAction("ForgotPassword");
        }

        var collection = _mongoService.GetCollection<Usuario>("usuarios");
        var usuario = collection.Find(u => u.Email.ToLower() == email.ToLower()).FirstOrDefault();

        if (usuario == null)
        {
            TempData["Error"] = "No se encontró un usuario con este correo electrónico.";
            return RedirectToAction("ForgotPassword");
        }

        var recoveryCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
        usuario.RecoveryCode = recoveryCode;

        collection.ReplaceOne(u => u.Id == usuario.Id, usuario);

        var body = $@"
            <h1>Recuperación de contraseña</h1>
            <p>Hola {usuario.Username},</p>
            <p>Tu código de recuperación es:</p>
            <h2 style='color:blue;'>{recoveryCode}</h2>
            <p>Por favor, úsalo para restablecer tu contraseña.</p>
        ";

        try
        {
            await _emailService.SendEmailAsync(email, "Recuperación de contraseña", body);
            TempData["Message"] = "Se ha enviado un correo con el código de recuperación. Revisa tu bandeja de entrada.";
            TempData["Email"] = email; // Guardar email en TempData
            return RedirectToAction("ResetPassword");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ForgotPassword] Error al enviar correo: {ex.Message}");
            TempData["Error"] = "Ocurrió un error al enviar el correo de recuperación.";
            return RedirectToAction("ForgotPassword");
        }
    }

    // Acción para mostrar la vista de Restablecer Contraseña
    [HttpGet]
    public IActionResult ResetPassword()
    {
        ViewBag.Email = TempData["Email"]?.ToString(); // Recuperar correo desde TempData
        return View();
    }

    // Acción para procesar la nueva contraseña
    [HttpPost]
    public IActionResult ResetPassword(string email, string recoveryCode, string newPassword, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(recoveryCode) ||
            string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
        {
            TempData["Error"] = "Todos los campos son obligatorios.";
            return RedirectToAction("ResetPassword");
        }

        if (newPassword != confirmPassword)
        {
            TempData["Error"] = "Las contraseñas no coinciden.";
            return RedirectToAction("ResetPassword");
        }

        var collection = _mongoService.GetCollection<Usuario>("usuarios");
        var usuario = collection.Find(u => u.Email.ToLower() == email.ToLower() && u.RecoveryCode == recoveryCode).FirstOrDefault();

        if (usuario == null)
        {
            TempData["Error"] = "Correo electrónico o código de recuperación inválido.";
            return RedirectToAction("ResetPassword");
        }

        try
        {
            var filter = Builders<Usuario>.Filter.Eq(u => u.Id, usuario.Id);
            var update = Builders<Usuario>.Update
                .Set(u => u.Password, newPassword) // En producción, utiliza un hash seguro
                .Set(u => u.RecoveryCode, null); // Limpia el código de recuperación

            collection.UpdateOne(filter, update);

            TempData["Message"] = "Tu contraseña ha sido restablecida exitosamente.";
            return RedirectToAction("Login");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ResetPassword] Error: {ex.Message}");
            TempData["Error"] = "Ocurrió un error al restablecer la contraseña.";
            return RedirectToAction("ResetPassword");
        }
    }

    // Acción para manejar acceso denegado
    public IActionResult AccessDenied()
    {
        return View();
    }
}
