using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Utilities;
using Microsoft.EntityFrameworkCore;
using PhoneNumbers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Repositories
{
    public class MembershipRepository : IMembershipRepository
    {
        private readonly DataContext _context;

        public MembershipRepository(DataContext context)
        {
            _context = context;
        }
        public async Task<MemberShipTypeModel?> GetMemberShipTypeById(long id)
        {
            var type = await _context.MemberShipTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (type == null)
                return null;

            var response = type.Map();
            return response;
        }


        public async Task<List<MemberShipTypeModel>> GetMembershipTypes(BusinessRegistrationArgs args) 
        {

            var type = await _context.MemberShipTypes.Where(m=>m.BusinessCode==args.businessCode).ToListAsync();
            if (type.Count > 0)
            {
                return type.Select(x => x.Map()).ToList();
            }
            return null;
        }
        public async Task<(bool status, MemberShipTypeModel data)> CreateUpdateMemberShipType(MemberShipTypeModel data, BusinessRegistrationArgs args)
        {
            try
            {
                var existingType = await _context.MemberShipTypes
                    .FirstOrDefaultAsync(x => x.Id == data.Id);

                if (existingType != null)
                {
                    if (!string.IsNullOrWhiteSpace(data.Name))
                        existingType.Name = data.Name;

                    if (!string.IsNullOrWhiteSpace(data.Description))
                        existingType.Description = data.Description;

                    existingType.UpdatedOn = DateTimeOffset.Now;
                    existingType.UpdatedBy = args.UserId;
                    _context.MemberShipTypes.Update(existingType);
                    await _context.SaveChangesAsync();

                    return (true, existingType.Map());
                }

                var entity = data.Map();
                entity.CreatedBy = args.UserId;
                entity.BusinessCode = args.businessCode;
                entity.CreatedOn= DateTimeOffset.Now;
                await _context.MemberShipTypes.AddAsync(entity);
                await _context.SaveChangesAsync();

                return (true, entity.Map());
            }
            catch (BaseException ex)
            {
                throw new BaseException(
                    Core.Enums.ExceptionType.OPERATION_FAILED,
                    ex.Message);
            }
            catch (Exception ex)
            {
                throw new NotSuccessfulException(
                    $"Could not save agent details: {ex.Message}");
            }
        }

        public async Task<(bool status, MemberShipModel data)> CreateUpdateMembership(MemberShipModel data)
        {
            try
            {
                var existingCooperative = await _context.MemberShips
                    .FirstOrDefaultAsync(x => x.UserId == data.UserId);

                if (existingCooperative != null)
                {
                    if (!string.IsNullOrWhiteSpace(data.State))
                        existingCooperative.State = data.State;

                    if (!string.IsNullOrWhiteSpace(data.Lga))
                        existingCooperative.Lga = data.Lga;

                    if (!string.IsNullOrWhiteSpace(data.ContactPerson))
                        existingCooperative.ContactPerson = data.ContactPerson;

                    if (!string.IsNullOrWhiteSpace(data.Email))
                        existingCooperative.Email = data.Email;

                    if (!string.IsNullOrWhiteSpace(data.Location))
                        existingCooperative.Location = data.Location;

                    if (!string.IsNullOrWhiteSpace(data.PhoneNumber))
                        existingCooperative.PhoneNumber = data.PhoneNumber;

                    if (!string.IsNullOrWhiteSpace(data.MembershipLeaderFirstName))
                        existingCooperative.MembershipLeaderFirstName = data.MembershipLeaderFirstName;

                    if (!string.IsNullOrWhiteSpace(data.MembershipLeaderGender))
                        existingCooperative.MembershipLeaderGender = data.MembershipLeaderGender;

                    if (!string.IsNullOrWhiteSpace(data.MembershipLeaderMiddleName))
                        existingCooperative.MembershipLeaderMiddleName = data.MembershipLeaderMiddleName;

                    if (!string.IsNullOrWhiteSpace(data.MembershipLeaderLastName))
                        existingCooperative.MembershipLeaderLastName = data.MembershipLeaderLastName;

                    if (!string.IsNullOrWhiteSpace(data.Name))
                        existingCooperative.Name = data.Name;

                    if (!string.IsNullOrWhiteSpace(data.IsAssignedToAgentId))
                        existingCooperative.IsAssignedToAgentId = data.IsAssignedToAgentId;

                    if (!string.IsNullOrWhiteSpace(data.Ward))
                        existingCooperative.Ward = data.Ward;

                    _context.MemberShips.Update(existingCooperative);
                    await _context.SaveChangesAsync();

                    return (true, existingCooperative.Map());
                }

                var entity = data.Map();

                await _context.MemberShips.AddAsync(entity);
                await _context.SaveChangesAsync();

                return (true, entity.Map());
            }
            catch (BaseException ex)
            {
                throw new BaseException(
                    Core.Enums.ExceptionType.OPERATION_FAILED,
                    ex.Message);
            }
            catch (Exception ex)
            {
                throw new NotSuccessfulException(
                    $"Could not save agent details: {ex.Message}");
            }
        }

        public async Task<(bool status, MemberShipModel data)> CreateCooperative(MemberShipModel data)
        {
            var existingCooperative = await _context.MemberShips
                 .FirstOrDefaultAsync(x => x.UserId == data.UserId);

            if (existingCooperative != null)
            {
                return (true, existingCooperative.Map());
            }

            var entity =data.Map();

            await _context.AddAsync(entity);
            await _context.SaveChangesAsync();

            return (true, entity.Map());
        }

        public async Task<MemberShipModel> GetCooperativeDetailByUserId(string userId)
        {
            var cooperative = await _context.MemberShips.Where(x => x.UserId == userId).FirstOrDefaultAsync();
            if (cooperative == null)
            {
                return null;
            }
            return cooperative.Map();
        }
        public async Task<MemberShipModel?> GetCooperativeDetailById(long id)
        {
            var cooperative = await _context.MemberShips
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (cooperative == null)
                return null;

            // Fetch wallet sequentially (safe)
            var wallet = await _context.Wallets
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.UserId == cooperative.UserId);

            // Fetch farmers belonging to this cooperative via FarmerCooperatives
            var farmers = await (
                from f in _context.Farmers.AsNoTracking()
                join fc in _context.FarmerCooperatives.AsNoTracking()
                    on f.Id equals fc.FarmerId
                where fc.CooperativeId == cooperative.Id
                select f
            ).ToListAsync();

            // Map results
            var response = cooperative.Map();
            response.Wallet = wallet?.Map();
            response.Farmers = farmers?.Select(f => f.Map()).ToList() ?? new List<FarmerModel>();

            return response;
        }


        public async Task<List<MemberShipModel>> GetCooperatives() //restricted to agrovesto
        {

            var cooperative = await _context.MemberShips.ToListAsync();
            if (cooperative.Count > 0)
            {
                return cooperative.Select(x => x.Map()).OrderByDescending(x => x.OnBoardedOn).ToList();
            }
            return null;
        }

        public async Task<List<MemberShipModel>> GetCooperativesByBusinessCode(string businessCode, ListQueryParams query)
        {
            var cooperatives = _context.MemberShips
                .Where(x => x.BelongToBusinessCode == businessCode)
                .AsQueryable();
            if (!string.IsNullOrEmpty(query.organizationCode))
            {
                cooperatives = cooperatives.Where(x => x.OrganizationCode == query.organizationCode);
            }

            var result = await cooperatives
       .OrderByDescending(s => s.OnBoardedOn)
       .Skip(query.PageNumber)
       .Take(query.PageSize)
        .Select(s => s.Map()).ToListAsync();


            if (result.Count > 0)
            {
                return result;
            }
            return null;
        }
        public async Task<List<MemberShipModel>> GetCooperativeByAgent(string agentId, ListQueryParams query)
        {
            // Base query: cooperatives assigned to this agent via join table
            var cooperativesQuery = _context.MemberShips
             .AsNoTracking()
             .Where(s => s.IsAssignedToAgentId == agentId || s.OnboardedById == agentId);


            // Filter by organization code
            if (!string.IsNullOrWhiteSpace(query.organizationCode))
            {
                cooperativesQuery = cooperativesQuery.Where(s => s.OrganizationCode == query.organizationCode);
            }

            var cooperatives = await cooperativesQuery
                .OrderByDescending(s => s.OnBoardedOn)
                .Select(s => new MemberShipModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Location = s.Location,
                    State = s.State,
                    Lga = s.Lga,
                    OnBoardedOn = s.OnBoardedOn,
                    BelongToBusinessCode = s.BelongToBusinessCode,
                    ContactPerson = s.ContactPerson,
                    Email = s.Email,
                    PhoneNumber = s.PhoneNumber,
                    UserId = s.UserId,
                    IsLeaderAFarmer = s.IsLeaderAFarmer,
                    Tier=EnumExtensions.GetDisplayName(s.Tier),
                    MembershipType= s.MemberShipType.Name,
                    
                    // Count farmers via FarmerCooperatives
                    TotalFarmers = _context.FarmerCooperatives.Count(fc => fc.CooperativeId == s.Id)
                })
                .ToListAsync();

            return cooperatives.Any() ? cooperatives : new List<MemberShipModel>();
        }

      

        public async Task<AgentModel> AssignSupervisorToAgent(string agentId, string supervisorId)
        {
            var agent = await _context.Agents.Where(c => c.UserId == agentId).FirstOrDefaultAsync();
            if (agent == null) return null;
            agent.SupervisorUserId = supervisorId;

            await _context.SaveChangesAsync();

            return agent.Map();
        }

        public async Task<MemberShipModel> AssignCooperativeToAgent(string agentId, long cooperativeId)
        {
            var cooperative = await _context.MemberShips.Where(c => c.Id == cooperativeId).FirstOrDefaultAsync();
            if (cooperative == null) return null;
            cooperative.IsAssignedToAgentId = agentId;
            cooperative.HasbeenAssignedAgent = true;
            await _context.SaveChangesAsync();

            return cooperative.Map();
        }
        public async Task<List<FarmerModel>> AssignFarmerToAgent(string agentUserId, long cooperativeId)
        {
            var agent= await _context.Agents.Where(c => c.UserId == agentUserId).FirstOrDefaultAsync();
            // Get all farmers in this cooperative via FarmerCooperatives
            var farmers = await (
                from f in _context.Farmers.AsNoTracking()
                join fc in _context.FarmerCooperatives.AsNoTracking()
                    on f.Id equals fc.FarmerId
                where fc.CooperativeId == cooperativeId
                select f
            ).ToListAsync();

            if (!farmers.Any())
                return new List<FarmerModel>();

            foreach (var farmer in farmers)
            {
                // Check if the assignment already exists
                var alreadyAssigned = await _context.FarmerAgents
                    .AnyAsync(fa => fa.FarmerId == farmer.Id && fa.AgentId == agent.Id);

                if (!alreadyAssigned)
                {
                    // Assign agent
                    await _context.FarmerAgents.AddAsync(new FarmerAgent
                    {
                        FarmerId = farmer.Id,
                        AgentId = agent.Id,
                        AssignedOn = DateTimeOffset.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();

            // Return mapped farmers
            return farmers.Select(f => f.Map()).ToList();
        }

        public async Task<AgentModel> ViewAgentAssignedToCooperative(long cooperativeId)
        {
            var agent =
                await (from c in _context.MemberShips
                       join a in _context.Agents
                       on c.IsAssignedToAgentId equals a.UserId
                       where c.Id == cooperativeId
                       select a).FirstOrDefaultAsync();

            if (agent != null) { return agent.Map(); }
            return null;
            
        }

        public async Task<List<AgentModel>> AgentsBySupervisor(string supervisorId, ListQueryParams query)
        {
            var agentsQuery = _context.Agents
                .AsNoTracking()
                .Where(s => s.SupervisorUserId == supervisorId);

            if (!string.IsNullOrWhiteSpace(query.organizationCode))
            {
                agentsQuery = agentsQuery.Where(s => s.OrganizationCode == query.organizationCode);
            }

            var agents = await agentsQuery
                .OrderByDescending(s => s.OnBoardedOn)
                //.Skip((query.PageNumber - 1) * query.PageSize)
                //.Take(query.PageSize)
                .Select(s => new AgentModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Location = s.Location,
                    State = s.State,
                    Lga = s.Lga,
                    OnBoardedOn = s.OnBoardedOn,
                    BelongToBusinessCode = s.BelongToBusinessCode,
                    Email = s.Email,
                    PhoneNumber = s.PhoneNumber,
                    OrganizationCode = s.OrganizationCode,
                    Ward = s.Ward,
                    SupervisorUserId = s.SupervisorUserId,
                    UserId = s.UserId
                })
                .ToListAsync();

            return agents.Any() ? agents : null;
        }


    }
}

