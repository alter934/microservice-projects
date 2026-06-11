using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using ProductApi;
using ProductApi.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// 1. Controller ve Modern Validasyon Kayıtları (Zero Warning)
builder.Services.AddControllers();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddFluentValidationAutoValidation();

// 2. CORS Ayarı (Vue.js ön yüzünün erişebilmesi için)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// ⏳ DİNAMİK VERİTABANI BAĞLANTI YÖNETİMİ (Product_DB Hedefli)
string connectionString = builder.Configuration.GetConnectionString("PostgreSQL") ?? "";
var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
if (!string.IsNullOrEmpty(dbHost))
{
    var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "nuri";
    var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "aycan_secret";
    var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "product_db"; // Hedef Product_DB
    var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
    connectionString = $"Host={dbHost};Port={dbPort};Username={dbUser};Password={dbPassword};Database={dbName};";
}

// 🚀 [08. EF Core]: DbContext Kaydı
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions => 
    {
        // 🚀 Veritabanı ayağa kalkarken yaşanacak gecikmeleri tolere eder
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5, 
            maxRetryDelay: TimeSpan.FromSeconds(5), 
            errorCodesToAdd: null);
    }));

// 🚀 [Servisler Arası İletişim]: Stok servisiyle konuşacak merkezi HttpClient tanımı
builder.Services.AddHttpClient("StokServisClient", client =>
{
    // Docker ağı içindeki servis adını ve portunu hedefliyoruz
    client.BaseAddress = new Uri("http://localhost:5002/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

var app = builder.Build();

app.UseCors("AllowAll");

// 🚀 Uygulama ayağa kalkarken product_db yoksa oluştur ve şemayı güncelle
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    try
    {
        Console.WriteLine(".NET Product API: Veritabanı varlığı kontrol ediliyor...");
        // Veritabanı yoksa fiziksel olarak oluşturur
        await db.Database.EnsureCreatedAsync();

        Console.WriteLine(".NET Product API: Şema göçleri (Migrations) işleniyor...");
        await db.Database.MigrateAsync();
        Console.WriteLine(".NET Product API: Veritabanı ve tablolar hazır!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($".NET Product API Başlangıç Hatası: {ex.Message}");
    }
}

// 🚀 [16. Exception Handling]: Küresel Hata Yönetim Zırhı
app.UseMiddleware<ExceptionMiddleware>();

app.MapControllers();

app.Run();