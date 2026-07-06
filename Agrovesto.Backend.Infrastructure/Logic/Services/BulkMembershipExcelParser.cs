using Agrovesto.Backend.Core.Models.Businesses;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Services
{
    public interface IBulkMembershipExcelParser
    {
        List<BulkMembershipRowDto> Parse(Stream fileStream,
            string businessCode, string fileName, string onboardedById, 
            string? organizationCode = null, long? agentId = null);
    }
    public class BulkMembershipExcelParser : IBulkMembershipExcelParser
    {
        // Expected headers: S/N, MembershipName, MembershipType, State,
        //                   LGA, Location, LeaderName, LeaderPhoneNumber
        private static readonly string[] AllowedExtensions = { ".csv", ".xlsx", ".xls" };
        private const string ExpectedMembershipTypeDefault = "Cooperative"; // TODO: confirm if this should ever vary

        public List<BulkMembershipRowDto> Parse(
            Stream fileStream,
            string fileName,
            string businessCode,
            string onboardedById,
            string? organizationCode = null,
            long? agentId = null)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension))
                throw new NotSupportedException($"Unsupported file type '{extension}'.");

            var rawRows = extension switch
            {
                ".csv" => ReadCsvRows(fileStream),
                ".xlsx" or ".xls" => ReadExcelRows(fileStream),
                _ => throw new NotSupportedException($"Unsupported file type '{extension}'.")
            };

            var rows = new List<BulkMembershipRowDto>();

            // Row 1 is headers — start from row 2 (index 1)
            for (int i = 1; i < rawRows.Count; i++)
            {
                var cells = rawRows[i];
                var rowNumber = i + 1; // 1-based, matches original ClosedXML row numbering

                string Get(int columnIndex) =>
                    columnIndex < cells.Count ? cells[columnIndex]?.Trim() ?? string.Empty : string.Empty;

                var membershipName = Get(1);   // Cell(2)
                var leaderName = Get(6);       // Cell(7)

                // Skip structurally blank rows
                if (string.IsNullOrWhiteSpace(membershipName) && string.IsNullOrWhiteSpace(leaderName))
                    continue;

                rows.Add(new BulkMembershipRowDto
                {
                    RowNumber = rowNumber,
                    MembershipName = membershipName,
                    MembershipType = ExpectedMembershipTypeDefault,
                    State = Get(3),             // Cell(4)
                    LGA = Get(4),                // Cell(5)
                    Location = Get(5),           // Cell(6)
                    LeaderName = leaderName,
                    LeaderPhoneNumber = Get(7),  // Cell(8)
                });
            }

            return rows;
        }

        private static List<List<string>> ReadCsvRows(Stream csvStream)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
            };

            var rows = new List<List<string>>();

            using var reader = new StreamReader(csvStream);
            using var csv = new CsvReader(reader, config);

            while (csv.Read())
            {
                var record = new List<string>();
                int col = 0;
                while (csv.TryGetField<string>(col, out var value))
                {
                    record.Add(value ?? string.Empty);
                    col++;
                }
                rows.Add(record);
            }

            return rows;
        }

        private static List<List<string>> ReadExcelRows(Stream excelStream)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using var excelReader = ExcelReaderFactory.CreateReader(excelStream);
            var dataSet = excelReader.AsDataSet();

            if (dataSet.Tables.Count == 0)
                throw new InvalidOperationException("The Excel file contains no worksheets.");

            var table = dataSet.Tables[0];
            var rows = new List<List<string>>();

            foreach (DataRow dr in table.Rows)
            {
                var record = new List<string>();
                foreach (var value in dr.ItemArray)
                {
                    if (value == DBNull.Value)
                    {
                        record.Add(string.Empty);
                    }
                    else if (value is DateTime dt)
                    {
                        record.Add(dt.ToString("yyyy-MM-dd"));
                    }
                    else
                    {
                        record.Add(value?.ToString() ?? string.Empty);
                    }
                }
                rows.Add(record);
            }

            return rows;
        }
    
        
        //public List<BulkMembershipRowDto> Parse(
        //    Stream fileStream,
        //    string businessCode,
        //    string onboardedById,
        //    string? organizationCode = null,
        //    long? agentId = null)
        //{
        //    using var workbook = new XLWorkbook(fileStream);
        //    var sheet = workbook.Worksheets.First();
        //    var rows = new List<BulkMembershipRowDto>();

        //    // Start from row 2 — row 1 is headers
        //    foreach (var row in sheet.RowsUsed().Skip(1))
        //    {
        //        var dto = new BulkMembershipRowDto
        //        {
        //            RowNumber = row.RowNumber(),
        //            MembershipName = row.Cell(2).GetString().Trim(),
        //            MembershipType = "Cooperative",//row.Cell(3).GetString().Trim(),
        //            State = row.Cell(4).GetString().Trim(),
        //            LGA = row.Cell(5).GetString().Trim(),
        //            Location = row.Cell(6).GetString().Trim(),
        //            LeaderName = row.Cell(7).GetString().Trim(),
        //            LeaderPhoneNumber = row.Cell(8).GetString().Trim()

        //        };

        //        rows.Add(dto);
        //    }

        //    return rows;
        //}


    }
}
