using OrigamiPlatform.Application.Validators.AdminConfiguration;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.AdminConfiguration;

public class AssignRoleHandler
{
    private readonly IUserRepository _userRepo;
    private readonly IAuditLogRepository _auditLog;

    public AssignRoleHandler(IUserRepository userRepo, IAuditLogRepository auditLog)
        => (_userRepo, _auditLog) = (userRepo, auditLog);

    public async Task HandleAsync(AssignRoleCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        AssignRoleRequestValidator.Validate(req.Role);

        var user = await _userRepo.GetByIdAsync(command.UserId, ct)
            ?? throw new NotFoundException($"User {command.UserId} not found.");

        var roleType = Enum.Parse<UserRoleType>(req.Role);
        var currentRoles = user.Roles.Select(r => r.Role).ToList();

        if (currentRoles.Count == 1 && currentRoles[0] == roleType)
            return; // already exactly this role — no-op

        if (command.ActorId == command.UserId && currentRoles.Contains(UserRoleType.Admin) && roleType != UserRoleType.Admin)
            throw new BadRequestException("Cannot remove your own Admin role.");

        // Assign-role is authoritative: the account ends up with exactly the one role the admin picked.
        foreach (var existingRole in currentRoles)
            await _userRepo.RemoveRoleAsync(command.UserId, existingRole, ct);

        var userRole = new UserRole
        {
            UserId = command.UserId,
            Role = roleType,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepo.AddRoleAsync(userRole, ct);

        await _auditLog.LogAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = command.ActorId,
            Action = "AssignRole",
            EntityType = "User",
            EntityId = command.UserId.ToString(),
            OldValue = null,
            NewValue = req.Role,
            CreatedAt = DateTime.UtcNow
        }, ct);
    }
}
