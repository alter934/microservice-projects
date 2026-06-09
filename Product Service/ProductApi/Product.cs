using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductApi
{
    [Table("products")] // PostgreSQL'deki tablo adı
    public class Product
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("urun_kodu")]
        public string UrunKodu { get; set; } = string.Empty;

        [Column("urun_adi")]
        public string UrunAdi { get; set; } = string.Empty;

        [Column("birim")]
        public string Birim { get; set; } = string.Empty; // Örn: Adet, KG, Metre

        [Column("aciklama")]
        public string Aciklama { get; set; } = string.Empty;
    }
}