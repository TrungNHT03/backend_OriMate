using OrigamiPlatform.Application.DTOs.Tutorials;

namespace OrigamiPlatform.Application.Commands.Tutorials;

public record UpdateWorkingCopyCommand(Guid WorkingCopyId, Guid AuthorId, UpdateTutorialRequest Request);
