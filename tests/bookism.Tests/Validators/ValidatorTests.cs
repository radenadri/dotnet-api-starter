using FluentAssertions;
using bookism.Dto;
using bookism.Validators;
using Xunit;

namespace bookism.Tests.Validators;

/// <summary>
/// Unit tests for FluentValidation rules across Auth, Todo, and Book requests.
/// </summary>
public class ValidatorTests
{
    [Theory]
    [InlineData("", "password123", false)]
    [InlineData("ab", "password123", false)]
    [InlineData("validuser", "12345", false)]
    [InlineData("validuser", "validpassword", true)]
    public void LoginDtoValidator_RulesCheck(string username, string password, bool expectedValid)
    {
        var validator = new LoginDtoValidator();
        var result = validator.Validate(new LoginDto(username, password));
        result.IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData("user", "user@example.com", "pass1234", "pass1234", true)]
    [InlineData("user", "invalid-email", "pass1234", "pass1234", false)]
    [InlineData("user", "user@example.com", "pass1234", "mismatch", false)]
    public void RegisterDtoValidator_RulesCheck(string username, string email, string password, string confirm, bool expectedValid)
    {
        var validator = new RegisterDtoValidator();
        var result = validator.Validate(new RegisterDto(username, email, password, confirm));
        result.IsValid.Should().Be(expectedValid);
    }

    [Fact]
    public void TodoValidator_WhenTitleEmpty_ShouldFail()
    {
        var validator = new TodoCreateDtoValidator();
        var result = validator.Validate(new TodoCreateDto(""));
        result.IsValid.Should().BeFalse();

        var updateValidator = new TodoUpdateDtoValidator();
        var updateResult = updateValidator.Validate(new TodoUpdateDto("", true));
        updateResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public void BookValidator_WhenRatingOutOfRange_ShouldFail()
    {
        var validator = new BookCreateDtoValidator();
        var result = validator.Validate(new BookCreateDto("Book", null, null, null, null, null, 100, 2020, 6.0m, null, null, null, null, null, null, null));
        result.IsValid.Should().BeFalse();

        var updateValidator = new BookUpdateDtoValidator();
        var updateResult = updateValidator.Validate(new BookUpdateDto("Book", null, null, null, null, null, 100, 2020, 6.0m, null, null, null, null, null, null, null));
        updateResult.IsValid.Should().BeFalse();

        var validUpdate = updateValidator.Validate(new BookUpdateDto("Valid Book", null, null, null, null, null, 100, 2020, 4.5m, null, null, null, null, null, null, null));
        validUpdate.IsValid.Should().BeTrue();
    }
}
