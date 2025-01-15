using Microsoft.AspNetCore.Mvc;

public class PlanesController : Controller
{
    // Método que carga la vista principal de los planes
    public IActionResult Index()
{
    var userRole = HttpContext.Session.GetString("UserRole");
    if (userRole != "cliente" && userRole != "administrador") // Permitir clientes y administradores
    {
        TempData["Error"] = "Acceso denegado. Solo los clientes pueden acceder.";
        return RedirectToAction("AccessDenied", "Account");
    }

    // Lógica de carga de datos de planes
    return View();
}

}
