using Microsoft.AspNetCore.Mvc;

public class TestController : Controller
{
    private readonly MongoDbService _mongoService;

    public TestController(MongoDbService mongoService)
    {
        _mongoService = mongoService;
    }

    public IActionResult Index()
    {
        return Content("Conexión a MongoDB exitosa");
    }
}
