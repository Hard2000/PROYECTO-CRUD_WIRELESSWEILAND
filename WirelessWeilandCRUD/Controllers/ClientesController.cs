using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using WirelessWeilandCRUD.Models;
using System.Collections.Generic;
using System.Linq;

public class ClientesController : Controller
{
    private readonly MongoDbService _mongoService;
    private static readonly List<string> PlanesDeRenta = new List<string>
    {
        "Básico - $300",
        "Estándar - $450",
        "Premium - $600"
    };

    public ClientesController(MongoDbService mongoService)
    {
        _mongoService = mongoService;
    }

    // Método para listar todos los clientes
public IActionResult Index(string sortOrder = "fecha_desc")
{
    // Verificar si el usuario tiene el rol de administrador
    var userRole = HttpContext.Session.GetString("UserRole");
    if (userRole != "administrador")
    {
        TempData["Error"] = "Acceso denegado. Solo los administradores pueden acceder a esta sección.";
        return RedirectToAction("AccessDenied", "Account");
    }

    try
    {
        var collection = _mongoService.GetCollection<Cliente>("clientes");

        // Ordenar según el parámetro 'sortOrder'
        var sortDefinition = sortOrder switch
        {
            "fecha_desc" => Builders<Cliente>.Sort.Descending(c => c.FechaInicio),
            "fecha_asc" => Builders<Cliente>.Sort.Ascending(c => c.FechaInicio),
            _ => Builders<Cliente>.Sort.Descending(c => c.FechaInicio) // Predeterminado: fecha más reciente primero
        };

        var clientes = collection.Find(Builders<Cliente>.Filter.Empty).Sort(sortDefinition).ToList();

        return View(clientes);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Index] Error al obtener clientes: {ex.Message}");
        TempData["Error"] = "Error al cargar los clientes.";
        return View("Error");
    }
}


    // Método GET para mostrar la vista de creación
    public IActionResult Create()
    {
        // Verificar si el usuario tiene el rol de administrador
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "administrador")
        {
            TempData["Error"] = "Acceso denegado. Solo los administradores pueden acceder a esta sección.";
            return RedirectToAction("AccessDenied", "Account");
        }

        ViewBag.PlanesDeRenta = PlanesDeRenta;
        return View(new Cliente { Activo = true });
    }



