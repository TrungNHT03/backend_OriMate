using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Tutorials;

public class UpdateWorkingCopyHandler
{
    private readonly ITutorialRepository _tutorialRepo;

    public UpdateWorkingCopyHandler(ITutorialRepository tutorialRepo)
        => _tutorialRepo = tutorialRepo;

    public async Task<TutorialResponse> HandleAsync(UpdateWorkingCopyCommand command, CancellationToken ct = default)
    {
        var request = command.Request;
        var workingCopy = await _tutorialRepo.GetByIdWithStepsAsync(command.WorkingCopyId, ct)
            ?? throw new NotFoundException($"Tutorial {command.WorkingCopyId} not found.");

        if (workingCopy.Status != TutorialStatus.EditPendingReview
            && workingCopy.Status != TutorialStatus.RevisionRequired)
            throw new DomainException("Only working copies in edit or revision state can be updated.");

        if (workingCopy.AuthorId != command.AuthorId)
            throw new ForbiddenException("You are not the author of this tutorial.");

        if (!Enum.TryParse<TutorialType>(request.Type, ignoreCase: true, out var tutorialType))
            throw new DomainException($"Invalid tutorial type '{request.Type}'. Valid values: Free, VIP.");

        if (!Enum.TryParse<TutorialDifficulty>(request.Difficulty, ignoreCase: true, out var tutorialDifficulty))
            throw new DomainException($"Invalid difficulty '{request.Difficulty}'. Valid values: Beginner, Intermediate, Advanced.");

        if (tutorialType == TutorialType.VIP)
        {
            var vipSettings = await _tutorialRepo.GetActiveCreatorVipSettingsAsync(command.AuthorId, ct);
            if (vipSettings is null)
                throw new DomainException(
                    "You must have an active VIP pricing tier to set tutorial type to VIP. BR-13.");
        }

        var category = await _tutorialRepo.GetActiveCategoryAsync(request.CategoryId, ct);
        if (category is null)
            throw new DomainException($"Category {request.CategoryId} does not exist or is not active.");

        workingCopy.Title = request.Title;
        workingCopy.Description = request.Description;
        workingCopy.CategoryId = request.CategoryId;
        workingCopy.Difficulty = tutorialDifficulty;
        workingCopy.Type = tutorialType;
        workingCopy.CoverImageUrl = request.CoverImageUrl;
        workingCopy.MetaTitle = request.MetaTitle;
        workingCopy.MetaDescription = request.MetaDescription;
        workingCopy.Tags = request.Tags;
        workingCopy.UpdatedAt = DateTime.UtcNow;

        // Replace steps
        await _tutorialRepo.DeleteStepsByTutorialIdAsync(command.WorkingCopyId, ct);

        var newSteps = new List<TutorialStep>();
        if (request.Steps is { Count: > 0 })
        {
            foreach (var stepReq in request.Steps)
            {
                newSteps.Add(new TutorialStep
                {
                    Id = Guid.NewGuid(),
                    TutorialId = command.WorkingCopyId,
                    StepOrder = stepReq.StepOrder,
                    Description = stepReq.Description,
                    ImageUrl = stepReq.ImageUrl,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _tutorialRepo.UpdateAsync(workingCopy, ct);
        if (newSteps.Count > 0)
            await _tutorialRepo.AddStepsAsync(newSteps, ct);

        workingCopy.Steps = newSteps;
        return MapToResponse(workingCopy);
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
