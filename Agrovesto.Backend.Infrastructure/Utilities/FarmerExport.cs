using Agrovesto.Backend.Core.Models.Admin;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Utilities
{
    public static class FarmerExport
    {
        public static byte[] ToCsv(List<FarmerListItemDto> rows)
        {
            static string Esc(string? v) =>
                $"\"{(v ?? "").Replace("\"", "\"\"")}\"";

            var sb = new StringBuilder();
            sb.AppendLine("S/n,Farmer ID,Full Name,Phone Number,Reg Date,Status,Farms,Assigned Agent,Organization");
            foreach (var r in rows)
                sb.AppendLine(string.Join(",",
                    //r.SerialNo,
                    Esc(r.FarmerId),
                    Esc(r.FullName), Esc(r.PhoneNumber),
                    r.RegDate.ToString("dd/MM/yyyy"), Esc(r.Status), r.Farms,
                    Esc(r.AssignedAgent), Esc(r.Organization)));

            return new UTF8Encoding(true).GetPreamble()          // BOM so Excel reads UTF-8
                .Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }

        // dotnet add package ClosedXML
        public static byte[] ToXlsx(List<FarmerListItemDto> rows)
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Farmers");
            var headers = new[] { "S/n", "Farmer ID", "Full Name", "Phone Number",
                              "Reg Date", "Status", "Farms", "Assigned Agent", "Organization" };
            for (int c = 0; c < headers.Length; c++)
                ws.Cell(1, c + 1).Value = headers[c];

            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                //ws.Cell(i + 2, 1).Value = r.SerialNo;
                ws.Cell(i + 2, 2).Value = r.FarmerId;
                ws.Cell(i + 2, 3).Value = r.FullName;
                ws.Cell(i + 2, 4).Value = r.PhoneNumber;      // string, so leading 0 is kept
                ws.Cell(i + 2, 5).Value = r.RegDate.ToString("dd/MM/yyyy");
                ws.Cell(i + 2, 6).Value = r.Status;
                ws.Cell(i + 2, 7).Value = r.Farms;
                ws.Cell(i + 2, 8).Value = r.AssignedAgent;
                ws.Cell(i + 2, 9).Value = r.Organization;
            }

            ws.Row(1).Style.Font.Bold = true;
            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }
    }
}
