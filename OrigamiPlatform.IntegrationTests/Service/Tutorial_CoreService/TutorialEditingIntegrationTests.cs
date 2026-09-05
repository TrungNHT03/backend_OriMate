using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Services.Tutorial_CoreService;

public class TutorialEditingIntegrationTests : IntegrationTestBase
{
    public TutorialEditingIntegrationTests(CustomWebApplicationFactory factory) : base(factory) { }

    // 🔬 Coverage Technique: Happy Path (Creator creates working copy of a published tutorial)
    [Fact]
    public async Task CreateWorkingCopy_AsAuthor_ReturnsSuccess_HappyPath()
    {
        // Arrange
        var authorId = await AuthenticateAsAsync("User");
        var category = new Domain.Entities.Category { Name = "Origami Fold", IsActive = true };
        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync();

        var publishedTutorial = new Domain.Entities.Tutorial
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            CategoryId = category.Id,
            Title = "Published Crane",
            Description = "A valid published description meeting requirement length.",
            Slug = "published-crane",
            CoverImageUrl = "https://img.com/cover.jpg",
            Type = TutorialType.Free,
            Difficulty = TutorialDifficulty.Beginner,
            Status = TutorialStatus.Published,
            PublishedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Tutorials.Add(publishedTutorial);
        await _dbContext.SaveChangesAsync();

        // Act: Creator tạo working copy để chỉnh sửa sau khi publish
        var response = await _client.PostAsync($"/api/tutorials/{publishedTutorial.Id}/edit", null);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("status").GetString().Should().Be("EditPendingReview");

        _dbContext.ChangeTracker.Clear();
        var workingCopy = await _dbContext.Tutorials.FirstOrDefaultAsync(t => t.ParentTutorialId == publishedTutorial.Id);
        workingCopy.Should().NotBeNull();
        workingCopy!.Status.Should().Be(TutorialStatus.EditPendingReview);
    }

    // 🔬 Coverage Technique: Transaction Boundary & Happy Path (Manager approves working copy, performing atomic swap)
    [Fact]
    public async Task ManagerApproveEdit_WorkingCopy_PerformsAtomicSwap_TransactionBoundary()
    {
        // Arrange
        var authorId = await AuthenticateAsAsync("User");
        var category = new Domain.Entities.Category { Name = "Origami Modular", IsActive = true };
        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync();

        var publishedTutorial = new Domain.Entities.Tutorial
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            CategoryId = category.Id,
            Title = "Old Title",
            Description = "Old description meeting requirement length.",
            Slug = "old-title-slug",
            CoverImageUrl = "https://img.com/cover.jpg",
            Type = TutorialType.Free,
            Difficulty = TutorialDifficulty.Beginner,
            Status = TutorialStatus.Published,
            PublishedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        var workingCopy = new Domain.Entities.Tutorial
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            CategoryId = category.Id,
            ParentTutorialId = publishedTutorial.Id,
            Title = "New Updated Title",
            Description = "New updated description meeting requirement length.",
            Slug = "old-title-slug-edit",
            CoverImageUrl = "https://img.com/cover-new.jpg",
            Type = TutorialType.Free,
            Difficulty = TutorialDifficulty.Beginner,
            Status = TutorialStatus.PendingManagerReview,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Tutorials.AddRange(publishedTutorial, workingCopy);
        await _dbContext.SaveChangesAsync();

        // Đăng nhập bằng quyền Manager
        await AuthenticateAsAsync("Manager");

        // Act: Manager phê duyệt working copy
        var response = await _client.PutAsync($"/api/tutorials/{workingCopy.Id}/approve-edit", null);

        // Assert
        response.EnsureSuccessStatusCode();

        _dbContext.ChangeTracker.Clear();
        var dbOriginal = await _dbContext.Tutorials.FirstAsync(t => t.Id == publishedTutorial.Id);
        dbOriginal.Title.Should().Be("New Updated Title"); // Nội dung đã được swap atomic vào bản gốc

        var dbWorkingCopy = await _dbContext.Tutorials.FirstAsync(t => t.Id == workingCopy.Id);
        dbWorkingCopy.Status.Should().Be(TutorialStatus.Merged); // Working copy chuyển sang trạng thái Merged (terminal)
    }
}