[HttpGet]
public IActionResult BuscarClientes(string query)
{
    // Verificar si el usuario tiene el rol de administrador
    var userRole = HttpContext.Session.GetString("UserRole");
    if (userRole != "administrador")
    {
        TempData["Error"] = "Acceso denegado. Solo los administradores pueden acceder a esta sección.";
        return Unauthorized(); // Devuelve un código de estado HTTP 401
    }

    try
    {
        var collection = _mongoService.GetCollection<Cliente>("clientes");

        // Si la consulta está vacía, devolver todos los clientes
        var filtro = string.IsNullOrWhiteSpace(query)
            ? Builders<Cliente>.Filter.Empty
            : Builders<Cliente>.Filter.Regex("Nombre", new BsonRegularExpression(query, "i"));

        var clientes = collection.Find(filtro).ToList();

        var resultado = clientes.Select(cliente => new
        {
            id = cliente.Id,
            nombre = cliente.Nombre,
            direccion = cliente.Direccion ?? "No proporcionada", // Agregar la dirección y manejar valores nulos
            telefono = cliente.Telefono,
            planRenta = cliente.PlanRenta,
            activo = cliente.Activo,
            fechaInicio = cliente.FechaInicio.HasValue ? cliente.FechaInicio.Value.ToString("yyyy-MM-dd") : null
        });

        return Json(resultado);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[BuscarClientes] Error: {ex.Message}");
        return StatusCode(500, "Error interno del servidor al buscar clientes.");
    }
}

    // Método POST para crear un nuevo cliente
    [HttpPost]
    public IActionResult Create(Cliente cliente)
    {
        // Verificar si el usuario tiene el rol de administrador
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "administrador")
        {
            TempData["Error"] = "Acceso denegado. Solo los administradores pueden acceder a esta sección.";
            return RedirectToAction("AccessDenied", "Account");
        }

        // Validaciones personalizadas para el teléfono
        if (!string.IsNullOrWhiteSpace(cliente.Telefono) && !System.Text.RegularExpressions.Regex.IsMatch(cliente.Telefono, @"^[2-9][0-9]{9}$"))
        {
            ModelState.AddModelError("Telefono", "El teléfono debe ser un número válido de 10 dígitos que no comience con 0 o 1.");
        }

        // Validaciones personalizadas para el correo electrónico
        if (!string.IsNullOrWhiteSpace(cliente.CorreoElectronico))
        {
        // Convertir el correo ingresado a minúsculas para validación
        cliente.CorreoElectronico = cliente.CorreoElectronico.ToLower();

        // Expresión regular para validar los correos permitidos
        if (!System.Text.RegularExpressions.Regex.IsMatch(
        cliente.CorreoElectronico, 
        @"^[a-zA-Z0-9._%+-]+@(gmail\.com|outlook\.com|hotmail\.com)$"))
        {
        ModelState.AddModelError(
            "CorreoElectronico", 
            "Debe ingresar un correo válido de @gmail.com, @outlook.com o @hotmail.com."
        );
    }
}


        // Validación para comentarios (opcional, pero limitado a 500 caracteres)
        if (!string.IsNullOrWhiteSpace(cliente.Comentarios) && cliente.Comentarios.Length > 500)
        {
            ModelState.AddModelError("Comentarios", "Los comentarios no pueden exceder los 500 caracteres.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.PlanesDeRenta = PlanesDeRenta;
            return View(cliente);
        }

        var collection = _mongoService.GetCollection<Cliente>("clientes");

        // Verificar duplicados: Nombre
        var clienteDuplicadoNombre = collection.Find(Builders<Cliente>.Filter.Eq(c => c.Nombre, cliente.Nombre)).FirstOrDefault();
        if (clienteDuplicadoNombre != null)
        {
            ModelState.AddModelError("Nombre", "Ya existe un cliente con este nombre.");
            ViewBag.PlanesDeRenta = PlanesDeRenta;
            return View(cliente);
        }

        try
        {
            cliente.Id = ObjectId.GenerateNewId().ToString();
            collection.InsertOne(cliente);
            TempData["Message"] = "Cliente creado exitosamente.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Create] Error: {ex.Message}");
            TempData["Error"] = "Error al crear el cliente.";
            return View("Error");
        }
    }

    // Método GET para cargar la vista de edición
    public IActionResult Edit(string id)
    {
        // Verificar si el usuario tiene el rol de administrador
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "administrador")
        {
            TempData["Error"] = "Acceso denegado. Solo los administradores pueden acceder a esta sección.";
            return RedirectToAction("AccessDenied", "Account");
        }

        if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
        {
            return NotFound("El ID proporcionado no es válido.");
        }

        var collection = _mongoService.GetCollection<Cliente>("clientes");
        var cliente = collection.Find(Builders<Cliente>.Filter.Eq(c => c.Id, id)).FirstOrDefault();

        if (cliente == null)
        {
            return NotFound("No se encontró un cliente con el ID proporcionado.");
        }

        ViewBag.PlanesDeRenta = PlanesDeRenta;
        return View(cliente);
    }



