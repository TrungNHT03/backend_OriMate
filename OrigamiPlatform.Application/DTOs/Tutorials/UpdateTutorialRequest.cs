namespace OrigamiPlatform.Application.DTOs.Tutorials;

public record UpdateTutorialRequest(
    string Title,
    string Description,
    int CategoryId,
    string Difficulty,
    string Type,
    string? CoverImageUrl,
    List<CreateTutorialStepRequest>? Steps,
    string? MetaTitle = null,
    string? MetaDescription = null,
    string? Tags = null
);
