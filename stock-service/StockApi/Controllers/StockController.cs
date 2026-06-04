using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace StockApi.Controllers
{
    [ApiController] // 🚀 Bu sınıfın bir Web API Controller olduğunu belirtir
    [Route("api/stoklar")] // 🚀 Bu controller'a gelecek isteklerin kök adresini tanımlar (http://localhost:5002/api/stoklar)
    public class StokController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        // 🚀 [02. Dependency Injection]: .NET Core, AppDbContext bağımlılığını 
        // constructor üzerinden bu sınıfa otomatik enjekte eder.
        public StokController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet] // 🚀 GET api/stoklar isteği geldiğinde bu metot tetiklenir
        public async Task<IActionResult> GetStoklar()
        {
            Console.WriteLine(".NET Controller: Stok listesi EF Core ile çekiliyor...");
            
            try
            {
                var stocksList = await _dbContext.Stocks.ToListAsync();

                // Ön yüzün beklediği { 1: 15, 2: 42 } formatına (int key, int value) dönüştürüyoruz
                var stokMap = stocksList.ToDictionary(s => s.UrunId, s => s.StokMiktari);

                return Ok(stokMap); // 200 OK Durumu ile JSON verisini dön
            }
            catch (Exception ex)
            {
                Console.WriteLine($".NET Controller Hatası: {ex.Message}");
                return Problem("Veritabanı bağlantı hatası.");
            }
        }
    }
}