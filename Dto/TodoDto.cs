namespace bookism.Dto;

public record TodoCreateDto(string Title);
public record TodoUpdateDto(string Title, bool IsCompleted);
public record TodoResponseDto(int Id, string Title, bool IsCompleted);