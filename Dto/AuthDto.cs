namespace bookism.Dto;

public record LoginDto(string Username, string Password);

public record RegisterDto(string Username, string? Email, string Password, string ConfirmPassword);

public record UserProfileDto(Guid Id, string Username, string? Email, string Role, bool IsActive, DateTime CreatedAt);
