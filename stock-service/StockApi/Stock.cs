using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// PostgreSQL'deki mevcut "stocks" tablosuyla eşleştiriyoruz
[Table("stocks")]
public class Stock
{
    [Key]
    [Column("urun_id")]
    public int UrunId { get; set; }

    [Column("stok_miktari")]
    public int StokMiktari { get; set; }
}