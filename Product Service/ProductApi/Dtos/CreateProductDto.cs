namespace ProductApi.Dtos
{
    public class CreateProductDto
    {
        public string UrunKodu { get; set; } = string.Empty;
        public string UrunAdi { get; set; } = string.Empty;
        public string Birim { get; set; } = string.Empty;
        public string Aciklama { get; set; } = string.Empty;
        public int IlkStokMiktari { get; set; }
    }
}
