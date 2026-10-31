using FluentValidation;
using bookism.Dto;

namespace bookism.Validators;

public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("Username wajib diisi")
            .MinimumLength(3)
            .WithMessage("Username minimal 3 karakter");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password wajib diisi")
            .MinimumLength(6)
            .WithMessage("Password minimal 6 karakter");
    }
}

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("Username wajib diisi")
            .MinimumLength(3)
            .WithMessage("Username minimal 3 karakter");

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Format email tidak valid");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password wajib diisi")
            .MinimumLength(6)
            .WithMessage("Password minimal 6 karakter");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password)
            .WithMessage("Konfirmasi password harus cocok dengan password");
    }
}
