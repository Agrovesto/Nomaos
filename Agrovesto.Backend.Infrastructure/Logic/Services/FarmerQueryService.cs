using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.DB;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Services
{
    public interface IFarmerQueryService
    {
        IQueryable<Farmer> Build(FarmerFilter filter);
    }
    public class FarmerQueryService : IFarmerQueryService
    {
        private readonly DataContext _context;

        public FarmerQueryService(DataContext context)
        {
            _context = context;
        }

        public IQueryable<Farmer> Build(FarmerFilter filter)
        {
            var query = _context.Farmers.AsNoTracking()
                .Where(f =>
                    _context.FarmerCooperatives.Any(fc =>
                        fc.FarmerId == f.Id &&
                        fc.CooperativeId == filter.CooperativeId));
            if (!string.IsNullOrWhiteSpace(filter.BusinessCode))
            {
                query = query.Where(f => f.BelongToBusinessCode == filter.BusinessCode);
            }

            if (!string.IsNullOrWhiteSpace(filter.State))
            {
                query = query.Where(f => f.State == filter.State);
            }

            if (filter.Id.HasValue && filter.Id > 0)
            {
                query = query.Where(f => f.Id == filter.Id);
            }

            if (filter.StartDate.HasValue || filter.EndDate.HasValue)
            {
                var start = filter.StartDate?.Date ?? DateTime.MinValue.Date;
                var end = filter.EndDate?.Date ?? DateTime.UtcNow.Date;

                query = query.Where(f =>
                    f.OnBoardedOn.Date >= start &&
                    f.OnBoardedOn.Date <= end);
            }

            if (!string.IsNullOrWhiteSpace(filter.OrganizationCode))
            {
                query = query.Where(f =>
                    _context.FarmerOrganizations.Any(fo =>
                        fo.FarmerId == f.Id &&
                        fo.OrganizationCode == filter.OrganizationCode));
            }

            return query.Distinct();
        }
    }
}
