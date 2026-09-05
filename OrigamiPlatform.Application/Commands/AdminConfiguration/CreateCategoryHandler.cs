using OrigamiPlatform.Application.DTOs.AdminConfiguration;
using OrigamiPlatform.Application.Validators.AdminConfiguration;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.AdminConfiguration;

public class CreateCategoryHandler
{
    private readonly ICategoryRepository _categoryRepo;
    private readonly IAuditLogRepository _auditLog;

    public CreateCategoryHandler(ICategoryRepository categoryRepo, IAuditLogRepository auditLog)
        => (_categoryRepo, _auditLog) = (categoryRepo, auditLog);

    public async Task<CategoryResponse> HandleAsync(CreateCategoryCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        CreateCategoryRequestValidator.Validate(req.Name);

        if (await _categoryRepo.ExistsByNameAsync(req.Name, null, ct))
            throw new ConflictException("Category name already exists.");

        var category = new Category
        {
            Name = req.Name.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _categoryRepo.AddAsync(category, ct);

        await _auditLog.LogAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = command.ActorId,
            Action = "CreateCategory",
            EntityType = "Category",
            EntityId = created.Id.ToString(),
            OldValue = null,
            NewValue = created.Name,
            CreatedAt = DateTime.UtcNow
        }, ct);

        return new CategoryResponse(created.Id, created.Name, created.IsActive, created.CreatedAt);
    }
}
