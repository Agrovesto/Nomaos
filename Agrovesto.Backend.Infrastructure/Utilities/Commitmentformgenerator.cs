using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Utilities
{
    public class CommitmentFormData
    {
        public string FullName { get; set; } = "";
        public long SubscriptionId { get; set; } 
        public string SubscriberType { get; set; } = "";  
        public string PhoneNumber { get; set; } = "";
        public string State { get; set; } = "";
        public string LGA { get; set; } = "";

        public string PackageName { get; set; } = "";
        public string Crop { get; set; } = "";
        public string GrowthCycleSeason { get; set; } = "";
        public decimal PackageInputValue { get; set; }
        public decimal CommitmentAmount { get; set; }
        public string RepaymentDuration { get; set; } = "";
        public string CommitmentPlan { get; set; } = "";  

        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }

        // Helper used in the declaration paragraph
        public string CommitmentAmountInWords { get; set; } = "";
    }
    public static class CommitmentFormGenerator
    {
        private static readonly string BrandColor = "#0B6E5B";

        /// <summary>Builds the PDF in memory and returns the bytes (e.g. to return from an API controller).</summary>
        public static byte[] Generate(CommitmentFormData d)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(18);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Header().Element(c => ComposeHeader(c));
                    page.Content().Element(c => ComposeBody(c, d));
                    page.Footer().Element(c => ComposeFooter(c));
                });
            });

            return document.GeneratePdf();
        }

        /// <summary>Convenience overload: writes the PDF straight to disk.</summary>
        public static void GenerateToFile(CommitmentFormData d, string filePath)
        {
            System.IO.File.WriteAllBytes(filePath, Generate(d));
        }

        // ShowOnce() ensures this only renders on the first page. If content still
        // overflows onto a second page, that page will simply continue the body
        // content with no repeated header/title block.
        private static void ComposeHeader(IContainer container)
        {
            container.ShowOnce().Column(col =>
            {
                col.Item().AlignCenter().Text("INPUT FINANCING PROGRAMME")
                    .FontSize(15).Bold().FontColor(BrandColor);
                col.Item().AlignCenter().Text("Agricultural Input Financing").FontSize(8).FontColor(Colors.Grey.Darken1);
                col.Item().PaddingTop(5).AlignCenter().Text("SUBSCRIBER COMMITMENT FORM")
                    .FontSize(12).Bold().FontColor(BrandColor);
                col.Item().AlignCenter().Text("Agricultural Input Financing - Commitment Agreement").FontSize(7).Italic();
                col.Item().PaddingTop(4).LineHorizontal(1).LineColor(BrandColor);
            });
        }

        private static void ComposeBody(IContainer container, CommitmentFormData d)
        {
            container.PaddingTop(6).Column(col =>
            {
                col.Spacing(2);

                SectionTitle(col, "A. SUBSCRIBER INFORMATION");
                FieldRow(col, "Full Name", d.FullName);
                FieldRow(col, "Subscription ID", d.SubscriptionId.ToString());
                FieldRow(col, "Type", d.SubscriberType);
                FieldRow(col, "Phone Number", d.PhoneNumber);
                FieldRow(col, "State / LGA", $"{d.State} / {d.LGA}");

                col.Item().PaddingTop(5);
                SectionTitle(col, "B. PACKAGE DETAILS");
                FieldRow(col, "Package Name", d.PackageName);
                FieldRow(col, "Crop / Commodity", d.Crop);
                FieldRow(col, "Growth Cycle / Season", d.GrowthCycleSeason);
                FieldRow(col, "Package Input Value", $"₦{d.PackageInputValue:N2}");
                FieldRow(col, "Commitment Amount", $"₦{d.CommitmentAmount:N2}");
                FieldRow(col, "Repayment Duration", $"{d.RepaymentDuration}");
                FieldRow(col, "Commitment Plan", d.CommitmentPlan);

                col.Item().PaddingTop(5);
                SectionTitle(col, "C. COMMITMENT DECLARATION");
                col.Item().Padding(6).Background(Colors.Grey.Lighten4).Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(8).LineHeight(1.3f));
                    text.Span("I, ");
                    text.Span(d.FullName).Bold().FontColor(BrandColor);
                    text.Span(", of Subscription ID ");
                    text.Span(d.SubscriptionId.ToString()).Bold().FontColor(BrandColor);
                    text.Span(", hereby commit to pay a commitment fee of ");
                    text.Span($"₦{d.CommitmentAmount:N2}").Bold();
                    text.Span($" ({d.CommitmentAmountInWords} Naira Only) for the ");
                    text.Span(d.PackageName).Bold().FontColor(BrandColor);
                    text.Span(" package covering the ");
                    text.Span(d.GrowthCycleSeason).Bold().FontColor(BrandColor);
                    text.Span(" input financing programme, for the period of ");
                    text.Span(d.StartDate?.ToString("MMMM yyyy")).Bold();
                    text.Span(" to ");
                    text.Span(d.EndDate?.ToString("MMMM yyyy")).Bold();
                    text.Span(".");
                });

                col.Item().PaddingTop(4).Text(t => t.Line("By signing this form, I acknowledge and agree to the following terms:").FontSize(6.5f));
                foreach (var clause in DeclarationClauses)
                {
                    col.Item().Text(t =>
                    {
                        t.Span("• ").FontSize(5.5f);
                        t.Span(clause).FontSize(5.5f);
                    });
                }

                // --- Section D is intentionally left BLANK for manual completion ---
                col.Item().PaddingTop(8);
                SectionTitle(col, "D. SIGNATURES (To be completed by hand)");
                col.Item().PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().Column(c => SignatureBlock(c, "SUBSCRIBER",
                        new[] { "Signature & Date", "Full Name (Print)", "Date" }));
                    row.ConstantItem(16);
                    row.RelativeItem().Column(c => SignatureBlock(c, "HEAD OF COMMUNITY/RELIGIOUS LEADER",
                        new[] { "Authorized Signature & Stamp", "Name & Designation", "Date" }));
                });
            });
        }

        private static readonly string[] DeclarationClauses =
        {
        "I understand that this commitment amount is a prerequisite for the release of my input package.",
        "I agree to repay the outstanding input financing balance in accordance with the agreed repayment schedule.",
        "I accept that failure to honour my repayment obligations may result in suspension from future financing cycles.",
        "I confirm that all information provided in this form is accurate and complete to the best of my knowledge.",
        "I consent to the organization contacting me via the registered phone number for repayment reminders and updates.",
        "I hereby authorize the organization to debit any amount due and outstanding under this commitment agreement from any of my bank accounts or registered payment instruments, without further notice, should I fail to meet my repayment obligations on the agreed due dates."
    };

        private static void SectionTitle(ColumnDescriptor col, string title)
        {
            col.Item().Background(BrandColor).Padding(4).Text(title).FontColor(Colors.White).Bold().FontSize(9);
        }

        private static void FieldRow(ColumnDescriptor col, string label, string value)
        {
            col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Row(row =>
            {
                row.ConstantItem(140).Background(Colors.Grey.Lighten4).Padding(3)
                    .Text(label).Bold().FontSize(8);
                row.RelativeItem().Padding(3).Text(string.IsNullOrWhiteSpace(value) ? "-" : value).FontSize(8);
            });
        }

        private static void SignatureBlock(ColumnDescriptor col, string title, string[] lines)
        {
            col.Item().Text(title).Bold().FontSize(8).FontColor(BrandColor);
            foreach (var label in lines)
            {
                col.Item().PaddingTop(16).LineHorizontal(0.75f).LineColor(Colors.Grey.Darken1);
                col.Item().Text(label).FontSize(6.5f).FontColor(Colors.Grey.Darken2);
            }
        }

        private static void ComposeFooter(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                col.Item().PaddingTop(3).AlignCenter()
                    .Text("This form is an official document of the Input Financing Programme.").FontSize(6.5f).FontColor(Colors.Grey.Darken1);
                col.Item().AlignCenter()
                    .Text("Retain one (1) copy each: Subscriber | Agent | Organization").FontSize(6.5f).FontColor(Colors.Grey.Darken1);
            });
        }
    }

}


