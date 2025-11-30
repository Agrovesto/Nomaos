using Agrovesto.Backend.Core.Exceptions;
using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using Agrovesto.Backend.Infrastructure.Logic.Repositories;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using Document = iTextSharp.text.Document;

namespace Agrovesto.Backend.Infrastructure.ExternalServices
{
    public class ExcelConverterService
      : IExcelConverterService
    {

        private readonly ICooperativeRepository _cooperativeRepository;
        private readonly IFarmerRepository _farmerRepository;
        public ExcelConverterService(
                                     ICooperativeRepository cooperativeRepository,
                                     IFarmerRepository farmerRepository
                                     )
        {
            _cooperativeRepository = cooperativeRepository;
            _farmerRepository = farmerRepository;
        }
    
        public static (string FirstName, string MiddleName, string LastName) SplitFullName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return (null, null, null);

            // Clean: trim and remove multiple spaces
            var parts = fullName
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 1)
            {
                // Only one name present
                return (parts[0], null, null);
            }

            if (parts.Length == 2)
            {
                // First + Last only
                return (parts[0], null, parts[1]);
            }

            if (parts.Length == 3)
            {
                // First + Middle + Last
                return (parts[0], parts[1], parts[2]);
            }

            // More than 3 parts → First name = first token, Last name = last token,
            // Middle name = everything in between
            string firstName = parts[0];
            string lastName = parts[^1];
            string middleName = string.Join(" ", parts.Skip(1).Take(parts.Length - 2));

            return (firstName, middleName, lastName);
        }


