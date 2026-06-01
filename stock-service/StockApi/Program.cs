using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// 1. CORS Politikasını Tanımlayalım (Ön yüzün rahat erişmesi için)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();
app.UseCors("AllowAll");

// 2. Docker Compose'dan gelecek olan Çevre Değişkenlerini (Environment Variables) okuyalım
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "nuri";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "aycan_secret";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "stock_db";
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";

// Postgres Bağlantı Cümlesi (Connection String)
string connectionString = $"Host={dbHost};Port={dbPort};Username={dbUser};Password={dbPassword};Database={dbName};";

// 3. /api/stoklar Uç Noktasını (Endpoint) Tanımlayalım
app.MapGet("/api/stoklar", async () =>
{
    Console.WriteLine(".NET Core: Postgres'ten stok listesi talep ediliyor...");
    
    // Hatırlarsan Vue tarafı bizden { "1": 15, "2": 42 } şeklinde bir Key-Value (Sözlük) yapısı bekliyordu.
    var stokMap = new Dictionary<string, int>();

    try
    {
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var command = dataSource.CreateCommand("SELECT urun_id, stok_miktari FROM stocks");
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            string urunId = reader["urun_id"].ToString() ?? "";
            int stokMiktari = Convert.ToInt32(reader["stok_miktari"]);
            
            if (!string.IsNullOrEmpty(urunId))
            {
                stokMap[urunId] = stokMiktari;
            }
        }

        return Results.Ok(stokMap);
    }
    catch (Exception ex)
    {
        Console.WriteLine($".NET Core Hatası: {ex.Message}");
        return Results.Problem("Veritabanına bağlanılamadı veya tablo bulunamadı.");
    }
});

app.Run();