using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Admin
{
    public record PermissionDto(int Id, string Name, string? Group, string? Description, bool IsSystem);
    public record RoleListItemDto(string Id, string Name, string? Description, int PermissionCount, int UserCount, bool IsProtected);
    public record RoleDetailDto(string Id, string Name, string? Description, bool IsProtected, IReadOnlyList<PermissionDto> Permissions);

    public class SaveRoleRequest
    {
        [Required(ErrorMessage = "Role name is required."), StringLength(100)]
        public string Name { get; set; } = default!;
        [StringLength(500)]
        public string? Description { get; set; }
    }

    public class CreateRoleRequest : SaveRoleRequest { public List<int>? PermissionIds { get; set; } }

    public class SetRolePermissionsRequest
    {
        [Required(ErrorMessage = "Permission list is required.")]
        public List<int> PermissionIds { get; set; } = new();
    }

    public class CreatePermissionRequest
    {
        [Required(ErrorMessage = "Permission name is required."), StringLength(100)]
        public string Name { get; set; } = default!;          // Resource.Action, e.g. Reports.View
        [StringLength(100)] public string? Group { get; set; }
        [StringLength(300)] public string? Description { get; set; }
    }

    public class UpdatePermissionRequest
    {
        [StringLength(100)] public string? Group { get; set; }
        [StringLength(300)] public string? Description { get; set; }
    }

    public record AccessResult<T>(T? Data, string? Error, int StatusCode) { public bool Succeeded => Error is null; }
    public static class AccessResult
    {
        public static AccessResult<T> Ok<T>(T data) => new(data, null, 200);
        public static AccessResult<T> Fail<T>(int status, string error) => new(default, error, status);
    }
}
