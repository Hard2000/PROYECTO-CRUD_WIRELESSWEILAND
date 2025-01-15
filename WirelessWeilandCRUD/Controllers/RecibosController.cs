using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using WirelessWeilandCRUD.Models;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

public class RecibosController : Controller
{
    private readonly MongoDbService _mongoService;

    public RecibosController(MongoDbService mongoService)
    {
        _mongoService = mongoService;
    }

[HttpGet]
public IActionResult GenerarRecibo()
{
    // Verificar si el usuario tiene el rol de administrador
    var userRole = HttpContext.Session.GetString("UserRole");
    if (userRole != "administrador")
    {
        TempData["Error"] = "Acceso denegado. Solo los administradores pueden acceder a esta sección.";
        return RedirectToAction("AccessDenied", "Account");
    }

    // Crear el ViewModel con los meses
    var viewModel = new ReciboViewModel
    {
        Meses = new List<string>
        {
            "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
            "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
        }
    };

    return View(viewModel);
}



    [HttpPost]
    public IActionResult GuardarRecibo(ReciboViewModel viewModel)
    {
        if (string.IsNullOrEmpty(viewModel.ClienteId))
        {
            ModelState.AddModelError("ClienteId", "Debes seleccionar un cliente.");
            return View("GenerarRecibo", viewModel);
        }

        var clienteCollection = _mongoService.GetCollection<Cliente>("clientes");
        var cliente = clienteCollection.Find(c => c.Id == viewModel.ClienteId).FirstOrDefault();
        string clienteNombre = cliente != null ? cliente.Nombre : "Nombre no encontrado";

        var reciboCollection = _mongoService.GetCollection<Recibo>("recibos");
        var ultimoRecibo = reciboCollection.Find(Builders<Recibo>.Filter.Empty)
                                            .SortByDescending(r => r.NumeroRecibo)
                                            .FirstOrDefault();
        int siguienteNumeroRecibo = (ultimoRecibo?.NumeroRecibo ?? 0) + 1;

        var recibo = new Recibo
        {
            ClienteId = viewModel.ClienteId,
            NombreCliente = clienteNombre,
            Mes = viewModel.Mes,
            Subtotal = viewModel.Subtotal,
            Descuento = viewModel.Descuento,
            Total = viewModel.Total,
            Direccion = viewModel.Direccion ?? "Dirección no especificada",
            Fecha = DateTime.Now,
            NumeroRecibo = siguienteNumeroRecibo
        };

        try
        {
            reciboCollection.InsertOne(recibo);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, "Error al guardar el recibo: " + ex.Message);
            return View("GenerarRecibo", viewModel);
        }

