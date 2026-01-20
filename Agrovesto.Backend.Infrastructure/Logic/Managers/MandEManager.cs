using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.MandE;
using Agrovesto.Backend.Core.Models.Common.MandEService;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Businesses.MandE;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using Agrovesto.Backend.Infrastructure.Logic.Repositories.MandEService;
using Agrovesto.Backend.Infrastructure.Logic.Services.MandE;
using CsvHelper;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Formats.Asn1;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Managers
{
   
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
        Task<long> CreateDatasetAsync(CreateMandEDatasetDto dto, ListQueryParams args);
        Task<IEnumerable<MandEDatasetListDto>> GetDatasetsAsync(ListQueryParams args);
        Task<long> CreateDataSetFieldAsync(
            //long datasetId, 
            CreateMandEFieldDto dto, ListQueryParams args);
        Task<IEnumerable<MandEFieldDto>> GetDataSetFieldsAsync(long datasetId);
        Task<long> CreateDataSetRecordAsync(long datasetId, CreateMandERecordDto dto, ListQueryParams args);
        Task<IEnumerable<MandERecordDto>> GetDataSetRecordsAsync(long datasetId, ListQueryParams args);
        Task<BulkUploadResultDto> BulkDataSetFieldUploadAsync(long datasetId, IFormFile file, ListQueryParams args);
        Task<IEnumerable<ChartDataPoint>> AggregateAsync(
        MandEChartDefinition chart,
        IEnumerable<MandERecord> records);
        Task<long> CreateChartAsync(long datasetId, CreateMandEChartDto dto);
        Task<IEnumerable<MandEChartDefinition>> GetChartsAsync(long datasetId);
        // Task EnsureRoleAsync(long datasetId, string userId, string requiredRole);
        Task<DatasetOverviewDto> GetDatasetOverviewAsync(ListQueryParams query);
        Task UpdateDataSetRecordAsync(long recordId, UpdateMandERecordDto dto, ListQueryParams args);
    }

    public class MandEActivityManager : IMandEActivityManager
    {
        private readonly IFarmerRepository _farmerRepository;
        private readonly IMandERepositories _catalog;
        private readonly IActivityEntryRepository _entries;
        private readonly DataContext _db;
        private readonly IBusinessRepository _businessRepository;
        private readonly ILogger<MandEActivityManager> _log;
        private readonly IMandEFormulaEngine _formulaEngine;
        private readonly IMandEServiceRepository _mandEServiceRepository;
        public MandEActivityManager(ILogger<MandEActivityManager> log, IFarmerRepository farmerRepository, 
            IBusinessRepository businessRepository, 
            IMandERepositories catalog, IActivityEntryRepository entries,
            DataContext db,
            IMandEServiceRepository mandEServiceRepository,
            IMandEFormulaEngine formulaEngine)
        { _catalog = catalog; 
            _entries = entries;
            _businessRepository = businessRepository;
            _formulaEngine = formulaEngine; 
            _farmerRepository = farmerRepository;
            _db = db;
            _log = log;
            _mandEServiceRepository = mandEServiceRepository;
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

        public async Task<long> CreateDatasetAsync(CreateMandEDatasetDto dto, ListQueryParams args)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
             ?? throw new Exception("Business does not exist");

                // Validate sub-activity belongs to activity type
                var sub = await _mandEServiceRepository.GetDataSetAsync(args);
                if (sub != null && sub.Any(s => s.Name == dto.Name && s.MandEOwnerType.ToString() == dto.OwnerType))
                {
                    throw new InvalidOperationException("Dataset with the same name already exists.");
                }

                var entry = new MandEDataset
                {
                    Description = dto.Description,
                    Name = dto.Name,
                    CreatedOn= DateTimeOffset.UtcNow,
                    BelongToBusinessCode = args.businessCode,
                    OrganizationCode = args.organizationCode,
                    MandEOwnerType = dto.OwnerType switch
                    {
                        "Individual" => MandEOwnerType.Individual,
                        "Organization" => MandEOwnerType.Organization,
                        "Business" => MandEOwnerType.Business,
                        "External" => MandEOwnerType.External,
                        _ => MandEOwnerType.Business
                    }
                };
                if(!string.IsNullOrEmpty(args.UserId)){
                    entry.OwnerId = args.UserId;
                }
                await _mandEServiceRepository.AddDataSetAsync(entry);
                return entry.Id;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}", ex);
            }

        }

        public async Task<IEnumerable<MandEDatasetListDto>> GetDatasetsAsync(ListQueryParams args)
        {
            try
            {
                args.PageNumber = (args.PageNumber - 1) * args.PageSize;
                var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode);
              
                if (business == null)
                {
                    throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Business Record Not Found");
                }

                var dataSets = await _mandEServiceRepository.GetDataSetAsync(args);
                //Dataset name Purpose(short description) Number of fields Number of records Status: Draft / Active
                ///
                //
                if (dataSets == null) return null;
                return dataSets.Select(x => new MandEDatasetListDto
                {
                    FieldCount=x.Fields.Count,
                    Description=x.Description,
                    Name=x.Name,
                    RecordCount=x.Records.Count,
                    Id=x.Id
                }).ToList();
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

        public async Task<long> CreateDataSetFieldAsync(
            //long datasetId, 
            CreateMandEFieldDto dto, ListQueryParams args)
        {
             if (dto.IsComputed && string.IsNullOrWhiteSpace(dto.Formula))
                throw new InvalidOperationException("Computed field must have a formula.");

            var existing = await _mandEServiceRepository.GetFieldByDatasetAsync(args.Id);
                //datasetId);
            if (existing.Any(f => f.Key.Equals(dto.Key, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Field with key '{dto.Key}' already exists.");

            if (dto.IsComputed && dto.Type != "number")
                throw new InvalidOperationException("Only number fields can be computed.");

            if (dto.Type == "select" && (dto.Options == null || !dto.Options.Any()))
                throw new InvalidOperationException("Select field must have options.");

            if (dto.Type == "reference" && dto.ReferenceDatasetId == null)
                throw new InvalidOperationException("Reference field must select a dataset.");


            if (dto.IsComputed)
            {
                _formulaEngine.Validate(dto.Formula!, dto.Key, existing);
            }
            if (dto.Order <= 0)
                dto.Order = 1;


            var field = new MandEDataFieldDefinition
            {
                MandEDatasetId =args.Id, // datasetId,
                Name = dto.Name,
                Label = dto.Label,
                
                Key = dto.Key,
                Type = dto.Type,
                IsComputed = dto.IsComputed,
                Formula = dto.Formula,
                IsRequired = dto.IsRequired,
                Order = dto.Order,
                Options=dto.Options,
                BelongToBusinessCode = args.businessCode,
                OrganizationCode = args.organizationCode
            };

             await _mandEServiceRepository.AddFieldAsync(field);
            return field.Id;

        }

        public async Task<IEnumerable<MandEFieldDto>> GetDataSetFieldsAsync(long datasetId)
        {
            var fields = await _mandEServiceRepository.GetFieldByDatasetAsync(datasetId);

            return fields
                .OrderBy(f => f.Order)
                .Select(f => new MandEFieldDto(
                    f.Id,
                    f.Name,
                    f.Key,
                    f.Type,
                    f.IsComputed,
                    f.Formula,
                    f.IsRequired,
                    f.Order,
                    f.Options,
                    f.Label,
                    f.MandEDataset.Name,
                    f.MandEDatasetId
                ));
        }

        public async Task<long> CreateDataSetRecordAsync(
     long datasetId,
     CreateMandERecordDto dto,
     ListQueryParams args)
        {
            try
            {
                var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
                    ?? throw new Exception("Business does not exist");

                var fields = await _mandEServiceRepository.GetFieldByDatasetAsync(datasetId);

                var inputData = new Dictionary<string, object>();
                var numericData = new Dictionary<string, decimal>();
    

                foreach (var field in fields)
                {
                    if (string.IsNullOrWhiteSpace(field.Key))
                        continue;

                    if (!dto.Data.TryGetValue(field.Key, out var rawValue))
                        continue;

                    if (rawValue is JsonElement jsonEl)
                    {
                        rawValue = UnwrapJsonElement(jsonEl);
                    }


                    if (rawValue == null)
                    {
                        inputData[field.Key] = null!;
                        continue;
                    }

                    switch (field.Type.ToLowerInvariant())
                    {
                        case "number":
                            inputData[field.Key] = rawValue == null
                                ? null!
                                : Convert.ToDecimal(rawValue);
                            numericData[field.Key] = Convert.ToDecimal(rawValue);
                            break;

                        case "boolean":
                        case "bool":
                            inputData[field.Key] = Convert.ToBoolean(rawValue);
                            break;

                        case "date":
                            inputData[field.Key] = Convert.ToDateTime(rawValue);
                            break;

                        case "select":
                        case "text":
                        case "reference":
                            inputData[field.Key] = rawValue?.ToString()!;
                            break;

                        default:
                            inputData[field.Key] = rawValue!;
                            break;
                    }
                }

                // ✅ Validate required fields
                var missing = fields
                    .Where(f =>
                        f.IsRequired &&
                        (!inputData.ContainsKey(f.Key) ||
                         inputData[f.Key] == null ||
                         string.IsNullOrWhiteSpace(inputData[f.Key]?.ToString())))
                    .Select(f => f.Key)
                    .ToList();

                if (missing.Any())
                    throw new InvalidOperationException($"Missing fields: {string.Join(", ", missing)}");

                // ✅ Compute derived fields
                foreach (var field in fields.Where(f => f.IsComputed))
                {
                    var value = _formulaEngine.Evaluate(field.Formula!, numericData);

                    inputData[field.Key] = value;
                    numericData[field.Key] = value; // 👈 allow chained formulas
                }


                var record = new MandERecord
                {
                    MandEDatasetId = datasetId,
                    DataJson = JsonSerializer.Serialize(inputData),
                    CreatedByUserId = args.UserId,
                    CreatedOn = DateTimeOffset.UtcNow,
                    BelongToBusinessCode = args.businessCode,
                    OrganizationCode = args.organizationCode
                };

                await _mandEServiceRepository.AddRecordAsync(record);

                return record.Id;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public async Task UpdateDataSetRecordAsync(
            long recordId,
            UpdateMandERecordDto dto, // similar to CreateMandERecordDto but optional fields
            ListQueryParams args)
        {
            try
            {
                args.Id = recordId;
                // 1. Load record
                var record = await _mandEServiceRepository.GetRecordByIdAsync(args)
                    ?? throw new Exception("Record does not exist");

                // Optional: check business code/organization for security
                if (record.BelongToBusinessCode != args.businessCode)
                    throw new UnauthorizedAccessException();

                // 2. Load dataset fields
                var fields = await _mandEServiceRepository.GetFieldByDatasetAsync(record.MandEDatasetId);

                // 3. Deserialize existing data
                var existingData = JsonSerializer.Deserialize<Dictionary<string, object>>(record.DataJson)
                                   ?? new Dictionary<string, object>();

                var numericData = new Dictionary<string, decimal>();

                // 4. Merge updated fields
                foreach (var field in fields)
                {
                    if (string.IsNullOrWhiteSpace(field.Key))
                        continue;

                    if (!dto.Data.TryGetValue(field.Key, out var rawValue))
                        continue; // skip fields not in dto (i.e., not changed)

                    if (rawValue is JsonElement jsonEl)
                        rawValue = UnwrapJsonElement(jsonEl);

                    // Convert field types
                    switch (field.Type.ToLowerInvariant())
                    {
                        case "number":
                            var decVal = rawValue != null ? Convert.ToDecimal(rawValue) : 0m;
                            existingData[field.Key] = decVal;
                            numericData[field.Key] = decVal;
                            break;

                        case "boolean":
                        case "bool":
                            existingData[field.Key] = Convert.ToBoolean(rawValue);
                            break;

                        case "date":
                            existingData[field.Key] = Convert.ToDateTime(rawValue);
                            break;

                        case "select":
                        case "text":
                        case "reference":
                            existingData[field.Key] = rawValue?.ToString() ?? "";
                            break;

                        default:
                            existingData[field.Key] = rawValue!;
                            break;
                    }
                }

                // 5. Recompute computed fields
                foreach (var field in fields.Where(f => f.IsComputed))
                {
                    var value = _formulaEngine.Evaluate(field.Formula!, numericData);
                    existingData[field.Key] = value;
                    numericData[field.Key] = Convert.ToDecimal(value); // allow chained formulas
                }

                // 6. Validate required fields (optional)
                var missing = fields
                    .Where(f => f.IsRequired &&
                                (!existingData.ContainsKey(f.Key) ||
                                 existingData[f.Key] == null ||
                                 string.IsNullOrWhiteSpace(existingData[f.Key]?.ToString())))
                    .Select(f => f.Key)
                    .ToList();

                if (missing.Any())
                    throw new InvalidOperationException($"Missing fields: {string.Join(", ", missing)}");

                // 7. Save updated record
                record.DataJson = JsonSerializer.Serialize(existingData);
                record.UpdatedOn = DateTimeOffset.UtcNow;
                record.UpdatedByUserId = args.UserId;

                await _mandEServiceRepository.UpdateRecordAsync(record);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }
        private static object? UnwrapJsonElement(JsonElement el)
        {
            return el.ValueKind switch
            {
                JsonValueKind.Number =>
                    el.TryGetDecimal(out var d) ? d : el.GetDouble(),

                JsonValueKind.String =>
                    el.GetString(),

                JsonValueKind.True => true,
                JsonValueKind.False => false,

                JsonValueKind.Null => null,

                _ => el.ToString()
            };
        }

        public async Task<IEnumerable<MandERecordDto>> GetDataSetRecordsAsync(long datasetId, ListQueryParams args)
        {
            var records = await _mandEServiceRepository.GetRecordByDatasetAsync(datasetId, args);

            return records.Select(r =>
            {
                // Safely handle null or empty JSON
                var data = string.IsNullOrWhiteSpace(r.DataJson)
                    ? new Dictionary<string, object>()    // empty dictionary if no data
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(r.DataJson)
                      ?? new Dictionary<string, object>(); // fallback

                return new MandERecordDto(
                    r.Id,
                    data,
                    r.CreatedOn
                );
            });
        }

        public async Task<long> CreateChartAsync(long datasetId, CreateMandEChartDto dto)
        {
            var chart = new MandEChartDefinition
            {
                MandEDatasetId = datasetId,
                ChartType = dto.ChartType,
                XKey = dto.XKey,
                YKey = dto.YKey,
                GroupByKey = dto.GroupByKey
            };

            await _mandEServiceRepository.AddChartAsync(chart);
            return chart.Id;
        }

        public async Task<IEnumerable<MandEChartDefinition>> GetChartsAsync(long datasetId)
        {
            return await _mandEServiceRepository.GetChartByDatasetAsync(datasetId);
        }
        public async Task<BulkUploadResultDto> BulkDataSetFieldUploadAsync(
    long datasetId,
    IFormFile file,
    ListQueryParams args)
        {
            var result = new BulkUploadResultDto();
            var fields = (await _mandEServiceRepository.GetFieldByDatasetAsync(datasetId)).ToList();

            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            var records = csv.GetRecords<dynamic>().ToList();
            result.Total = records.Count;

            foreach (var row in records)
            {
                try
                {
                    var data = ((IDictionary<string, object>)row)
                        .ToDictionary(k => k.Key, v => Convert.ToDecimal(v.Value));

                    await CreateDataSetRecordAsync(datasetId, new CreateMandERecordDto
                    {
                        Data = data.ToDictionary(x => x.Key, x => (object)x.Value)
                    }, args);

                    result.Success++;
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.Errors.Add(ex.Message);
                }
            }

            return result;
        }
        public async Task<IEnumerable<ChartDataPoint>> AggregateAsync(
        MandEChartDefinition chart,
        IEnumerable<MandERecord> records)
        {
            var rows = records.Select(r =>
                JsonSerializer.Deserialize<Dictionary<string, object>>(r.DataJson)!
            );

            var grouped = chart.GroupByKey != null
                ? rows.GroupBy(r => r[chart.GroupByKey!]?.ToString())
                : new[] { rows.GroupBy(_ => "ALL") }.SelectMany(x => x);

            return grouped.Select(g => new ChartDataPoint
            {
                X = g.Key!,
                Y = g.Sum(r => Convert.ToDecimal(r[chart.YKey]))
            });
        }
        //public async Task EnsureRoleAsync(long datasetId, string userId, string role)
        //{
        //    var permission = await _mandEServiceRepository.GetAsync(datasetId, userId);

        //    if (permission == null || permission.RoleRank < RoleRank(role))
        //        throw new UnauthorizedAccessException("Insufficient permission.");
        //}
        public async Task<DatasetOverviewDto> GetDatasetOverviewAsync(ListQueryParams query)
        {
            var fieldCount = await _mandEServiceRepository.GetFieldCountAsync(query.Id);
            var recordCount = await _mandEServiceRepository.GetRecordCountAsync(query.Id);
            //var validations = await _repo.GetFieldValidationsAsync(datasetId);
            var recentRecords = await _mandEServiceRepository.GetRecentRecordsAsync(query.Id, query.PageSize);
            //var activityLog = await _repo.GetActivityLogAsync(datasetId, 10);
            var datasets = await _mandEServiceRepository.GetDataSetAsync(query);
            var dataset = datasets.First(x => x.Id ==query.Id);
            var overview = new DatasetOverviewDto
            {
                Id = dataset.Id,
                Name = dataset.Name,
                Description = dataset.Description,
                FieldCount = fieldCount,
                RecordCount = recordCount,
               // Validations = validations,
                RecentRecords = recentRecords,
              //  ActivityLog = activityLog
            };
            return overview;
        }
    }

}
