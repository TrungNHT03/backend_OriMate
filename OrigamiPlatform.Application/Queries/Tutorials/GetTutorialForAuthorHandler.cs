using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Queries.Tutorials;

/// <summary>Fetches a tutorial owned by the caller regardless of Status — used to pre-fill the edit form
/// (Draft, RevisionRequired, PendingManagerReview, Published...). Not for public/reader access.</summary>
public class GetTutorialForAuthorHandler
{
    private readonly ITutorialRepository _tutorialRepo;

    public GetTutorialForAuthorHandler(ITutorialRepository tutorialRepo) => _tutorialRepo = tutorialRepo;

    public async Task<TutorialAuthorDetailResponse> HandleAsync(
        GetTutorialForAuthorQuery query, CancellationToken ct = default)
    {
        var tutorial = await _tutorialRepo.GetByIdWithStepsAsync(query.TutorialId, ct)
            ?? throw new NotFoundException($"Tutorial {query.TutorialId} not found.");

        if (tutorial.AuthorId != query.AuthorId)
            throw new ForbiddenException("You are not the author of this tutorial.");

        var steps = tutorial.Steps
            .OrderBy(s => s.StepOrder)
            .Select(s => new TutorialStepDto(s.Id, s.StepOrder, s.Description, s.ImageUrl))
            .ToList();

        return new TutorialAuthorDetailResponse(
            tutorial.Id,
            tutorial.Slug,
            tutorial.Title,
            tutorial.Description,
            tutorial.CoverImageUrl,
            tutorial.Type.ToString(),
            tutorial.Difficulty.ToString(),
            tutorial.CategoryId,
            tutorial.Status.ToString(),
            steps,
            tutorial.CreatedAt,
            tutorial.UpdatedAt,
            tutorial.ParentTutorialId);
    }
}
