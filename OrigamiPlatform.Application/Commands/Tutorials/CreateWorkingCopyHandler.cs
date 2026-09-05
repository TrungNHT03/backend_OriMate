using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Tutorials;

public class CreateWorkingCopyHandler
{
    private readonly ITutorialRepository _tutorialRepo;

    public CreateWorkingCopyHandler(ITutorialRepository tutorialRepo) => _tutorialRepo = tutorialRepo;

    public async Task<WorkingCopyResponse> HandleAsync(CreateWorkingCopyCommand command, CancellationToken ct = default)
    {
        var original = await _tutorialRepo.GetByIdWithStepsAsync(command.TutorialId, ct)
            ?? throw new NotFoundException($"Tutorial {command.TutorialId} not found.");

        if (original.AuthorId != command.AuthorId)
            throw new ForbiddenException("You are not the author of this tutorial.");

        if (original.Status != TutorialStatus.Published)
            throw new DomainException("Only published tutorials can be edited.");

        var existing = await _tutorialRepo.GetWorkingCopyByParentIdAsync(command.TutorialId, ct);
        if (existing is not null)
            throw new DomainException("An edit is already in progress for this tutorial.");

        var editSlug = await GenerateEditSlugAsync(original.Slug, ct);

        var workingCopy = new Tutorial
        {
            Id = Guid.NewGuid(),
            AuthorId = original.AuthorId,
            CategoryId = original.CategoryId,
            ParentTutorialId = original.Id,
            Title = original.Title,
            Description = original.Description,
            Slug = editSlug,
            CoverImageUrl = original.CoverImageUrl,
            Type = original.Type,
            Difficulty = original.Difficulty,
            Status = TutorialStatus.EditPendingReview,
            PublishedAt = null,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var s in original.Steps)
        {
            workingCopy.Steps.Add(new TutorialStep
            {
                Id = Guid.NewGuid(),
                TutorialId = workingCopy.Id,
                StepOrder = s.StepOrder,
                Description = s.Description,
                ImageUrl = s.ImageUrl,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _tutorialRepo.AddAsync(workingCopy, ct);

        return new WorkingCopyResponse(workingCopy.Id, original.Id, workingCopy.Status.ToString());
    }

    private async Task<string> GenerateEditSlugAsync(string originalSlug, CancellationToken ct)
    {
        var baseSlug = $"{originalSlug}-edit";
        if (baseSlug.Length > 120)
            baseSlug = baseSlug[..120].TrimEnd('-');

        var slug = baseSlug;
        var suffix = 2;
        while (await _tutorialRepo.SlugExistsAsync(slug, ct))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix++;
        }
        return slug;
    }
}
