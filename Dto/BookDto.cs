namespace bookism.Dto;

public record AuthorSummaryDto(int Id, string Name);
public record CategorySummaryDto(int Id, string Name);
public record PublisherSummaryDto(int Id, string Name);
public record FormatSummaryDto(int Id, string Name);

public record BookDetailDto(
    int Id,
    string Name,
    string? Summary,
    string? Image,
    string? Language,
    string? Isbn10,
    string? Isbn13,
    int? TotalPages,
    int? ReleaseDate,
    decimal? Rating,
    string? Status,
    int? Progress,
    string? CurrentPage,
    DateOnly? DateStarter,
    DateOnly? DateFinished,
    PublisherSummaryDto? Publisher,
    FormatSummaryDto? Format,
    List<AuthorSummaryDto> Authors,
    List<CategorySummaryDto> Categories,
    DateTime? CreatedAt,
    DateTime? UpdatedAt
);

public record BookCreateDto(
    string Name,
    string? Summary,
    string? Image,
    string? Language,
    string? Isbn10,
    string? Isbn13,
    int? TotalPages,
    int? ReleaseDate,
    decimal? Rating,
    string? Status,
    int? Progress,
    string? CurrentPage,
    int? PublisherId,
    int? FormatId,
    List<int>? AuthorIds,
    List<int>? CategoryIds
);

public record BookUpdateDto(
    string Name,
    string? Summary,
    string? Image,
    string? Language,
    string? Isbn10,
    string? Isbn13,
    int? TotalPages,
    int? ReleaseDate,
    decimal? Rating,
    string? Status,
    int? Progress,
    string? CurrentPage,
    int? PublisherId,
    int? FormatId,
    List<int>? AuthorIds,
    List<int>? CategoryIds
);

