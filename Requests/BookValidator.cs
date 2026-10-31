using FluentValidation;
using bookism.Dto;

namespace bookism.Validators;

public class BookCreateDtoValidator : AbstractValidator<BookCreateDto>
{
    public BookCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nama buku wajib diisi")
            .MaximumLength(255).WithMessage("Nama buku maksimal 255 karakter");

        RuleFor(x => x.Rating)
            .InclusiveBetween(0, 5)
            .When(x => x.Rating.HasValue)
            .WithMessage("Rating harus bernilai antara 0 sampai 5");

        RuleFor(x => x.TotalPages)
            .GreaterThan(0)
            .When(x => x.TotalPages.HasValue)
            .WithMessage("Total halaman harus lebih besar dari 0");
    }
}

public class BookUpdateDtoValidator : AbstractValidator<BookUpdateDto>
{
    public BookUpdateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nama buku wajib diisi")
            .MaximumLength(255).WithMessage("Nama buku maksimal 255 karakter");

        RuleFor(x => x.Rating)
            .InclusiveBetween(0, 5)
            .When(x => x.Rating.HasValue)
            .WithMessage("Rating harus bernilai antara 0 sampai 5");
    }
}

