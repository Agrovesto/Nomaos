//using Agrovesto.Backend.Core.Interfaces.Repositories;
//using Agrovesto.Backend.Core.Models.Market;
//using Agrovesto.Backend.Infrastructure.DB;
//using Agrovesto.Backend.Infrastructure.Entities.Common;
//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
//{
//    public interface IMarketRepository
//    {
//        Task<MarketDto?> GetByIdAsync(long marketId);
//        Task<List<MarketDto>> GetByStateAsync(long stateId);
//        Task<List<MarketDto>> GetByLgaAsync(long lgaId);

//        Task AddAsync(Market market);
//        Task UpdateAsync(Market market);
//        Task SaveChangesAsync();
//    }

//    public class MarketRepository : IMarketRepository
//    {
//        private readonly DataContext _context;

//        public MarketRepository(DataContext context)
//        {
//            _context = context;
//        }

//        public async Task AddAsync(Market market)
//        {
//            await _context.Markets.AddAsync(market);
//        }

//        public async Task<MarketDto?> GetByIdAsync(long marketId)
//        {
//            var query = await _context.Markets
//            .Where(x => x.Id == marketId)
//            .Select(x => new MarketDto
//            {
//                Id = x.Id,
//                Name = x.Name,
//                //MarketCommodities = x.MarketCommodities.Select(mc => new MarketCommodityDto
//                //{
//                //    CommodityId=mc.CommodityId,
//                //    CommodityName=mc.Commodity.Name,
//                //    MarketId=mc.MarketId
//                //}).ToList(),
//                EstimatedCapacityKg = x.EstimatedCapacityKg,
//                Latitude=x.Latitude,
//                Longitude=x.Longitude,
//                LgaId=x.LgaId,
//                StateName=x.State.State,
//                LgaName=x.Lga.Name,
//                Location=x.Location,
//                StateId=x.StateId,
//                MarketType=x.MarketType.ToString(),
//                MarketDays = x.MarketDays.Select(md => new MarketDayDto
//                {
//                   Id=md.Id,
//                   DayOfWeek=md.DayOfWeek.ToString(),
//                   CycleDay=md.CycleDay,
//                   DayType=md.DayType.ToString(),
//                   IsActive=md.IsActive,
//                   MarketId=md.MarketId
//                }).ToList()
//            }).FirstOrDefaultAsync();

//            return query;
//        }

//        public async Task<List<MarketDto>> GetByLgaAsync(long lgaId)
//        {
//           var query = await _context.Markets
//            .Where(x => x.LgaId == lgaId)
//            .Select(x => new MarketDto
//            {
//                Id = x.Id,
//                Name = x.Name,
//                MarketType = x.MarketType.ToString(),
//                StateId = x.StateId,
//                LgaId = x.LgaId,
//                Location = x.Location
//            })
//            .ToListAsync();
//            return query;
//        }

//        public async Task<List<MarketDto>> GetByStateAsync(long stateId)
//        {
//           var query = await _context.Markets
//            .Where(x => x.StateId == stateId)
//            .Select(x => new MarketDto
//            {
//                Id = x.Id,
//                Name = x.Name,
//                MarketType = x.MarketType.ToString(),
//                StateId = x.StateId,
//                LgaId = x.LgaId,
//                Location = x.Location,
//                EstimatedCapacityKg = x.EstimatedCapacityKg
//            })
//            .ToListAsync(); 
//            return query;
//        }

//        public async Task SaveChangesAsync()
//        {
//            await _context.SaveChangesAsync();
//        }

//        public Task UpdateAsync(Market market)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}
