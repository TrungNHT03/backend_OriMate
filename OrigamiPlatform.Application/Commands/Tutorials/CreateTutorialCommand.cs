using OrigamiPlatform.Application.DTOs.Tutorials;

namespace OrigamiPlatform.Application.Commands.Tutorials;

public record CreateTutorialCommand(Guid AuthorId, CreateTutorialRequest Request);
