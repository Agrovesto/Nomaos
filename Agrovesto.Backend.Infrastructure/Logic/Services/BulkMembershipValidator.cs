using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.V2.Onboarding;
using Agrovesto.Backend.Infrastructure.DB;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Services
{
    public class BulkMembershipValidator
    {
        private readonly DataContext _db;

        public BulkMembershipValidator(DataContext db) => _db = db;

        public async Task<List<RowError>> ValidateAllAsync(
            List<BulkMembershipRowDto> rows, OnBoardingArgs args)
        {
            var errors = new List<RowError>();

            var validTypes = (await _db.MemberShipTypes
     .Where(t => t.BusinessCode == args.businessCode)
     .Select(t => t.Name)
     .ToListAsync())
     .ToHashSet(StringComparer.OrdinalIgnoreCase);


            foreach (var row in rows)
            {
                var rowErrors = new List<string>();

                if (string.IsNullOrWhiteSpace(row.MembershipName))
                    rowErrors.Add("MembershipName is required.");

                if (string.IsNullOrWhiteSpace(row.MembershipType))
                    rowErrors.Add("MembershipType is required.");
                else if (!validTypes.Contains(row.MembershipType.ToLower()))
                    rowErrors.Add($"MembershipType '{row.MembershipType}' not found for this business.");

                if (string.IsNullOrWhiteSpace(row.State))
                    rowErrors.Add("State is required.");

                if (!string.IsNullOrWhiteSpace(row.LeaderPhoneNumber)
                    && !IsValidPhone(row.LeaderPhoneNumber))
                    rowErrors.Add("LeaderPhoneNumber format is invalid.");

               
                if (rowErrors.Any())
                    errors.Add(new RowError
                    {
                        RowNumber = row.RowNumber,
                        MembershipName = row.MembershipName,
                        Messages = rowErrors
                    });
            }

            return errors;
        }

        private static bool IsValidPhone(string phone) =>
            System.Text.RegularExpressions.Regex.IsMatch(phone, @"^\+?[0-9\s\-]{7,15}$");
    }
}
