using Agrovesto.Backend.Core.Interfaces.Repositories;
using Agrovesto.Backend.Core.Interfaces.Services;
using Agrovesto.Backend.Core.Models.Businesses;
using System.Drawing;
using System.Text;
using System.Xml.Linq;
using Color = System.Drawing.Color;
using Document = iTextSharp.text.Document;
using DocumentFormat.OpenXml.Office2010.PowerPoint;
using DocumentFormat.OpenXml.Wordprocessing;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ClosedXML.Excel;

namespace Agrovesto.Backend.Infrastructure.ExternalServices
{
    public class ExcelConverterService
      : IExcelConverterService
    {

        private readonly ICooperativeRepository _farmerRepository;

        public ExcelConverterService(
                                     ICooperativeRepository farmerRepository
                                     )
        {
            _farmerRepository = farmerRepository;
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

    }
}