        TempData["SuccessMessage"] = "Recibo guardado correctamente.";
        return RedirectToAction("GenerarRecibo");
    }

    [HttpGet]
    public IActionResult GenerarPdf(string reciboId)
    {
        if (string.IsNullOrEmpty(reciboId) || !ObjectId.TryParse(reciboId, out _))
        {
            return BadRequest("El ID del recibo es inválido.");
        }

        try
        {
            var reciboCollection = _mongoService.GetCollection<Recibo>("recibos");
            var recibo = reciboCollection.Find(r => r.Id == reciboId).FirstOrDefault();

            if (recibo == null)
            {
                return NotFound("No se encontró el recibo solicitado.");
            }

            var pdfStream = GenerarPdf(recibo, "Felix Méndez");
            return File(pdfStream.ToArray(), "application/pdf", $"Recibo_{recibo.NumeroRecibo}.pdf");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GenerarPdf] Error: {ex.Message}");
            return StatusCode(500, "Ocurrió un error al generar el PDF.");
        }
    }

    private MemoryStream GenerarPdf(Recibo recibo, string encargadoNombre = "Felix Méndez")
    {
        var stream = new MemoryStream();

        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(595, 421);
                page.Margin(30);

                page.Header().Row(header =>
                {
                    header.RelativeItem().Column(stack =>
                    {
                        stack.Item().Text("WIRELESS WEILAND")
                            .FontSize(24).Bold().FontColor("#333333");
                        stack.Item().Text("Facturación y servicios de internet")
                            .FontSize(12).Italic().FontColor("#666666");
                    });

                    header.ConstantItem(100).Height(50).Image("wwwroot/images/logo.png");
                });

                page.Content().PaddingVertical(10).Column(content =>
                {
                    content.Item().PaddingBottom(10).Background("#f5f5f5").Padding(10).Row(row =>
                    {
                        row.RelativeItem().Column(inner =>
                        {
                            inner.Item().Text("CLIENTE").FontSize(12).Bold();
                            inner.Item().Text($"Nombre: {recibo.NombreCliente}").FontSize(10);
                            inner.Item().Text($"Dirección: {recibo.Direccion}").FontSize(10);
                        });

                        row.ConstantItem(150).Column(inner =>
                        {
                            inner.Item().Text("DETALLES DEL RECIBO").FontSize(12).Bold();
                            inner.Item().Text($"Recibo No.: {recibo.NumeroRecibo}").FontSize(10);
                            inner.Item().Text($"Fecha: {recibo.Fecha:yyyy-MM-dd}").FontSize(10);
                            inner.Item().Text($"Mes: {recibo.Mes}").FontSize(10);
                        });
                    });

                    content.Item().PaddingVertical(10).Column(inner =>
                    {
                        inner.Item().Text("DETALLES DEL PAGO").FontSize(14).Bold().Underline();
                        inner.Item().Text($"Subtotal: {recibo.Subtotal:C}").FontSize(12);
                        inner.Item().Text($"Descuento: {recibo.Descuento:C}").FontSize(12);
                        inner.Item().Text($"Total a Pagar: {recibo.Total:C}")
                            .FontSize(14).Bold().FontColor("#007BFF");
                    });

                    content.Item().PaddingVertical(30).AlignRight().Row(row =>
                    {
                        row.RelativeItem().Column(inner =>
                        {
                            inner.Item().AlignCenter().Text("_____________________________").FontSize(10);
                            inner.Item().AlignCenter().Text(encargadoNombre).FontSize(12).Bold();
                            inner.Item().AlignCenter().Text("Encargado de Facturación").FontSize(10).Italic();
                        });
                    });
                });

                page.Footer().AlignCenter().Text("WIRELESS WEILAND | Contacto: 228 365 72 47 | Email: garciajasiel39@gmail.com")
                    .FontSize(10).FontColor("#999999");
            });
        });

        document.GeneratePdf(stream);
        stream.Position = 0;

        return stream;
    }

[HttpGet]
public IActionResult ListaRecibos()
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
        var collection = _mongoService.GetCollection<Recibo>("recibos");

        // Ordenar los recibos por fecha en orden descendente
        var recibos = collection.Find(Builders<Recibo>.Filter.Empty)
                                    .SortByDescending(r => r.Fecha)
                                    .ToList();

        return View(recibos);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ListaRecibos] Error: {ex.Message}");
        return StatusCode(500, "Ocurrió un error al cargar la lista de recibos.");
    }
}
















    [HttpGet]
    public IActionResult GenerarReporteMensual(string mes)
    {
        if (string.IsNullOrEmpty(mes))
        {
            TempData["ErrorMessage"] = "Por favor, selecciona un mes válido.";
            return RedirectToAction("ListaRecibos");
        }

        try
        {
            var reciboCollection = _mongoService.GetCollection<Recibo>("recibos");
            var recibos = reciboCollection.Find(r => r.Mes == mes).ToList();

            if (!recibos.Any())
            {
                TempData["ErrorMessage"] = "No hay recibos para el mes seleccionado.";
                return RedirectToAction("ListaRecibos");
            }

            var pdfStream = GenerarReporteMensualPdf(recibos, mes);
            return File(pdfStream.ToArray(), "application/pdf", $"CorteCaja_{mes}.pdf");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GenerarReporteMensual] Error: {ex.Message}");
            TempData["ErrorMessage"] = "Ocurrió un error al generar el reporte.";
            return RedirectToAction("ListaRecibos");
        }
    }
