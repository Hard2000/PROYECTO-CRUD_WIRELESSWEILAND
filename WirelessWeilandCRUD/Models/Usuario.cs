using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Usuario
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("Username")]
    public string Username { get; set; } = string.Empty;

    [BsonElement("Password")]
    public string Password { get; set; } = string.Empty;

    [BsonElement("Email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("Role")] // Agregar la propiedad Role
    public string Role { get; set; } = string.Empty; // "administrador" o "cliente"
    [BsonElement("RecoveryCode")]
    public string? RecoveryCode { get; set; } // Nuevo campo para el código de recuperación
}

