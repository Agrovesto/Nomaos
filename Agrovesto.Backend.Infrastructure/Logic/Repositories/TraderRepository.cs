using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.DB;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
{
   

    public class TraderRepository : ITraderRepository
    {
        private readonly DataContext _context;

        public TraderRepository(DataContext context)
        {
            _context = context;
        }
        public async Task<(bool status, TraderModel data)> CreateTrader(TraderModel data)
        {
            try
            {
         
                var existingTrader = await _context.Traders
                    .FirstOrDefaultAsync(x =>x.UserId == data.UserId);

                if (existingTrader != null)
                {
                    // Trader already exists, return the existing one
                    return (true, existingTrader.Map());
                }

                // Trader doesn't exist, create new one
                var entity = data.Map();

                await _context.AddAsync(entity);
                await _context.SaveChangesAsync();

                return (true, entity.Map());
            }
            catch (BaseException ex)
            {
                throw new BaseException(Core.Enums.ExceptionType.OPERATION_FAILED, ex.Message);
            }
            catch (Exception ex)
            {
                throw new NotSuccessfulException($"Could not save Trader details: {ex.Message}");
            }
        }

        //public async Task<(bool status, TraderModel data)> CreateTrader(TraderModel data)
        //{
        //    try
        //    {
        //        var exist= await _context.Traders.
        //        var entity = data.Map();

        //        await _context.AddAsync(entity);
        //        await _context.SaveChangesAsync();

        //        return (true, entity.Map());
        //    }
        //    catch (BaseException ex)
        //    {
        //        throw new BaseException(Core.Enums.ExceptionType.OPERATION_FAILED, ex.Message);
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new NotSuccessfulException($"Could not save Trader details: {ex.Message}");
        //    }
        //}

        public async Task<TraderModel> GetTraderDetailByUserId(string userId)
        {
            var Trader = await _context.Traders.Where(x => x.UserId == userId).FirstOrDefaultAsync();
            if (Trader == null)
            {
                return null;
            }
            return Trader.Map();
        }

        public async Task<TraderModel> GetTraderDetailById(long Id)
        {
            var Trader = await _context.Traders.Where(x => x.Id == Id).FirstOrDefaultAsync();
            if (Trader == null)
            {
                return null;
            }
            return Trader.Map();
        }
        public async Task<List<TraderModel>> GetTraders() //restricted to agrovesto
        {

            var Trader = await _context.Traders.ToListAsync();
            if (Trader.Count > 0)
            {
                return Trader.Select(x => x.Map()).OrderByDescending(x => x.OnBoardedOn).ToList();
            }
            return null;
        }


        public async Task<List<TraderModel>> GetTradersByBusinessCode(string businessCode, ListQueryParams query)
        {

            var Traders = await _context.Traders
                .AsNoTracking()
                .Where(s
                =>
                //(!string.IsNullOrEmpty(query.Location) && (s.Location.Contains(query.Location) || s.State.Contains(query.Location)
                //|| s.Lga.Contains(query.Location))) 
                //  &&
                s.BelongToBusinessCode == businessCode)
                .OrderByDescending(s => s.OnBoardedOn)
                .Skip(query.PageNumber)
                .Take(query.PageSize)
                 .Select(s => s.Map()).ToListAsync();


            if (Traders.Count > 0)
            {
                return Traders;
            }
            return null;

            //var Trader = await _context.Traders.Where(x => x.BelongToBusinessCode == businessCode).ToListAsync();
            //if (Traders.Count > 0)
            //{
            //    return Trader.Select(x => x.Map()).OrderByDescending(x => x.OnBoardedOn).ToList();
            //}
            //return null;
        }


    }
}

