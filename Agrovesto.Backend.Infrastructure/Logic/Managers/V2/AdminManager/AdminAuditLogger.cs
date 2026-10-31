using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers.V2.AdminManager
{
    public interface IAdminAuditLogger
    {
        Task LogAsync(string action, string? actorUserId, string? targetUserId, string? details = null, string? module = null, string? affectedRecord = null, CancellationToken ct = default);
    }

    public class AdminAuditLogger : IAdminAuditLogger
    {
        private readonly DataContext _db;
        private readonly IHttpContextAccessor _http;
        private readonly ILogger<AdminAuditLogger> _log;
        private readonly IClientInfoProvider _clientInfoProvider;
        public AdminAuditLogger(DataContext db,  ILogger<AdminAuditLogger> log, IClientInfoProvider clientInfoProvider)
        { _db = db;  _log = log; _clientInfoProvider = clientInfoProvider; }

        public async Task LogAsync(string action, string? actorUserId, string? targetUserId,
            string? details = null, string? module =null, string? affectedRecord=null, CancellationToken ct = default)
        {
            try
            {
                var clientInfo = _clientInfoProvider.Get();
                _db.AdminActivityLogs.Add(new AdminActivityLog
                {
                    ActorUserId = actorUserId,
                    TargetUserId = targetUserId,
                    Action = action,
                    Details = details,
                    Module = module,
                    AffectedRecord = affectedRecord,
                    OccurredAt = DateTimeOffset.UtcNow,
                    IpAddress = clientInfo.IpAddress,
                    Browser = clientInfo.Browser,
                    OperatingSystem = clientInfo.OperatingSystem,
                    DeviceType = clientInfo.DeviceType,
                    DeviceName = clientInfo.DeviceName,
                    UserAgent = clientInfo.UserAgent
                });
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex) { _log.LogError(ex, "Failed to write audit log {Action}", action); }   // never break the main flow
        }
    }
}
