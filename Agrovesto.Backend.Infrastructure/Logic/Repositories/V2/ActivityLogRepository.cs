using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories.V2
{
    public interface IActivityLogRepository
    {
        Task<List<ActivityLog>> GetActivityLogsAsync(ListQueryParams query);
        Task<(bool status, string message)> OnLogActivtyAsync(ActivityLog activity);
    }
    public class ActivityLogRepository : IActivityLogRepository
    {
        private readonly DataContext _context;
        public ActivityLogRepository(DataContext context)
        {
            _context = context;
        }
        public async Task<(bool status, string message)> OnLogActivtyAsync(ActivityLog activity)
        {
            await _context.AddAsync(activity);
            await _context.SaveChangesAsync();

            return (true, "Activity Logged");
        }
        public async Task<List<ActivityLog>> GetActivityLogsAsync(ListQueryParams query)
        {
            var activityLogsQuery = _context.ActivityLogs.AsQueryable();

            activityLogsQuery = activityLogsQuery
                .Where(x =>x.BusinessCode == query.businessCode);

            if (!string.IsNullOrWhiteSpace(query.UserId))
            {
                activityLogsQuery = activityLogsQuery
                    .Where(x => x.PerformedByUserId == query.UserId);
            }
            if (!string.IsNullOrWhiteSpace(query.organizationCode))
            {
                activityLogsQuery = activityLogsQuery
                    .Where(x => x.OrganizationCode == query.organizationCode);
            }

            if (query.startDate.HasValue && query.endDate.HasValue)
            {
                activityLogsQuery = activityLogsQuery
                    .Where(x => x.CreatedOn >= query.startDate.Value && x.CreatedOn <= query.endDate.Value);
            }
            else if (query.startDate.HasValue)
            {
                var date = query.startDate.Value.Date;
                activityLogsQuery = activityLogsQuery
                    .Where(x => x.CreatedOn.Date == date);
            }
            int pageNumber = query.PageNumber > 0 ? query.PageNumber : 1; 
            int pageSize = query.PageSize > 0 ? query.PageSize : 50;  
            activityLogsQuery = activityLogsQuery
        .OrderByDescending(x => x.CreatedOn)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize);
            var activityLogs = await activityLogsQuery.ToListAsync();

            return activityLogs;
        }

    }

}
