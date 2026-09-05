using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Tutorials;

// BR-TUT-01: single review round — Draft/RevisionRequired go straight to Manager, no Contributor Reviewer step
public class SubmitTutorialHandler
{
    private readonly ITutorialRepository _tutorialRepo;
    private readonly INotificationService _notifications;

    public SubmitTutorialHandler(ITutorialRepository tutorialRepo, INotificationService notifications)
        => (_tutorialRepo, _notifications) = (tutorialRepo, notifications);

    public async Task<TutorialResponse> HandleAsync(SubmitTutorialCommand command, CancellationToken ct = default)
    {
        var tutorial = await _tutorialRepo.GetByIdWithStepsAsync(command.TutorialId, ct)
            ?? throw new NotFoundException($"Tutorial {command.TutorialId} not found.");

        if (tutorial.AuthorId != command.AuthorId)
            throw new ForbiddenException("You are not the author of this tutorial.");

        if (tutorial.Status != TutorialStatus.Draft && tutorial.Status != TutorialStatus.RevisionRequired)
            throw new DomainException("Tutorial cannot be submitted in its current status.");

        // BR-12 — collect all failures before throwing
        var errors = new List<string>();

        if (tutorial.Title.Length < 5 || tutorial.Title.Length > 150)
            errors.Add("Title must be between 5 and 150 characters.");
        if (tutorial.Description.Length < 20 || tutorial.Description.Length > 500)
            errors.Add("Description must be between 20 and 500 characters.");
        if (string.IsNullOrEmpty(tutorial.CoverImageUrl))
            errors.Add("Cover image is required.");

        var category = await _tutorialRepo.GetActiveCategoryAsync(tutorial.CategoryId, ct);
        if (category is null)
            errors.Add("Category does not exist or is not active.");

        if (tutorial.Steps.Count < 3 || tutorial.Steps.Count > 30)
        {
            errors.Add($"Tutorial must have between 3 and 30 steps (currently {tutorial.Steps.Count}).");
        }
        else
        {
            var badStepOrders = tutorial.Steps
                .Where(s => string.IsNullOrWhiteSpace(s.Description) || string.IsNullOrWhiteSpace(s.ImageUrl))
                .Select(s => s.StepOrder)
                .ToList();
            if (badStepOrders.Count > 0)
                errors.Add($"Steps {string.Join(", ", badStepOrders)} are missing description or image.");
        }

        if (tutorial.Type == TutorialType.VIP)
        {
            var vipSettings = await _tutorialRepo.GetActiveCreatorVipSettingsAsync(command.AuthorId, ct);
            if (vipSettings is null)
                errors.Add("VIP tutorials require an active VIP pricing tier. BR-13.");
        }

        if (errors.Count > 0)
            throw new DomainException(string.Join(" ", errors));

        var fromStatus = tutorial.Status;
        tutorial.Status = TutorialStatus.PendingManagerReview;
        tutorial.UpdatedAt = DateTime.UtcNow;

        await _tutorialRepo.UpdateAsync(tutorial, ct);

        // IMMUTABLE — INSERT only (BR-17)
        await _tutorialRepo.AddReviewHistoryAsync(new TutorialReviewHistory
        {
            Id = Guid.NewGuid(),
            TutorialId = tutorial.Id,
            ReviewerId = command.AuthorId,
            ReviewerRole = UserRoleType.User,
            FromStatus = fromStatus,
            ToStatus = TutorialStatus.PendingManagerReview,
            Action = "Submit",
            Reason = null,
            CreatedAt = DateTime.UtcNow
        }, ct);

        await _notifications.NotifyUsersWithRoleAsync(
            UserRoleType.Manager,
            NotificationType.TutorialReadyForManagerApproval,
            $"Hướng dẫn mới \"{tutorial.Title}\" đang chờ bạn duyệt.",
            "Tutorial",
            tutorial.Id,
            ct);

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
