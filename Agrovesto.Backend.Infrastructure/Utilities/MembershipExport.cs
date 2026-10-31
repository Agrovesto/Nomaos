using Agrovesto.Backend.Core.Models.Admin;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Utilities
{
    public class MembershipExport
    {
        public static byte[] ToCsv(List<MemberShipListItemDto> rows)
        {
            static string Esc(string? v) =>
                $"\"{(v ?? "").Replace("\"", "\"\"")}\"";

            var sb = new StringBuilder();
            sb.AppendLine("S/n,Membership ID,Representative ID,Membership Name," +
                "Membership Type," +
                "State,Members,Organizations,Reg Date,Status");
            foreach (var r in rows)
                sb.AppendLine(string.Join(",",
                    //r.SerialNo,
                    r.Id,
                    Esc(r.MemberShipLeaderId),
                    Esc(r.MemberShipName),
                    Esc(r.MemberShipType),
                    Esc(r.State),
                    r.Members,
                     r.LinkedOrganizations,
                    r.RegDate.ToString("dd/MM/yyyy"), Esc(r.Status)));

            return new UTF8Encoding(true).GetPreamble()          // BOM so Excel reads UTF-8
                .Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }

        // dotnet add package ClosedXML
        public static byte[] ToXlsx(List<MemberShipListItemDto> rows)
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Membership");
            var headers = new[] { "S/n", "Membership ID", "Representative ID", "Membership Name", "Membership Type",
                              "State", "Members", "Organizations", "Reg Date", "Status" };
            for (int c = 0; c < headers.Length; c++)
                ws.Cell(1, c + 1).Value = headers[c];

            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                var serialNo = i + 1;
                ws.Cell(i + 2, 1).Value = serialNo;
                ws.Cell(i + 2, 2).Value = r.Id;
                ws.Cell(i + 2, 3).Value = r.MemberShipLeaderId;
                ws.Cell(i + 2, 4).Value = r.MemberShipName;
                ws.Cell(i + 2, 5).Value = r.MemberShipType;
                ws.Cell(i + 2, 6).Value = r.State;
                ws.Cell(i + 2, 7).Value = r.Members;
                ws.Cell(i + 2, 8).Value = r.LinkedOrganizations;
                ws.Cell(i + 2, 9).Value = r.RegDate.ToString("dd/MM/yyyy");
                ws.Cell(i + 2, 10).Value = r.Status;
            }

            ws.Row(1).Style.Font.Bold = true;
            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

    }

}
