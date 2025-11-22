using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.MandE;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{
    internal class MandEManager
    {
    }
    public interface IMandEActivityManager
    {
        Task<IEnumerable<MandEActivityTypeModel>> GetActivityTypesAsync(string userId, ListQueryParams query)
        ;
        Task<IEnumerable<SubMandEActivityModel>> GetSubActivitiesAsync(long activityTypeId);
        Task<IEnumerable<TrackableMandEFieldModel>> GetFieldsAsync(long subActivityId);
        Task<long> SubmitEntryAsync(SubmitFarmerActivityEntryModel dto, BusinessRegistrationArgs args);
        Task<List<MandEActivityLogModel>> GetMandELogsAsync(ListQueryParams args);
        Task<List<MandEActivityLogModel>> GetMandELogsAsync(long subActivityId, ListQueryParams args);
        Task<long> CreateActivityTypes(CreateMandEActivityTypeModel dto, ListQueryParams args);
        Task<long> CreateActivityTypeSubActivities(CreateMandESubActivityModel dto, ListQueryParams args);
        Task<IEnumerable<MandEFieldDefinition>> CreateFieldsBulkAsync(IEnumerable<CreateMandEFieldDefinitionModel> models, ListQueryParams args);
        Task<MandEFieldDefinition> CreateFieldAsync(MandEFieldDefinition model,  ListQueryParams args);
    }

    public class MandEActivityManager : IMandEActivityManager
    {
        private readonly IFarmerRepository _farmerRepository;
        private readonly IMandERepositories _catalog;
        private readonly IActivityEntryRepository _entries;
        private readonly DataContext _db;
        private readonly IBusinessRepository _businessRepository;
        private readonly ILogger<MandEActivityManager> _log;
        public MandEActivityManager(ILogger<MandEActivityManager> log, IFarmerRepository farmerRepository, IBusinessRepository businessRepository, IMandERepositories catalog, IActivityEntryRepository entries, DataContext db)
        { _catalog = catalog; 
            _entries = entries;
            _businessRepository = businessRepository;

            _farmerRepository = farmerRepository;
            _db = db;
            _log = log;
        }

        //check if organizationcode is present
        //only return for the specified organzation otherwise, return for the business

        public async Task<long> CreateActivityTypes(CreateMandEActivityTypeModel dto, ListQueryParams args)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
             ?? throw new Exception("Business does not exist");

                // Validate sub-activity belongs to activity type
                var sub = await _catalog.GetActivityTypesAsync(args.businessCode, args);
               if(sub != null && sub.Any(s =>s.Name == dto.Name))
                {
                    throw new InvalidOperationException("Activity Type with the same name already exists.");
                }
                
                var entry = new MandEActivityType
                {
                  Description= dto.Description,
                  Name= dto.Name,
                    BelongToBusinessCode= args.businessCode,
                    OrganizationCode= args.organizationCode
                };
                await _catalog.CreateActivityTypes(entry);
                return entry.Id;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }


        }


        public async Task<long> CreateActivityTypeSubActivities(CreateMandESubActivityModel dto, ListQueryParams args)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
             ?? throw new Exception("Business does not exist");

                // Validate sub-activity belongs to activity type
                var sub = await _catalog.GetActivityTypesAsync(args.businessCode, args);
               if(sub ==null)
                {
                    throw new InvalidOperationException("Activity Type does not exist.");
                }

                var entry = new MandESubActivity
                {
                    Description = dto.Description,
                    Name = dto.Name,
                    MandEActivityTypeId=args.Id,
                    BelongToBusinessCode = args.businessCode,
                    OrganizationCode = args.organizationCode
                };
                await _catalog.CreateActivityTypesSubActivities(entry);
                return entry.Id;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }


        }

        public async Task<MandEFieldDefinition> CreateFieldAsync(MandEFieldDefinition model, ListQueryParams args)
        {
            // Optional: validate uniqueness per subactivity and business
            var exists = (await _catalog.GetFieldsAsync(model.MandESubActivityId))
                         .Any(f => f.Key.Equals(model.Key, StringComparison.OrdinalIgnoreCase)
                                   && f.BelongToBusinessCode == model.BelongToBusinessCode);

            if (exists)
                throw new InvalidOperationException($"Field with key '{model.Key}' already exists for this sub-activity.");
            model.BelongToBusinessCode = args.businessCode;
            model.OrganizationCode = args.organizationCode;
            model.MandESubActivityId=args.Id;
            return await _catalog.AddFieldDefinitionAsync(model);
        }

        public async Task<IEnumerable<MandEFieldDefinition>> CreateFieldsBulkAsync(IEnumerable<CreateMandEFieldDefinitionModel> models, ListQueryParams args)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
             ?? throw new Exception("Business does not exist");

                var existingFields = (await _catalog.GetFieldsAsync(args.Id))
                              .Select(f => f.Key.ToLower())
                              .ToHashSet();

                var toInsert = models
                    .Where(d => !existingFields.Contains(d.Key.ToLower()))
                    .Select(d => new MandEFieldDefinition
                    {
                        MandESubActivityId = args.Id,
                        Label = d.Label,
                        Key = d.Key,
                        Type = d.Type,
                        OptionsJson = d.Options != null ? JsonSerializer.Serialize(d.Options) : null,
                        IsRequired = d.IsRequired,
                        BelongToBusinessCode = args.businessCode,
                        OrganizationCode = args.organizationCode
                    })
                    .ToList();

                if (!toInsert.Any())
                    return Enumerable.Empty<MandEFieldDefinition>();
                return await _catalog.AddBulkFieldDefinitionAsync(toInsert);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }

        }
        public async Task<IEnumerable<MandEActivityTypeModel>> GetActivityTypesAsync(string userId, ListQueryParams query)
        {
            try
            {
                query.PageNumber = (query.PageNumber - 1) * query.PageSize;
                var business = await _businessRepository.GetBusinessByUserIdAsync(userId);
                if (business == null && !string.IsNullOrWhiteSpace(query.businessCode))
                {
                    business = await _businessRepository.GetBusinessByCodeAsync(query.businessCode);
                }
                if (business == null)
                {
                    throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "No Record Found");
                }

                var activityTypes = await _catalog.GetActivityTypesAsync(business.BusinessCode, query);

                if (activityTypes == null) return null;
                return activityTypes.Select(a => new MandEActivityTypeModel(a.Id, a.Name, a.Description, a.BelongToBusinessCode, a.OrganizationCode));
            

            }
            catch (BaseException ex)
            {
                _log.LogError($"Error retrieving Agents: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError($"Unexpected error retrieving Agents: {ex.Message}");
                throw new NotFoundException("No Record Found");
            }
            }
        public async Task<IEnumerable<SubMandEActivityModel>> GetSubActivitiesAsync(long activityTypeId) =>
            (await _catalog.GetSubActivitiesByTypeAsync(activityTypeId))
            .Select(s => new SubMandEActivityModel(s.Id, s.MandEActivityTypeId, s.Name, s.Description, s.BelongToBusinessCode, s.OrganizationCode));

        public async Task<IEnumerable<TrackableMandEFieldModel>> GetFieldsAsync(long subActivityId)
        {
            var fields = await _catalog.GetFieldsAsync(subActivityId);
            return fields.Select(f => new TrackableMandEFieldModel(
                f.Id, f.MandESubActivityId, f.Label, f.Key, f.Type, f.IsRequired,
                string.IsNullOrWhiteSpace(f.OptionsJson) ? null :
                System.Text.Json.JsonSerializer.Deserialize<string[]>(f.OptionsJson),f.BelongToBusinessCode,f.OrganizationCode
                
                
            ));
        }

        public async Task<long> SubmitEntryAsync(SubmitFarmerActivityEntryModel dto, BusinessRegistrationArgs args)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
             ?? throw new Exception("Business does not exist");
                var farmer = await _farmerRepository.GetFarmerByUserId(dto.FarmerUserId);
                if (farmer == null) throw new Exception("Farmer does not exist");

                // Validate sub-activity belongs to activity type
                var sub = await _db.MandESubActivities.AsNoTracking()
                .SingleOrDefaultAsync(s => 
                s.Id == dto.SubActivityId &&
                s.MandEActivityTypeId == dto.ActivityTypeId);
            if (sub is null) throw new InvalidOperationException("Invalid ActivityType/SubActivity combination.");

            // Validate field keys exist
            var fields = await _db.MandEFieldDefinitions.AsNoTracking()
                .Where(f => f.MandESubActivityId == dto.SubActivityId).ToListAsync();

            var requiredMissing = fields.Where(f => 
            f.IsRequired && 
            !dto.FieldData.ContainsKey(f.Key)).ToList();
            if (requiredMissing.Any())
                throw new InvalidOperationException("Missing required fields: " + string.Join(", ", requiredMissing.Select(x => x.Key)));

            var entry = new MandEFarmActivity
            {
               FarmerName= farmer.Name,
               CreatedOn= DateTimeOffset.Now,
                FarmerUserId = dto.FarmerUserId,
                FarmPlotId = dto.FarmPlotId,
                MandEActivityTypeId = dto.ActivityTypeId,
                MandESubActivityId = dto.SubActivityId,
                ActivityDate = dto.ActivityDate == default ? DateTime.UtcNow : dto.ActivityDate,
                PerformedByUserId = args.UserId,
                DataJson = System.Text.Json.JsonSerializer.Serialize(dto.FieldData),
                AttachmentUrl = dto.AttachmentUrl,
                BelongToBusinessCode = dto.BelongToBusinessCode,
                OrganizationCode = args.organizationCode,
                Notes = dto.Notes,
            };
            await _entries.AddAsync(entry);
            return entry.Id;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }


        }



        public async Task<List<MandEActivityLogModel>> GetMandELogsAsync(ListQueryParams args)
        {
            var logs = await _entries.GetMandELogsAsync(args);
            return logs.Select(l => new MandEActivityLogModel
            {
                Id = l.Id,
                FarmerUserId = l.FarmerUserId,
                FarmPlotId = l.FarmPlotId,
                ActivityTypeId = l.MandEActivityTypeId,
                SubActivityId = l.MandESubActivityId,
                ActivityDate = l.ActivityDate.Date,
                Notes = l.Notes,
                AttachmentUrl = l.AttachmentUrl,
                FieldData = string.IsNullOrWhiteSpace(l.DataJson)
    ? new Dictionary<string, object>()
    : JsonSerializer.Deserialize<Dictionary<string, object>>(l.DataJson)

            }).ToList();
        }

        public async Task<List<MandEActivityLogModel>> GetMandELogsAsync(long subActivityId, ListQueryParams args)
        {
            var logs = await _entries.GetMandELogsAsync(subActivityId,args);
            return logs.Select(l => new MandEActivityLogModel
            {
                Id = l.Id,
                FarmerUserId = l.FarmerUserId,
                FarmPlotId = l.FarmPlotId,
                ActivityTypeId = l.MandEActivityTypeId,
                SubActivityId = l.MandESubActivityId,
                ActivityDate = l.ActivityDate.Date,
                Notes = l.Notes,
                AttachmentUrl = l.AttachmentUrl,
                FieldData = string.IsNullOrWhiteSpace(l.DataJson)
    ? new Dictionary<string, object>()
    : JsonSerializer.Deserialize<Dictionary<string, object>>(l.DataJson)

            }).ToList();
        }
    }

}
