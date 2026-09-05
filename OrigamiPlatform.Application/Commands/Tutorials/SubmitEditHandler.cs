using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Tutorials;

// BR-TUT-01: single review round — working copy goes straight to Manager, no Contributor Reviewer step
public class SubmitEditHandler
{
    private readonly ITutorialRepository _tutorialRepo;
    private readonly INotificationService _notifications;

    public SubmitEditHandler(ITutorialRepository tutorialRepo, INotificationService notifications)
        => (_tutorialRepo, _notifications) = (tutorialRepo, notifications);

    public async Task<TutorialResponse> HandleAsync(SubmitEditCommand command, CancellationToken ct = default)
    {
        var workingCopy = await _tutorialRepo.GetByIdWithStepsAsync(command.WorkingCopyId, ct)
            ?? throw new NotFoundException($"Tutorial {command.WorkingCopyId} not found.");

        if (workingCopy.Status != TutorialStatus.EditPendingReview
            && workingCopy.Status != TutorialStatus.RevisionRequired)
            throw new DomainException("Only working copies in edit or revision state can be submitted.");

        if (workingCopy.AuthorId != command.AuthorId)
            throw new ForbiddenException("You are not the author of this tutorial.");

        // BR-12 validations
        var errors = new List<string>();

        if (workingCopy.Title.Length < 5 || workingCopy.Title.Length > 150)
            errors.Add("Title must be between 5 and 150 characters.");
        if (workingCopy.Description.Length < 20 || workingCopy.Description.Length > 500)
            errors.Add("Description must be between 20 and 500 characters.");
        if (string.IsNullOrEmpty(workingCopy.CoverImageUrl))
            errors.Add("Cover image is required.");

        var category = await _tutorialRepo.GetActiveCategoryAsync(workingCopy.CategoryId, ct);
        if (category is null)
            errors.Add("Category does not exist or is not active.");

        if (workingCopy.Steps.Count < 3 || workingCopy.Steps.Count > 30)
        {
            errors.Add($"Tutorial must have between 3 and 30 steps (currently {workingCopy.Steps.Count}).");
        }
        else
        {
            var badStepOrders = workingCopy.Steps
                .Where(s => string.IsNullOrWhiteSpace(s.Description) || string.IsNullOrWhiteSpace(s.ImageUrl))
                .Select(s => s.StepOrder)
                .ToList();
            if (badStepOrders.Count > 0)
                errors.Add($"Steps {string.Join(", ", badStepOrders)} are missing description or image.");
        }

        if (workingCopy.Type == TutorialType.VIP)
        {
            var vipSettings = await _tutorialRepo.GetActiveCreatorVipSettingsAsync(command.AuthorId, ct);
            if (vipSettings is null)
                errors.Add("VIP tutorials require an active VIP pricing tier. BR-13.");
        }

        if (errors.Count > 0)
            throw new DomainException(string.Join(" ", errors));

        var fromStatus = workingCopy.Status;
        workingCopy.Status = TutorialStatus.PendingManagerReview;
        workingCopy.UpdatedAt = DateTime.UtcNow;

        await _tutorialRepo.UpdateAsync(workingCopy, ct);

        // IMMUTABLE — INSERT only (BR-17)
        await _tutorialRepo.AddReviewHistoryAsync(new TutorialReviewHistory
        {
            Id = Guid.NewGuid(),
            TutorialId = workingCopy.Id,
            ReviewerId = command.AuthorId,
            ReviewerRole = UserRoleType.User,
            FromStatus = fromStatus,
            ToStatus = TutorialStatus.PendingManagerReview,
            Action = "SubmitEdit",
            Reason = null,
            CreatedAt = DateTime.UtcNow
        }, ct);

        await _notifications.NotifyUsersWithRoleAsync(
            UserRoleType.Manager,
            NotificationType.TutorialReadyForManagerApproval,
            $"Bản chỉnh sửa hướng dẫn \"{workingCopy.Title}\" đang chờ bạn duyệt.",
            "Tutorial",
            workingCopy.Id,
            ct);

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
