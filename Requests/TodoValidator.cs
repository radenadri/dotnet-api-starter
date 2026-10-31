using FluentValidation;
using bookism.Dto;

namespace bookism.Validators;

public class TodoCreateDtoValidator : AbstractValidator<TodoCreateDto>
{
    public TodoCreateDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title wajib diisi")
            .MaximumLength(100).WithMessage("Title maksimal 100 karakter");
    }
}

public class TodoUpdateDtoValidator : AbstractValidator<TodoUpdateDto>
{
    public TodoUpdateDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title wajib diisi")
            .MaximumLength(100).WithMessage("Title maksimal 100 karakter");
    }
}

