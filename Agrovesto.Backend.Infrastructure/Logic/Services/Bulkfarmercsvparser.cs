
using Agrovesto.Backend.Core.Models.Businesses;
using CsvHelper;
using CsvHelper.Configuration;
using ExcelDataReader;
using System.Data;
using System.Globalization;
using System.Text.Json;

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

        private static List<BulkFarmerRowDto> ParseCsv(Stream csvStream)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
            };

            using var reader = new StreamReader(csvStream);
            using var csv = new CsvReader(reader, config);

            csv.Read();
            csv.ReadHeader();

            var rawHeaders = csv.HeaderRecord!.ToList();
            var headerMap = HeaderMapper.Resolve(rawHeaders);
            var unmatchedCanonicalKeys = HeaderMapper.KnownCanonicalKeys.Except(headerMap.Keys); ;
            var rows = new List<BulkFarmerRowDto>();
            int rowIndex = 1;

            while (csv.Read())
            {
                string? GetField(string canonicalKey) =>
                    headerMap.TryGetValue(canonicalKey, out var rawHeader)
                        ? csv.GetField(rawHeader)
                        : null;

                rows.Add(BuildRow(headerMap.Keys.ToHashSet(), GetField, rowIndex++));
            }

            return rows;
        }

        private static List<BulkFarmerRowDto> ParseExcel(Stream excelStream)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using var excelReader = ExcelReaderFactory.CreateReader(excelStream);
            var dataSet = excelReader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
            });

            if (dataSet.Tables.Count == 0)
                throw new InvalidOperationException("The Excel file contains no worksheets.");

            var table = dataSet.Tables[0];
            var rawHeaders = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            var headerMap = HeaderMapper.Resolve(rawHeaders);
            var unmatchedCanonicalKeys = HeaderMapper.KnownCanonicalKeys.Except(headerMap.Keys); ;
            // canonicalKey -> column index (avoids re-searching rawHeaders per row)
            var canonicalToIndex = headerMap.ToDictionary(
                kv => kv.Key,
                kv => rawHeaders.FindIndex(h => h == kv.Value));

            var rows = new List<BulkFarmerRowDto>();
            int rowIndex = 1;

            foreach (DataRow dr in table.Rows)
            {
                if (dr.ItemArray.All(v => v == DBNull.Value || string.IsNullOrWhiteSpace(v?.ToString())))
                    continue;

                string? GetField(string canonicalKey)
                {
                    if (!canonicalToIndex.TryGetValue(canonicalKey, out var idx) || idx < 0)
                        return null;

                    var value = dr[idx];
                    if (value == DBNull.Value) return null;
                    if (value is DateTime dt) return dt.ToString("dd-MM-yyyy");
                    return value.ToString();
                }

                rows.Add(BuildRow(headerMap.Keys.ToHashSet(), GetField, rowIndex++));
            }

            return rows;
        }

        private static BulkFarmerRowDto BuildRow(
           HashSet<string> canonicalHeaders,
           Func<string, string?> getField,
           int rowIndex)
        {
            var firstName = getField("firstname");
            var middleName = getField("middlename");
            var lastName = getField("lastname");

            var fullName = string.Join(" ",
                new[] { firstName, middleName, lastName }.Where(n => !string.IsNullOrWhiteSpace(n)));

            var row = new BulkFarmerRowDto
            {
                RowIndex = rowIndex,
                Name = !string.IsNullOrWhiteSpace(fullName) ? fullName : (getField("name") ?? string.Empty),
                PhoneNumber = getField("phonenumber") ?? string.Empty,
                Email = getField("email"),
                Gender = getField("gender"),
                DateOfBirth = getField("dateofbirth"),
                NIN = getField("nin"),
                BVN = getField("bvn"),
                Location = getField("location"),
                State = getField("state"),
                Lga = getField("lga"),
                Ward = getField("ward"),
                FarmerCategory = getField("farmercategory"),
                HighestLevelOfEducation = getField("highestlevelofeducation"),
            };

            var yearsRaw = getField("yearsinagric");
            if (!string.IsNullOrWhiteSpace(yearsRaw))
            {
                var digitsOnly = new string(yearsRaw.Where(char.IsDigit).ToArray());
                if (long.TryParse(digitsOnly, out var years))
                    row.YearsInAgric = years;
            }

            bool hasJsonColumn = canonicalHeaders.Contains("valuechains");
            bool hasFlatColumns = canonicalHeaders.Contains("valuechain1_categoryid");
            bool hasNamedColumns = canonicalHeaders.Contains("agricpart") || canonicalHeaders.Contains("productsdealtin");

            if (hasJsonColumn)
            {
                row.SelectedItems = ParseJsonValueChains(getField("valuechains"));
            }
            else if (hasFlatColumns)
            {
                row.SelectedItems = ParseFlatValueChains(getField);
            }
            else if (hasNamedColumns)
            {
                var partOfAgric = getField("agricpart") ?? string.Empty;
                var productsRaw = getField("productsdealtin") ?? string.Empty;

                var items = productsRaw
                    .Split(new[] { ",", ";" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                row.SelectedItems = items.Count > 0
                    ? new List<ValueChainSelectionDto>
                      {
                  new ValueChainSelectionDto { ValueChainType = partOfAgric, Items = items }
                      }
                    : new List<ValueChainSelectionDto>();
            }

            return row;
        }
        
        //public static List<BulkFarmerRowDto> Parse(Stream csvStream)
        //{
        //    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        //    {
        //        HeaderValidated = null,
        //        MissingFieldFound = null,
        //        PrepareHeaderForMatch = args => args.Header.ToLowerInvariant().Trim(),
        //    };

        //    using var reader = new StreamReader(csvStream);
        //    using var csv = new CsvReader(reader, config);

        //    csv.Read();
        //    csv.ReadHeader();

        //    var headers = csv.HeaderRecord!
        //        .Select(h => h.ToLowerInvariant().Trim())
        //        .ToHashSet();

        //    bool hasJsonColumn = headers.Contains("valuechains");
        //    bool hasFlatColumns = headers.Contains("valuechain1_categoryid");

        //    var rows = new List<BulkFarmerRowDto>();
        //    int rowIndex = 1;

        //    while (csv.Read())
        //    {
        //        var row = new BulkFarmerRowDto
        //        {
        //            RowIndex = rowIndex++,

        //            PhoneNumber = csv.GetField("phonenumber") ?? string.Empty,
        //            Email = csv.GetField("email"),
        //            Name = csv.GetField("name") ?? string.Empty,
        //            Gender = csv.GetField("gender"),
        //            DateOfBirth = csv.GetField("dateofbirth"),
        //            NIN = csv.GetField("nin"),
        //            BVN = csv.GetField("bvn"),
        //            Location = csv.GetField("location"),
        //            State = csv.GetField("state"),
        //            Lga = csv.GetField("lga"),
        //            Ward = csv.GetField("ward"),
        //            FarmerCategory = csv.GetField("farmercategory"),
        //            HighestLevelOfEducation = csv.GetField("highestlevelofeducation"),
        //        };

        //        if (long.TryParse(csv.GetField("yearsinagric"), out var years))
        //            row.YearsInAgric = years;


        //        if (hasJsonColumn)
        //        {
        //            row.SelectedItems = ParseJsonValueChains(csv.GetField("valuechains"));
        //        }
        //        else if (hasFlatColumns)
        //        {
        //            row.SelectedItems = ParseFlatValueChains(csv);
        //        }

        //        rows.Add(row);
        //    }

        //    return rows;
        //}

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
        //private static List<ValueChainSelectionDto> ParseFlatValueChains(CsvReader csv)
        //{
        //    var selections = new List<ValueChainSelectionDto>();

        //    for (int n = 1; n <= MaxFlatValueChains; n++)
        //    {
        //        var categoryIdStr = csv.GetField($"valuechain{n}_categoryid");

        //        if (string.IsNullOrWhiteSpace(categoryIdStr))
        //            continue;

        //        if (!long.TryParse(categoryIdStr, out var categoryId) || categoryId == 0)
        //            continue;

        //        var typeName = csv.GetField($"valuechain{n}_typename") ?? string.Empty;

        //        var itemsRaw = csv.GetField($"valuechain{n}_itemname") ?? string.Empty;

        //        var items = itemsRaw
        //            .Split(',', StringSplitOptions.RemoveEmptyEntries)
        //            .Select(x => x.Trim())
        //            .Where(x => !string.IsNullOrWhiteSpace(x))
        //            .ToList();

        //        if (items.Count == 0)
        //            continue;

        //        selections.Add(new ValueChainSelectionDto
        //        {
        //            ValueChainType = typeName,
        //            Items = items
        //        });
        //    }

        //    return selections;
        //}


    }
    public static class HeaderMapper
    {
        private static readonly Dictionary<string, string[][]> AliasRules = new()
        {
            ["firstname"] = new[] { new[] { "first", "name" } },
            ["middlename"] = new[] { new[] { "middle", "name" } },
            ["lastname"] = new[] { new[] { "last", "name" }, new[] { "surname" } },
            ["name"] = new[] { new[] { "name" } },
            ["dateofbirth"] = new[] { new[] { "date", "birth" } },
            ["gender"] = new[] { new[] { "gender" }, new[] { "sex" } },
            ["state"] = new[] { new[] { "state" } },
            ["lga"] = new[] { new[] { "lga" }, new[] { "local", "government" } },
            ["ward"] = new[] { new[] { "ward" } },
            ["location"] = new[] { new[] { "residential", "address" }, new[] { "address" } },
            ["phonenumber"] = new[] { new[] { "phone" } },
            ["email"] = new[] { new[] { "email" } },
            ["farmercategory"] = new[] { new[] { "category", "farmer" } },
            ["agricpart"] = new[] { new[] { "part", "agriculture" } },
            ["productsdealtin"] = new[] { new[] { "type", "products" }, new[] { "products", "deal" } },
            ["yearsinagric"] = new[] { new[] { "how", "long" }, new[] { "years", "agribusiness" }, new[] { "years", "agric" } },
            ["highestlevelofeducation"] = new[] { new[] { "highest", "education" } },
            ["bvn"] = new[] { new[] { "bvn" } },
            ["nin"] = new[] { new[] { "nin" } },
            ["valuechains"] = new[] { new[] { "valuechains" } },
            ["valuechain1_categoryid"] = new[] { new[] { "valuechain1_categoryid" } },
        };

        // Exposes the known canonical keys so callers can check for unmatched ones
        // (e.g. in tests or startup diagnostics) without needing access to AliasRules itself.
        public static IReadOnlyCollection<string> KnownCanonicalKeys => AliasRules.Keys;

        public static Dictionary<string, string> Resolve(IEnumerable<string> rawHeaders)
        {
            var map = new Dictionary<string, string>();
            var headerList = rawHeaders.ToList();

            foreach (var (canonicalKey, keywordSets) in AliasRules)
            {
                var match = headerList.FirstOrDefault(raw => MatchesAnySet(raw, keywordSets));
                if (match != null)
                    map[canonicalKey] = match;
            }

            return map;
        }

        private static bool MatchesAnySet(string rawHeader, string[][] keywordSets)
        {
            var normalized = Normalize(rawHeader);
            return keywordSets.Any(set => set.All(keyword => normalized.Contains(keyword)));
        }

        private static string Normalize(string header) =>
            new string(header.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }
}