public async Task<(List<UserModel> ValidItems, List<(int Row, string ErrorMessage)> Errors)>
ExtractFarmersFromExcel(Stream content, BusinessRegistrationArgs args)
    {
        var errors = new List<(int Row, string ErrorMessage)>();
        var validItems = new List<UserModel>();
        var seenPhoneNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Fetch all existing DB phone numbers once
        var existingPhones = await _farmerRepository.GetAllPhonesForOrganizationAsync(args.organizationCode);
        var existingPhoneSet = new HashSet<string>(existingPhones, StringComparer.OrdinalIgnoreCase);

        using var workbook = new XLWorkbook(content);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
        {
            errors.Add((0, "The Excel file does not contain any worksheets."));
            return (validItems, errors);
        }

        int rowCount = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        if (rowCount < 2)
        {
            errors.Add((0, "The selected worksheet is empty."));
            return (validItems, errors);
        }

        for (int row = 2; row <= rowCount; row++)
        {
            var rowErrors = new List<string>();

            // --- Read Excel Cells ---
            var dateString = worksheet.Cell(row, 1).GetValue<string>().Trim();
            var fullName = worksheet.Cell(row, 2).GetValue<string>().Trim();
            var (first, middle, last) = SplitFullName(fullName);

            var firstName = first;
            var middleName = middle;
            var lastName = last;
            var dob = worksheet.Cell(row, 3).GetValue<string>().Trim();
            var gender = worksheet.Cell(row, 4).GetValue<string>().Trim();
            var state = worksheet.Cell(row, 5).GetValue<string>().Trim();
            var lga = worksheet.Cell(row, 6).GetValue<string>().Trim();
            var ward = worksheet.Cell(row, 7).GetValue<string>().Trim();
            var location = worksheet.Cell(row, 8).GetValue<string>().Trim();
            var phoneNumber = worksheet.Cell(row, 9).GetValue<string>().Trim();
            var email = worksheet.Cell(row, 10).GetValue<string>().Trim();
            var farmerCategory = worksheet.Cell(row, 11).GetValue<string>().Trim();
            var valueChainType = worksheet.Cell(row, 12).GetValue<string>().Trim();
            var valueChainItem = worksheet.Cell(row, 13).GetValue<string>().Trim();
            var yearsInAgricRaw = worksheet.Cell(row, 14).GetValue<string>().Trim();
            var education = worksheet.Cell(row, 15).GetValue<string>().Trim();
            var bvn = worksheet.Cell(row, 16).GetValue<string>().Trim();
            var nin = worksheet.Cell(row, 17).GetValue<string>().Trim();
            var shouldVerifyNIN = worksheet.Cell(row, 18).GetValue<string>().Trim();

            var itemsList = !string.IsNullOrEmpty(valueChainItem)
                ? valueChainItem.Split(',', StringSplitOptions.RemoveEmptyEntries)
                               .Select(i => i.Trim()).ToList()
                : new List<string>();

            int.TryParse(yearsInAgricRaw, out int yearsInAgric);

            // --- Validations ---
            if (string.IsNullOrEmpty(firstName)) rowErrors.Add("First Name cannot be empty.");
            //if (string.IsNullOrEmpty(lastName)) rowErrors.Add("Last Name cannot be empty.");
            if (string.IsNullOrEmpty(phoneNumber)) rowErrors.Add("Phone number cannot be empty.");
            else
            {
                if (seenPhoneNumbers.Contains(phoneNumber))
                    rowErrors.Add($"Duplicate phone number in this Excel: {phoneNumber}.");
                else
                {
                    seenPhoneNumbers.Add(phoneNumber);
                    if (existingPhoneSet.Contains(phoneNumber))
                        rowErrors.Add($"Phone number {phoneNumber} already exists in this business.");
                }
            }
            if (string.IsNullOrEmpty(gender)) rowErrors.Add("Gender cannot be empty.");
           // if (string.IsNullOrEmpty(state)) rowErrors.Add("State cannot be empty.");
            //if (string.IsNullOrEmpty(lga)) rowErrors.Add("LGA cannot be empty.");
           // if (string.IsNullOrEmpty(farmerCategory)) rowErrors.Add("Farmer Category cannot be empty.");
            //if (yearsInAgric < 0) rowErrors.Add("YearsInAgric cannot be negative.");

            if (rowErrors.Any())
            {
                errors.Add((row, string.Join("; ", rowErrors)));
                continue;
            }

            // --- Build UserModel ---
            var farmer = new UserModel
            {
                FirstName = firstName,
                MiddleName = middleName,
                LastName = lastName,
                PhoneNumber = phoneNumber,
                Email = email,
                Gender = gender,
                Location = location,
                State = state,
                Lga = lga,
                Ward = ward,
                DateOfBirth = dob,
                FarmerCategory = farmerCategory,
                HighestLevelOfEducation = education,
                YearsInAgric = yearsInAgric,
                UserType = "Farmer",
                shouldVerifyNin = false,
                BusinessCode = args.businessCode,
                Password = args.DefaultPassword,
                Pin = "0000",
                OrganizationCode = args.organizationCode,
                CreatedOn = dateString != null ? DateTimeOffset.Parse(dateString) : DateTimeOffset.UtcNow,
                Bvn = bvn,
                NIN = nin,
                Name= $"{first} {middle} {last}",

                ValueChainDto = new List<ValueChainSelectionDto>
            {
                new ValueChainSelectionDto
                {
                    ValueChainType = valueChainType,
                    Items = itemsList
                }
            }
            };

            validItems.Add(farmer);
        }

        return (validItems, errors);
    }

    public byte[] ConvertFarmerTransactionsToExcel(IEnumerable<FarmerSalesTrackerModel> farmers)

        {
            var list = farmers.ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Farmer");


            // Header
            var headers = new[]
            {
        "S/N", "FARMER NAME", "CROP SOLD", "MARKET NAME",  "STATE",
        "MARKET DAY", "PRICE SOLD", "QUANTITY SOLD",
        "UNIT TYPE", "TRANSACTION TYPE", "MODE OF PAYMENT",  "DATE"
    };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                //worksheet.Cell(1, i + 1).Style.Font.SetFontColor(Color.Green);
            }

            int serialNo = 1;
            // Rows

            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                int row = i + 2;
                int col = 1;

                worksheet.Cell(row, col++).Value = serialNo++;
                worksheet.Cell(row, col++).Value = item.FarmerUserId;
                worksheet.Cell(row, col++).Value = item.CropSold;
                worksheet.Cell(row, col++).Value = item.MarketName;
                worksheet.Cell(row, col++).Value = item.MarketState;
                worksheet.Cell(row, col++).Value = item.MarketDay;
                worksheet.Cell(row, col++).Value = item.TotalPriceSold.ToString();
                worksheet.Cell(row, col++).Value = item.SoldCropUnitTypeName;
                worksheet.Cell(row, col++).Value = item.SalesTypeName;
                worksheet.Cell(row, col++).Value = item.ModeOfPayment;
                worksheet.Cell(row, col++).Value = item.TransactionActualDate.ToString();
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        public byte[] ConvertFarmerTransactionsToPdf(IEnumerable<FarmerSalesTrackerModel> farmers)
        {
            var list = farmers.ToList();

            using var stream = new MemoryStream();
            var document = new Document(iTextSharp.text.PageSize.A4.Rotate(), 20f, 20f, 20f, 20f);
            PdfWriter.GetInstance(document, stream);

            document.Open();

            var fontHeader = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.CYAN);
            var fontBody = FontFactory.GetFont(FontFactory.HELVETICA, 9);


            var table = new PdfPTable(14)
            {
                WidthPercentage = 100
            };

            var headers = new[]
            {
 "S/N", "FARMER NAME", "CROP SOLD", "MARKET NAME",  "STATE",
        "MARKET DAY", "PRICE SOLD", "QUANTITY SOLD",
        "UNIT TYPE", "TRANSACTION TYPE", "MODE OF PAYMENT", "DATE"
    };

            // headers
            foreach (var header in headers)
            {
                var cell = new PdfPCell(new Phrase(header, fontHeader))
                {
                    BackgroundColor = new BaseColor(230, 255, 255),
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 5
                };
                table.AddCell(cell);
            }

            // rows
            int serialNo = 1;
            foreach (var item in list)
            {
                table.AddCell(new Phrase(serialNo++.ToString(), fontBody));
                table.AddCell(new Phrase(item.FarmerUserId, fontBody));
                table.AddCell(new Phrase(item.CropSold, fontBody));
                table.AddCell(new Phrase(item.MarketName.ToString(), fontBody));
                table.AddCell(new Phrase(item.MarketState, fontBody));
                table.AddCell(new Phrase(item.MarketDay, fontBody));
                table.AddCell(new Phrase(item.TotalPriceSold.ToString(), fontBody));
                table.AddCell(new Phrase(item.SoldCropUnitTypeName, fontBody));
                table.AddCell(new Phrase(item.SalesTypeName, fontBody));
                table.AddCell(new Phrase(item.ModeOfPayment, fontBody));
                table.AddCell(new Phrase(item.TransactionActualDate.ToString(), fontBody));
            }

            document.Add(table);
            document.Close();

            return stream.ToArray();
        }


        public byte[] ConvertFarmerDetailsToExcel(IEnumerable<FarmerModel> farmers)

        {
            var list = farmers.ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Farmer");


            // Header
            var headers = new[]
            {
        "S/N", "NAME", "LOCATION", "LGA", "STATE",
        "PHONENUMBER", "EMAIL", "GENDER", "DATE OF BIRTH",
        "COOPERATIVE NAME", "REGISTRATION DATE"
    };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                //worksheet.Cell(1, i + 1).Style.Font.SetFontColor(Color.Green);
            }

            int serialNo = 1;
            // Rows

            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                int row = i + 2;
                int col = 1;

                worksheet.Cell(row, col++).Value = serialNo++;
                worksheet.Cell(row, col++).Value = item.Name;
                worksheet.Cell(row, col++).Value = item.Location;
                worksheet.Cell(row, col++).Value = item.Lga;
                worksheet.Cell(row, col++).Value = item.State;
                worksheet.Cell(row, col++).Value = item.PhoneNumber;
                worksheet.Cell(row, col++).Value = item.Email;
                worksheet.Cell(row, col++).Value = item.Gender;
                worksheet.Cell(row, col++).Value = item.DateOfBirth;
                worksheet.Cell(row, col++).Value = item.CooperativeName;
                worksheet.Cell(row, col++).Value = item.OnBoardedOn.ToString();
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }


        public byte[] ConvertFarmerDetailsToPdf(IEnumerable<FarmerModel> farmers)
        {
            var list = farmers.ToList();

            using var stream = new MemoryStream();
            var document = new Document(iTextSharp.text.PageSize.A4.Rotate(), 20f, 20f, 20f, 20f);
            PdfWriter.GetInstance(document, stream);

            document.Open();

            var fontHeader = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.CYAN);
            var fontBody = FontFactory.GetFont(FontFactory.HELVETICA, 9);


            var table = new PdfPTable(14)
            {
                WidthPercentage = 100
            };

            var headers = new[]
            {
     "S/N", "NAME", "LOCATION", "LGA", "STATE",
        "PHONENUMBER", "EMAIL", "GENDER", "DATE OF BIRTH",
        "COOPERATIVE NAME", "REGISTRATION DATE"
    };

            // headers
            foreach (var header in headers)
            {
                var cell = new PdfPCell(new Phrase(header, fontHeader))
                {
                    BackgroundColor = new BaseColor(230, 255, 255),
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 5
                };
                table.AddCell(cell);
            }

            // rows
            int serialNo = 1;
            foreach (var item in list)
            {
                table.AddCell(new Phrase(serialNo++.ToString(), fontBody));
                table.AddCell(new Phrase(item.Name, fontBody));
                table.AddCell(new Phrase(item.Location, fontBody));
                table.AddCell(new Phrase(item.Lga.ToString(), fontBody));
                table.AddCell(new Phrase(item.State, fontBody));
                table.AddCell(new Phrase(item.PhoneNumber, fontBody));
                table.AddCell(new Phrase(item.Email, fontBody));
                table.AddCell(new Phrase(item.Gender, fontBody));
                table.AddCell(new Phrase(item.DateOfBirth, fontBody));
                table.AddCell(new Phrase(item.CooperativeName, fontBody));
                table.AddCell(new Phrase(item.OnBoardedOn.ToString(), fontBody));
            }

            document.Add(table);
            document.Close();

            return stream.ToArray();
        }

        public async Task<byte[]> GenerateFarmerExcel(List<FarmerDetailDto> farmers)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Farmer");
            // Header
            var headers = new[]
            {
                "Farmer Name", "Phone", "Gender", "State", "LGA", "Ward", "Location",
                "Cooperative", "Agent",  "NIN", "BVN",
                "Farm Cordinate 1", "Farm Cordinate 2", "Farm Coordinate 3",
                    "Farm Coordinate 4",
                    "Farm Size in Sq", "Farm Size in Hectare",
                    "Farm Location",
                   // "Farm Ward",
                    "Farm LGA",
                    "Farm State",
                    "Most Recent Visitation",
                    "Value Chain"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // Rows
            int row = 2;
            foreach (var farmer in farmers)
            {
                var f = farmer.Farmer;

                worksheet.Cell(row, 1).Value = f.Name;
                worksheet.Cell(row, 2).Value = f.PhoneNumber;
                worksheet.Cell(row, 3).Value = f.Gender;
                worksheet.Cell(row, 4).Value = f.State;
                worksheet.Cell(row, 5).Value = f.Lga;
                worksheet.Cell(row, 6).Value = f.Ward;
                worksheet.Cell(row, 7).Value = f.Location;
                worksheet.Cell(row, 8).Value = farmer.Cooperative?.Name;
                worksheet.Cell(row, 9).Value = farmer.Agent?.Name;

                //  worksheet.Cells[row, 10].Value = f.SSN;
                worksheet.Cell(row, 10).Value = f.NIN;
                worksheet.Cell(row, 11).Value = f.BVN;
                var recentPlot = farmer.PlotDetails.FirstOrDefault();

                worksheet.Cell(row, 12).Value = recentPlot != null ? $"{recentPlot.Longitude1},{recentPlot.Latitude1}" : "NA";
                worksheet.Cell(row, 13).Value = recentPlot != null ? $"{recentPlot.Longitude2},{recentPlot.Latitude2}" : "NA";
                worksheet.Cell(row, 14).Value = recentPlot != null ? $"{recentPlot.Longitude3},{recentPlot.Latitude3}" : "NA";
                worksheet.Cell(row, 15).Value = recentPlot != null ? $"{recentPlot.Longitude4},{recentPlot.Latitude4}" : "NA";
                worksheet.Cell(row, 16).Value = recentPlot != null ? recentPlot.AreaInSqMetre : "NA";
                worksheet.Cell(row, 17).Value = recentPlot != null ? recentPlot.AreaInHectare : "NA";
                worksheet.Cell(row, 18).Value = recentPlot != null ? recentPlot.FarmLocation : "NA";
                //worksheet.Cell(row, 19).Value = recentPlot != null ? recentPlot.farmward :"NA";
                worksheet.Cell(row, 19).Value = recentPlot != null ? recentPlot.FarmLga : "NA";
                worksheet.Cell(row, 20).Value = recentPlot != null ? recentPlot.FarmState : "NA";

                // Most recent visitation
                var recentVisit = farmer.Visitations.FirstOrDefault();
                worksheet.Cell(row, 21).Value = recentVisit != null
                    ? $"{recentVisit.Description} on {recentVisit.CreatedOn:yyyy-MM-dd}, " +
                    $"Recommendation {recentVisit.Recommendation}" +
                    $"Supporting Document {recentVisit.SupportingImageUrl}"
                    : "";

                var groupedItems = farmer.ValueChainItems
            .GroupBy(x => x.TypeName)
            .Select(g => $"{g.Key}: {string.Join(", ", g.Select(i => i.ItemName))}");

                worksheet.Cell(row, 22).Value = string.Join(" | ", groupedItems); // Added here

                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();

        }

        #region inputs

        public byte[] ConvertSubscriptionToExcel(IEnumerable<BundleSubscriptionInterestModel> farmers)

        {
            var list = farmers.ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Farmer");


            // Header
            var headers = new[]
            {
        "S/N", "BUNDLE NAME", "QUANTITY OF INTEREST", "AMOUNT", "FARMER NAME", "LOCATION",
          "PAYMENT DATE",      "PAYMENT STATUS", "REFERENCE",
        "MATURITY DATE",
    };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                //worksheet.Cell(1, i + 1).Style.Font.SetFontColor(Color.Green);
            }

            int serialNo = 1;
            // Rows

            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                int row = i + 2;
                int col = 1;

                worksheet.Cell(row, col++).Value = serialNo++;
                worksheet.Cell(row, col++).Value = item.InterestedBundleName;
                worksheet.Cell(row, col++).Value = item.Location;
                worksheet.Cell(row, col++).Value = item.TotalQuantityPaidFor;
                worksheet.Cell(row, col++).Value = item.TotalEquityPaid;
                worksheet.Cell(row, col++).Value = item.FarmerName;
                worksheet.Cell(row, col++).Value = item.PaymentDate.ToString();
                worksheet.Cell(row, col++).Value = item.PaymentStatus;
                worksheet.Cell(row, col++).Value = item.Reference;
                worksheet.Cell(row, col++).Value = item.MaturityDate.ToString();
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }


        public byte[] ConvertSubscriptionToPDF(IEnumerable<BundleSubscriptionInterestModel> farmers)
        {
            var list = farmers.ToList();

            using var stream = new MemoryStream();
            var document = new Document(iTextSharp.text.PageSize.A4.Rotate(), 20f, 20f, 20f, 20f);
            PdfWriter.GetInstance(document, stream);

            document.Open();

            var fontHeader = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.CYAN);
            var fontBody = FontFactory.GetFont(FontFactory.HELVETICA, 9);


            var table = new PdfPTable(14)
            {
                WidthPercentage = 100
            };

            var headers = new[]
            {
                "S/N", "BUNDLE NAME", "QUANTITY OF INTEREST", "AMOUNT", "FARMER NAME", "LOCATION",
          "PAYMENT DATE",      "PAYMENT STATUS", "REFERENCE",
        "MATURITY DATE",
            };

            // headers
            foreach (var header in headers)
            {
                var cell = new PdfPCell(new Phrase(header, fontHeader))
                {
                    BackgroundColor = new BaseColor(230, 255, 255),
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 5
                };
                table.AddCell(cell);
            }

            // rows
            int serialNo = 1;
            foreach (var item in list)
            {
                table.AddCell(new Phrase(serialNo++.ToString(), fontBody));
                table.AddCell(new Phrase(item.InterestedBundleName, fontBody));
                table.AddCell(new Phrase(item.Location, fontBody));
                table.AddCell(new Phrase(item.TotalQuantityPaidFor.ToString(), fontBody));
                table.AddCell(new Phrase(item.TotalEquityPaid.ToString(), fontBody));
                table.AddCell(new Phrase(item.FarmerName, fontBody));
                table.AddCell(new Phrase(item.PaymentDate.ToString(), fontBody));
                table.AddCell(new Phrase(item.PaymentStatus, fontBody));
                table.AddCell(new Phrase(item.Reference, fontBody));
                table.AddCell(new Phrase(item.MaturityDate.ToString(), fontBody));
            }

            document.Add(table);
            document.Close();

            return stream.ToArray();
        }

        #endregion inputs
    }
}
