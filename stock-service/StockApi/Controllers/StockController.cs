using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StockApi.Dtos;

namespace StockApi.Controllers
{
    [ApiController] // 🚀 Bu sınıfın bir Web API Controller olduğunu belirtir
    [Route("api/stoklar")] // 🚀 Bu controller'a gelecek isteklerin kök adresini tanımlar (http://localhost:5002/api/stoklar)
    public class StokController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly IMemoryCache _memoryCache;

        // 🚀 [02. Dependency Injection]: .NET Core, AppDbContext bağımlılığını 
        // constructor üzerinden bu sınıfa otomatik enjekte eder.

        // .NET Core, hem DbContext'i hem de IMemoryCache'i buraya otomatik enjekte eder
        public StokController(AppDbContext dbContext, IMemoryCache memoryCache)
        {
            _dbContext = dbContext;
            _memoryCache = memoryCache;
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

            // 🚀 KRİTİK ADIM: Stok güncellendiği için RAM'deki eski önbelleği (Cache Eviction) siliyoruz!
            // Böylece sonraki ilk GET isteği güncel veriyi veritabanından zorunlu olarak çeker.
            _memoryCache.Remove("tum_stoklar");
            
            return Ok(new { Message = "Stok başarıyla güncellendi." });
        }

        [HttpGet] // 🚀 GET api/stoklar isteği geldiğinde bu metot tetiklenir
        public async Task<IActionResult> GetStoklar()
        {
           string cacheKey = "tum_stoklar";

            // 🚀 Önce RAM belleğe bakıyoruz, veri orada var mı?
            if (!_memoryCache.TryGetValue(cacheKey, out Dictionary<int, int>? stokMap))
            {
                Console.WriteLine("⚠️ [CACHE MISS] Veri RAM'de yok! Veritabanına gidiliyor...");

                // RAM'de yoksa veritabanından çekiyoruz
                var stocksList = await _dbContext.Stocks.ToListAsync();
                stokMap = stocksList.ToDictionary(s => s.UrunId, s => s.StokMiktari);

                // Veritabanından aldığımız veriyi 1 dakikalığına RAM'e mühürlüyoruz
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(1)); // 1 dakika sonra cache'i patlat

                _memoryCache.Set(cacheKey, stokMap, cacheOptions);
            }
            else
            {
                Console.WriteLine("⚡ [CACHE HIT] Harika! Veri doğrudan RAM bellekten şimşek hızında çekildi.");
            }

            return Ok(stokMap);
        }

        
    }
}