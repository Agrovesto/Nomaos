using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Admin
{
   

    public record UserManagementSummaryDto(int TotalUsers, int ActiveUsers, int SuspendedUsers, int PendingInvitations);

    public record AdminUserListItemDto(
        int SerialNo, string Id, string UserId, string FullName, string Email,
        string? Department, IReadOnlyList<string> Roles, string Status,
        DateTimeOffset? LastLogin, DateTimeOffset RegDate);

    public record AdminUserListQuery(
        string? Search, string? Status, string? Role, string? Department,
        int pageNumber = 1, int PageSize = 20);

    public record PagedResult<T>(IReadOnlyList<T> Items, int pageNumber, int PageSize, int TotalCount);
}
