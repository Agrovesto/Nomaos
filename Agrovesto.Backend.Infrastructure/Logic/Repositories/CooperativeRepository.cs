using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using PhoneNumbers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
{
    public class CooperativeRepository : ICooperativeRepository
    {
        private readonly DataContext _context;

        public CooperativeRepository(DataContext context)
        {
            _context = context;
        }
        public async Task<(bool status, CooperativeModel data)> CreateCooperative(CooperativeModel data)
        {
            var entity =data.Map();

            await _context.AddAsync(entity);
            await _context.SaveChangesAsync();

            return (true, entity.Map());
        }

        public async Task<CooperativeModel> GetCooperativeDetailByUserId(string userId)
        {
            var cooperative = await _context.Cooperatives.Where(x => x.UserId == userId).FirstOrDefaultAsync();
            if (cooperative == null)
            {
                return null;
            }
            return cooperative.Map();
        }

        public async Task<CooperativeModel> GetCooperativeDetailById(long Id)
        {
            var cooperative = await _context.Cooperatives.Where(x => x.Id == Id).FirstOrDefaultAsync();
            if (cooperative == null)
            {
                return null;
            }
            return cooperative.Map();
        }
        public async Task<List<CooperativeModel>> GetCooperatives() //restricted to agrovesto
        {

            var cooperative = await _context.Cooperatives.ToListAsync();
            if (cooperative.Count > 0)
            {
                return cooperative.Select(x => x.Map()).OrderByDescending(x => x.OnBoardedOn).ToList();
            }
            return null;
        }

        public async Task<List<CooperativeModel>> GetCooperativesByBusinessCode(string businessCode, ListQueryParams query)
        {
            var cooperatives = await _context.Cooperatives
                .AsNoTracking()
                .Where(s => s.BelongToBusinessCode == businessCode)
                .OrderByDescending(s => s.OnBoardedOn)
                //.Skip(query.PageNumber)
                //.Take(query.PageSize)
                .Select(s => new CooperativeModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Location = s.Location,
                    State = s.State,
                    Lga = s.Lga,
                    OnBoardedOn = s.OnBoardedOn,
                    BelongToBusinessCode =s.BelongToBusinessCode,
                    ContactPerson=s.ContactPerson,
                    Email=s.Email,
                    PhoneNumber=s.PhoneNumber,
                    UserId=s.UserId,
                    HasbeenAssignedAgent=s.HasbeenAssignedAgent,
                    IsAssignedToAgentId=s.IsAssignedToAgentId,
                    TotalFarmers = _context.Farmers.Count(f => f.BelongToCooperativeId == s.Id)
                })
                .ToListAsync();

            return cooperatives;
        }

        public async Task<List<CooperativeModel>> GetCooperativeByAgent(string AgentId, ListQueryParams query)
        {
            var cooperatives = await _context.Cooperatives
                .AsNoTracking()
                .Where(s => s.IsAssignedToAgentId == AgentId)
                .OrderByDescending(s => s.OnBoardedOn)
                //.Skip(query.PageNumber)
                //.Take(query.PageSize)
                .Select(s => new CooperativeModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Location = s.Location,
                    State = s.State,
                    Lga = s.Lga,
                    IsAssignedToAgentId = s.IsAssignedToAgentId,
                    OnBoardedOn = s.OnBoardedOn,
                    BelongToBusinessCode=s.BelongToBusinessCode,
                    ContactPerson=s.ContactPerson,
                    Email=s.Email,
                    HasbeenAssignedAgent=s.HasbeenAssignedAgent,
                    PhoneNumber=s.PhoneNumber,
                    UserId=s.UserId,
                    TotalFarmers = _context.Farmers.Count(f => f.BelongToCooperativeId == s.Id) // Count farmers per cooperative
                })
                .ToListAsync();

            return cooperatives.Any() ? cooperatives : null;
        }
        //public async Task<List<CooperativeModel>> GetCooperativesByBusinessCode(string businessCode, ListQueryParams query)
        //{

        //    var agents = await _context.Cooperatives
        //        .AsNoTracking()
        //        .Where(s
        //        =>
        //        //(!string.IsNullOrEmpty(query.Location) && (s.Location.Contains(query.Location) || s.State.Contains(query.Location)
        //        // || s.Lga.Contains(query.Location))) &&
        //        s.BelongToBusinessCode == businessCode)
        //        .OrderByDescending(s => s.OnBoardedOn)
        //        .Skip(query.PageNumber)
        //        .Take(query.PageSize)
        //         .Select(s => s.Map()).ToListAsync();


        //    if (agents.Count > 0)
        //    {
        //        return agents;
        //    }
        //    return null;
        //}


        //public async Task<List<CooperativeModel>> GetCooperativeByAgent(long AgentId, ListQueryParams query)
        //{

        //    var cooperatives = await _context.Cooperatives
        //        .AsNoTracking()
        //        .Where(s
        //        =>
        //        //(!string.IsNullOrEmpty(query.Location) && (s.Location.Contains(query.Location) || s.State.Contains(query.Location)
        //        // || s.Lga.Contains(query.Location))) &&
        //        s.IsAssignedToAgentId == AgentId)
        //        .OrderByDescending(s => s.OnBoardedOn)
        //        .Skip(query.PageNumber)
        //        .Take(query.PageSize)
        //         .Select(s => s.Map()).ToListAsync();


        //    if (cooperatives.Count > 0)
        //    {
        //        return cooperatives;
        //    }
        //    return null;
        //}

        public async Task<CooperativeModel> AssignCooperativeToAgent(string agentId, long cooperativeId)
        {
            var cooperative = await _context.Cooperatives.Where(c => c.Id == cooperativeId).FirstOrDefaultAsync();
            if (cooperative == null) return null;
            cooperative.IsAssignedToAgentId = agentId;
            cooperative.HasbeenAssignedAgent = true;
            await _context.SaveChangesAsync();

            return cooperative.Map();
        }
        public async Task<List<FarmerModel>> AssignFarmerToAgent(string agentUserId, long cooperativeId)
        {
            var farmers = await _context.Farmers
                .Where(f => f.BelongToCooperativeId == cooperativeId)
                .ToListAsync();

            if (!farmers.Any())
                return null;

            foreach (var farmer in farmers)
            {
                farmer.OnboardedByUserId = agentUserId;
            }

            await _context.SaveChangesAsync();

            return farmers.Select(x=>x.Map()).ToList();

        }

        public async Task<AgentModel> ViewAgentAssignedToCooperative(long cooperativeId)
        {
            var agent =
                await (from c in _context.Cooperatives
                       join a in _context.Agents
                       on c.IsAssignedToAgentId equals a.UserId
                       where c.Id == cooperativeId
                       select a).FirstOrDefaultAsync();

            if (agent != null) { return agent.Map(); }
            return null;
            
        }
    }
}

