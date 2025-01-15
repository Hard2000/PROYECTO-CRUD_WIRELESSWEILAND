using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

public class Cliente
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("Nombre")]
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre no puede exceder los 50 caracteres.")]
    [RegularExpression(@"^[A-ZÁÉÍÓÚÜÑ ]+$", ErrorMessage = "El nombre solo puede contener letras mayúsculas y espacios.")]
    public string Nombre { get; set; } = string.Empty;

    [BsonElement("Direccion")]
    [StringLength(200, ErrorMessage = "La dirección no puede exceder los 200 caracteres.")]
    [RegularExpression(@"^[A-ZÁÉÍÓÚÜÑ0-9#,\-\s]*$", ErrorMessage = "La dirección solo puede contener letras, números y caracteres válidos.")]
    public string? Direccion { get; set; } // Opcional

    [BsonElement("Telefono")]
    [RegularExpression(@"^[2-9][0-9]{9}$", ErrorMessage = "El teléfono debe ser un número válido de 10 dígitos que no comience con 0 o 1.")]
    public string? Telefono { get; set; } // Opcional

    [BsonElement("CorreoElectronico")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@(gmail\.com|outlook\.com|hotmail\.com)$", ErrorMessage = "El correo debe ser válido y pertenecer a @gmail.com, @outlook.com o @hotmail.com.")]
    public string? CorreoElectronico { get; set; } // Opcional

    [BsonElement("PlanRenta")]
    [Required(ErrorMessage = "Debe seleccionar un plan de renta.")]
    public string PlanRenta { get; set; } = string.Empty;

    [BsonElement("Activo")]
    public bool Activo { get; set; }


    [BsonElement("FechaInicio")]
    [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
    public DateTime? FechaInicio { get; set; } = null;

    [BsonElement("Comentarios")]
    [StringLength(500, ErrorMessage = "Los comentarios no pueden exceder los 500 caracteres.")]
    public string? Comentarios { get; set; } // Opcional
}
