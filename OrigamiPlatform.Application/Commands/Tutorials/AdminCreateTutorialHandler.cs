using System.Text.RegularExpressions;
using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Constants;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Tutorials;

/// <summary>
/// Admin/Manager authoring shortcut: the tutorial is always Free and goes straight to Published —
/// no Draft/PendingManagerReview hop, since the actor already has publishing authority.
/// The tutorial is always attributed to the fixed <see cref="SystemUsers.OfficialTutorialAuthorId"/>
/// account rather than the acting Admin/Manager's own Id, so every officially-authored tutorial
/// shares one consistent author for later features to key off of.
/// </summary>
public class AdminCreateTutorialHandler
{
    private readonly ITutorialRepository _tutorialRepo;

    public AdminCreateTutorialHandler(ITutorialRepository tutorialRepo)
        => _tutorialRepo = tutorialRepo;

    public async Task<TutorialResponse> HandleAsync(AdminCreateTutorialCommand command, CancellationToken ct = default)
    {
        var request = command.Request;
        var actorId = command.ActorId;

        if (request.Title.Length < 5 || request.Title.Length > 150)
            throw new DomainException("Title must be between 5 and 150 characters. BR-12.");
        if (request.Description.Length < 20 || request.Description.Length > 500)
            throw new DomainException("Description must be between 20 and 500 characters. BR-12.");
        if (string.IsNullOrWhiteSpace(request.CoverImageUrl))
            throw new DomainException("A cover image is required to publish directly.");

        var steps = request.Steps ?? new List<CreateTutorialStepRequest>();
        if (steps.Count < 3 || steps.Count > 30)
            throw new DomainException($"Need 3 to 30 steps to publish directly (got {steps.Count}).");

        if (!Enum.TryParse<TutorialDifficulty>(request.Difficulty, ignoreCase: true, out var tutorialDifficulty))
            throw new DomainException($"Invalid difficulty '{request.Difficulty}'. Valid values: Beginner, Intermediate, Advanced.");

        var category = await _tutorialRepo.GetActiveCategoryAsync(request.CategoryId, ct);
        if (category is null)
            throw new DomainException($"Category {request.CategoryId} does not exist or is not active.");

        var slug = await GenerateUniqueSlugAsync(request.Title, ct);
        var now = DateTime.UtcNow;

        // Fixed rule: admin/manager-authored tutorials are always Free, published immediately,
        // and always attributed to the one fixed official author (never the acting staff account).
        var tutorial = new Tutorial
        {
            Id = Guid.NewGuid(),
            AuthorId = SystemUsers.OfficialTutorialAuthorId,
            CategoryId = request.CategoryId,
            Title = request.Title,
            Description = request.Description,
            Slug = slug,
            CoverImageUrl = request.CoverImageUrl,
            Type = TutorialType.Free,
            Difficulty = tutorialDifficulty,
            Status = TutorialStatus.Published,
            IsOfficial = true,
            PublishedAt = now,
            CreatedAt = now
        };

        foreach (var stepReq in steps)
        {
            if (string.IsNullOrWhiteSpace(stepReq.Description) || string.IsNullOrWhiteSpace(stepReq.ImageUrl))
                throw new DomainException($"Step {stepReq.StepOrder} needs both a description and an image to publish directly.");

            tutorial.Steps.Add(new TutorialStep
            {
                Id = Guid.NewGuid(),
                TutorialId = tutorial.Id,
                StepOrder = stepReq.StepOrder,
                Description = stepReq.Description,
                ImageUrl = stepReq.ImageUrl,
                CreatedAt = now
            });
        }

        await _tutorialRepo.AddAsync(tutorial, ct);

        // IMMUTABLE — INSERT only (BR-17), keeps the same audit trail shape as the normal review flow.
        await _tutorialRepo.AddReviewHistoryAsync(new TutorialReviewHistory
        {
            Id = Guid.NewGuid(),
            TutorialId = tutorial.Id,
            ReviewerId = actorId,
            ReviewerRole = command.ActorRole,
            FromStatus = TutorialStatus.Draft,
            ToStatus = TutorialStatus.Published,
            Action = "AdminAutoPublish",
            Reason = $"Published directly by {command.ActorRole}.",
            CreatedAt = now
        }, ct);

        return MapToResponse(tutorial);
    }

    private async Task<string> GenerateUniqueSlugAsync(string title, CancellationToken ct)
    {
        var baseSlug = Regex.Replace(title.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        if (baseSlug.Length > 110)
            baseSlug = baseSlug[..110].TrimEnd('-');

        var slug = baseSlug;
        var suffix = 2;
        while (await _tutorialRepo.SlugExistsAsync(slug, ct))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix++;
        }
        return slug;
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
