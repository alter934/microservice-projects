namespace Service.Contracts
{
    // MassTransit'in en iyi pratiklerine (Best Practices) göre event'ler interface veya record olmalıdır
    public record ProductDeletedEvent
    {
        public int Id { get; init; }
        public required string UrunKodu { get; init; }
    }
}