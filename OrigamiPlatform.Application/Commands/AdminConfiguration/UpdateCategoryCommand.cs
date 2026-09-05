using OrigamiPlatform.Application.DTOs.AdminConfiguration;

namespace OrigamiPlatform.Application.Commands.AdminConfiguration;

public record UpdateCategoryCommand(Guid ActorId, int CategoryId, UpdateCategoryRequest Request);
