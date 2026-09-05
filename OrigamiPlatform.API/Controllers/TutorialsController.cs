using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.AdminConfiguration;
using OrigamiPlatform.Application.Commands.Tutorials;
using OrigamiPlatform.Application.DTOs;
using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Application.Queries.AdminConfiguration;
using OrigamiPlatform.Application.Queries.Tutorials;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/tutorials")]
public class TutorialsController : ControllerBase
{
    private readonly GetTutorialsHandler _getTutorials;
    private readonly GetTutorialBySlugHandler _getTutorialBySlug;
    private readonly GetMyTutorialsHandler _getMyTutorials;
    private readonly GetTutorialForAuthorHandler _getTutorialForAuthor;
    private readonly GetCategoriesHandler _getCategories;
    private readonly GetManagerQueueHandler _getManagerQueue;
    private readonly CreateTutorialHandler _createTutorial;
    private readonly AdminCreateTutorialHandler _adminCreateTutorial;
    private readonly UpdateTutorialHandler _updateTutorial;
    private readonly SubmitTutorialHandler _submitTutorial;
    private readonly ManagerPublishHandler _managerPublish;
    private readonly ManagerRejectHandler _managerReject;
    private readonly ManagerRemoveHandler _managerRemove;
    private readonly CreateWorkingCopyHandler _createWorkingCopy;
    private readonly UpdateWorkingCopyHandler _updateWorkingCopy;
    private readonly SubmitEditHandler _submitEdit;
    private readonly ManagerApproveEditHandler _managerApproveEdit;
    private readonly ManagerRejectEditHandler _managerRejectEdit;
    private readonly GetAdminTutorialsHandler _getAdminTutorials;
    private readonly GetTutorialForAdminHandler _getTutorialForAdmin;
    private readonly AdminUpdateTutorialHandler _adminUpdateTutorial;
    private readonly SetOfficialTutorialHandler _setOfficialTutorial;
    private readonly GetRecommendedTutorialsHandler _getRecommendedTutorials;
    private readonly AddVariantHandler _addVariant;
    private readonly RemoveVariantHandler _removeVariant;
    private readonly GetVariantsHandler _getVariants;

    public TutorialsController(
        GetTutorialsHandler getTutorials,
        GetTutorialBySlugHandler getTutorialBySlug,
        GetMyTutorialsHandler getMyTutorials,
        GetTutorialForAuthorHandler getTutorialForAuthor,
        GetCategoriesHandler getCategories,
        GetManagerQueueHandler getManagerQueue,
        CreateTutorialHandler createTutorial,
        AdminCreateTutorialHandler adminCreateTutorial,
        UpdateTutorialHandler updateTutorial,
        SubmitTutorialHandler submitTutorial,
        ManagerPublishHandler managerPublish,
        ManagerRejectHandler managerReject,
        ManagerRemoveHandler managerRemove,
        CreateWorkingCopyHandler createWorkingCopy,
        UpdateWorkingCopyHandler updateWorkingCopy,
        SubmitEditHandler submitEdit,
        ManagerApproveEditHandler managerApproveEdit,
        ManagerRejectEditHandler managerRejectEdit,
        GetAdminTutorialsHandler getAdminTutorials,
        GetTutorialForAdminHandler getTutorialForAdmin,
        AdminUpdateTutorialHandler adminUpdateTutorial,
        SetOfficialTutorialHandler setOfficialTutorial,
        GetRecommendedTutorialsHandler getRecommendedTutorials,
        AddVariantHandler addVariant,
        RemoveVariantHandler removeVariant,
        GetVariantsHandler getVariants)
    {
        _getTutorials = getTutorials;
        _getTutorialBySlug = getTutorialBySlug;
        _getMyTutorials = getMyTutorials;
        _getTutorialForAuthor = getTutorialForAuthor;
        _getCategories = getCategories;
        _getManagerQueue = getManagerQueue;
        _createTutorial = createTutorial;
        _adminCreateTutorial = adminCreateTutorial;
        _updateTutorial = updateTutorial;
        _submitTutorial = submitTutorial;
        _managerPublish = managerPublish;
        _managerReject = managerReject;
        _managerRemove = managerRemove;
        _createWorkingCopy = createWorkingCopy;
        _updateWorkingCopy = updateWorkingCopy;
        _submitEdit = submitEdit;
        _managerApproveEdit = managerApproveEdit;
        _managerRejectEdit = managerRejectEdit;
        _getAdminTutorials = getAdminTutorials;
        _getTutorialForAdmin = getTutorialForAdmin;
        _adminUpdateTutorial = adminUpdateTutorial;
        _setOfficialTutorial = setOfficialTutorial;
        _getRecommendedTutorials = getRecommendedTutorials;
        _addVariant = addVariant;
        _removeVariant = removeVariant;
        _getVariants = getVariants;
    }

