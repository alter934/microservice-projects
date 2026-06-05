using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockApi.Dtos;

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

        [HttpPost("guncelle")] // 🚀 POST api/stoklar/guncelle
        public async Task<IActionResult> UpdateStok([FromBody] UpdateStockDto dto)
        {
            // .NET Core ve FluentValidation arkada el sıkışır. 
            // Eğer kurallara uymayan bir veri gelirse, ModelState otomatik olarak geçersiz (Invalid) olur.
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); // Hataları 400 Bad Request ile frontend'e zarifçe dön
            }

            Console.WriteLine($"[STOK GÜNCELLE] Ürün: {dto.UrunId} -> Yeni Stok: {dto.StokMiktari}");

            var stock = await _dbContext.Stocks.FirstOrDefaultAsync(s => s.UrunId == dto.UrunId);
            
            if (stock == null)
            {
                // Eğer veritabanında bu ürün yoksa yeni satır oluşturalım
                stock = new Stock { UrunId = dto.UrunId, StokMiktari = dto.StokMiktari };
                await _dbContext.Stocks.AddAsync(stock);
            }
            else
            {
                // Varsa miktarını güncelleyelim
                stock.StokMiktari = dto.StokMiktari;
            }

            await _dbContext.SaveChangesAsync();
            return Ok(new { Message = "Stok başarıyla güncellendi." });
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