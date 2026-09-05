using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.Tutorials;

/// <summary>Lists tutorials awaiting manager review — both new submissions (ParentTutorialId is null,
/// handled via publish/reject) and edit submissions (ParentTutorialId set, handled via approve-edit/reject-edit).</summary>
public class GetManagerQueueHandler
{
    private readonly ITutorialRepository _tutorialRepo;

    public GetManagerQueueHandler(ITutorialRepository tutorialRepo) => _tutorialRepo = tutorialRepo;

    public async Task<PagedResult<ManagerQueueItemResponse>> HandleAsync(
        GetManagerQueueQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var result = await _tutorialRepo.GetPendingManagerReviewAsync(page, pageSize, ct);

        var items = result.Items.Select(t => new ManagerQueueItemResponse(
            t.Id,
            t.Title,
            t.Slug,
            t.Author.Profile?.DisplayName ?? t.Author.Email,
            t.Steps.Count,
            t.CreatedAt,
            t.ParentTutorialId is not null,
            t.ParentTutorialId
        )).ToList();

        return new PagedResult<ManagerQueueItemResponse>(
            items, result.TotalCount, result.Page, result.PageSize, result.TotalPages);
    }
}
