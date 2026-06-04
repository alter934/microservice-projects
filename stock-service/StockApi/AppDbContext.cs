using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    // Dependency Injection konteynerinden gelecek olan ayarları ana sınıfa paslıyoruz
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Veritabanındaki 'stocks' tablosuna bu mülk üzerinden erişeceğiz
    public DbSet<Stock> Stocks { get; set; }
}