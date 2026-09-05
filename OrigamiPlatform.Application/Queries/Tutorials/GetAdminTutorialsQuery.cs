namespace OrigamiPlatform.Application.Queries.Tutorials;

/// <summary>Admin tutorial management list: every main tutorial, any author, any status.</summary>
public record GetAdminTutorialsQuery(
    string? Search,
    string? Status,
    int? CategoryId,
    bool? IsOfficial,
    string? Difficulty,
    int Page,
    int PageSize
);
