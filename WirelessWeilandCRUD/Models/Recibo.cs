using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace WirelessWeilandCRUD.Models
{
    public class Recibo
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonElement("ClienteId")]
        public string ClienteId { get; set; } = string.Empty;

        [BsonElement("NombreCliente")] // Nuevo campo para el nombre del cliente
        public string NombreCliente { get; set; } = string.Empty;

        [BsonElement("Mes")]
        public string Mes { get; set; } = string.Empty;

        [BsonElement("Subtotal")]
        public decimal Subtotal { get; set; }

        [BsonElement("Descuento")]
        public decimal Descuento { get; set; }

        [BsonElement("Total")]
        public decimal Total { get; set; }

        [BsonElement("Direccion")]
        public string Direccion { get; set; } = string.Empty;

        [BsonElement("Fecha")]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [BsonElement("NumeroRecibo")]
        public int NumeroRecibo { get; set; }
    }
}
