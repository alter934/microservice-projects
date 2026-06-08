using Microsoft.EntityFrameworkCore;

namespace StockApi.Services
{
    // 🚀 [19. Background Services]: BackgroundService sınıfından türeyen kurumsal işçi mimarisi
    public class StokRaporWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        // Arka plan servisleri "Singleton" ömürlüdür. "Scoped" olan DbContext'e erişmek için IServiceProvider enjekte ederiz.
        public StokRaporWorker(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("🤖 [İŞÇİ AYAĞA KALKTI] Kritik Stok Takip İşçisi arka planda göreve başladı...");

            // Uygulama kapatılmadığı sürece bu döngü arka planda asenkron dönecek
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        // Stok miktarı 10'dan az olan kritik durumdaki ürünleri sorgula
                        var kritikStoklar = await dbContext.Stocks
                            .Where(s => s.StokMiktari < 10)
                            .ToListAsync(stoppingToken);

                        if (kritikStoklar.Any())
                        {
                            Console.WriteLine($"🚨 [KRİTİK STOK UYARISI] Şu an stok seviyesi tehlikede olan {kritikStoklar.Count} adet ürün var!");
                            foreach (var stok in kritikStoklar)
                            {
                                Console.WriteLine($"   -> Ürün ID: {stok.UrunId} | Kalan Stok: {stok.StokMiktari}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("✅ [İŞÇİ RAPORU] Arka plan kontrolü temiz: Kritik seviyede stok bulunmuyor.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[İŞÇİ HATASI] Arka plan görevi yürütülürken hata oluştu: {ex.Message}");
                }

                // 10 saniye boyunca asenkron olarak uyu (API'yi asla yormaz ve kilitlemez)
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}