using FluentValidation;

namespace StockApi.Dtos
{
    public class UpdateStockDtoValidator : AbstractValidator<UpdateStockDto>
    {
        public UpdateStockDtoValidator()
        {
            // 🚀 Kurumsal İş Kuralları Doğrulaması
            RuleFor(x => x.UrunId)
                .NotEmpty().WithMessage("Ürün ID boş geçilemez.")
                .GreaterThan(0).WithMessage("Geçersiz Ürün ID.");

            RuleFor(x => x.StokMiktari)
                .GreaterThanOrEqualTo(0).WithMessage("Stok miktarı 0'dan küçük olamaz!");
        }
    }
}