[HttpPost]
public IActionResult Edit(string id, Cliente cliente)
{
    // Verificar si el usuario tiene el rol de administrador
    var userRole = HttpContext.Session.GetString("UserRole");
    if (userRole != "administrador")
    {
        TempData["Error"] = "Acceso denegado. Solo los administradores pueden acceder a esta sección.";
        return RedirectToAction("AccessDenied", "Account");
    }

    if (!ModelState.IsValid)
    {
        ViewBag.PlanesDeRenta = PlanesDeRenta;
        return View(cliente);
    }

    var collection = _mongoService.GetCollection<Cliente>("clientes");

    // Verificar duplicados: Nombre
    var clienteDuplicadoNombre = collection.Find(
        Builders<Cliente>.Filter.Eq(c => c.Nombre, cliente.Nombre) &
        Builders<Cliente>.Filter.Ne(c => c.Id, id)
    ).FirstOrDefault();

    if (clienteDuplicadoNombre != null)
    {
        ModelState.AddModelError("Nombre", "Ya existe un cliente con este nombre.");
        ViewBag.PlanesDeRenta = PlanesDeRenta;
        return View(cliente);
    }

    // Verificar duplicados: Teléfono (solo si se proporciona uno)
    if (!string.IsNullOrWhiteSpace(cliente.Telefono))
    {
        var clienteDuplicadoTelefono = collection.Find(
            Builders<Cliente>.Filter.Eq(c => c.Telefono, cliente.Telefono) &
            Builders<Cliente>.Filter.Ne(c => c.Id, id)
        ).FirstOrDefault();

        if (clienteDuplicadoTelefono != null)
        {
            ModelState.AddModelError("Telefono", "Ya existe un cliente con este número de teléfono.");
            ViewBag.PlanesDeRenta = PlanesDeRenta;
            return View(cliente);
        }
    }

    try
    {
        var filter = Builders<Cliente>.Filter.Eq(c => c.Id, id);

        // Procesar el valor del checkbox de Activo desde el formulario
        cliente.Activo = Request.Form["Activo"].ToString().Split(',').Last() == "true";

        // Construir el objeto de actualización
        var update = Builders<Cliente>.Update
            .Set(c => c.Nombre, cliente.Nombre.ToUpper()) // Convertir a mayúsculas
            .Set(c => c.Direccion, cliente.Direccion?.ToUpper()) // Convertir a mayúsculas si no es null
            .Set(c => c.PlanRenta, cliente.PlanRenta) // Actualizar el plan
            .Set(c => c.Activo, cliente.Activo) // Actualizar el estado activo/inactivo
            .Set(c => c.FechaInicio, cliente.FechaInicio); // Actualizar la fecha de inicio

        // Agregar campos opcionales solo si tienen valor
        if (!string.IsNullOrWhiteSpace(cliente.Telefono))
        {
            update = update.Set(c => c.Telefono, cliente.Telefono);
        }

        if (!string.IsNullOrWhiteSpace(cliente.CorreoElectronico))
        {
            // Convertir el correo electrónico a minúsculas antes de guardar
            cliente.CorreoElectronico = cliente.CorreoElectronico.ToLower();
            update = update.Set(c => c.CorreoElectronico, cliente.CorreoElectronico);
        }

        if (!string.IsNullOrWhiteSpace(cliente.Comentarios))
        {
            update = update.Set(c => c.Comentarios, cliente.Comentarios.ToUpper());
        }

        collection.UpdateOne(filter, update);

        TempData["Message"] = "Cliente actualizado exitosamente.";
        return RedirectToAction("Index");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Edit] Error: {ex.Message}");
        TempData["Error"] = "Error al actualizar el cliente.";
        return View("Error");
    }
}

    // Método GET para confirmar la eliminación
    public IActionResult Delete(string id)
    {
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "administrador")
        {
            TempData["Error"] = "Acceso denegado. Solo los administradores pueden acceder a esta sección.";
            return RedirectToAction("AccessDenied", "Account");
        }

        if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
        {
            TempData["Error"] = "El ID proporcionado no es válido.";
            return RedirectToAction("Index");
        }

        var collection = _mongoService.GetCollection<Cliente>("clientes");
        var cliente = collection.Find(Builders<Cliente>.Filter.Eq(c => c.Id, id)).FirstOrDefault();

        if (cliente == null)
        {
            TempData["Error"] = "No se encontró un cliente con el ID proporcionado.";
            return RedirectToAction("Index");
        }

        return View(cliente);
    }

    // Método POST para confirmar la eliminación
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(string id)
    {
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "administrador")
        {
            TempData["Error"] = "Acceso denegado. Solo los administradores pueden acceder a esta sección.";
            return RedirectToAction("AccessDenied", "Account");
        }

        if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out _))
        {
            TempData["Error"] = "El ID proporcionado no es válido.";
            return RedirectToAction("Index");
        }

        try
        {
            var collection = _mongoService.GetCollection<Cliente>("clientes");
            var result = collection.DeleteOne(Builders<Cliente>.Filter.Eq(c => c.Id, id));

            if (result.DeletedCount == 0)
            {
                TempData["Error"] = "No se pudo eliminar el cliente.";
                return RedirectToAction("Index");
            }

            TempData["Message"] = "Cliente eliminado exitosamente.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DeleteConfirmed] Error al eliminar cliente: {ex.Message}");
            TempData["Error"] = "Error al eliminar el cliente.";
            return RedirectToAction("Index");
        }
    }

}
