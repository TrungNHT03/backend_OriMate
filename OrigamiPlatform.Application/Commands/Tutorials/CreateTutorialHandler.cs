using System.Text.RegularExpressions;
using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Tutorials;

public class CreateTutorialHandler
{
    private readonly ITutorialRepository _tutorialRepo;

    public CreateTutorialHandler(ITutorialRepository tutorialRepo)
        => _tutorialRepo = tutorialRepo;

    public async Task<TutorialResponse> HandleAsync(CreateTutorialCommand command, CancellationToken ct = default)
    {
        var request = command.Request;
        var authorId = command.AuthorId;

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
            var vipSettings = await _tutorialRepo.GetActiveCreatorVipSettingsAsync(authorId, ct);
            if (vipSettings is null)
                throw new DomainException(
                    "You must configure a VIP pricing tier before creating VIP tutorials. BR-13.");
        }

        var category = await _tutorialRepo.GetActiveCategoryAsync(request.CategoryId, ct);
        if (category is null)
            throw new DomainException($"Category {request.CategoryId} does not exist or is not active.");

        var slug = await GenerateUniqueSlugAsync(request.Title, ct);

        var tutorial = new Tutorial
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            CategoryId = request.CategoryId,
            Title = request.Title,
            Description = request.Description,
            Slug = slug,
            CoverImageUrl = request.CoverImageUrl,
            Type = tutorialType,
            Difficulty = tutorialDifficulty,
            Status = TutorialStatus.Draft,
            MetaTitle = request.MetaTitle,
            MetaDescription = request.MetaDescription,
            Tags = request.Tags,
            CreatedAt = DateTime.UtcNow
        };

        if (request.Steps is { Count: > 0 })
        {
            foreach (var stepReq in request.Steps)
            {
                tutorial.Steps.Add(new TutorialStep
                {
                    Id = Guid.NewGuid(),
                    TutorialId = tutorial.Id,
                    StepOrder = stepReq.StepOrder,
                    Description = stepReq.Description,
                    ImageUrl = stepReq.ImageUrl,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _tutorialRepo.AddAsync(tutorial, ct);
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
