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

        // 🚀 1. YENİ ÜRÜN EKLE: POST /api/urunler/ekle
        [HttpPost("ekle")]
        public async Task<IActionResult> Ekle([FromBody] CreateProductDto dto)
        {
            Console.WriteLine($"[ÜRÜN EKLE] Ürün: {dto.UrunAdi} süreci başladı.");

            var strategy = _dbContext.Database.CreateExecutionStrategy();

            try
            {
                return await strategy.ExecuteAsync<IActionResult>(async () =>
                {
                    using var transaction = await _dbContext.Database.BeginTransactionAsync();

                    try
                    {
                        var urun = new Product
                        {
                            UrunKodu = dto.UrunKodu,
                            UrunAdi = dto.UrunAdi,
                            Birim = dto.Birim,
                            Aciklama = dto.Aciklama
                        };

                        await _dbContext.Products.AddAsync(urun);
                        await _dbContext.SaveChangesAsync();

                        var client = _httpClientFactory.CreateClient("StokServisClient");
                        var stokPaketi = new { urunId = urun.Id, stokMiktari = dto.IlkStokMiktari };
                        var jsonIcerik = new StringContent(JsonSerializer.Serialize(stokPaketi), Encoding.UTF8, "application/json");

                        Console.WriteLine($"-> Stok servisine ilk stok oluşturma isteği gönderiliyor... Ürün ID: {urun.Id}");
                        var stokYaniti = await client.PostAsync("api/stoklar/guncelle", jsonIcerik);

                        if (!stokYaniti.IsSuccessStatusCode)
                        {
                            throw new Exception("Stok servisi ilk stok oluşturma işlemini reddetti! İşlem iptal ediliyor.");
                        }

                        await transaction.CommitAsync();
                        Console.WriteLine("✅ [BAŞARILI] Ürün ve stok kaydı tutarlı şekilde oluşturuldu.");

                        return Ok(new { Message = "Ürün başarıyla eklendi ve ilk stok kaydı oluşturuldu.", ProductId = urun.Id });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        Console.WriteLine($"❌ [İPTAL EDİLDİ] Dağıtık işlem iç hatası: {ex.Message}");
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [İPTAL EDİLDİ] Dağıtık işlem dış hatası: {ex.Message}");
                return Problem($"Ürün ekleme sırasında bir hata oluştu: {ex.Message}");
            }
        }

        // 🚀 2. ÜRÜN TANIMINI GÜNCELLE: POST /api/urunler/guncelle
        [HttpPost("guncelle")]
        public async Task<IActionResult> UpdateProduct([FromBody] UpdateProductDto dto)
        {
            Console.WriteLine($"[ÜRÜN & STOK GÜNCELLE] ID: {dto.Id} süreci başladı.");

            var strategy = _dbContext.Database.CreateExecutionStrategy();

            try
            {
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

                        product.UrunAdi = dto.UrunAdi;
                        product.Birim = dto.Birim;
                        product.Aciklama = dto.Aciklama;
                        await _dbContext.SaveChangesAsync();

                        var client = _httpClientFactory.CreateClient("StokServisClient");
                        var stokPaketi = new { urunId = dto.Id, stokMiktari = dto.StokMiktari };
                        var jsonIcerik = new StringContent(JsonSerializer.Serialize(stokPaketi), Encoding.UTF8, "application/json");

                        Console.WriteLine($"-> Stok servisine güncelleme isteği gönderiliyor... Ürün ID: {dto.Id}");
                        var stokYaniti = await client.PostAsync("api/stoklar/guncelle", jsonIcerik);

                        if (!stokYaniti.IsSuccessStatusCode)
                        {
                            throw new Exception("Stok servisi güncellemeyi reddetti! İşlem iptal ediliyor.");
                        }

                        await transaction.CommitAsync();
                        Console.WriteLine("✅ [BAŞARILI] Ürün ve stok veritabanları tam tutarlılıkla güncellendi.");

                        return Ok(new { Message = "Ürün bilgileri ve stok miktarı başarıyla senkronize güncellendi." });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        Console.WriteLine($"❌ [İPTAL EDİLDİ] Dağıtık işlem iç hatası: {ex.Message}");
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [İPTAL EDİLDİ] Dağıtık işlem dış hatası: {ex.Message}");
                return Problem($"Güncelleme sırasında bir hata oluştu: {ex.Message}");
            }
        }

        [HttpDelete("sil/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                using var transaction = await _dbContext.Database.BeginTransactionAsync();
                try
                {
                    // 1. Ürünün varlığını kontrol et
                    var product = await _dbContext.Products.FindAsync(id);
                    if (product == null)
                    {
                        return NotFound(new { message = "Silinmek istenen ürün bulunamadı." });
                    }

                    // 2. Arka planda StockApi'ye bağlanıp o ürüne ait stokları sildir
                    // (Hatırla: Debug ederken yerelde kaldığımız için localhost:5002 portunu kullanıyoruz)
                    var client = _httpClientFactory.CreateClient("StokServisClient");
                    var response = await client.DeleteAsync($"api/stoklar/sil/{id}");

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception("Stok servisi envanter kayıtlarını silmeyi reddetti. Silme işlemi iptal edildi.");
                    }

                    // 3. Stoklar başarıyla silindiyse şimdi ürünü kendi DB'mizden silebiliriz
                    _dbContext.Products.Remove(product);
                    await _dbContext.SaveChangesAsync();

                    // Her iki tarafta da işlem okeyse transaction'ı mühürle
                    await transaction.CommitAsync();
                    return Ok(new { message = "Ürün ve bağlı tüm envanter kayıtları başarıyla silindi." });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new { message = $"Silme operasyonu başarısız: {ex.Message}" });
                }
            });
        }
    }
}