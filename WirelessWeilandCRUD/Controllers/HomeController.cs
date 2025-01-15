using Microsoft.AspNetCore.Mvc;

namespace WirelessWeiland.Controllers
{
    public class HomeController : Controller
    {
        // Acción para la página de inicio
        public IActionResult Index()
        {
            // Permitir acceso a todos (visitantes y administrador)
            return View();
        }

        // Acción para la página de planes
        public IActionResult Planes()
        {
            // Permitir acceso a todos (visitantes y administrador)
            return View();
        }

        // Acción para la página de clientes (solo administrador)
        public IActionResult Clientes()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "administrador")
            {
                TempData["Error"] = "Acceso denegado. Solo el administrador puede acceder.";
                return RedirectToAction("AccessDenied", "Account");
            }

            return View();
        }

        // Acción para facturación (solo administrador)
        public IActionResult Facturacion()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "administrador")
            {
                TempData["Error"] = "Acceso denegado. Solo el administrador puede acceder.";
                return RedirectToAction("AccessDenied", "Account");
            }

            return View();
        }

        // Acción para manejar la página de acceso denegado
        public IActionResult AccessDenied()
        {
            return View();
        }

        // Acción para la página del Aviso de Privacidad
        public IActionResult AvisoDePrivacidad()
        {
            // Permitir acceso a todos (visitantes y administrador)
            return View();
        }
    }
}
