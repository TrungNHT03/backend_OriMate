using OrigamiPlatform.Application.DTOs.Tutorials;

namespace OrigamiPlatform.Application.Commands.Tutorials;

public record ManagerRejectEditCommand(Guid WorkingCopyId, Guid ManagerId, ManagerRejectRequest Request);
