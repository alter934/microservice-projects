namespace ProductApi.Dtos
{
    public class UpdateProductDto
    {
        public int Id { get; set; }
        public string UrunAdi { get; set; } = string.Empty;
        public string Birim { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;

        // 🚀 MES ekranından gelen stok bilgisini burada yakalıyoruz
        public int StokMiktari { get; set; }
    }
}