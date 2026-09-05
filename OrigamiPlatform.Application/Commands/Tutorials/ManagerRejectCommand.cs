using OrigamiPlatform.Application.DTOs.Tutorials;

namespace OrigamiPlatform.Application.Commands.Tutorials;

public record ManagerRejectCommand(Guid TutorialId, Guid ManagerId, ManagerRejectRequest Request);
