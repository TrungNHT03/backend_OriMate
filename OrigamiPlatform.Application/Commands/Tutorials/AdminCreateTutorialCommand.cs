using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Commands.Tutorials;

/// <summary>
/// Admin/Manager creates a tutorial — always Free, published immediately (no review queue).
/// The tutorial's AuthorId is always <see cref="OrigamiPlatform.Domain.Constants.SystemUsers.OfficialTutorialAuthorId"/>,
/// not the acting Admin/Manager's own account; ActorId is kept only for the audit trail.
/// </summary>
public record AdminCreateTutorialCommand(Guid ActorId, UserRoleType ActorRole, CreateTutorialRequest Request);
