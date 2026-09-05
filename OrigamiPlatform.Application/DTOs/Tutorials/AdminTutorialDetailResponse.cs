using OrigamiPlatform.Application.DTOs.Tutorials;

namespace OrigamiPlatform.Application.DTOs.Tutorials;

/// <summary>Full tutorial detail for the admin edit form — any author, any status.</summary>
public record AdminTutorialDetailResponse(
    Guid Id,
    string Slug,
    string Title,
    string Description,
    string? CoverImageUrl,
    string Type,
    string Difficulty,
    int CategoryId,
    string Status,
    string AuthorName,
    bool IsOfficial,
    IReadOnlyList<TutorialStepDto> Steps,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
