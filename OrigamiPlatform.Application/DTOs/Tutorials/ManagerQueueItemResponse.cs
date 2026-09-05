namespace OrigamiPlatform.Application.DTOs.Tutorials;

/// <summary>One row in the manager review queue. IsEdit distinguishes a working copy
/// (approve-edit/reject-edit apply) from a new tutorial submission (publish/reject apply).</summary>
public record ManagerQueueItemResponse(
    Guid Id,
    string Title,
    string Slug,
    string AuthorName,
    int StepCount,
    DateTime CreatedAt,
    bool IsEdit,
    Guid? ParentTutorialId
);
