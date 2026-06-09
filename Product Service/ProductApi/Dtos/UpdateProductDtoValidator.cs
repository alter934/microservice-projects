using FluentValidation;

namespace ProductApi.Dtos
{
    public class UpdateProductDtoValidator : AbstractValidator<UpdateProductDto>
    {
        public UpdateProductDtoValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Geçersiz Ürün ID.");

            RuleFor(x => x.UrunAdi)
                .NotEmpty().WithMessage("Ürün adı boş geçilemez.")
                .MaximumLength(100).WithMessage("Ürün adı en fazla 100 karakter olabilir.");

            RuleFor(x => x.Birim)
                .NotEmpty().WithMessage("Ürün birimi (Adet, KG vb.) seçilmelidir.");
        }
    }
}