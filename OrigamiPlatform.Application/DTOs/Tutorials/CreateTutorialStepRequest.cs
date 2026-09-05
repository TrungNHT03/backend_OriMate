namespace OrigamiPlatform.Application.DTOs.Tutorials;

public record CreateTutorialStepRequest(
    int StepOrder,
    string Description,
    string? ImageUrl
);
