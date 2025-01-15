using MongoDB.Driver;

public class MongoDbService
{
    private readonly IMongoDatabase _database;

    public MongoDbService(IConfiguration configuration)
    {
        // Obtén la configuración para MongoDB
        var settings = configuration.GetSection("MongoDB").Get<MongoDbSettings>() 
                       ?? throw new ArgumentNullException(nameof(configuration), "La configuración de MongoDB no puede ser nula.");

        // Valida que la cadena de conexión no sea nula o vacía
        var connectionString = settings.ConnectionString 
                               ?? throw new ArgumentNullException(nameof(settings.ConnectionString), "La cadena de conexión no puede ser nula o vacía.");

        // Valida que el nombre de la base de datos no sea nulo o vacío
        var databaseName = settings.DatabaseName 
                           ?? throw new ArgumentNullException(nameof(settings.DatabaseName), "El nombre de la base de datos no puede ser nulo o vacío.");

        // Crea el cliente de MongoDB y obtiene la base de datos
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName) 
                    ?? throw new Exception("No se pudo conectar a la base de datos especificada.");
    }

    public IMongoCollection<T> GetCollection<T>(string collectionName)
    {
        // Devuelve la colección especificada
        return _database.GetCollection<T>(collectionName);
    }
}
