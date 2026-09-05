using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.LearningAndDiscovery;

public class SearchAndFilterTests : IntegrationTestBase
{
    public SearchAndFilterTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<Tutorial> SeedTutorialAsync(int categoryId, Guid authorId, string title)
    {
        var tutorial = new Tutorial
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = title.ToLower().Replace(" ", "-"),
            CategoryId = categoryId,
            AuthorId = authorId,
            Status = TutorialStatus.Published,
            PublishedAt = DateTime.UtcNow,
            Difficulty = TutorialDifficulty.Beginner
        };
        _dbContext.Tutorials.Add(tutorial);
        await _dbContext.SaveChangesAsync();
        return tutorial;
    }

    // [Happy Path] (AC-01) - Tìm kiếm hợp lệ trả về đúng kết quả
    [Fact]
    public async Task SearchTutorials_WithValidKeyword_ReturnsMatchingResults()
    {
        // 1. Arrange
        var prereq = await SeedDefaultPrerequisitesAsync();
        await SeedTutorialAsync(prereq.CategoryId, prereq.AuthorId, "Origami Dragon");
        await SeedTutorialAsync(prereq.CategoryId, prereq.AuthorId, "Paper Crane");

        // 2. Act
        var response = await _client.GetAsync($"/api/tutorials?searchTerm=Dragon&categoryId={prereq.CategoryId}");

        // 3. Assert
        response.EnsureSuccessStatusCode();

        // Deserialize chính xác theo DTO của Backend (PagedResult<TutorialListItemResponse>)
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TutorialListItemResponse>>();

        result.Should().NotBeNull();
        result!.Items.Should().Contain(t => t.Title == "Origami Dragon");

        // Đoạn này sẽ FAIL nếu Backend không lọc từ khóa "Dragon"
        result.Items.Should().NotContain(t => t.Title == "Paper Crane");
    }

    // [Error Path / Suppression] (NAC-03) - Tìm CategoryId không tồn tại trả về list rỗng
    [Fact]
    public async Task SearchTutorials_WithNonExistentCategory_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/tutorials?categoryId=999999");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TutorialListItemResponse>>();

        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
    }

    // [Boundary Value Analysis] (NAC-02 / BV-01) - PageSize vượt trần 100 sẽ tự cap lại, không ném lỗi
    [Fact]
    public async Task SearchTutorials_WithExcessivePageSize_CapsToMaximumWithoutError()
    {
        var response = await _client.GetAsync("/api/tutorials?pageSize=500");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TutorialListItemResponse>>();

        result.Should().NotBeNull();

        // SỬA Ở ĐÂY: Dùng BeLessThanOrEqualTo thay vì BeLessOrEqualTo
        result!.PageSize.Should().BeLessThanOrEqualTo(100);
    }
}