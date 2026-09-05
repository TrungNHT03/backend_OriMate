using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Queries.Tutorials;

public class GetTutorialForAdminHandler
{
    private readonly ITutorialRepository _tutorialRepo;

    public GetTutorialForAdminHandler(ITutorialRepository tutorialRepo) => _tutorialRepo = tutorialRepo;

    public async Task<AdminTutorialDetailResponse> HandleAsync(
        GetTutorialForAdminQuery query, CancellationToken ct = default)
    {
        var tutorial = await _tutorialRepo.GetByIdWithStepsAsync(query.TutorialId, ct)
            ?? throw new NotFoundException($"Tutorial {query.TutorialId} not found.");

        var steps = tutorial.Steps
            .OrderBy(s => s.StepOrder)
            .Select(s => new TutorialStepDto(s.Id, s.StepOrder, s.Description, s.ImageUrl))
            .ToList();

        return new AdminTutorialDetailResponse(
            tutorial.Id,
            tutorial.Slug,
            tutorial.Title,
            tutorial.Description,
            tutorial.CoverImageUrl,
            tutorial.Type.ToString(),
            tutorial.Difficulty.ToString(),
            tutorial.CategoryId,
            tutorial.Status.ToString(),
            tutorial.Author.Profile?.DisplayName ?? tutorial.Author.Email,
            tutorial.IsOfficial,
            steps,
            tutorial.CreatedAt,
            tutorial.UpdatedAt);
    }
}
