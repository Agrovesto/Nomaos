using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.IO.RecyclableMemoryStreamManager;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
{
    
    public interface IMandERepositories
    {
        Task<List<MandEActivityType>> GetActivityTypesAsync(string businessCode, ListQueryParams query);
        Task<List<MandESubActivity>> GetSubActivitiesByTypeAsync(long activityTypeId);
        Task<List<MandEFieldDefinition>> GetFieldsAsync(long subActivityId);
        Task CreateActivityTypes(MandEActivityType entry);
        Task CreateActivityTypesSubActivities(MandESubActivity entry);
        Task<MandEFieldDefinition> AddFieldDefinitionAsync(MandEFieldDefinition field);
        Task<IEnumerable<MandEFieldDefinition>> AddBulkFieldDefinitionAsync(IEnumerable<MandEFieldDefinition> fields);
        Task<List<MandESubActivity>> GetSubActivitiesAsync(long subactivityId);
    }

    public class MandERepositories : IMandERepositories
    {
        private readonly DataContext _db;
        public MandERepositories(DataContext db) => _db = db;


        public async Task CreateActivityTypes(MandEActivityType entry)
        {
            _db.MandEActivityTypes.Add(entry);
            await _db.SaveChangesAsync();
        }
        public async Task CreateActivityTypesSubActivities(MandESubActivity entry)
        {
            _db.MandESubActivities.Add(entry);
            await _db.SaveChangesAsync();
        }
        public async Task<MandEFieldDefinition> AddFieldDefinitionAsync(MandEFieldDefinition field)
        {
            _db.MandEFieldDefinitions.Add(field);
            await _db.SaveChangesAsync();
            return field;
        }

        public async Task<IEnumerable<MandEFieldDefinition>> AddBulkFieldDefinitionAsync(IEnumerable<MandEFieldDefinition> fields)
        {
            _db.MandEFieldDefinitions.AddRange(fields);
            await _db.SaveChangesAsync();
            return fields;
        }
        public async Task<List<MandEActivityType>> GetActivityTypesAsync(string businessCode, ListQueryParams query)
        {
            var activityTypes =  _db.MandEActivityTypes.Where(x => x.BelongToBusinessCode == businessCode)
                .AsQueryable();

            if (!string.IsNullOrEmpty(query.organizationCode))
            {
                activityTypes = activityTypes.Where(x => x.OrganizationCode == query.organizationCode);
            }

            var result = await activityTypes
             //  .Skip(query.PageNumber)
               //.Take(query.PageSize)
               // .Select(s => s.Map())
                .ToListAsync();


            if (result.Count > 0)
            {
                return result;
            }
            return null;
        }
        public Task<List<MandESubActivity>> GetSubActivitiesByTypeAsync(long activityTypeId) =>
            _db.MandESubActivities.AsNoTracking().Where(x => x.MandEActivityTypeId == activityTypeId).OrderBy(x => x.Name).ToListAsync();
        public Task<List<MandESubActivity>> GetSubActivitiesAsync(long subactivityId) =>
            _db.MandESubActivities.AsNoTracking().Where(x => x.Id == subactivityId).OrderBy(x => x.Name).ToListAsync();

        public Task<List<MandEFieldDefinition>> GetFieldsAsync(long subActivityId) =>
            _db.MandEFieldDefinitions.AsNoTracking().Where(x => x.MandESubActivityId == subActivityId).OrderBy(x => x.Label).ToListAsync();
    }

    public interface IActivityEntryRepository
    {
        Task AddAsync(MandEFarmActivity entry);
        Task<List<MandEFarmActivity>> GetByFarmerAsync(string farmerUserId);
        Task<List<MandEFarmActivity>> GetMandELogsAsync(ListQueryParams args);
        Task<List<MandEFarmActivity>> GetMandELogsAsync(long subActivityId, ListQueryParams args);
    }

    public class ActivityEntryRepository : IActivityEntryRepository
    {
        private readonly DataContext _db;
        public ActivityEntryRepository(DataContext db) => _db = db;

        public async Task AddAsync(MandEFarmActivity entry)
        {
            _db.MandEFarmActivities.Add(entry);
            await _db.SaveChangesAsync();
        }

        public Task<List<MandEFarmActivity>> GetByFarmerAsync(string farmerUserId) =>
            _db.MandEFarmActivities.AsNoTracking()
                .Where(x => x.FarmerUserId == farmerUserId)
                .OrderByDescending(x => x.ActivityDate)
                .ToListAsync();



        public Task<List<MandEFarmActivity>> GetMandELogsAsync(ListQueryParams args)
        {
            var query = _db.MandEFarmActivities.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(args.organizationCode))
            {
                query = query.Where(x => x.OrganizationCode == args.organizationCode);
            }

            return query
                .OrderByDescending(x => x.ActivityDate)
                .ToListAsync();
        }

        public Task<List<MandEFarmActivity>> GetMandELogsAsync(long subActivityId, ListQueryParams args)
        {
            var query = _db.MandEFarmActivities
                .AsNoTracking()
                .Where(x => x.MandESubActivityId == subActivityId);

            if (!string.IsNullOrWhiteSpace(args.organizationCode))
            {
                query = query.Where(x => x.OrganizationCode == args.organizationCode);
            }

            return query
                .OrderByDescending(x => x.ActivityDate)
                .ToListAsync();
        }

    }

}
