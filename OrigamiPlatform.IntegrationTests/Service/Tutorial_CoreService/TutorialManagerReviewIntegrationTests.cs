using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Services.Tutorial_CoreService;

public class TutorialManagerReviewIntegrationTests : IntegrationTestBase
{
    public TutorialManagerReviewIntegrationTests(CustomWebApplicationFactory factory) : base(factory) { }

    // 🔬 Coverage Technique: Happy Path (Manager successfully publishes a tutorial from review queue)
    [Fact]
    public async Task ManagerPublish_PendingTutorial_SetsStatusToPublished_HappyPath()
    {
        // Arrange: Tạo user (Author) và Category trước để thỏa mãn Foreign Key trong DB
        var authorId = await AuthenticateAsAsync("User");

        var category = new Domain.Entities.Category { Name = "Origami Animals", IsActive = true };
        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync();

        var tutorial = new Domain.Entities.Tutorial
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId, // Sử dụng authorId hợp lệ vừa được tạo
            CategoryId = category.Id,
            Title = "Crane for Publishing",
            Description = "A complete tutorial description meeting length requirements.",
            Slug = "crane-for-publishing",
            CoverImageUrl = "https://img.com/cover.jpg",
            Type = TutorialType.Free,
            Difficulty = TutorialDifficulty.Beginner,
            Status = TutorialStatus.PendingManagerReview,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Tutorials.Add(tutorial);
        await _dbContext.SaveChangesAsync();

        // Đăng nhập bằng quyền Manager
        await AuthenticateAsAsync("Manager");

        // Act: Manager gọi API Publish
        var response = await _client.PutAsync($"/api/tutorials/{tutorial.Id}/publish", null);

        // Assert
        response.EnsureSuccessStatusCode();

        _dbContext.ChangeTracker.Clear();
        var dbTutorial = await _dbContext.Tutorials.FirstAsync(t => t.Id == tutorial.Id);
        dbTutorial.Status.Should().Be(TutorialStatus.Published);
        dbTutorial.PublishedAt.Should().NotBeNull();

        // Kiểm tra TutorialReviewHistory được ghi nhận
        var history = await _dbContext.TutorialReviewHistories.FirstOrDefaultAsync(h => h.TutorialId == tutorial.Id);
        history.Should().NotBeNull();
        history!.Action.Should().Be("Publish");
    }

    // 🔬 Coverage Technique: Error Path & Boundary Value Analysis (Manager rejects tutorial with short reason < 10 chars)
    [Fact]
    public async Task ManagerReject_ShortReason_ReturnsBadRequest_BoundaryError()
    {
        // Arrange
        var authorId = await AuthenticateAsAsync("User");

        var category = new Domain.Entities.Category { Name = "Origami Flowers", IsActive = true };
        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync();

        var tutorial = new Domain.Entities.Tutorial
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId, // Sử dụng authorId hợp lệ
            CategoryId = category.Id,
            Title = "Flower for Rejection",
            Description = "A valid description meeting requirements.",
            Slug = "flower-for-rejection",
            CoverImageUrl = "https://img.com/cover.jpg",
            Type = TutorialType.Free,
            Difficulty = TutorialDifficulty.Beginner,
            Status = TutorialStatus.PendingManagerReview,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Tutorials.Add(tutorial);
        await _dbContext.SaveChangesAsync();

        await AuthenticateAsAsync("Manager");

        var request = new ManagerRejectRequest("Too short");

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tutorials/{tutorial.Id}/reject", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        _dbContext.ChangeTracker.Clear();
        var dbTutorial = await _dbContext.Tutorials.FirstAsync(t => t.Id == tutorial.Id);
        dbTutorial.Status.Should().Be(TutorialStatus.PendingManagerReview, "Trạng thái không được thay đổi nếu lý do vi phạm độ dài");
    }

    // 🔬 Coverage Technique: Error Path & Security Constraints (Non-manager trying to publish tutorial returns 403 Forbidden)
    [Fact]
    public async Task ManagerPublish_AsNonManagerUser_ReturnsForbidden_ErrorPath()
    {
        // Arrange
        var authorId = await AuthenticateAsAsync("User");

        var category = new Domain.Entities.Category { Name = "Origami Boxes", IsActive = true };
        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync();

        var tutorial = new Domain.Entities.Tutorial
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId, // Sử dụng authorId hợp lệ
            CategoryId = category.Id,
            Title = "Box Tutorial",
            Description = "A valid description meeting requirements.",
            Slug = "box-tutorial",
            CoverImageUrl = "https://img.com/cover.jpg",
            Type = TutorialType.Free,
            Difficulty = TutorialDifficulty.Beginner,
            Status = TutorialStatus.PendingManagerReview,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Tutorials.Add(tutorial);
        await _dbContext.SaveChangesAsync();

        // Đăng nhập bằng quyền User thường (không phải Manager/Admin)
        await AuthenticateAsAsync("User");

        // Act
        var response = await _client.PutAsync($"/api/tutorials/{tutorial.Id}/publish", null);

        // Assert (NAC-02 FT-05: Phải trả về 403 Forbidden)
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}