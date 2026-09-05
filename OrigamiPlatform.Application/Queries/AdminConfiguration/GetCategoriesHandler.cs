using OrigamiPlatform.Application.DTOs.AdminConfiguration;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.AdminConfiguration;

public class GetCategoriesHandler
{
    private readonly ICategoryRepository _categoryRepo;

    public GetCategoriesHandler(ICategoryRepository categoryRepo) => _categoryRepo = categoryRepo;

    public async Task<List<CategoryResponse>> HandleAsync(GetCategoriesQuery query, CancellationToken ct = default)
    {
        var categories = await _categoryRepo.GetAllAsync(ct);
        return categories
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CategoryResponse(c.Id, c.Name, c.IsActive, c.CreatedAt))
            .ToList();
    }
}
