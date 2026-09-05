namespace OrigamiPlatform.Application.DTOs.Tutorials;

/// <summary>One row in the admin tutorial management list — every "main" tutorial (not review
/// working copies) regardless of author or status, so Admin/Manager can find and re-edit anything.</summary>
public record AdminTutorialListItemResponse(
    Guid Id,
    string Title,
    string Slug,
    string? CoverImageUrl,
    string Type,
    string Difficulty,
    string Status,
    int CategoryId,
    string CategoryName,
    string AuthorName,
    bool IsOfficial,
    int StepCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? PublishedAt
);
