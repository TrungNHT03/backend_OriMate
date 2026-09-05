namespace OrigamiPlatform.Application.DTOs.Tutorials;

public record TutorialListItemResponse(
    Guid Id,
    string Title,
    string Slug,
    string? CoverImageUrl,
    string Type,
    string Difficulty,
    string Status,
    int StepCount,
    DateTime CreatedAt,
    Guid? ParentTutorialId
);
