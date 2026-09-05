using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.DTOs.AdminConfiguration;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Queries.AdminConfiguration;

public class GetUsersHandler
{
    private readonly IUserRepository _userRepo;

    public GetUsersHandler(IUserRepository userRepo) => _userRepo = userRepo;

    public async Task<PagedResult<AdminUserResponse>> HandleAsync(GetUsersQuery query, CancellationToken ct = default)
    {
        AccountStatus? accountStatus = query.Status is not null && Enum.TryParse<AccountStatus>(query.Status, true, out var s)
            ? s : null;

        UserRoleType? roleType = query.Role is not null && Enum.TryParse<UserRoleType>(query.Role, true, out var r)
            ? r : null;

        var paged = await _userRepo.SearchAsync(query.Keyword, accountStatus, roleType, query.Page, query.PageSize, ct);

        var items = paged.Items.Select(u => new AdminUserResponse(
            u.Id,
            u.Email,
            u.Profile?.DisplayName,
            u.Profile?.AvatarUrl,
            u.Status.ToString(),
            u.Roles.Select(r => r.Role.ToString()).ToList(),
            u.CreatedAt)).ToList();

        return new PagedResult<AdminUserResponse>(items, paged.TotalCount, paged.Page, paged.PageSize, paged.TotalPages);
    }
}
