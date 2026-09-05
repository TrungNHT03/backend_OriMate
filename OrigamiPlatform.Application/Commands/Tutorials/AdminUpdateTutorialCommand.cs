using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Commands.Tutorials;

/// <summary>Admin/Manager edits any main tutorial's content directly — regardless of author or
/// review status — since they already hold publishing authority. The tutorial's Status is left
/// unchanged (a Published tutorial stays Published with the new content applied immediately).</summary>
public record AdminUpdateTutorialCommand(Guid TutorialId, Guid ActorId, UserRoleType ActorRole, UpdateTutorialRequest Request);
