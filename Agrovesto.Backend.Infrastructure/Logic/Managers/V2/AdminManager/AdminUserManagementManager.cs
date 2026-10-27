using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Admin;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.ExternalServices.NotificationService;
using Agrovesto.Backend.Infrastructure.Logic.Services.RolePermissionService;
using Amazon.Runtime.Internal.Util;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2.AdminManager
{
    public interface IAdminUserManagementManager
    {
        Task<(byte[], string, string)> ExportAsync(AdminUserListQuery q, string format, CancellationToken ct);
        Task<UserManagementSummaryDto> GetSummaryAsync(CancellationToken ct);
        Task<Core.Models.Admin.PagedResult<AdminUserListItemDto>> GetUsersAsync(AdminUserListQuery q, CancellationToken ct);
        Task<IReadOnlyList<AdminRoleOptionDto>> GetAssignableRolesAsync(CancellationToken ct);
        Task<InviteResult> InviteAsync(InviteAdminUserRequest req, string invitedByUserId, CancellationToken ct);
        Task<InviteResult> ResendAsync(string userId, string invitedByUserId, CancellationToken ct);
        Task<AcceptResult> AcceptAsync(AcceptInviteRequest req, CancellationToken ct);   // new
        Task<AccessResult<AdminUserStatusDto>> DeactivateUserAsync(string userId, string actingUserId, CancellationToken ct);
        Task<AccessResult<AdminUserDetailDto>> GetUserDetailAsync(string adminId, string actingUserId, CancellationToken ct);
        Task<AccessResult<AdminUserAccessDto>> GetUserAccessAsync(string adminId, string actingUserId, CancellationToken ct);
        Task<AccessResult<Core.Models.Admin.PagedResult<AdminActivityDto>>> GetUserActivityAsync(string adminId, string actingUserId, int page, int pageSize, CancellationToken ct);
        Task<AccessResult<Core.Models.Admin.PagedResult<AdminLoginHistoryDto>>> GetUserLoginHistoryAsync(string adminId, string actingUserId, int page, int pageSize, CancellationToken ct);
        Task<AccessResult<AdminUserDetailDto>> UpdateUserAsync(string userId, UpdateAdminUserRequest req, string actingUserId, CancellationToken ct);
        Task<AccessResult<AdminUserStatusDto>> ReactivateUserAsync(string adminId, string actingUserId, CancellationToken ct);
    }
    public class AdminUserManagementManager : IAdminUserManagementManager
    {
        private readonly IPermissionCacheInvalidator _cache;
        private readonly IConfiguration _configuration;
        private readonly DataContext _db;
        private readonly UserManager<User> _users;
        private readonly IAWSService _email;              // your existing email abstraction
        private readonly AdminPanelOptions _opts;
        private readonly ILogger<AdminUserManagementManager> _log;
        private readonly IAdminAuditLogger _audit;
        public AdminUserManagementManager(IConfiguration configuration,
            IPermissionCacheInvalidator cache,
            DataContext db, UserManager<User> users,
            IAWSService email, IOptions<AdminPanelOptions> opts,
            ILogger<AdminUserManagementManager> log, IAdminAuditLogger audit)
        {
            _configuration = configuration;
            _db = db;
            _users = users;
            _email = email;
            _audit = audit;
            _opts = opts.Value;
            _log = log;
            _cache = cache;
        }

        public async Task<UserManagementSummaryDto> GetSummaryAsync(CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;
            var admins = _db.Users.AsNoTracking().Where(u => u.IsAdminUser);

            var counts = await admins
                .GroupBy(_ => 1)
             .Select(g => new
             {
                 Total = g.Count(),   // "registered" = accepted
                 Active = g.Count(u => u.Status == AdminUserStatus.Active),
                 Suspended = g.Count(u => u.Status == AdminUserStatus.Suspended),
                 Pending = g.Count(u => u.Status == AdminUserStatus.Pending)
             })
                .FirstOrDefaultAsync(ct);

            //var pending = await _db.AdminInvitations.AsNoTracking()
            //    .CountAsync(i => i.AcceptedAt == null && i.RevokedAt == null && i.ExpiresAt > now, ct);

            return new(counts?.Total ?? 0, counts?.Active ?? 0,
                counts?.Suspended ?? 0, counts?.Pending ?? 0);
        }

        // Shared by list and export so both always return the same data
        private IQueryable<User> BuildQuery(AdminUserListQuery q)
        {
            var query = _db.Users.AsNoTracking().Where(u => u.IsAdminUser);

            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                var s = q.Search.Trim().ToLower();
                query = query.Where(u =>
                    (u.FirstName + " " + u.LastName).ToLower().Contains(s) ||
                    (u.FirstName + " " + u.MiddleName + " " + u.LastName).ToLower().Contains(s) ||
                    u.Email!.ToLower().Contains(s) ||
                    u.AdminCode.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(q.Status) &&
                Enum.TryParse<AdminUserStatus>(q.Status, true, out var status))
                query = query.Where(u => u.Status == status);

            if (!string.IsNullOrWhiteSpace(q.Department))
                query = query.Where(u => u.Department == q.Department);

            if (!string.IsNullOrWhiteSpace(q.Role))
                query = query.Where(u => _db.UserRoles.Any(ur =>
                    ur.UserId == u.Id && ur.IsActive &&
                    _db.Roles.Any(r => r.Id == ur.RoleId && r.Name == q.Role)));

            return query;
        }

        public async Task<Core.Models.Admin.PagedResult<AdminUserListItemDto>> GetUsersAsync(AdminUserListQuery q, CancellationToken ct)
        {
            var page = Math.Max(1, q.pageNumber);
            var size = Math.Clamp(q.PageSize, 1, 100);
            var query = BuildQuery(q);

            var total = await query.CountAsync(ct);

            var rows = await query
                .OrderByDescending(u => u.CreatedOn).ThenBy(u => u.Id)
                .Skip((page - 1) * size).Take(size)
                .Select(u => new Row(u.Id, u.AdminCode, u.FirstName, u.MiddleName, u.LastName,
                                     u.Email!, u.Department, u.Status.Value, u.LastLoginDate, u.CreatedOn))
                .ToListAsync(ct);

            var roles = await LoadRolesAsync(rows.Select(r => r.Id).ToList(), ct);

            var items = rows.Select((r, i) => ToDto((page - 1) * size + i + 1, r, roles)).ToList();
            return new(items, page, size, total);
        }

        private async Task<Dictionary<string, List<string>>> LoadRolesAsync(List<string> userIds, CancellationToken ct)
        {
            var pairs = await (from ur in _db.UserRoles.AsNoTracking()
                               join r in _db.Roles on ur.RoleId equals r.Id
                               where userIds.Contains(ur.UserId) && ur.IsActive
                               select new { ur.UserId, r.Name }).Distinct().ToListAsync(ct);

            return pairs.GroupBy(p => p.UserId).ToDictionary(g => g.Key, g => g.Select(x => x.Name!).ToList());
        }

        private static AdminUserListItemDto ToDto(int sn, Row r, Dictionary<string, List<string>> roles) => new(
            sn, r.AdminCode, r.Id,
            string.Join(" ", new[] { r.FirstName, r.MiddleName, r.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))),
            r.Email, r.Department,
            roles.TryGetValue(r.Id, out var rl) ? rl : new List<string>(),
            r.Status.ToString(), r.LastLoginAt, r.CreatedAt);

        private record Row(string Id, string AdminCode, string FirstName, string? MiddleName, string LastName,
                           string Email, string? Department, AdminUserStatus Status,
                           DateTimeOffset? LastLoginAt, DateTimeOffset CreatedAt);


        public async Task<(byte[], string, string)> ExportAsync(AdminUserListQuery q, string format, CancellationToken ct)
        {
            var rows = await BuildQuery(q).OrderByDescending(u => u.CreatedOn)
                .Select(u => new Row(u.Id, u.AdminCode, u.FirstName, u.MiddleName, u.LastName,
                                     u.Email!, u.Department, u.Status.Value, u.LastLoginDate, u.CreatedOn))
                .ToListAsync(ct);

            var roles = await LoadRolesAsync(rows.Select(r => r.Id).ToList(), ct);
            var data = rows.Select((r, i) => ToDto(i + 1, r, roles)).ToList();
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmm");

            return format.ToLowerInvariant() switch
            {
                "csv" => (BuildCsv(data), "text/csv", $"admin-users-{stamp}.csv"),
                "xlsx" => (BuildXlsx(data), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"admin-users-{stamp}.xlsx"),
                _ => throw new ArgumentException("Format must be csv or xlsx.")
            };
        }

        private static readonly string[] Headers =
            { "S/n", "ID", "Full Name", "Email", "Department", "Role(s)", "Status", "Last Login", "Reg Date" };

        private static byte[] BuildCsv(List<AdminUserListItemDto> data)
        {
            static string Esc(string? v)
            {
                v ??= "";
                // Neutralise CSV/Excel formula injection from user-controlled fields
                if (v.Length > 0 && "=+-@\t\r".Contains(v[0])) v = "'" + v;
                return "\"" + v.Replace("\"", "\"\"") + "\"";
            }

            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", Headers.Select(Esc)));
            foreach (var d in data)
                sb.AppendLine(string.Join(",", new[]
                {
            d.SerialNo.ToString(), d.Id, d.FullName, d.Email, d.Department,
            string.Join("; ", d.Roles), d.Status,
            d.LastLogin?.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"), d.RegDate.ToString("yyyy-MM-dd")
        }.Select(Esc)));

            return new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray(); // BOM so Excel reads UTF-8
        }

        private static byte[] BuildXlsx(List<AdminUserListItemDto> data)
        {
            using var wb = new ClosedXML.Excel.XLWorkbook();
            var ws = wb.Worksheets.Add("Admin Users");
            for (int c = 0; c < Headers.Length; c++) ws.Cell(1, c + 1).Value = Headers[c];
            ws.Row(1).Style.Font.Bold = true;

            for (int i = 0; i < data.Count; i++)
            {
                var d = data[i]; var r = i + 2;
                ws.Cell(r, 1).Value = d.SerialNo;
                ws.Cell(r, 2).Value = d.Id;
                ws.Cell(r, 3).Value = d.FullName;
                ws.Cell(r, 4).Value = d.Email;
                ws.Cell(r, 5).Value = d.Department ?? "";
                ws.Cell(r, 6).Value = string.Join("; ", d.Roles);
                ws.Cell(r, 7).Value = d.Status;
                ws.Cell(r, 8).Value = d.LastLogin?.UtcDateTime.ToString("yyyy-MM-dd HH:mm") ?? "";
                ws.Cell(r, 9).Value = d.RegDate.UtcDateTime.ToString("yyyy-MM-dd");
            }
            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }


        public async Task<IReadOnlyList<AdminRoleOptionDto>> GetAssignableRolesAsync(CancellationToken ct)
        => await _db.Roles.AsNoTracking()
            .Where(r => r.IsAvailableToAdmin && (r.BusinessId == null || r.BusinessId == _opts.BusinessId))
            .OrderBy(r => r.Name)
            .Select(r => new AdminRoleOptionDto(r.Id, r.Name!, r.Descriptions))
            .ToListAsync(ct);


        public async Task<InviteResult> InviteAsync(InviteAdminUserRequest req, string invitedByUserId, CancellationToken ct)
        {
            var email = req.Email.Trim().ToLowerInvariant();
            var phone = NormalizePhone(req.PhoneNumber);
            if (phone is null)
                return Fail(400, "Please enter a valid phone number.");

            var role = await _db.Roles.AsNoTracking().FirstOrDefaultAsync(r =>
                r.Id == req.RoleId && r.IsAvailableToAdmin &&
                (r.BusinessId == null || r.BusinessId == _opts.BusinessId), ct);
            if (role is null) return Fail(400, "Please select a role.");

            var existing = await _users.FindByEmailAsync(email);
            if (existing is not null)
                return existing.IsAdminUser && existing.Status == AdminUserStatus.Pending
                    ? Fail(409, "This email already has a pending invitation. Use Resend invitation instead.")
                    : Fail(409, "A user with this email already exists.");

            if (await _db.Users.AnyAsync(u => u.PhoneNumber == phone, ct))
                return Fail(409, "A user with this phone number already exists.");

            var rawToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
            var now = DateTimeOffset.UtcNow;
            var expires = now.AddDays(_opts.InviteExpiryDays);

            User? user = null;
            InviteResult? failure = null;

            var strategy = _db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                // This block may run more than once if a transient error occurs,
                // so reset all state and drop entities tracked by a failed attempt.
                _db.ChangeTracker.Clear();
                user = null;
                failure = null;

                await using var tx = await _db.Database.BeginTransactionAsync(ct);

                var seq = new AdminCodeSequence();
                _db.Add(seq);
                await _db.SaveChangesAsync(ct);

                var first = req.FirstName.Trim();
                var last = req.LastName.Trim();

                user = new User
                {
                    UserName = email,
                    Email = email,
                    PhoneNumber = phone,
                    FirstName = first,
                    LastName = last,
                    Name = $"{first} {last}",
                    IsAdminUser = true,
                    AdminCode = $"ADM{seq.Id:D6}",
                    Status = AdminUserStatus.Pending,
                    IsActive = false,                     // cannot log in until the invite is accepted
                    UserType = "Agrovesto",
                    SignupMethod = SignupMethod.Email,
                    EmailConfirmed = false,
                    IsEmailVerified = false,
                    CreatedOn = now,
                    CreatedBy = invitedByUserId,
                    LastLoginDate = null,                 // never logged in
                    DateModified = now
                };

                // No password yet; it is set when the invite is accepted
                var created = await _users.CreateAsync(user);
                if (!created.Succeeded)
                {
                    failure = Fail(400, string.Join(" ", created.Errors.Select(e => e.Description)));
                    return;                               // tx is disposed without commit, so it rolls back
                }

                _db.UserBusinesses.Add(new UserBusiness
                {
                    UserId = user.Id,
                    BusinessId = _opts.BusinessId,
                    IsActive = true,
                    IsDefault = true
                });
                _db.UserRoles.Add(new ApplicationUserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id,
                    BusinessId = _opts.BusinessId,
                    IsActive = true
                });
                _db.AdminInvitations.Add(new AdminInvitation
                {
                    UserId = user.Id,
                    TokenHash = Hash(rawToken),
                    SentAt = now,
                    ExpiresAt = expires,
                    InvitedByUserId = invitedByUserId
                });

                try { await _db.SaveChangesAsync(ct); }
                catch (DbUpdateException ex)              // race on the unique email/phone/code index
                {
                    _log.LogWarning(ex, "Invite conflict for {Email}", email);
                    failure = Fail(409, "A user with these details already exists.");
                    return;
                }

                await tx.CommitAsync(ct);
            });

            if (failure is not null) return failure;
            await _audit.LogAsync(
                "UserInvited",
                invitedByUserId,
                user!.Id,
                $"Invited user to login as Role: {role.Name}",
                AdminActiviLogModule.AdminPanel.UserMgt,
                 $"{email}"

                );   // here
            // Send after commit so a mail outage never leaves a half-created user
            var sent = await TrySendInviteAsync(user!, rawToken, expires, role.Name);

            return new(new InviteAdminUserResponse(user!.Id, user.AdminCode!, "Pending", sent, expires), null, 201);
        }
        public async Task<InviteResult> ResendAsync(string adminId, string invitedByUserId, CancellationToken ct)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u =>
                u.AdminCode == adminId && u.IsAdminUser && u.Status == AdminUserStatus.Pending, ct);
            if (user is null) return Fail(404, "No pending invitation found for this user.");

            var invite = await _db.AdminInvitations
                .Where(i => i.UserId == user.Id && i.AcceptedAt == null && i.RevokedAt == null)
                .OrderByDescending(i => i.SentAt).FirstOrDefaultAsync(ct);
            if (invite is null) return Fail(404, "No pending invitation found for this user.");

            var rawToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
            invite.TokenHash = Hash(rawToken);               // old link stops working
            invite.SentAt = DateTimeOffset.UtcNow;
            invite.ExpiresAt = invite.SentAt.AddDays(_opts.InviteExpiryDays);
            invite.InvitedByUserId = invitedByUserId;
            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync("InviteResent", invitedByUserId, user.Id, "Resent login invite",AdminActiviLogModule.AdminPanel.UserMgt, $"{user.Email}", ct);                          // here


            var sent = await TrySendInviteAsync(user, rawToken, invite.ExpiresAt, null);
            return new(new InviteAdminUserResponse(user.Id, user.AdminCode!, "Pending", sent, invite.ExpiresAt), null, 200);
        }

        private async Task<bool> TrySendInviteAsync(User user, string rawToken, DateTimeOffset expires, string role)
        {
            try
            {
                var senderEmail = _configuration["Official:Email"];
                var link = $"{_opts.PortalBaseUrl.TrimEnd('/')}/accept-invite?token={Uri.EscapeDataString(rawToken)}&name={Uri.EscapeDataString(user.FirstName)} {Uri.EscapeDataString(user.LastName)}&email={Uri.EscapeDataString(user.Email)}&role={Uri.EscapeDataString(role)}";
                var name = WebUtility.HtmlEncode(user.FirstName);
                var body = $@"<p>Hello {name},</p>
<p>You have been invited to join the NomaOS Admin Panel.</p>
<p><a href=""{link}"">Accept invitation and set your password</a></p>
<p>This link expires on {expires:dd MMM yyyy} and can only be used once.</p>";
                await _email.SendEmailAsync(
                    senderEmail!,
                    user.Email!,
                    "You've been invited to the NomaOS Admin Panel",
                    body,
                    body);
                return true;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to send admin invitation to user {UserId}", user.Id);  // never log the token
                return false;
            }
        }

        private static string Hash(string raw) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

        private static string? NormalizePhone(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            // Keep digits only (drops +, spaces, dashes, brackets)
            var digits = new string(input.Where(char.IsDigit).ToArray());

            if (digits.StartsWith("234")) digits = digits[3..];     // +234 or 234 prefix
            if (digits.Length == 10) digits = "0" + digits;         // 8012345678 -> 08012345678

            // Nigerian mobile format: 11 digits, starts with 0, then 7/8/9, then 0/1
            return Regex.IsMatch(digits, @"^0[789][01]\d{8}$") ? digits : null;
        }

        private static InviteResult Fail(int code, string msg) => new(null, msg, code);

        public async Task<AcceptResult> AcceptAsync(AcceptInviteRequest req, CancellationToken ct)
        {
            const string invalid = "This invitation link is invalid or has expired.";
            var hash = Hash(req.Token);

            var strategy = _db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync<AcceptResult>(async () =>
            {
                _db.ChangeTracker.Clear();
                var now = DateTimeOffset.UtcNow;

                await using var tx = await _db.Database.BeginTransactionAsync(ct);

                var invite = await _db.AdminInvitations.FirstOrDefaultAsync(i =>
                    i.TokenHash == hash && i.AcceptedAt == null && i.RevokedAt == null && i.ExpiresAt > now, ct);
                if (invite is null) return new AcceptResult(false, invalid, 400);

                var user = await _users.FindByIdAsync(invite.UserId);
                if (user is null || !user.IsAdminUser || user.Status != AdminUserStatus.Pending)
                    return new AcceptResult(false, invalid, 400);

                var pw = await _users.AddPasswordAsync(user, req.Password);
                if (!pw.Succeeded)
                    return new AcceptResult(false, string.Join(" ", pw.Errors.Select(e => e.Description)), 400);

                user.Status = AdminUserStatus.Active;
                user.IsActive = true;
                user.EmailConfirmed = true;
                user.IsEmailVerified = true;
                user.DateModified = now;
                user.ModifiedBy = user.Id;
                invite.AcceptedAt = now;

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                await _audit.LogAsync("InviteAccepted", user.Id, user.Id, "Accepted login invite", AdminActiviLogModule.AdminPanel.UserMgt, $"{user.Email}", ct);                                // here

                return new AcceptResult(true, null, 200);
            });
        }

        public async Task<AccessResult<AdminUserStatusDto>> DeactivateUserAsync(string adminId, string actingUserId, CancellationToken ct)
        {



            var user = await _db.Users.FirstOrDefaultAsync(u => u.AdminCode == adminId && u.IsAdminUser, ct);
            if (user is null)
                return AccessResult.Fail<AdminUserStatusDto>(404, "User not found.");


            if (user.Id == actingUserId) // change actinguserid to activeuser admincode
                return AccessResult.Fail<AdminUserStatusDto>(400, "You cannot deactivate your own account.");

            if (user.Status == AdminUserStatus.Deactivated)
                return AccessResult.Fail<AdminUserStatusDto>(409, "This user is already deactivated.");

            if (user.Status == AdminUserStatus.Pending)
                return AccessResult.Fail<AdminUserStatusDto>(409,
                    "This user has not accepted their invitation yet. Revoke the invitation instead.");

            // Never leave the platform without an active Super Admin
            var isSuperAdmin = await _db.UserRoles.AnyAsync(ur =>
                ur.UserId == user.Id && ur.IsActive && ur.Role.Name == PermissionSeeder.SuperAdminRole, ct);

            if (isSuperAdmin)
            {
                var otherActiveSuperAdmins = await _db.UserRoles.CountAsync(ur =>
                    ur.UserId != user.Id && ur.IsActive &&
                    ur.Role.Name == PermissionSeeder.SuperAdminRole &&
                    ur.User.IsActive && ur.User.Status == AdminUserStatus.Active, ct);

                if (otherActiveSuperAdmins == 0)
                    return AccessResult.Fail<AdminUserStatusDto>(409,
                        "You cannot deactivate the last active Super Admin.");
            }

            user.Status = AdminUserStatus.Deactivated;
            user.IsActive = false;                          // keep in sync; the admin login checks this
            user.DateModified = DateTimeOffset.UtcNow;
            user.ModifiedBy = actingUserId;
            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync("UserDeactivated", actingUserId, user.Id, "Deactivated user account", AdminActiviLogModule.AdminPanel.UserMgt, $"{user.Email}", ct);                          // here


            await _users.UpdateSecurityStampAsync(user);    // invalidates anything keyed on the security stamp
            _cache.InvalidateAll();                         // permission checks stop working immediately

            // Kill refresh tokens so the user cannot mint new access tokens.
            // Best effort: IsActive is already false, so a failure here is not fatal.
            try
            {
                await _db.RefreshTokens.Where(t => t.UserId == user.Id).ExecuteDeleteAsync(ct);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to revoke refresh tokens for deactivated user {adminId}", adminId);
            }

            _log.LogInformation("Admin user {adminId} deactivated by {ActingUserId}", adminId, actingUserId);

            return AccessResult.Ok(new AdminUserStatusDto(user.Id, user.Status!.Value.ToString()));
        }

        public async Task<AccessResult<AdminUserStatusDto>> ReactivateUserAsync(
            string adminId, string actingUserId, CancellationToken ct)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.AdminCode == adminId && u.IsAdminUser, ct);
            if (user is null)
                return AccessResult.Fail<AdminUserStatusDto>(404, "User not found.");

            // Safeguard: an admin should never act on their own account.
            // (If actingUserId is an Id, keep comparing against user.Id;
            //  if you switch it to an AdminCode, compare against user.AdminCode.)
            if (user.Id == actingUserId)
                return AccessResult.Fail<AdminUserStatusDto>(400, "You cannot reactivate your own account.");

            if (user.Status == AdminUserStatus.Active)
                return AccessResult.Fail<AdminUserStatusDto>(409, "This user is already active.");

            if (user.Status == AdminUserStatus.Pending)
                return AccessResult.Fail<AdminUserStatusDto>(409,
                    "This user has not accepted their invitation yet. Revoke the invitation instead.");

            if (user.Status != AdminUserStatus.Deactivated)
                return AccessResult.Fail<AdminUserStatusDto>(409,
                    "Only deactivated users can be reactivated.");

            user.Status = AdminUserStatus.Active;
            user.IsActive = true;                        // keep in sync; the admin login checks this
            user.DateModified = DateTimeOffset.UtcNow;
            user.ModifiedBy = actingUserId;
            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync(
                "UserReactivated", actingUserId, user.Id,
                "Reactivated user account",
                AdminActiviLogModule.AdminPanel.UserMgt,
                $"{user.Email}", ct);

            await _users.UpdateSecurityStampAsync(user); // refresh stamp so stale state is not reused
            _cache.InvalidateAll();                      // permission checks pick up the change immediately

            _log.LogInformation("Admin user {AdminId} reactivated by {ActingUserId}", adminId, actingUserId);

            return AccessResult.Ok(new AdminUserStatusDto(user.Id, user.Status!.Value.ToString()));
        }
        public async Task<AccessResult<AdminUserDetailDto>> GetUserDetailAsync(string adminId, string actingUserId, CancellationToken ct)
        {
            var u = await _db.Users.AsNoTracking()
                .Where(x => x.AdminCode == adminId && x.IsAdminUser)
                .Select(x => new
                {
                    x.Id,
                    x.AdminCode,
                    x.FirstName,
                    x.MiddleName,
                    x.LastName,
                    x.Email,
                    x.PhoneNumber,
                    x.Department,
                    x.Status,
                    x.IsActive,
                    x.CreatedOn,
                    x.CreatedBy,
                    x.DateModified,
                    x.LastLoginDate
                })
                .FirstOrDefaultAsync(ct);

            if (u is null) return AccessResult.Fail<AdminUserDetailDto>(404, "User not found.");

            var roleRows = await (from ur in _db.UserRoles.AsNoTracking()
                                  join r in _db.Roles on ur.RoleId equals r.Id
                                  where ur.UserId == u.Id  && ur.IsActive
                                  select new { r.Id, Name = r.Name! }).Distinct().OrderBy(r => r.Name).ToListAsync(ct);
            var roles = roleRows.Select(r => r.Name).ToList();


            var invite = await _db.AdminInvitations.AsNoTracking()
                .Where(i => i.UserId == u.Id)
                .OrderByDescending(i => i.SentAt)
                .Select(i => new { i.InvitedByUserId, i.AcceptedAt })
                .FirstOrDefaultAsync(ct);

            // Resolve "created by" and "invited by" ids to names in one query
            var ids = new[] { u.CreatedBy, invite?.InvitedByUserId }
                .Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();

            var people = await _db.Users.AsNoTracking()
                .Where(x => ids.Contains(x.Id))
                .Select(x => new { x.Id, x.FirstName, x.MiddleName, x.LastName, x.Name })
                .ToListAsync(ct);

            string? NameOf(string? id)
            {
                var p = people.FirstOrDefault(x => x.Id == id);
                if (p is null) return null;
                var n = JoinName(p.FirstName, p.MiddleName, p.LastName);
                return string.IsNullOrWhiteSpace(n) ? p.Name : n;
            }

            // Legacy admin rows may predate the Status column
            var status = u.Status ?? (u.IsActive ? AdminUserStatus.Active : AdminUserStatus.Deactivated);

            await _audit.LogAsync("UserDetailViewed", actingUserId, u.Id, "Viewed user details", AdminActiviLogModule.AdminPanel.UserMgt, $"{u.Email}", ct);                          // here

            var dto = new AdminUserDetailDto(
                u.Id, u.AdminCode ?? "",
                JoinName(u.FirstName, u.MiddleName, u.LastName), u.FirstName ?? "", u.MiddleName, u.LastName ?? "",
                status.ToString(), u.Department, roles,
                u.Email ?? "", u.PhoneNumber,
                //FormatPhoneDisplay(u.PhoneNumber),
                u.CreatedOn, NameOf(u.CreatedBy) ?? "System", u.DateModified, u.LastLoginDate,
                invite is null ? "Direct" : "Invitation",
                NameOf(invite?.InvitedByUserId),
                invite?.AcceptedAt,
                ActionsFor(status, isSelf: u.Id == actingUserId),
                roleRows.FirstOrDefault()?.Id);

            return AccessResult.Ok(dto);
        }

        private static string JoinName(params string?[] parts) =>
            string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));

        // +234 803 456 7891 for display; storage stays 08034567891
        private static string? FormatPhoneDisplay(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return null;
            var d = new string(phone.Where(char.IsDigit).ToArray());
            if (d.StartsWith("234")) d = d[3..];
            if (d.StartsWith("0")) d = d[1..];
            return d.Length == 10 ? $"+234 {d[..3]} {d[3..6]} {d[6..]}" : phone;
        }

        private static List<string> ActionsFor(AdminUserStatus s, bool isSelf) => s switch
        {
            AdminUserStatus.Active => isSelf ? new() { "Edit" } : new() { "Edit", "Deactivate" },
            AdminUserStatus.Suspended => new() { "Edit", "Activate", "Deactivate" },
            AdminUserStatus.Pending => new() { "ResendInvite", "RevokeInvite" },
            _ => new() { "Activate" }
        };

        public async Task<AccessResult<AdminUserAccessDto>> GetUserAccessAsync(string adminId, string actingUserId, CancellationToken ct)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.AdminCode == adminId && u.IsAdminUser, ct);

            if (user is null)
                return AccessResult.Fail<AdminUserAccessDto>(404, "User not found.");

            var rows = await (from ur in _db.UserRoles.AsNoTracking()
                              join r in _db.Roles on ur.RoleId equals r.Id
                              where ur.UserId == user.Id && ur.IsActive
                              select new
                              {
                                  r.Id,
                                  r.Name,
                                  r.Descriptions,
                                  Perms = r.RolePermissions.Select(rp => new
                                  {
                                      rp.Permission.Name,
                                      rp.Permission.Group,
                                      rp.Permission.Description
                                  })
                              }).ToListAsync(ct);

            var roles = rows.GroupBy(r => r.Id).Select(g => g.First())      // same role can appear once per business
                .OrderBy(r => r.Name)
                .Select(r => new UserRoleAccessDto(r.Id, r.Name!, r.Descriptions,
                    r.Perms.OrderBy(p => p.Group).ThenBy(p => p.Name)
                           .Select(p => new UserPermissionDto(p.Name, p.Group, p.Description)).ToList()))
                .ToList();

            // Union across roles, grouped for display
            var effective = roles.SelectMany(r => r.Permissions)
                .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase).Select(g => g.First())
                .GroupBy(p => p.Group ?? "Other").OrderBy(g => g.Key)
                .Select(g => new PermissionGroupDto(g.Key, g.Select(p => p.Name).OrderBy(n => n).ToList()))
                .ToList();

            await _audit.LogAsync("UserAccessViewed", actingUserId, user.Id, "Viewed user access", AdminActiviLogModule.AdminPanel.UserMgt, $"{user.Email}", ct);                          // here

            return AccessResult.Ok(new AdminUserAccessDto(roles
               // , effective
                ));
        }

        public async Task<AccessResult<Core.Models.Admin.PagedResult<AdminActivityDto>>> GetUserActivityAsync(string adminId, string actingUserId, int page, int pageSize, CancellationToken ct)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.AdminCode == adminId && u.IsAdminUser, ct);
            if (user is null)
                return AccessResult.Fail<Core.Models.Admin.PagedResult<AdminActivityDto>>(404, "User not found.");

            page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _db.AdminActivityLogs.AsNoTracking()
                .Where(a => a.ActorUserId == user.Id || a.TargetUserId == user.Id);

            var total = await query.CountAsync(ct);
            var rows = await (from a in query.OrderByDescending(a => a.OccurredAt).Skip((page - 1) * pageSize).Take(pageSize)
                              join actor in _db.Users on a.ActorUserId equals actor.Id into g
                              from actor in g.DefaultIfEmpty()
                              select new
                              {
                                  a.Id,
                                  a.Action,
                                  a.Details,
                                  a.OccurredAt,
                                  a.IpAddress,
                                  actor.FirstName,
                                  actor.MiddleName,
                                  actor.LastName,
                                  a.AffectedRecord,
                                  a.Module,
                                  a.Browser,
                                  a.OperatingSystem,
                                  a.DeviceType,
                                  a.DeviceName
                              }).ToListAsync(ct);

            var items = rows.Select(r => new AdminActivityDto(r.Id, r.Action,r.Module, r.Details,
                string.IsNullOrWhiteSpace(JoinName(r.FirstName, r.MiddleName, r.LastName)) ? "System" : JoinName(r.FirstName, r.MiddleName, r.LastName),
                r.OccurredAt, r.AffectedRecord, r.IpAddress, r.Browser, r.OperatingSystem, r.DeviceType, r.DeviceName)).ToList();

            await _audit.LogAsync("UserActivityViewed", actingUserId, user.Id, "Viewed user activity log", AdminActiviLogModule.AdminPanel.UserMgt, $"{user.Email}", ct);                          // here

            return AccessResult.Ok(new Core.Models.Admin.PagedResult<AdminActivityDto>(items, page, pageSize, total));
        }

        public async Task<AccessResult<Core.Models.Admin.PagedResult<AdminLoginHistoryDto>>> GetUserLoginHistoryAsync(string adminId, string actingUserId, int page, int pageSize, CancellationToken ct)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.AdminCode == adminId && u.IsAdminUser, ct);
            if (user is null)
                return AccessResult.Fail<Core.Models.Admin.PagedResult<AdminLoginHistoryDto>>(404, "User not found.");

            page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _db.AdminLoginHistories.AsNoTracking().Where(l => l.UserId == user.Id);
            var total = await query.CountAsync(ct);

            var items = await query.OrderByDescending(l => l.LoggedInAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(l => new AdminLoginHistoryDto(l.Id, l.LoggedInAt, l.Succeeded, l.FailureReason, l.IpAddress, l.Browser, l.OperatingSystem, l.DeviceType, l.DeviceName))
                .ToListAsync(ct);

            await _audit.LogAsync("UserLoginHistoryViewed", actingUserId, user.Id, "Viewed user login history", AdminActiviLogModule.AdminPanel.UserMgt, $"{user.Email}", ct);                          // here

            return AccessResult.Ok(new Core.Models.Admin.PagedResult<AdminLoginHistoryDto>(items, page, pageSize, total));
        }

        public async Task<AccessResult<AdminUserDetailDto>> UpdateUserAsync(
    string adminId,
    UpdateAdminUserRequest req, string actingUserId, CancellationToken ct)
        {
            var phone = NormalizePhone(req.PhoneNumber);
            if (phone is null)
                return AccessResult.Fail<AdminUserDetailDto>(400, "Please enter a valid phone number.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.AdminCode == adminId && u.IsAdminUser, ct);
            if (user is null)
                return AccessResult.Fail<AdminUserDetailDto>(404, "User not found.");

            var status = user.Status ?? (user.IsActive ? AdminUserStatus.Active : AdminUserStatus.Deactivated);
            if (status is AdminUserStatus.Pending or AdminUserStatus.Deactivated)
                return AccessResult.Fail<AdminUserDetailDto>(409,
                    status == AdminUserStatus.Pending
                        ? "This user has not accepted their invitation yet and cannot be edited."
                        : "This user is deactivated. Activate the account before editing it.");

            var role = await _db.Roles.AsNoTracking().FirstOrDefaultAsync(r =>
                r.Id == req.RoleId && r.IsAvailableToAdmin &&
                (r.BusinessId == null || r.BusinessId == _opts.BusinessId), ct);
            if (role is null)
                return AccessResult.Fail<AdminUserDetailDto>(400, "Please select a role.");

            if (await _db.Users.AnyAsync(u => u.Id != user.Id && u.PhoneNumber == phone, ct))
                return AccessResult.Fail<AdminUserDetailDto>(409, "A user with this phone number already exists.");

            // Current admin-panel role assignments
            var assignments = await _db.UserRoles.Include(ur => ur.Role)
                .Where(ur => ur.UserId == user.Id && ur.BusinessId == _opts.BusinessId)
                .ToListAsync(ct);
            var activeAssignments = assignments.Where(a => a.IsActive).ToList();

            var roleChanged = !(activeAssignments.Count == 1 && activeAssignments[0].RoleId == role.Id);

            if (roleChanged)
            {
                if (user.Id == actingUserId)
                    return AccessResult.Fail<AdminUserDetailDto>(400, "You cannot change your own role.");

                var isSuperAdminNow = activeAssignments.Any(a =>
                    string.Equals(a.Role.Name, PermissionSeeder.SuperAdminRole, StringComparison.OrdinalIgnoreCase));
                var staysSuperAdmin = string.Equals(role.Name, PermissionSeeder.SuperAdminRole, StringComparison.OrdinalIgnoreCase);

                if (isSuperAdminNow && !staysSuperAdmin)
                {
                    var otherSuperAdmins = await _db.UserRoles.CountAsync(ur =>
                        ur.UserId != user.Id && ur.IsActive &&
                        ur.Role.Name == PermissionSeeder.SuperAdminRole &&
                        ur.User.IsActive && ur.User.Status == AdminUserStatus.Active, ct);

                    if (otherSuperAdmins == 0)
                        return AccessResult.Fail<AdminUserDetailDto>(409,
                            "You cannot change the role of the last active Super Admin.");
                }
            }

            // ---- Apply profile changes and record which fields changed ----
            var first = req.FirstName.Trim();
            var middle = string.IsNullOrWhiteSpace(req.MiddleName) ? null : req.MiddleName.Trim();
            var last = req.LastName.Trim();
            var department = string.IsNullOrWhiteSpace(req.Department) ? null : req.Department.Trim();

            var changes = new List<string>();
            void Track(string field, string? oldValue, string? newValue)
            {
                if (!string.Equals(oldValue ?? "", newValue ?? "", StringComparison.Ordinal)) changes.Add(field);
            }

            Track("FirstName", user.FirstName, first);
            Track("MiddleName", user.MiddleName, middle);
            Track("LastName", user.LastName, last);
            Track("PhoneNumber", user.PhoneNumber, phone);
            Track("Department", user.Department, department);

            user.FirstName = first;
            user.MiddleName = middle;
            user.LastName = last;
            user.Name = JoinName(first, middle, last);
            user.PhoneNumber = phone;
            user.Department = department;

            // ---- Apply role change: deactivate old rows, reactivate or add the new one ----
            string? roleChange = null;
            if (roleChanged)
            {
                var oldNames = string.Join(", ", activeAssignments.Select(a => a.Role.Name));
                roleChange = $"Role: {(string.IsNullOrEmpty(oldNames) ? "none" : oldNames)} -> {role.Name}";

                foreach (var a in activeAssignments.Where(a => a.RoleId != role.Id))
                    a.IsActive = false;                                   // keep history instead of deleting

                var existing = assignments.FirstOrDefault(a => a.RoleId == role.Id);
                if (existing is not null) existing.IsActive = true;
                else _db.UserRoles.Add(new ApplicationUserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id,
                    BusinessId = _opts.BusinessId,
                    IsActive = true
                });
            }

            if (changes.Count > 0 || roleChanged)
            {
                user.DateModified = DateTimeOffset.UtcNow;
                user.ModifiedBy = actingUserId;

                try { await _db.SaveChangesAsync(ct); }                  // one save, so profile and role change together
                catch (DbUpdateException ex)
                {
                    _log.LogWarning(ex, "Update conflict for admin user {UserId}", user.Id);
                    return AccessResult.Fail<AdminUserDetailDto>(409, "These details conflict with another user.");
                }

                if (roleChanged) _cache.InvalidateAll();                 // permissions are role-based, so refresh the cache

                var details = string.Join("; ", new[]
                {
            changes.Count > 0 ? $"Fields: {string.Join(", ", changes)}" : null,
            roleChange
        }.Where(x => x is not null));
                await _audit.LogAsync("UserUpdated", actingUserId, user.Id, details, AdminActiviLogModule.AdminPanel.UserMgt, $"{user.Email}", ct);                          // here
            }

            return await GetUserDetailAsync(adminId, actingUserId, ct);   // refreshed page data
        }
    }


}
