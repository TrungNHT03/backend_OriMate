using System.Text.Json;
using OrigamiPlatform.Application.DTOs.AdminConfiguration;
using OrigamiPlatform.Application.Validators.AdminConfiguration;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.AdminConfiguration;

public class UpdateCategoryHandler
{
    private readonly ICategoryRepository _categoryRepo;
    private readonly IAuditLogRepository _auditLog;

    public UpdateCategoryHandler(ICategoryRepository categoryRepo, IAuditLogRepository auditLog)
        => (_categoryRepo, _auditLog) = (categoryRepo, auditLog);

    public async Task<CategoryResponse> HandleAsync(UpdateCategoryCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        UpdateCategoryRequestValidator.Validate(req.Name);

        var category = await _categoryRepo.GetByIdAsync(command.CategoryId, ct)
            ?? throw new NotFoundException($"Category {command.CategoryId} not found.");

        if (req.Name is not null && await _categoryRepo.ExistsByNameAsync(req.Name, command.CategoryId, ct))
            throw new ConflictException("Category name already exists.");

        var oldState = JsonSerializer.Serialize(new { category.Name, category.IsActive });

        if (req.Name is not null) category.Name = req.Name.Trim();
        if (req.IsActive.HasValue) category.IsActive = req.IsActive.Value;
        category.UpdatedAt = DateTime.UtcNow;

        await _categoryRepo.UpdateAsync(category, ct);

        var newState = JsonSerializer.Serialize(new { category.Name, category.IsActive });

        await _auditLog.LogAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = command.ActorId,
            Action = "UpdateCategory",
            EntityType = "Category",
            EntityId = command.CategoryId.ToString(),
            OldValue = oldState,
            NewValue = newState,
            CreatedAt = DateTime.UtcNow
        }, ct);

        return new CategoryResponse(category.Id, category.Name, category.IsActive, category.CreatedAt);
    }
}
