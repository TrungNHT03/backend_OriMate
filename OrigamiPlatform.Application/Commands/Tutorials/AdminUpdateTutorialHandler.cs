using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Tutorials;

/// <summary>Admin/Manager edits any main tutorial's content directly, bypassing the author-only and
/// Draft/RevisionRequired-only restrictions in UpdateTutorialHandler. The tutorial keeps its current
/// Status (a Published tutorial stays Published) — content just changes in place, no re-review needed.</summary>
public class AdminUpdateTutorialHandler
{
    private readonly ITutorialRepository _tutorialRepo;

    public AdminUpdateTutorialHandler(ITutorialRepository tutorialRepo)
        => _tutorialRepo = tutorialRepo;

    public async Task<TutorialResponse> HandleAsync(AdminUpdateTutorialCommand command, CancellationToken ct = default)
    {
        var request = command.Request;
        var tutorial = await _tutorialRepo.GetByIdWithStepsAsync(command.TutorialId, ct)
            ?? throw new NotFoundException($"Tutorial {command.TutorialId} not found.");

        if (!tutorial.IsOfficial)
            throw new ForbiddenException("Chỉ có thể chỉnh sửa nội dung bài viết chính thức của Admin/Manager. Bài viết của người dùng chỉ có thể thay đổi trạng thái.");

        if (tutorial.ParentTutorialId is not null || tutorial.Status == TutorialStatus.Merged)
            throw new DomainException("Cannot directly edit a review working copy. Use the manager review flow instead.");

        // BR-12
        if (request.Title.Length < 5 || request.Title.Length > 150)
            throw new DomainException("Title must be between 5 and 150 characters. BR-12.");
        if (request.Description.Length < 20 || request.Description.Length > 500)
            throw new DomainException("Description must be between 20 and 500 characters. BR-12.");

        if (!Enum.TryParse<TutorialType>(request.Type, ignoreCase: true, out var tutorialType))
            throw new DomainException($"Invalid tutorial type '{request.Type}'. Valid values: Free, VIP.");

        if (!Enum.TryParse<TutorialDifficulty>(request.Difficulty, ignoreCase: true, out var tutorialDifficulty))
            throw new DomainException($"Invalid difficulty '{request.Difficulty}'. Valid values: Beginner, Intermediate, Advanced.");

        // BR-13: VIP requires the original author to have active CreatorVipSettings
        if (tutorialType == TutorialType.VIP)
        {
            var vipSettings = await _tutorialRepo.GetActiveCreatorVipSettingsAsync(tutorial.AuthorId, ct);
            if (vipSettings is null)
                throw new DomainException(
                    "The author must have a VIP pricing tier configured before this tutorial can be set to VIP. BR-13.");
        }

        var category = await _tutorialRepo.GetActiveCategoryAsync(request.CategoryId, ct);
        if (category is null)
            throw new DomainException($"Category {request.CategoryId} does not exist or is not active.");

        var fromStatus = tutorial.Status;
        var now = DateTime.UtcNow;

        tutorial.Title = request.Title;
        tutorial.Description = request.Description;
        tutorial.CategoryId = request.CategoryId;
        tutorial.Difficulty = tutorialDifficulty;
        tutorial.Type = tutorialType;
        tutorial.CoverImageUrl = request.CoverImageUrl;
        tutorial.UpdatedAt = now;

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
                    CreatedAt = now
                });
            }
        }

        await _tutorialRepo.UpdateAsync(tutorial, ct);
        if (newSteps.Count > 0)
            await _tutorialRepo.AddStepsAsync(newSteps, ct);

        tutorial.Steps = newSteps;

        // IMMUTABLE — INSERT only (BR-17); Status doesn't change, just an audit trail entry.
        await _tutorialRepo.AddReviewHistoryAsync(new TutorialReviewHistory
        {
            Id = Guid.NewGuid(),
            TutorialId = tutorial.Id,
            ReviewerId = command.ActorId,
            ReviewerRole = command.ActorRole,
            FromStatus = fromStatus,
            ToStatus = fromStatus,
            Action = "AdminDirectEdit",
            Reason = $"Content edited directly by {command.ActorRole} via tutorial management.",
            CreatedAt = now
        }, ct);

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
