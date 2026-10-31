using Agrovesto.Backend.Core.Enums;
using Agrovesto.Backend.Core.Models.Admin;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static Agrovesto.Backend.Infrastructure.Entities.Common.Permissions;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2.AdminManager
{
    public interface IAdminSettingsManager
    {
        Task<AccessResult<object>> ChangePasswordAsync(string userId, ChangePasswordRequest req, CancellationToken ct);

        Task<AccessResult<AdminPersonalInfoDto>> GetPersonalInfoAsync(string userId, CancellationToken ct);
        Task<AccessResult<AdminPersonalInfoDto>> UpdatePersonalInfoAsync(string userId, UpdatePersonalInfoRequest req, CancellationToken ct);

        Task<AccessResult<AdminProfileDto>> GetProfileAsync(string userId, CancellationToken ct);
        Task<AccessResult<ProfileImageDto>> UpdateProfileImageAsync(string userId, Stream file, long length, CancellationToken ct);
    }
    public class AdminSettingsManager : IAdminSettingsManager
    {
        private readonly IProfileImageStore _images;

        private readonly DataContext _context;
        private readonly UserManager<User> _users;
        private readonly IAdminAuditLogger _audit;
        private const long MaxImageBytes = 2 * 1024 * 1024;
        public AdminSettingsManager(IProfileImageStore images, DataContext context, UserManager<User> users, IAdminAuditLogger audit)
        {
            _context = context;
            _users = users;
            _audit = audit;
            _images = images;
        }


        public async Task<AccessResult<ProfileImageDto>> UpdateProfileImageAsync(
            string userId, Stream file, long length, CancellationToken ct)
        {
            if (length <= 0)
                return AccessResult.Fail<ProfileImageDto>(400, "Please select an image.");
            if (length > MaxImageBytes)
                return AccessResult.Fail<ProfileImageDto>(400, "Image must be 2 MB or smaller.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsAdminUser && u.IsActive, ct);
            if (user is null)
                return AccessResult.Fail<ProfileImageDto>(403, "Your account is not allowed to perform this action.");

            using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, ct);
            if (buffer.Length > MaxImageBytes)                 // the declared length can lie
                return AccessResult.Fail<ProfileImageDto>(400, "Image must be 2 MB or smaller.");

            var bytes = buffer.ToArray();
            var kind = DetectImage(bytes);
            if (kind is null)
                return AccessResult.Fail<ProfileImageDto>(400, "Please upload a JPG, PNG or WebP image.");

            var newKey = $"admin-profiles/{userId}/{Guid.NewGuid():N}{kind.Value.Ext}";
            var oldKey = user.ProfileImageKey;

            buffer.Position = 0;
            await _images.SaveAsync(newKey, buffer, kind.Value.ContentType, ct);

            try
            {
                user.ProfileImageKey = newKey;
                user.DateModified = DateTimeOffset.UtcNow;
                user.ModifiedBy = userId;
                await _context.SaveChangesAsync(ct);
            }
            catch
            {
                await _images.DeleteAsync(newKey, ct);          // don't leave an orphaned upload behind
                throw;
            }

            if (!string.IsNullOrEmpty(oldKey))
            {
                try { await _images.DeleteAsync(oldKey, ct); }  // best effort; the new image is already saved
                catch { /* log it if you like; an orphaned old file is harmless */ }
            }

            await _audit.LogAsync("ProfileImageChanged", user.Id, user.Id, "Changed Profile Image", AdminActiviLogModule.AdminPanel.Profile, $"{user.Email}", ct);                                // here
        
            return AccessResult.Ok(new ProfileImageDto(_images.GetUrl(newKey)));
        }

        private static (string Ext, string ContentType)? DetectImage(byte[] b)
        {
            if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
                return (".jpg", "image/jpeg");
            if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47
                && b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A)
                return (".png", "image/png");
            if (b.Length >= 12 && b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46
                && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50)
                return (".webp", "image/webp");
            return null;
        }
        public async Task<AccessResult<AdminProfileDto>> GetProfileAsync(string userId,
            CancellationToken ct)
        {
            var u = await _context.Users.AsNoTracking()
                .Where(x => x.Id == userId && x.IsAdminUser)
                .Select(x => new
                {
                    x.Id,
                    x.AdminCode,
                    x.FirstName,
                    x.MiddleName,
                    x.LastName,
                    x.Status,
                    x.IsActive,
                    x.Department,
                    x.CreatedOn,
                    x.LastLoginDate,
                    x.ProfileImageKey,
                    x.DateOfBirth,
                    x.Gender
                })
                .FirstOrDefaultAsync(ct);

            if (u is null) return AccessResult.Fail<AdminProfileDto>(404, "User not found.");

            var roles = await (from ur in _context.UserRoles.AsNoTracking()
                               join r in _context.Roles on ur.RoleId equals r.Id
                               where ur.UserId == userId && ur.IsActive
                               select r.Name!).Distinct().OrderBy(n => n).ToListAsync(ct);

            var personal = await GetPersonalInfoAsync(userId, ct);       // from Task 21
            if (!personal.Succeeded) return AccessResult.Fail<AdminProfileDto>(personal.StatusCode, personal.Error!);

            var status = u.Status ?? (u.IsActive ? AdminUserStatus.Active : AdminUserStatus.Deactivated);
            var fullName = string.Join(" ", new[] { u.FirstName, u.MiddleName, u.LastName }
                .Where(p => !string.IsNullOrWhiteSpace(p)));
            var level = roles.Count > 0 ? string.Join(", ", roles) : null;
         


            var accessDetails = JsonConvert.SerializeObject(new
            {
                action = "ViewLoggedInUserProfile",
                accessedRecords = new[]
                {
                    new { entity = "UserNRoles", id = u.Id, count = roles.Count },
                },
                sectionsViewed = new[] { "Header", "WorkInfo", "PersonalInfo" },
                isSelfView = true
            });

                        await _audit.LogAsync(
                             "ViewLoggedInUserProfile",
                              userId,          // actor
                              u.Id,            // target
                              accessDetails,
                              AdminActiviLogModule.AdminPanel.Settings,
                              $"{userId}",
                              ct);
            return AccessResult.Ok(new AdminProfileDto(
                u.Id, u.AdminCode ?? "", fullName,
                string.IsNullOrEmpty(u.ProfileImageKey) ? null : _images.GetUrl(u.ProfileImageKey),
                status.ToString(), u.Department, level,
                u.CreatedOn, u.LastLoginDate, u.Gender, u.DateOfBirth,
                new AdminWorkInfoDto(u.Department, level, roles, u.CreatedOn),
                personal.Data!));
        }
        public async Task<AccessResult<object>> ChangePasswordAsync(string userId, ChangePasswordRequest req, CancellationToken ct)
        {
            // Mismatch is also caught by [Compare]; this protects callers that bypass model validation
            if (req.NewPassword != req.ConfirmNewPassword)
                return AccessResult.Fail<object>(400, "Passwords do not match.");

            var unmet = PasswordPolicy.Validate(req.NewPassword);
            if (unmet.Count > 0)
                return AccessResult.Fail<object>(400, string.Join(" ", unmet));

            var user = await _users.FindByIdAsync(userId);
            if (user is null || !user.IsAdminUser || !user.IsActive)
                return AccessResult.Fail<object>(403, "Your account is not allowed to perform this action.");

            // Brute-force protection: someone on a stolen session could otherwise guess the current password
            if (await _users.IsLockedOutAsync(user))
                return AccessResult.Fail<object>(429, "Too many failed attempts. Please try again later.");

            if (!await _users.CheckPasswordAsync(user, req.CurrentPassword))
            {
                await _users.AccessFailedAsync(user);                    // counts toward lockout
                return AccessResult.Fail<object>(400, "Current password is incorrect. Please try again.");
            }
            await _users.ResetAccessFailedCountAsync(user);

            if (req.CurrentPassword == req.NewPassword)
                return AccessResult.Fail<object>(400, "New password must be different from your current password.");

            // Verifies the current password again, hashes the new one and rotates the security stamp
            var result = await _users.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
            if (!result.Succeeded)
                return AccessResult.Fail<object>(400, string.Join(" ", result.Errors.Select(e => e.Description)));

            user.HasDefaultPassword = false;
            user.DateModified = DateTimeOffset.UtcNow;
            user.ModifiedBy = user.Id;
            await _users.UpdateAsync(user);

            await _audit.LogAsync("PasswordChanged", userId, user.Id, "Changed Password", AdminActiviLogModule.AdminPanel.Settings, $"{user.Email}", ct);                                // here


            return AccessResult.Ok<object>(null!);
        }

        public async Task<AccessResult<AdminPersonalInfoDto>> GetPersonalInfoAsync(string userId, CancellationToken ct)
        {
            var u = await _context.Users.AsNoTracking()
                .Where(x => x.Id == userId && x.IsAdminUser)
                .Select(x => new
                {
                    x.Id,
                    x.AdminCode,
                    x.Gender,
                    x.DateOfBirth,
                    x.PhoneNumber,
                    x.Email,
                    x.AdminUserOfficeAddress,
                    x.AdminUserOfficeState,
                    x.AdminUserOfficeCity

                })
                .FirstOrDefaultAsync(ct);

            if (u is null)
                return AccessResult.Fail<AdminPersonalInfoDto>(404, "User not found.");


            var accessDetails = JsonConvert.SerializeObject(new
            {
                action = "ViewLoggedInUserProfile",
                accessedRecords = new[]
{
        new { entity = "User", id = u.Id }
    },
                sectionsViewed = new[] { "Header", "WorkInfo", "PersonalInfo" },
                isSelfView = true
            });


            await _audit.LogAsync(
    "ViewLoggedInUserPersonalInformation",
    userId,          // actor
    u.Id,            // target
    accessDetails,
    AdminActiviLogModule.AdminPanel.Settings,
    $"{userId}",
    ct);

            return AccessResult.Ok(new AdminPersonalInfoDto(
                u.Id, u.AdminCode ?? "",
                u.Gender, u.DateOfBirth,
                u.PhoneNumber,
                //PhoneNumberHelper.FormatDisplay(u.PhoneNumber),
                u.Email ?? "", u.AdminUserOfficeAddress, u.AdminUserOfficeState, u.AdminUserOfficeCity));
        }

        public async Task<AccessResult<AdminPersonalInfoDto>> UpdatePersonalInfoAsync(
            string userId, UpdatePersonalInfoRequest req, CancellationToken ct)
        {
            var phone = PhoneNumberHelper.NormalizeNigerian(req.PhoneNumber);
            if (phone is null)
                return AccessResult.Fail<AdminPersonalInfoDto>(400, "Please enter a valid phone number.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsAdminUser && u.IsActive, ct);
            if (user is null)
                return AccessResult.Fail<AdminPersonalInfoDto>(403, "Your account is not allowed to perform this action.");

            if (await _context.Users.AnyAsync(u => u.Id != userId && u.PhoneNumber == phone, ct))
                return AccessResult.Fail<AdminPersonalInfoDto>(409, "A user with this phone number already exists.");

            var address = req.OfficeAddress.Trim();
            var state = req.State.Trim();
            var city = req.Lga.Trim();
            string dob = null;
            string gender = null;



            var changed = new List<string>();
            void Track(string field, string? oldValue, string? newValue)
            {
                if (!string.Equals(oldValue ?? "", newValue ?? "", StringComparison.Ordinal)) changed.Add(field);
            }
            Track("PhoneNumber", user.PhoneNumber, phone);
            Track("OfficeAddress", user.AdminUserOfficeAddress, address);
            Track("State", user.AdminUserOfficeState, state);
            Track("Lga", user.AdminUserOfficeCity, city);
            if (!string.IsNullOrWhiteSpace(req.DateOfBirth))
            {
                dob = req.DateOfBirth;
                Track("DateOfBirth", user.DateOfBirth, dob);
            }

            if (!string.IsNullOrWhiteSpace(req.Gender))
            {
                gender = req.Gender;

                Track("Gender", user.Gender, gender);
            }



            if (changed.Count > 0)
            {
                user.PhoneNumber = phone;
                user.AdminUserOfficeAddress = address;
                user.AdminUserOfficeState = state;
                user.AdminUserOfficeCity = city;

                user.DateOfBirth = dob;
                user.Gender = gender;
                user.DateModified = DateTimeOffset.UtcNow;
                user.ModifiedBy = userId;

                await _context.SaveChangesAsync(ct);          // single save, no manual transaction



                await _audit.LogAsync("PersonalInfoUpdated", user.Id, user.Id, "Changed Password", AdminActiviLogModule.AdminPanel.Profile, $"Fields: {string.Join(", ", changed)}", ct);                                // here

            }

            return await GetPersonalInfoAsync(userId, ct);    // refreshed card data for the UI
        }
    }

}