private MemoryStream GenerarReporteMensualPdf(IEnumerable<Recibo> recibos, string mes)
{
    var stream = new MemoryStream();

    // Calcular los totales del corte de caja
    var subtotalTotal = recibos.Sum(r => r.Subtotal);
    var descuentoTotal = recibos.Sum(r => r.Descuento);
    var totalFinal = recibos.Sum(r => r.Total);

    var document = QuestPDF.Fluent.Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(QuestPDF.Helpers.PageSizes.A4);
            page.Margin(40);

            // Encabezado
            page.Header().Row(header =>
            {
                header.RelativeItem().Column(stack =>
                {
                    stack.Item().Text("WIRELESS WEILAND")
                        .FontSize(24).Bold().FontColor("#333333");
                    stack.Item().Text("Facturación y servicios de internet")
                        .FontSize(12).Italic().FontColor("#666666");
                });

                header.ConstantItem(100).Image("wwwroot/images/logo.png");
            });

            // Contenido principal
            page.Content().Column(content =>
            {
                // Resumen General
                content.Item().PaddingBottom(20).Column(column =>
                {
                    column.Item().Text("Resumen General").FontSize(16).Bold().Underline();
                    column.Item().Text($"Total Subtotal: {subtotalTotal:C}").FontSize(14);
                    column.Item().Text($"Total Descuentos: {descuentoTotal:C}").FontSize(14);
                    column.Item().Text($"Total Pagado: {totalFinal:C}").FontSize(14).Bold();
                });

                // Línea divisoria
                content.Item().PaddingVertical(10).Element(e =>
                {
                    e.LineHorizontal(1).LineColor("#cccccc");
                });

                // Tabla Detallada
                content.Item().PaddingBottom(20).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2); // Cliente
                        columns.RelativeColumn(1); // Subtotal
                        columns.RelativeColumn(1); // Descuento
                        columns.RelativeColumn(1); // Total
                        columns.RelativeColumn(1); // Fecha
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background("#f0f0f0").Text("Cliente").Bold();
                        header.Cell().Background("#f0f0f0").Text("Subtotal").Bold().AlignRight();
                        header.Cell().Background("#f0f0f0").Text("Descuento").Bold().AlignRight();
                        header.Cell().Background("#f0f0f0").Text("Total").Bold().AlignRight();
                        header.Cell().Background("#f0f0f0").Text("Fecha").Bold().AlignRight();
                    });

                    foreach (var recibo in recibos)
                    {
                        table.Cell().Text(recibo.NombreCliente);
                        table.Cell().Text(recibo.Subtotal.ToString("C")).AlignRight();
                        table.Cell().Text(recibo.Descuento.ToString("C")).AlignRight();
                        table.Cell().Text(recibo.Total.ToString("C")).AlignRight();
                        table.Cell().Text(recibo.Fecha.ToString("yyyy-MM-dd")).AlignRight();
                    }
                });

                // Firma
                content.Item().PaddingTop(30).AlignCenter().Column(column =>
                {
                    column.Item().Text("_____________________________").FontSize(12);
                    column.Item().Text("Firma del Encargado de Caja").FontSize(10).Italic();
                });
            });

            // Pie de página
            page.Footer().Row(footer =>
            {
                footer.RelativeItem().Text("WIRELESS WEILAND - Servicios de Internet").FontSize(10);
                footer.ConstantItem(100).AlignRight().Text($"Generado el {DateTime.Now:yyyy-MM-dd}").FontSize(10).Italic();
            });
        });
    });

    document.GeneratePdf(stream);
    stream.Position = 0;

    return stream;
}
[HttpPost]
public IActionResult Eliminar(string id)
{
    try
    {
        var collection = _mongoService.GetCollection<Recibo>("recibos");
        var filter = Builders<Recibo>.Filter.Eq(r => r.Id, id);
        var result = collection.DeleteOne(filter);

        if (result.DeletedCount > 0)
        {
            return Json(new { success = true, message = "Recibo eliminado correctamente." });
        }
        else
        {
            return Json(new { success = false, message = "No se encontró el recibo a eliminar." });
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Eliminar] Error: {ex.Message}");
        return Json(new { success = false, message = "Ocurrió un error al eliminar el recibo." });
    }
}



}
