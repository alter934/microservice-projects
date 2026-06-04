using Microsoft.EntityFrameworkCore;

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
    options.UseNpgsql(connectionString));

builder.Services.AddControllers(); // Controller servislerini IoC konteynerine kaydediyoruz

var app = builder.Build();
app.UseCors("AllowAll");


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