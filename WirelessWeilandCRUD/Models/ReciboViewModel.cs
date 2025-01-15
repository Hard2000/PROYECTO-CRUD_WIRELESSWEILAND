using System;
using System.Collections.Generic;

public class ReciboViewModel
{
    public string ClienteId { get; set; } = string.Empty;
    public string ClienteNombre { get; set; } = string.Empty;
    public string Mes { get; set; } = string.Empty;
    public decimal Subtotal { get; set; } = 0;
    public decimal Descuento { get; set; } = 0;
    public decimal Total { get; set; } = 0;
    public string? Direccion { get; set; } // Propiedad opcional
    public DateTime Fecha { get; set; } = DateTime.Now;

    public List<string> Meses { get; set; } = new List<string>
    {
        "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
        "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
    };
}
