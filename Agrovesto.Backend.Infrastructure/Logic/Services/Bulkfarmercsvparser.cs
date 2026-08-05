
using Agrovesto.Backend.Core.Models.Businesses;
using CsvHelper;
using CsvHelper.Configuration;
using ExcelDataReader;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Agrovesto.Backend.Infrastructure.Logic.Services
{
    public static class BulkFarmerCsvParser
    {
        private const int MaxFlatValueChains = 10;

        public static List<BulkFarmerRowDto> Parse(Stream stream, string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            return extension switch
            {
                ".csv" => ParseCsv(stream),
                ".xlsx" or ".xls" => ParseExcel(stream),
                _ => throw new NotSupportedException($"Unsupported file type '{extension}'.")
            };
        }

        private static List<BulkFarmerRowDto> ParseCsv(Stream stream)
        {
            stream.Position = 0;

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
                TrimOptions = TrimOptions.Trim
            };

            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, config);

            csv.Read();
            csv.ReadHeader();

            ValidateTemplate(csv.HeaderRecord!);

            var rows = new List<BulkFarmerRowDto>();
            var rowIndex = 1;

            while (csv.Read())
            {
                string? Get(int index)
                {
                    if (index >= csv.HeaderRecord!.Length)
                        return null;

                    return csv.GetField(index)?.Trim();
                }

                rows.Add(BuildRow(Get, rowIndex++));
            }

            return rows;
        }
        private static List<BulkFarmerRowDto> ParseExcel(Stream stream)
        {
            stream.Position = 0;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var reader = ExcelReaderFactory.CreateReader(stream);

            var ds = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                {
                    UseHeaderRow = true
                }
            });

            if (ds.Tables.Count == 0)
                throw new InvalidOperationException("No worksheet found.");

            var table = ds.Tables[0];

            ValidateTemplate(
                table.Columns.Cast<DataColumn>()
                    .Select(x => x.ColumnName));

            var rows = new List<BulkFarmerRowDto>();

            var rowIndex = 1;

            foreach (DataRow dr in table.Rows)
            {
                if (dr.ItemArray.All(x =>
                    x == DBNull.Value ||
                    string.IsNullOrWhiteSpace(x?.ToString())))
                    continue;

                string? Get(int index)
                {
                    if (index >= table.Columns.Count)
                        return null;

                    var value = dr[index];

                    if (value == DBNull.Value)
                        return null;

                    if (value is DateTime dt)
                        return dt.ToString("dd-MM-yyyy");

                    return value.ToString()?.Trim();
                }

                rows.Add(BuildRow(Get, rowIndex++));
            }

            return rows;
        }
        private static BulkFarmerRowDto BuildRow(
     Func<int, string?> get,
     int rowIndex)
        {
            var row = new BulkFarmerRowDto();

            row.RowIndex = rowIndex;

            row.Name = string.Join(" ",
                new[]
                {
            get(FarmerColumns.FirstName),
            get(FarmerColumns.MiddleName),
            get(FarmerColumns.LastName)
                }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

            row.DateOfBirth = get(FarmerColumns.DateOfBirth);
            row.Gender = get(FarmerColumns.Gender);
            row.State = get(FarmerColumns.State);
            row.Lga = get(FarmerColumns.Lga);
            row.Ward = get(FarmerColumns.Ward);
            row.Location = get(FarmerColumns.Address);
            row.PhoneNumber = get(FarmerColumns.PhoneNumber) ?? "";
            row.Email = get(FarmerColumns.Email);
            row.FarmerCategory = get(FarmerColumns.FarmerCategory);
            row.HighestLevelOfEducation = get(FarmerColumns.Education);
            row.BVN = get(FarmerColumns.BVN);
            row.NIN = get(FarmerColumns.NIN);

            var years = get(FarmerColumns.YearsInAgric);

            if (!string.IsNullOrWhiteSpace(years))
            {
                var digits = new string(years.Where(char.IsDigit).ToArray());

                if (long.TryParse(digits, out var value))
                    row.YearsInAgric = value;
            }

            var part = get(FarmerColumns.AgricPart);
            var products = get(FarmerColumns.Products);

            row.SelectedItems = ParseNamedValueChains(part, products);

            return row;
        }
        private static List<ValueChainSelectionDto> ParseNamedValueChains(
    string? valueChain,
    string? products)
        {
            if (string.IsNullOrWhiteSpace(valueChain))
                return new();

            var items = (products ?? "")
                .Split(new[] { ",", ";" },
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (items.Count == 0)
                return new();

            return new List<ValueChainSelectionDto>
    {
        new ValueChainSelectionDto
        {
            ValueChainType = valueChain,
            Items = items
        }
    };
        }

        private static void ValidateTemplate(IEnumerable<string> headers)
        {
            var list = headers.ToList();

            if (list.Count < FarmerColumns.TotalColumns)
                throw new InvalidOperationException(
                    $"Expected at least {FarmerColumns.TotalColumns} columns.");

            var expected = new[]
            {
        "FirstName",
        "MiddleName",
        "LastName",
        "DateOfBirth",
        "Gender",
        "State",
        "Local Government",
        "Ward",
        "Residential Address",
        "PhoneNumber",
        "Email",
        "Category",
        "Agriculture",
        "Products",
        "Agribusiness",
        "Education",
        "BVN",
        "NIN"
    };

            for (var i = 0; i < expected.Length; i++)
            {
                var actual = Normalize(list[i]);

                if (!actual.Contains(Normalize(expected[i])))
                {
                    throw new InvalidOperationException(
                        $"Invalid template. Expected '{expected[i]}' in column {(char)('A' + i)}.");
                }
            }
        }

        private static string Normalize(string value)
        {
            value = Regex.Replace(value ?? "", @"([a-z])([A-Z])", "$1 $2");

            return Regex.Replace(
                    value.ToLowerInvariant(),
                    @"[^a-z0-9]+",
                    " ")
                .Trim();
        }
        private static List<ValueChainSelectionDto> ParseJsonValueChains(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new();

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<List<ValueChainSelectionDto>>(json, options)
                       ?? new();
            }
            catch
            {
                return new();
            }
        }

        private static List<ValueChainSelectionDto> ParseFlatValueChains(Func<string, string?> getField)
        {
            var selections = new List<ValueChainSelectionDto>();

            for (int n = 1; n <= MaxFlatValueChains; n++)
            {
                var categoryIdStr = getField($"valuechain{n}_categoryid");

                if (string.IsNullOrWhiteSpace(categoryIdStr))
                    continue;

                if (!long.TryParse(categoryIdStr, out var categoryId) || categoryId == 0)
                    continue;

                var typeName = getField($"valuechain{n}_typename") ?? string.Empty;

                var itemsRaw = getField($"valuechain{n}_itemname") ?? string.Empty;

                var items = itemsRaw
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                if (items.Count == 0)
                    continue;

                selections.Add(new ValueChainSelectionDto
                {
                    ValueChainType = typeName,
                    Items = items
                });
            }

            return selections;
        }
    }

    public static class FarmerColumns
    {
        public const int FirstName = 0;      // A
        public const int MiddleName = 1;     // B
        public const int LastName = 2;       // C
        public const int DateOfBirth = 3;    // D
        public const int Gender = 4;         // E
        public const int State = 5;          // F
        public const int Lga = 6;            // G
        public const int Ward = 7;           // H
        public const int Address = 8;        // I
        public const int PhoneNumber = 9;    // J
        public const int Email = 10;         // K
        public const int FarmerCategory = 11;// L
        public const int AgricPart = 12;     // M
        public const int Products = 13;      // N
        public const int YearsInAgric = 14;  // O
        public const int Education = 15;     // P
        public const int BVN = 16;           // Q
        public const int NIN = 17;           // R

        public const int TotalColumns = 18;
    }
}