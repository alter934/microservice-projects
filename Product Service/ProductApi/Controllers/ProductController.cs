using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductApi.Dtos;
using System.Text;
using System.Text.Json;

namespace ProductApi.Controllers
{
    [ApiController]
    [Route("api/urunler")] // Nginx Gateway'in yönlendirdiği endpoint kökü
    public class ProductController : ControllerBase
    {
        private readonly ProductDbContext _dbContext;
        private readonly IHttpClientFactory _httpClientFactory; // 🚀 HTTP Fabrikası enjeksiyonu

        public ProductController(ProductDbContext dbContext, IHttpClientFactory httpClientFactory)
        {
            _dbContext = dbContext;
            _httpClientFactory = httpClientFactory;
        }

        // 🚀 1. TÜM ÜRÜNLERİ LİSTELE: GET /api/urunler
        [HttpGet]
        public async Task<IActionResult> GetUrunler()
        {
            Console.WriteLine(".NET Product API: Ürün listesi veritabanından çekiliyor...");
            var urunler = await _dbContext.Products.ToListAsync();
            return Ok(urunler);
        }

        // 🚀 2. ÜRÜN TANIMINI GÜNCELLE: POST /api/urunler/guncelle
        [HttpPost("guncelle")]
public async Task<IActionResult> UpdateProduct([FromBody] UpdateProductDto dto)
{
    Console.WriteLine($"[ÜRÜN & STOK GÜNCELLE] ID: {dto.Id} süreci başladı.");

    var strategy = _dbContext.Database.CreateExecutionStrategy();

    try
    {
        // 🚀 BÜYÜK DÜZELTME: <IActionResult> ekleyerek asenkron lambdanın geriye ne döneceğini derleyiciye açıkça dikte ediyoruz
        return await strategy.ExecuteAsync<IActionResult>(async () =>
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == dto.Id);
                if (product == null)
                {
                    return NotFound(new { Message = "Güncellenmek istenen ürün bulunamadı." });
                }

                // Ürün temel bilgilerini güncelle
                product.UrunAdi = dto.UrunAdi;
                product.Birim = dto.Birim;
                product.Aciklama = dto.Aciklama;
                await _dbContext.SaveChangesAsync();

                // Arka planda Stok Servisine HTTP isteği çekiyoruz
                var client = _httpClientFactory.CreateClient("StokServisClient");
                var stokPaketi = new { urunId = dto.Id, stokMiktari = dto.StokMiktari };
                var jsonIcerik = new StringContent(JsonSerializer.Serialize(stokPaketi), Encoding.UTF8, "application/json");

                Console.WriteLine($"-> Arka planda Stok Servisine istek atılıyor... Ürün ID: {dto.Id}");
                var stokYaniti = await client.PostAsync("api/stoklar/guncelle", jsonIcerik);

                if (!stokYaniti.IsSuccessStatusCode)
                {
                    throw new Exception("Stok servisi güncellemeyi reddetti! İşlem iptal ediliyor.");
                }

                await transaction.CommitAsync();
                Console.WriteLine("✅ [BAŞARILI] Ürün ve Stok veritabanları tam tutarlılıkla güncellendi.");

                return Ok(new { Message = "Ürün bilgileri ve stok miktarı başarıyla senkronize güncellendi." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"❌ [İPTAL EDİLDİ] Dağıtık işlem iç hatası: {ex.Message}");
                throw; // Stratejinin hatayı yönetebilmesi ve dış catch bloğuna paslaması için fırlatıyoruz
            }
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ [İPTAL EDİLDİ] Dağıtık işlem dış hatası: {ex.Message}");
        return Problem($"Güncelleme sırasında bir hata oluştu: {ex.Message}");
    }
}
    }
}