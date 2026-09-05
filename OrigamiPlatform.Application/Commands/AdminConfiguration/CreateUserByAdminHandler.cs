using OrigamiPlatform.Application.DTOs.AdminConfiguration;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.AdminConfiguration;

public class CreateUserByAdminHandler
{
    private static readonly HashSet<string> AssignableRoles =
    [
        nameof(UserRoleType.User),
        nameof(UserRoleType.ContributorReviewer),
        nameof(UserRoleType.Manager),
        nameof(UserRoleType.Admin)
    ];

    private readonly IUserRepository _userRepo;
    private readonly IPasswordHasher _hasher;
    private readonly IAuditLogRepository _auditLog;

    public CreateUserByAdminHandler(IUserRepository userRepo, IPasswordHasher hasher, IAuditLogRepository auditLog)
        => (_userRepo, _hasher, _auditLog) = (userRepo, hasher, auditLog);

    public async Task<AdminUserResponse> HandleAsync(CreateUserByAdminCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        Validate(req);

        var roleType = Enum.Parse<UserRoleType>(req.Role);
        var email = req.Email.Trim().ToLowerInvariant();

        if (await _userRepo.ExistsByEmailAsync(email, ct))
            throw new ConflictException("Email is already registered.");

        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Admin vouches for the identity directly — skip the email-verification step self-signup requires.
        var user = new User
        {
            Id = userId,
            Email = email,
            PasswordHash = _hasher.Hash(req.Password),
            Status = AccountStatus.Active,
            CreatedAt = now,
            Profile = new UserProfile
            {
                UserId = userId,
                DisplayName = req.DisplayName.Trim(),
                CreatedAt = now
            },
            Roles = new List<UserRole>
            {
                new UserRole { UserId = userId, Role = roleType, CreatedAt = now }
            }
        };

        await _userRepo.AddAsync(user, ct);

        await _auditLog.LogAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = command.ActorId,
            Action = "CreateUser",
            EntityType = "User",
            EntityId = userId.ToString(),
            OldValue = null,
            NewValue = req.Role,
            CreatedAt = now
        }, ct);

        return new AdminUserResponse(
            user.Id,
            user.Email,
            user.Profile.DisplayName,
            null,
            user.Status.ToString(),
            new List<string> { roleType.ToString() },
            user.CreatedAt);
    }

    private static void Validate(CreateUserByAdminRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
            throw new DomainException("Email is required.");

        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 6)
            throw new DomainException("Password must be at least 6 characters.");

        if (string.IsNullOrWhiteSpace(req.DisplayName))
            throw new DomainException("Display name is required.");

        if (!AssignableRoles.Contains(req.Role))
            throw new DomainException($"Invalid role. Allowed roles are: {string.Join(", ", AssignableRoles)}.");
    }
}