    // ── Public ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetTutorials(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] string? difficulty,
        [FromQuery] string? type,
        [FromQuery] string? sortBy,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _getTutorials.HandleAsync(
            new GetTutorialsQuery(search, categoryId, difficulty, type, sortBy, page, pageSize, GetCurrentUserId()), ct);
        return Ok(result);
    }

    [HttpGet("{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBySlug([FromRoute] string slug, CancellationToken ct)
    {
        var result = await _getTutorialBySlug.HandleAsync(
            new GetTutorialBySlugQuery(slug, GetCurrentUserId()), ct);
        return Ok(result);
    }

    /// <summary>GET /api/tutorials/recommended — FT-31 rule-based recommendation. Anonymous callers
    /// and users with no completed tutorial yet get the most-liked Beginner tutorials.</summary>
    [HttpGet("recommended")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRecommended(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _getRecommendedTutorials.HandleAsync(
            new GetRecommendedTutorialsQuery(GetCurrentUserId(), page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>GET /api/tutorials/categories — Active categories, for the authoring form's category dropdown
    /// and for the public library page's category filter.</summary>
    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var result = await _getCategories.HandleAsync(new GetCategoriesQuery(), ct);
        return Ok(result.Where(c => c.IsActive).ToList());
    }

    // ── Authoring ────────────────────────────────────────────────────────────

    /// <summary>POST /api/tutorials — Create a draft tutorial (any authenticated user).</summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateTutorial(
        [FromBody] CreateTutorialRequest request, CancellationToken ct)
    {
        var authorId = GetCurrentUserId()!.Value;
        var result = await _createTutorial.HandleAsync(new CreateTutorialCommand(authorId, request), ct);
        return CreatedAtAction(nameof(GetBySlug), new { slug = result.Slug }, result);
    }

    /// <summary>
    /// POST /api/tutorials/admin — Admin/Manager writes and publishes a tutorial directly: always Free,
    /// no review queue (they already hold publishing authority). The tutorial is attributed to the fixed
    /// official author account, not to the acting Admin/Manager's own account.
    /// </summary>
    [HttpPost("admin")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> AdminCreateTutorial(
        [FromBody] CreateTutorialRequest request, CancellationToken ct)
    {
        var actorId = GetCurrentUserId()!.Value;
        var actorRole = User.IsInRole("Admin") ? UserRoleType.Admin : UserRoleType.Manager;
        var result = await _adminCreateTutorial.HandleAsync(
            new AdminCreateTutorialCommand(actorId, actorRole, request), ct);
        return CreatedAtAction(nameof(GetBySlug), new { slug = result.Slug }, result);
    }

    /// <summary>GET /api/tutorials/{id} — Author's own tutorial detail (with steps), any status. Used to pre-fill the edit form.</summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetTutorialForAuthor(Guid id, CancellationToken ct)
    {
        var authorId = GetCurrentUserId()!.Value;
        var result = await _getTutorialForAuthor.HandleAsync(new GetTutorialForAuthorQuery(id, authorId), ct);
        return Ok(result);
    }

    /// <summary>PUT /api/tutorials/{id} — Edit a draft/revision-required tutorial (author only, before publish).</summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateTutorial(
        Guid id, [FromBody] UpdateTutorialRequest request, CancellationToken ct)
    {
        var authorId = GetCurrentUserId()!.Value;
        var result = await _updateTutorial.HandleAsync(new UpdateTutorialCommand(id, authorId, request), ct);
        return Ok(result);
    }

    /// <summary>PUT /api/tutorials/{id}/submit — Submit draft for manager review (author only, BR-TUT-01).</summary>
    [HttpPut("{id:guid}/submit")]
    [Authorize]
    public async Task<IActionResult> SubmitForReview(Guid id, CancellationToken ct)
    {
        var authorId = GetCurrentUserId()!.Value;
        var result = await _submitTutorial.HandleAsync(new SubmitTutorialCommand(id, authorId), ct);
        return Ok(result);
    }

    /// <summary>GET /api/tutorials/my-tutorials — Author's own tutorials across all statuses.</summary>
    [HttpGet("my-tutorials")]
    [Authorize]
    public async Task<IActionResult> GetMyTutorials(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
    {
        var authorId = GetCurrentUserId()!.Value;
        var result = await _getMyTutorials.HandleAsync(new GetMyTutorialsQuery(authorId, page, pageSize), ct);
        return Ok(result);
    }

    // ── Edit-after-publish ───────────────────────────────────────────────────

    /// <summary>POST /api/tutorials/{id}/edit — Author creates a working copy of a published tutorial.</summary>
    [HttpPost("{id:guid}/edit")]
    [Authorize]
    public async Task<IActionResult> CreateWorkingCopy(Guid id, CancellationToken ct)
    {
        var authorId = GetCurrentUserId()!.Value;
        var result = await _createWorkingCopy.HandleAsync(new CreateWorkingCopyCommand(id, authorId), ct);
        return Ok(result);
    }

    /// <summary>PUT /api/tutorials/{id}/edit-content — Author updates the working copy's content.</summary>
    [HttpPut("{id:guid}/edit-content")]
    [Authorize]
    public async Task<IActionResult> UpdateWorkingCopy(
        Guid id, [FromBody] UpdateTutorialRequest request, CancellationToken ct)
    {
        var authorId = GetCurrentUserId()!.Value;
        var result = await _updateWorkingCopy.HandleAsync(new UpdateWorkingCopyCommand(id, authorId, request), ct);
        return Ok(result);
    }

    /// <summary>PUT /api/tutorials/{id}/submit-edit — Author submits working copy for manager review (BR-TUT-01).</summary>
    [HttpPut("{id:guid}/submit-edit")]
    [Authorize]
    public async Task<IActionResult> SubmitEdit(Guid id, CancellationToken ct)
    {
        var authorId = GetCurrentUserId()!.Value;
        var result = await _submitEdit.HandleAsync(new SubmitEditCommand(id, authorId), ct);
        return Ok(result);
    }

    /// <summary>PUT /api/tutorials/{id}/approve-edit — Manager approves edit: swaps content into original.</summary>
    [HttpPut("{id:guid}/approve-edit")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ManagerApproveEdit(Guid id, CancellationToken ct)
    {
        var managerId = GetCurrentUserId()!.Value;
        await _managerApproveEdit.HandleAsync(new ManagerApproveEditCommand(id, managerId), ct);
        return Ok(new MessageResponse("Tutorial edit approved and published."));
    }

    /// <summary>PUT /api/tutorials/{id}/reject-edit — Manager rejects edit, returns working copy to author.</summary>
    [HttpPut("{id:guid}/reject-edit")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ManagerRejectEdit(
        Guid id, [FromBody] ManagerRejectRequest request, CancellationToken ct)
    {
        var managerId = GetCurrentUserId()!.Value;
        await _managerRejectEdit.HandleAsync(new ManagerRejectEditCommand(id, managerId, request), ct);
        return Ok(new MessageResponse("Tutorial edit rejected. Author has been notified."));
    }

    // ── Manager final approval ───────────────────────────────────────────────

    /// <summary>GET /api/tutorials/manager-queue — Tutorials awaiting manager review (new submissions + edits).</summary>
    [HttpGet("manager-queue")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetManagerQueue(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _getManagerQueue.HandleAsync(new GetManagerQueueQuery(page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>PUT /api/tutorials/{id}/publish — Manager publishes a tutorial (BR-16).</summary>
    [HttpPut("{id:guid}/publish")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ManagerPublish(Guid id, CancellationToken ct)
    {
        var managerId = GetCurrentUserId()!.Value;
        await _managerPublish.HandleAsync(new ManagerPublishCommand(id, managerId), ct);
        return Ok(new MessageResponse("Tutorial has been published successfully."));
    }

    /// <summary>PUT /api/tutorials/{id}/reject — Manager sends a tutorial back for revision (BR-TUT-01, BR-18: not terminal).</summary>
    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ManagerReject(
        Guid id, [FromBody] ManagerRejectRequest request, CancellationToken ct)
    {
        var managerId = GetCurrentUserId()!.Value;
        await _managerReject.HandleAsync(new ManagerRejectCommand(id, managerId, request), ct);
        return Ok(new MessageResponse("Tutorial has been sent back for revision."));
    }

    /// <summary>DELETE /api/tutorials/{id} — Manager soft-removes a published tutorial (BR-16, terminal).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ManagerRemove(
        Guid id, [FromBody] ManagerRemoveRequest? request, CancellationToken ct)
    {
        var managerId = GetCurrentUserId()!.Value;
        await _managerRemove.HandleAsync(new ManagerRemoveCommand(id, managerId, request), ct);
        return Ok(new MessageResponse("Tutorial has been removed."));
    }

    // ── Admin tutorial management ───────────────────────────────────────────

    /// <summary>GET /api/tutorials/admin/all — Every main tutorial (any author, any status), for the admin management page.</summary>
    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetAdminTutorials(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int? categoryId,
        [FromQuery] bool? isOfficial,
        [FromQuery] string? difficulty,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _getAdminTutorials.HandleAsync(
            new GetAdminTutorialsQuery(search, status, categoryId, isOfficial, difficulty, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>GET /api/tutorials/{id}/admin — Any tutorial's detail (any author, any status), to pre-fill the admin edit form.</summary>
    [HttpGet("{id:guid}/admin")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetTutorialForAdmin(Guid id, CancellationToken ct)
    {
        var result = await _getTutorialForAdmin.HandleAsync(new GetTutorialForAdminQuery(id), ct);
        return Ok(result);
    }

    /// <summary>PUT /api/tutorials/{id}/admin — Admin/Manager edits any main tutorial's content directly, in place, regardless of author or review status.</summary>
    [HttpPut("{id:guid}/admin")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> AdminUpdateTutorial(
        Guid id, [FromBody] UpdateTutorialRequest request, CancellationToken ct)
    {
        var actorId = GetCurrentUserId()!.Value;
        var actorRole = User.IsInRole("Admin") ? UserRoleType.Admin : UserRoleType.Manager;
        var result = await _adminUpdateTutorial.HandleAsync(
            new AdminUpdateTutorialCommand(id, actorId, actorRole, request), ct);
        return Ok(result);
    }

    /// <summary>
    /// PUT /api/admin/tutorials/{tutorialId}/official — FT-32: mark/unmark a tutorial as official curated content.
    /// Route uses "~/" to publish under /api/admin instead of this controller's /api/tutorials prefix, so it can
    /// carry its own [Authorize(Roles = "Admin,Manager")] without inheriting AdminController's Admin-only class
    /// restriction (which would otherwise AND together and lock Manager out).
    /// </summary>
    [HttpPut("~/api/admin/tutorials/{tutorialId:guid}/official")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> SetOfficialTutorial(
        Guid tutorialId, [FromBody] SetOfficialTutorialRequest request, CancellationToken ct)
    {
        var actorId = GetCurrentUserId()!.Value;
        await _setOfficialTutorial.HandleAsync(
            new SetOfficialTutorialCommand(actorId, tutorialId, request.IsOfficial), ct);
        return Ok(new MessageResponse("Tutorial official status updated."));
    }

    // ── Variants (FT-11) ─────────────────────────────────────────────────────

    /// <summary>POST /api/tutorials/{parentId}/variants — Author links another of their own tutorials as a variant.</summary>
    [HttpPost("{parentId:guid}/variants")]
    [Authorize]
    public async Task<IActionResult> AddVariant(
        Guid parentId, [FromBody] AddVariantRequest request, CancellationToken ct)
    {
        var requesterId = GetCurrentUserId()!.Value;
        await _addVariant.HandleAsync(
            new AddVariantCommand(requesterId, parentId, request.VariantTutorialId, request.DifficultyDelta), ct);
        return Ok(new MessageResponse("Variant linked."));
    }

    /// <summary>DELETE /api/tutorials/{parentId}/variants/{variantId} — Author unlinks a variant.</summary>
    [HttpDelete("{parentId:guid}/variants/{variantId:guid}")]
    [Authorize]
    public async Task<IActionResult> RemoveVariant(Guid parentId, Guid variantId, CancellationToken ct)
    {
        var requesterId = GetCurrentUserId()!.Value;
        await _removeVariant.HandleAsync(new RemoveVariantCommand(requesterId, parentId, variantId), ct);
        return Ok(new MessageResponse("Variant unlinked."));
    }

    /// <summary>GET /api/tutorials/{parentId}/variants — List variants linked to a tutorial.</summary>
    [HttpGet("{parentId:guid}/variants")]
    [AllowAnonymous]
    public async Task<IActionResult> GetVariants(Guid parentId, CancellationToken ct)
    {
        var result = await _getVariants.HandleAsync(new GetVariantsQuery(parentId), ct);
        return Ok(result);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return value is null ? null : Guid.Parse(value);
    }
}
