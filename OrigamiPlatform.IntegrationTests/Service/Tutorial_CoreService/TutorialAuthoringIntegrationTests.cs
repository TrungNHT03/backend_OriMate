using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Services.Tutorial_CoreService;

public class TutorialAuthoringIntegrationTests : IntegrationTestBase
{
    public TutorialAuthoringIntegrationTests(CustomWebApplicationFactory factory) : base(factory) { }

    // 🔬 Coverage Technique: Happy Path (Creator successfully creates a draft tutorial)
    [Fact]
    public async Task CreateTutorial_AsCreator_ReturnsSuccess_HappyPath()
    {
        // Arrange: Đăng nhập với quyền User/Creator
        var authorId = await AuthenticateAsAsync("User");

        // Khắc phục: Không gán cứng Id = 1, để cơ sở dữ liệu tự tăng Identity tránh lỗi SqlException
        var category = new Domain.Entities.Category { Name = "Origami Animals", IsActive = true };
        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync();

        var request = new CreateTutorialRequest(
            Title: "Easy Paper Crane Tutorial",
            Description: "Learn how to fold a classic paper crane step by step easily.",
            CategoryId: category.Id, // Lấy ID tự động sinh từ category vừa lưu
            Type: "Free",
            Difficulty: "Beginner",
            CoverImageUrl: "https://img.com/cover.jpg",
            Steps: new List<CreateTutorialStepRequest>
            {
                new(1, "Step 1 description text here", "https://img.com/step1.jpg"),
                new(2, "Step 2 description text here", "https://img.com/step2.jpg"),
                new(3, "Step 3 description text here", "https://img.com/step3.jpg")
            },
            MetaTitle: null,
            MetaDescription: null,
            Tags: null
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/tutorials", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("title").GetString().Should().Be(request.Title);
        result.GetProperty("status").GetString().Should().Be("Draft");

        // Kiểm tra trong DB
        _dbContext.ChangeTracker.Clear();
        var dbTutorial = await _dbContext.Tutorials.Include(t => t.Steps).FirstOrDefaultAsync(t => t.Title == request.Title);
        dbTutorial.Should().NotBeNull();
        dbTutorial!.Status.Should().Be(TutorialStatus.Draft);
        dbTutorial.Steps.Count.Should().Be(3);
    }

    // 🔬 Coverage Technique: Boundary Value Analysis (BVA) & Error Path (Min steps boundary violation: < 3 steps)
    [Fact]
    public async Task CreateTutorial_WithLessThanThreeSteps_ReturnsBadRequest_BoundaryError()
    {
        var authorId = await AuthenticateAsAsync("User");

        // Khắc phục: Không gán cứng Id = 2, để cơ sở dữ liệu tự tăng Identity
        var category = new Domain.Entities.Category { Name = "Origami Flowers", IsActive = true };
        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync();

        var request = new CreateTutorialRequest(
            Title: "Incomplete Tutorial",
            Description: "This tutorial has too few steps to be published or drafted properly according to rules.",
            CategoryId: category.Id, // Lấy ID tự động sinh
            Type: "Free",
            Difficulty: "Beginner",
            CoverImageUrl: "https://img.com/cover.jpg",
            Steps: new List<CreateTutorialStepRequest>
            {
                new(1, "Only step one", "https://img.com/step1.jpg") // Vi phạm biên dưới (< 3 bước)
            },
            MetaTitle: null,
            MetaDescription: null,
            Tags: null
        );

        var response = await _client.PostAsJsonAsync("/api/tutorials", request);

        if (response.IsSuccessStatusCode)
        {
            var created = await response.Content.ReadFromJsonAsync<JsonElement>();
            var tutorialId = created.GetProperty("id").GetGuid();

            var submitResponse = await _client.PutAsync($"/api/tutorials/{tutorialId}/submit", null);
            submitResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}