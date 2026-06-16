using Agrovesto.Backend.Core.Models.Businesses;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Services
{
    public interface IBulkMembershipExcelParser
    {
        List<BulkMembershipRowDto> Parse(Stream fileStream, string businessCode, string onboardedById, string? organizationCode = null, long? agentId = null);
    }
    public class BulkMembershipExcelParser : IBulkMembershipExcelParser
    {
        // Expected headers: S/N, MembershipName, MembershipType, State,
        //                   LGA, Location, LeaderName, LeaderPhoneNumber
        public List<BulkMembershipRowDto> Parse(
            Stream fileStream,
            string businessCode,
            string onboardedById,
            string? organizationCode = null,
            long? agentId = null)
        {
            using var workbook = new XLWorkbook(fileStream);
            var sheet = workbook.Worksheets.First();
            var rows = new List<BulkMembershipRowDto>();

            // Start from row 2 — row 1 is headers
            foreach (var row in sheet.RowsUsed().Skip(1))
            {
                var dto = new BulkMembershipRowDto
                {
                    RowNumber = row.RowNumber(),
                    MembershipName = row.Cell(2).GetString().Trim(),
                    MembershipType = "Cooperative",//row.Cell(3).GetString().Trim(),
                    State = row.Cell(4).GetString().Trim(),
                    LGA = row.Cell(5).GetString().Trim(),
                    Location = row.Cell(6).GetString().Trim(),
                    LeaderName = row.Cell(7).GetString().Trim(),
                    LeaderPhoneNumber = row.Cell(8).GetString().Trim()
                  
                };

                rows.Add(dto);
            }

            return rows;
        }
    }
}
