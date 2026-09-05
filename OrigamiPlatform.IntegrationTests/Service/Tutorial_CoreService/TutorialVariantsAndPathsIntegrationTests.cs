using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.LearningPaths;
using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Domain.Enums;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Services.Tutorial_CoreService;

public class TutorialVariantsAndPathsIntegrationTests : IntegrationTestBase
{
    public TutorialVariantsAndPathsIntegrationTests(CustomWebApplicationFactory factory) : base(factory) { }

    // 🔬 Coverage Technique: Happy Path (Creator links a variant tutorial successfully - FT-11)
    [Fact]
    public async Task AddVariant_ValidPair_LinksSuccessfully_HappyPath()
    {
        // Arrange
        var authorId = await AuthenticateAsAsync("User");
        var category = new Domain.Entities.Category { Name = "Origami Boxes", IsActive = true };
        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync();

        var parentId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        var parent = new Domain.Entities.Tutorial
        {
            Id = parentId,
            AuthorId = authorId,
            CategoryId = category.Id,
            Title = "Parent Crane",
            Description = "Parent tutorial description meeting requirements.",
            Slug = "parent-crane",
            CoverImageUrl = "https://img.com/cover.jpg",
            Type = TutorialType.Free,
            Difficulty = TutorialDifficulty.Beginner,
            Status = TutorialStatus.Published,
            CreatedAt = DateTime.UtcNow
        };

        var variant = new Domain.Entities.Tutorial
        {
            Id = variantId,
            AuthorId = authorId,
            CategoryId = category.Id,
            Title = "Variant Crane",
            Description = "Variant tutorial description meeting requirements.",
            Slug = "variant-crane",
            CoverImageUrl = "https://img.com/cover2.jpg",
            Type = TutorialType.Free,
            Difficulty = TutorialDifficulty.Intermediate,
            Status = TutorialStatus.Published,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Tutorials.AddRange(parent, variant);
        await _dbContext.SaveChangesAsync();

        var request = new AddVariantRequest(variantId, DifficultyDelta: 1);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/tutorials/{parentId}/variants", request);

        // Assert
        response.EnsureSuccessStatusCode();

        _dbContext.ChangeTracker.Clear();
        var link = await _dbContext.TutorialVariants.FirstOrDefaultAsync(v => v.ParentTutorialId == parentId && v.VariantTutorialId == variantId);
        link.Should().NotBeNull();
        link!.DifficultyDelta.Should().Be(1);
    }

    // 🔬 Coverage Technique: Happy Path & Security Constraints (Admin creates and publishes a Curated Learning Path - FT-33)
    [Fact]
    public async Task CreateAndPublishLearningPath_AsAdmin_ReturnsSuccess_HappyPath()
    {
        // Arrange: Đăng nhập Admin và tạo trước Category & LearningPathMode để thỏa mãn khóa ngoại
        var adminId = await AuthenticateAsAsync("Admin");

        var category = new Domain.Entities.Category { Name = "Origami Basics", IsActive = true };
        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync();

        var mode = new Domain.Entities.LearningPathMode { Id = Guid.NewGuid(), Name = "Beginner Roadmap", SortOrder = 1, IsActive = true };
        _dbContext.LearningPathModes.Add(mode);
        await _dbContext.SaveChangesAsync();

        var officialTutorial = new Domain.Entities.Tutorial
        {
            Id = Guid.NewGuid(),
            AuthorId = OrigamiPlatform.Domain.Constants.SystemUsers.OfficialTutorialAuthorId,
            CategoryId = category.Id,
            Title = "Official Basic Fold",
            Description = "Official tutorial description meeting requirements.",
            Slug = "official-basic-fold",
            CoverImageUrl = "https://img.com/cover.jpg",
            Type = TutorialType.Free,
            Difficulty = TutorialDifficulty.Beginner,
            Status = TutorialStatus.Published,
            IsOfficial = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Tutorials.Add(officialTutorial);
        await _dbContext.SaveChangesAsync();

        var createReq = new CreateLearningPathRequest(
            Title: "Beginner 5-Step Path",
            Description: "A structured learning sequence for absolute beginners starting out.",
            CoverImageUrl: "https://img.com/path-cover.jpg",
            LearningPathModeId: mode.Id,
            TutorialIds: new List<Guid> { officialTutorial.Id }
        );

        // Act 1: Admin tạo Learning Path ở trạng thái Draft
        var createResponse = await _client.PostAsJsonAsync("/api/learning-paths", createReq);
        createResponse.EnsureSuccessStatusCode();
        var createdPath = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var pathId = createdPath.GetProperty("id").GetGuid(); // Đã sửa từ createPath thành createdPath

        // Act 2: Admin xuất bản Learning Path
        var publishResponse = await _client.PutAsync($"/api/learning-paths/{pathId}/publish", null);

        // Assert
        publishResponse.EnsureSuccessStatusCode();

        _dbContext.ChangeTracker.Clear();
        var dbPath = await _dbContext.LearningPaths.Include(p => p.Items).FirstAsync(p => p.Id == pathId);
        dbPath.Status.Should().Be(LearningPathStatus.Published);
        dbPath.Items.Count.Should().Be(1);
    }
}