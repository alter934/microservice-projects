using Microsoft.EntityFrameworkCore;
using FluentValidation;
using StockApi.Dtos;
using StockApi.Middlewares;
using StockApi.Services;
using FluentValidation.AspNetCore;


var builder = WebApplication.CreateBuilder(args);

// 1. CORS Politikasını Tanımlayalım (Ön yüzün rahat erişmesi için)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// 🚀 YENİ KISIM: Önce appsettings.json'daki varsayılan bağlantı cümlesini oku
string connectionString = builder.Configuration.GetConnectionString("PostgreSQL") ?? "";

// 🚀 Eğer Docker Compose'dan ezici bir ortam değişkeni (DB_HOST) gelirse, bağlantıyı dinamik oluştur
var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
if (!string.IsNullOrEmpty(dbHost))
{
    var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "nuri";
    var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "aycan_secret";
    var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "stock_db";
    var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
    
    connectionString = $"Host={dbHost};Port={dbPort};Username={dbUser};Password={dbPassword};Database={dbName};";
}

// 🚀 [02. Dependency Injection & 08. EF Core]: DbContext'i sisteme kaydediyoruz
// .NET Core, veritabanı bağlantı havuzunu ve nesne ömrünü arka planda otomatik yönetecek.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions => 
    {
        // 🚀 Veritabanı açılana kadar veya anlık kopmalarda otomatik olarak yeniden denetir
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5, 
            maxRetryDelay: TimeSpan.FromSeconds(5), 
            errorCodesToAdd: null);
    }));

// 1. Standart Controller servisini yalın olarak kaydediyoruz
builder.Services.AddControllers();

builder.Services.AddMemoryCache(); // 🚀 [18. Caching]: Bellek önbellekleme servisini IoC konteynerine ekliyoruz
builder.Services.AddHostedService<StokRaporWorker>(); // 🚀 Arka plan işçisini .NET işletim sistemine zimmetliyoruz

// 2. 🚀 MODERN YÖNTEM: Validator sınıflarımızı Dependency Injection (DI) sistemine otomatik kaydet
builder.Services.AddValidatorsFromAssemblyContaining<UpdateStockDtoValidator>();

// 3. 🚀 MODERN YÖNTEM: Gelen istekleri Controller seviyesinde otomatik doğrulamaya (Auto-Validation) tabi tut
builder.Services.AddFluentValidationAutoValidation();

var app = builder.Build();
app.UseCors("AllowAll");

// 🚀 Uygulama ayağa kalkarken stock_db yoksa oluştur ve şemayı güncelle
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        Console.WriteLine(".NET Stok API: Veritabanı varlığı kontrol ediliyor...");
        // Veritabanı sunucuda fiziksel olarak yoksa sıfırdan oluşturur
        await db.Database.EnsureCreatedAsync(); 
        
        Console.WriteLine(".NET Stok API: Şema göçleri (Migrations) işleniyor...");
        await db.Database.MigrateAsync();
        Console.WriteLine(".NET Stok API: Veritabanı ve tablolar hazır!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($".NET Stok API Başlangıç Hatası: {ex.Message}");
    }
}

// 🚀 [16. Exception Handling]: Global Hata Yönetim Zırhını en başa takıyoruz!
app.UseMiddleware<ExceptionMiddleware>();


// 🚀 Özel Middleware: İstek Süresi Ölçer ve Loglar
app.Use(async (context, next) =>
{
    // 1. İstek boru hattına girdiği an bir kronometre başlatıyoruz
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
    var requestPath = context.Request.Path;
    var requestMethod = context.Request.Method;
    
    Console.WriteLine($"[KAPI] Gelen İstek: {requestMethod} {requestPath}");

    // 2. İsteyi boru hattındaki bir sonraki katmana (veya API endpoint'imize) paslıyoruz
    await next(context);

    // 3. Kod backend'de işlendi, veritabanına gitti geldi ve şu an geri dönüyor!
    stopwatch.Stop();
    var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
    
    Console.WriteLine($"[KAPI] Yanıt Gönderildi: {requestMethod} {requestPath} | Süre: {elapsedMilliseconds} ms | Durum: {context.Response.StatusCode}");
});


app.MapControllers(); //  Gelen HTTP isteklerini ilgili Controller'a haritalandırır
app.Run();