using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Tutorials;

/// <summary>Edits a tutorial that has not been published yet (Draft or RevisionRequired — BR-TUT-01,
/// same allowed states as SubmitTutorialHandler). Once PendingManagerReview/Published, use the
/// edit-after-publish working-copy flow (CreateWorkingCopy/UpdateWorkingCopy) instead.</summary>
public class UpdateTutorialHandler
{
    private readonly ITutorialRepository _tutorialRepo;

    public UpdateTutorialHandler(ITutorialRepository tutorialRepo)
        => _tutorialRepo = tutorialRepo;

    public async Task<TutorialResponse> HandleAsync(UpdateTutorialCommand command, CancellationToken ct = default)
    {
        var request = command.Request;
        var tutorial = await _tutorialRepo.GetByIdWithStepsAsync(command.TutorialId, ct)
            ?? throw new NotFoundException($"Tutorial {command.TutorialId} not found.");

        if (tutorial.AuthorId != command.AuthorId)
            throw new ForbiddenException("You are not the author of this tutorial.");

        if (tutorial.Status != TutorialStatus.Draft && tutorial.Status != TutorialStatus.RevisionRequired)
            throw new DomainException("Only draft or revision-required tutorials can be edited. Published tutorials must go through the edit-after-publish flow.");

        // BR-12
        if (request.Title.Length < 5 || request.Title.Length > 150)
            throw new DomainException("Title must be between 5 and 150 characters. BR-12.");
        if (request.Description.Length < 20 || request.Description.Length > 500)
            throw new DomainException("Description must be between 20 and 500 characters. BR-12.");

        if (!Enum.TryParse<TutorialType>(request.Type, ignoreCase: true, out var tutorialType))
            throw new DomainException($"Invalid tutorial type '{request.Type}'. Valid values: Free, VIP.");

        if (!Enum.TryParse<TutorialDifficulty>(request.Difficulty, ignoreCase: true, out var tutorialDifficulty))
            throw new DomainException($"Invalid difficulty '{request.Difficulty}'. Valid values: Beginner, Intermediate, Advanced.");

        // BR-13: VIP requires active CreatorVipSettings
        if (tutorialType == TutorialType.VIP)
        {
            var vipSettings = await _tutorialRepo.GetActiveCreatorVipSettingsAsync(command.AuthorId, ct);
            if (vipSettings is null)
                throw new DomainException(
                    "You must configure a VIP pricing tier before setting tutorial type to VIP. BR-13.");
        }

        var category = await _tutorialRepo.GetActiveCategoryAsync(request.CategoryId, ct);
        if (category is null)
            throw new DomainException($"Category {request.CategoryId} does not exist or is not active.");

        tutorial.Title = request.Title;
        tutorial.Description = request.Description;
        tutorial.CategoryId = request.CategoryId;
        tutorial.Difficulty = tutorialDifficulty;
        tutorial.Type = tutorialType;
        tutorial.CoverImageUrl = request.CoverImageUrl;
        tutorial.UpdatedAt = DateTime.UtcNow;

        // Replace steps (same pattern as UpdateWorkingCopyHandler)
        await _tutorialRepo.DeleteStepsByTutorialIdAsync(command.TutorialId, ct);

        var newSteps = new List<TutorialStep>();
        if (request.Steps is { Count: > 0 })
        {
            foreach (var stepReq in request.Steps)
            {
                newSteps.Add(new TutorialStep
                {
                    Id = Guid.NewGuid(),
                    TutorialId = command.TutorialId,
                    StepOrder = stepReq.StepOrder,
                    Description = stepReq.Description,
                    ImageUrl = stepReq.ImageUrl,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _tutorialRepo.UpdateAsync(tutorial, ct);
        if (newSteps.Count > 0)
            await _tutorialRepo.AddStepsAsync(newSteps, ct);

        tutorial.Steps = newSteps;
        return MapToResponse(tutorial);
    }

    private static TutorialResponse MapToResponse(Tutorial tutorial) => new(
        tutorial.Id,
        tutorial.Slug,
        tutorial.Title,
        tutorial.Description,
        tutorial.CoverImageUrl,
        tutorial.Type.ToString(),
        tutorial.Difficulty.ToString(),
        tutorial.CategoryId,
        tutorial.Status.ToString(),
        tutorial.CreatedAt,
        tutorial.UpdatedAt);
}
