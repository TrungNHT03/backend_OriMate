using OrigamiPlatform.Application.DTOs.AdminConfiguration;

namespace OrigamiPlatform.Application.Commands.AdminConfiguration;

public record CreateCategoryCommand(Guid ActorId, CreateCategoryRequest Request);
