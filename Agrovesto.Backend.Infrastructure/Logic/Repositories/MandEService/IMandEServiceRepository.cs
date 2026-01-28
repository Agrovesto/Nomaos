using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common.MandEService;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Businesses.MandE;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories.MandEService
{
    
    public interface IMandEServiceRepository
    {
        Task AddRecordAsync(MandERecord record);
        Task<List<MandERecord>> GetRecordByDatasetAsync(long datasetId, ListQueryParams query);
        Task<List<MandEDataFieldDefinition>> GetFieldByDatasetAsync(long datasetId);
        Task AddFieldAsync(MandEDataFieldDefinition field);
        Task AddDataSetAsync(MandEDataset dataset);
        Task<IEnumerable<MandEDataset>> GetDataSetAsync(ListQueryParams query);
        Task<IEnumerable<MandEDataFieldDefinition>> AddBulkFieldDefinitionAsync(IEnumerable<MandEDataFieldDefinition> fields);
        Task<IEnumerable<MandEChartDefinition>> GetChartByDatasetAsync(long datasetId);
        Task AddChartAsync(MandEChartDefinition chart);
        Task<DatasetOverviewDto> GetDatasetOverviewAsync(long datasetId);
        Task<long> GetFieldCountAsync(long datasetId);
        Task<long> GetRecordCountAsync(long datasetId);
        //Task<List<DatasetValidationDto>> GetFieldValidationsAsync(long datasetId);
        Task<List<DatasetRecordDto>> GetRecentRecordsAsync(long datasetId, int limit = 5);
        Task<MandERecord> GetRecordByIdAsync(ListQueryParams query);
        Task UpdateRecordAsync(MandERecord record);
        // Task<List<DatasetActivityDto>> GetActivityLogAsync(long datasetId, long limit = 10);

    }

    public class MandEServiceRepositories : IMandEServiceRepository
    {
        private readonly DataContext _db;
        public MandEServiceRepositories(DataContext db) => _db = db;

        public async Task AddRecordAsync(MandERecord record)
        {
            _db.MandERecords.Add(record);
            await _db.SaveChangesAsync();
        }
        public async Task UpdateRecordAsync(MandERecord record)
        {
            _db.MandERecords.Update(record);
            await _db.SaveChangesAsync();
        }

        public async Task AddDataSetAsync(MandEDataset dataset)
        {
            _db.MandEDatasets.Add(dataset);
            await _db.SaveChangesAsync();
        }
        public async Task AddFieldAsync(MandEDataFieldDefinition field)
        {
            _db.MandEDataFieldDefinitions.Add(field);
            await _db.SaveChangesAsync();
            }

        public async Task<IEnumerable<MandEDataFieldDefinition>> AddBulkFieldDefinitionAsync(IEnumerable<MandEDataFieldDefinition> fields)
        {
            _db.MandEDataFieldDefinitions.AddRange(fields);
            await _db.SaveChangesAsync();
            return fields;
        }
        public async Task<List<MandERecord>> GetRecordByDatasetAsync(
     long datasetId,
     ListQueryParams query)
        {
       
            var businessCode = query.businessCode?.Trim();
            var organizationCode = query.organizationCode?.Trim();

            var result= await _db.MandERecords
                .Where(x =>
                    x.MandEDatasetId == datasetId &&
                    x.BelongToBusinessCode == businessCode &&
                    (
                        string.IsNullOrEmpty(organizationCode) ||
                        x.OrganizationCode == organizationCode
                    )
                )
                .ToListAsync();

            return result;
        }

        public async Task<IEnumerable<MandEDataset>> GetDataSetAsync(ListQueryParams query)
        {
            var datasets = _db.MandEDatasets
                .Where(x => x.BelongToBusinessCode == query.businessCode)
                .AsQueryable();

            if (!string.IsNullOrEmpty(query.organizationCode))
            {
                datasets = datasets.Where(x => x.OrganizationCode == query.organizationCode);
            }

            if (query.Id > 0)
            {
                datasets = datasets.Where(x => x.Id == query.Id);
            }

            var result = await datasets
                .Select(d => new MandEDataset
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description,

                    Fields = d.Fields,      // SAFE → returns 0 if none
                    Records = d.Records     // SAFE → returns 0 if none
                })
                .ToListAsync();

            return result;
        }
        public Task<List<MandEDataFieldDefinition>> GetFieldByDatasetAsync(long datasetId) =>
            _db.MandEDataFieldDefinitions
                .AsNoTracking()
                .Where(x => x.MandEDatasetId == datasetId)
                .Include(x => x.MandEDataset) // <--- include the related dataset
                .OrderBy(x => x.Order)
                .ToListAsync();
        public async Task AddChartAsync(MandEChartDefinition chart)
        {
            _db.MandEChartDefinitions.Add(chart);
            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<MandEChartDefinition>> GetChartByDatasetAsync(long datasetId)
        {
            return await _db.MandEChartDefinitions
                .Where(c => c.MandEDatasetId == datasetId)
                .ToListAsync();
        }
        public async Task<DatasetOverviewDto> GetDatasetOverviewAsync(long datasetId)
        {
            // TODO: Replace with actual DB queries
            var dataset = await _db.MandEDatasets.FindAsync(datasetId);

            if (dataset == null)
                throw new KeyNotFoundException("Dataset not found");

            // Example stub data
            return new DatasetOverviewDto
            {
                Id = dataset.Id,
                Name = dataset.Name,
                Description = dataset.Description,
                IsLocked = false, // TODO: compute based on whether records exist
                Stats = new DatasetStatsDto
                {
                    RecordCount = 10,
                    FieldCount = 5,
                    LastUpdated = DateTime.UtcNow,
                    CompletenessPercentage = 90
                },
                Validations = new List<DatasetValidationDto>
            {
                new() { FieldName = "Weight", Message = "Cannot be negative", IsCritical = true }
            },
                RecentRecords = new List<DatasetRecordDto>
            {
                new() { Id = 1, Values = new Dictionary<string, object>{{"Weight", 100}}, CreatedOn = DateTimeOffset.UtcNow }
            },
                ActivityLog = new List<DatasetActivityDto>
            {
                new() { Timestamp = DateTime.UtcNow, User = "Admin", Action = "Created dataset" }
            }
            };
        }

        public async Task<long> GetFieldCountAsync(long datasetId)
       => await _db.MandEDataFieldDefinitions.CountAsync(f => f.MandEDatasetId == datasetId);

        public async Task<long> GetRecordCountAsync(long datasetId)
            => await _db.MandERecords.CountAsync(r => r.MandEDatasetId == datasetId);

        public async Task<MandERecord> GetRecordByIdAsync(
  ListQueryParams query)
        {

            var businessCode = query.businessCode?.Trim();
            var organizationCode = query.organizationCode?.Trim();

            var result = await _db.MandERecords
                .Where(x =>
                    x.Id == query.Id &&
                    x.BelongToBusinessCode == businessCode &&
                    (
                        string.IsNullOrEmpty(organizationCode) ||
                        x.OrganizationCode == organizationCode
                    )
                )
                .FirstOrDefaultAsync();

            return result;
        }


        //public async Task<List<FieldValidationDto>> GetFieldValidationsAsync(long datasetId)
        //    => await _db.FieldValidations
        //        .Where(v => v.DatasetId == datasetId)
        //        .OrderByDescending(v => v.OccurredAt)
        //        .Select(v => new FieldValidationDto
        //        {
        //            FieldKey = v.FieldKey,
        //            FieldName = v.FieldName,
        //            ValidationMessage = v.Message,
        //            OccurredAt = v.OccurredAt
        //        }).ToListAsync();

        public async Task<List<DatasetRecordDto>> GetRecentRecordsAsync(long datasetId, int limit = 5)
        {
            var records = await _db.MandERecords
                .Where(r => r.MandEDatasetId == datasetId)
                .OrderByDescending(r => r.CreatedOn)
                .Take(limit)
                .ToListAsync();

            return records.Select(r => new DatasetRecordDto
            {
                Id = r.Id,
                CreatedOn = r.CreatedOn,
                Values = JsonSerializer.Deserialize<Dictionary<string, object>>(r.DataJson)
            }).ToList();
        }

        //public async Task<List<ActivityLogDto>> GetActivityLogAsync(long datasetId, long limit = 10)
        //    => await _db.ActivityLogs
        //        .Where(a => a.DatasetId == datasetId)
        //        .OrderByDescending(a => a.Timestamp)
        //        .Take(limit)
        //        .Select(a => new ActivityLogDto
        //        {
        //            LogId = a.Id,
        //            Action = a.Action,
        //            PerformedBy = a.PerformedBy,
        //            Timestamp = a.Timestamp
        //        }).ToListAsync();

    }

}
