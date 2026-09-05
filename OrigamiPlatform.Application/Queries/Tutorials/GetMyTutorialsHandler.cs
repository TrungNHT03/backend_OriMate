using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.Tutorials;

public class GetMyTutorialsHandler
{
    private readonly ITutorialRepository _tutorialRepo;

    public GetMyTutorialsHandler(ITutorialRepository tutorialRepo) => _tutorialRepo = tutorialRepo;

    public async Task<PagedResult<TutorialListItemResponse>> HandleAsync(
        GetMyTutorialsQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var result = await _tutorialRepo.GetByAuthorAsync(query.AuthorId, page, pageSize, ct);

        var items = result.Items.Select(t => new TutorialListItemResponse(
            t.Id,
            t.Title,
            t.Slug,
            t.CoverImageUrl,
            t.Type.ToString(),
            t.Difficulty.ToString(),
            t.Status.ToString(),
            t.Steps.Count,
            t.CreatedAt,
            t.ParentTutorialId
        )).ToList();

        return new PagedResult<TutorialListItemResponse>(
            items,
            result.TotalCount,
            result.Page,
            result.PageSize,
            result.TotalPages);
    }
}
