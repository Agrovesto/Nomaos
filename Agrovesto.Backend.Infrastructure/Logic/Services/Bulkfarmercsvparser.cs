
using Agrovesto.Backend.Core.Models.Businesses;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text.Json;

namespace Agrovesto.Backend.Infrastructure.Logic.Services
{

    public static class BulkFarmerCsvParser
    {
        private const int MaxFlatValueChains = 10;

        public static List<BulkFarmerRowDto> Parse(Stream csvStream)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
                PrepareHeaderForMatch = args => args.Header.ToLowerInvariant().Trim(),
            };

            using var reader = new StreamReader(csvStream);
            using var csv = new CsvReader(reader, config);

            csv.Read();
            csv.ReadHeader();

            var headers = csv.HeaderRecord!
                .Select(h => h.ToLowerInvariant().Trim())
                .ToHashSet();

            bool hasJsonColumn = headers.Contains("valuechains");
            bool hasFlatColumns = headers.Contains("valuechain1_categoryid");

            var rows = new List<BulkFarmerRowDto>();
            int rowIndex = 1;

            while (csv.Read())
            {
                var row = new BulkFarmerRowDto
                {
                    RowIndex = rowIndex++,

                    PhoneNumber = csv.GetField("phonenumber") ?? string.Empty,
                    Email = csv.GetField("email"),
                    Name = csv.GetField("name") ?? string.Empty,
                    Gender = csv.GetField("gender"),
                    DateOfBirth = csv.GetField("dateofbirth"),
                    NIN = csv.GetField("nin"),
                    BVN = csv.GetField("bvn"),
                    Location = csv.GetField("location"),
                    State = csv.GetField("state"),
                    Lga = csv.GetField("lga"),
                    Ward = csv.GetField("ward"),
                    FarmerCategory = csv.GetField("farmercategory"),
                    HighestLevelOfEducation = csv.GetField("highestlevelofeducation"),
                };

                if (long.TryParse(csv.GetField("yearsinagric"), out var years))
                    row.YearsInAgric = years;

             
                if (hasJsonColumn)
                {
                    row.SelectedItems = ParseJsonValueChains(csv.GetField("valuechains"));
                }
                else if (hasFlatColumns)
                {
                    row.SelectedItems = ParseFlatValueChains(csv);
                }

                rows.Add(row);
            }

            return rows;
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

        private static List<ValueChainSelectionDto> ParseFlatValueChains(CsvReader csv)
        {
            var selections = new List<ValueChainSelectionDto>();

            for (int n = 1; n <= MaxFlatValueChains; n++)
            {
                var categoryIdStr = csv.GetField($"valuechain{n}_categoryid");

                if (string.IsNullOrWhiteSpace(categoryIdStr))
                    continue;

                if (!long.TryParse(categoryIdStr, out var categoryId) || categoryId == 0)
                    continue;

                var typeName = csv.GetField($"valuechain{n}_typename") ?? string.Empty;

                var itemsRaw = csv.GetField($"valuechain{n}_itemname") ?? string.Empty;

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
}