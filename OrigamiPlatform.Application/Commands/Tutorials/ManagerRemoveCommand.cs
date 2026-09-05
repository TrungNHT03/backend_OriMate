using OrigamiPlatform.Application.DTOs.Tutorials;

namespace OrigamiPlatform.Application.Commands.Tutorials;

public record ManagerRemoveCommand(Guid TutorialId, Guid ManagerId, ManagerRemoveRequest? Request);
