namespace OrigamiPlatform.Application.DTOs.Tutorials;

public record TutorialResponse(
    Guid Id,
    string Slug,
    string Title,
    string Description,
    string? CoverImageUrl,
    string Type,
    string Difficulty,
    int CategoryId,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
