using OrigamiPlatform.Application.DTOs.Tutorials;

namespace OrigamiPlatform.Application.Commands.Tutorials;

public record UpdateTutorialCommand(Guid TutorialId, Guid AuthorId, UpdateTutorialRequest Request);
