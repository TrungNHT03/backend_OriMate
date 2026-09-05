using OrigamiPlatform.Application.Validators.AdminConfiguration;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.AdminConfiguration;

public class SuspendUserHandler
{
    private readonly IUserRepository _userRepo;
    private readonly IAuditLogRepository _auditLog;
    private readonly INotificationService _notifications;

    public SuspendUserHandler(IUserRepository userRepo, IAuditLogRepository auditLog, INotificationService notifications)
        => (_userRepo, _auditLog, _notifications) = (userRepo, auditLog, notifications);

    public async Task HandleAsync(SuspendUserCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        SuspendUserRequestValidator.Validate(req.Reason);

        var user = await _userRepo.GetByIdAsync(command.UserId, ct)
            ?? throw new NotFoundException($"User {command.UserId} not found.");

        if (user.Roles.Any(r => r.Role == UserRoleType.Admin))
            throw new ForbiddenException("Cannot suspend an Admin account.");

        if (user.Status == AccountStatus.Suspended)
            throw new BadRequestException("User account is already suspended.");

        if (user.Status != AccountStatus.Active)
            throw new BadRequestException("Only Active accounts can be suspended.");

        user.Status = AccountStatus.Suspended;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(user, ct);

        await _auditLog.LogAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = command.ActorId,
            Action = "SuspendAccount",
            EntityType = "User",
            EntityId = command.UserId.ToString(),
            OldValue = null,
            NewValue = req.Reason,
            CreatedAt = DateTime.UtcNow
        }, ct);

        await _notifications.NotifyUserAsync(
            command.UserId,
            NotificationType.AccountSuspended,
            $"Tài khoản của bạn đã bị tạm khoá: {req.Reason}",
            "User",
            command.UserId,
            ct);
    }
}
