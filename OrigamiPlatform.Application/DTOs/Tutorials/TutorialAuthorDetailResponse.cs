using OrigamiPlatform.Application.DTOs.Tutorials;

namespace OrigamiPlatform.Application.DTOs.Tutorials;

/// <summary>Full tutorial detail for the author's own authoring/editing flow — includes steps regardless of Status.</summary>
public record TutorialAuthorDetailResponse(
    Guid Id,
    string Slug,
    string Title,
    string Description,
    string? CoverImageUrl,
    string Type,
    string Difficulty,
    int CategoryId,
    string Status,
    IReadOnlyList<TutorialStepDto> Steps,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    Guid? ParentTutorialId
);
