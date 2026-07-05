using QRCoder;
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
    public class PaymentReceiptData
    {
        public long SubscriptionId { get; set; }
        // Receipt meta
        public string ReceiptNumber { get; set; } = "";
        public DateTimeOffset PaymentDate { get; set; }

        // Subscriber
        public string FullName { get; set; } = "";
        public string SubscriberId { get; set; } = "";
        public string SubscriberType { get; set; } = "";

        // Package
        public string PackageName { get; set; } = "";
        public string GrowthCycle { get; set; } = "";
        public string RepaymentPlan { get; set; } = "";     // e.g. "Monthly"
        public string InstallmentLabel { get; set; } = "";   // e.g. "Installment 2/4"
        public string PeriodStart { get; set; } = "";        // e.g. "Mar 2026"
        public string PeriodEnd { get; set; } = "";          // e.g. "Jun 2026"

        // Payment
        public string PaymentChannel { get; set; } = "";     // e.g. "Cash"
        public string PaymentReference { get; set; } = "";   // e.g. "CASH-0616-002"
        public string CollectedByName { get; set; } = "";
        public string CollectedByCode { get; set; } = "";    // e.g. "AGT-0034"

        // Amounts
        public decimal AmountPaid { get; set; }
        public decimal RepaymentValue { get; set; }
        public decimal TotalPackageValue { get; set; }

        // Derived — set from NumberToWords.Convert(AmountPaid)
        public string AmountInWords { get; set; } = "";
        public string QrUrl { get; set; } = "";
    }
    public static class PaymentReceiptGenerator
    {
        private static readonly string GreenDark = "#0B6E5B";
        private static readonly string GreenLight = "#F0FAF7";
        private static readonly string GreenBorder = "#B7E4D8";
        private static readonly string RedLight = "#FFF5F5";
        private static readonly string RedBorder = "#FECACA";
        private static readonly string RedDark = "#DC2626";
        private static readonly string TextMain = "#111827";
        private static readonly string TextMuted = "#6B7280";
        private static readonly string BorderCol = "#E5E7EB";
        private static readonly string RowStripe = "#F9FAFB";

        public static byte[] Generate(PaymentReceiptData d)
        {
            var doc = Document.Create(c =>
            {
                c.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial").FontColor(TextMain));

                    page.Content().Column(col =>
                    {
                        col.Item().Element(Header);
                        col.Item().Element(c => Meta(c, d));
                        col.Item().Padding(10).Column(body =>
                        {
                            body.Spacing(6);
                            body.Item().Element(c => SubscriberSection(c, d));
                            body.Item().Element(c => PackageSection(c, d));
                            body.Item().Element(c => PaymentSection(c, d));
                            body.Item().Element(c => AmountBox(c, d));
                            body.Item().Element(c => SummaryTiles(c, d));
                            body.Item().Element(c => QrSection(c, d));
                        });
                        col.Item().Element(c => Footer(c, d));
                    });
                });
            });

            return doc.GeneratePdf();

            // ── HEADER ────────────────────────────────────────────────
            void Header(IContainer c) =>
                c.Background(GreenDark).Padding(10).PaddingLeft(14).PaddingRight(14).Row(row =>
                {
                    row.RelativeItem().Column(h =>
                    {
                        h.Item().Text("Commitment Payment Receipt")
                            .FontSize(13).Bold().FontColor(Colors.White);
                        h.Item().PaddingTop(2).Text("Agricultural Input Financing")
                            .FontSize(8).FontColor("#FFFFFFBF");
                    });
                    row.AutoItem().AlignMiddle()
                        .Border(1.5f).BorderColor(GreenDark)
                        .Background(GreenDark)
                        .Padding(4).PaddingLeft(8).PaddingRight(8)
                        .Text("PAID").FontSize(9).Bold().FontColor(Colors.White).LetterSpacing(1);
                });

            // ── META BAR ──────────────────────────────────────────────
            void Meta(IContainer c, PaymentReceiptData d) =>
                c.Background("#F7F8FA").BorderBottom(1).BorderColor(BorderCol)
                    .Padding(7).PaddingLeft(14).PaddingRight(14).Row(row =>
                    {
                        row.RelativeItem().AlignMiddle()
                            .Text("Present this receipt for input collection")
                            .FontSize(8).Italic().FontColor(TextMuted);
                        row.AutoItem().Column(ref_ =>
                        {
                            ref_.Item().AlignRight().Text(d.ReceiptNumber)
                                .FontSize(8).FontFamily("Courier New").Bold().FontColor(TextMain);
                            ref_.Item().PaddingTop(2).AlignRight().Row(dateRow =>
                            {
                                dateRow.AutoItem().Text(d.PaymentDate.ToString("dd MMM yyyy"))
                                    .FontSize(8).FontColor(TextMain);
                                dateRow.AutoItem().PaddingLeft(3).PaddingRight(3)
                                    .Text("•").FontColor(GreenDark).FontSize(8);
                                dateRow.AutoItem().Text(d.PaymentDate.ToString("hh:mm tt"))
                                    .FontSize(8).FontColor(TextMain);
                            });
                        });
                    });

            // ── SHARED: SECTION LABEL ─────────────────────────────────
            void SectionLabel(ColumnDescriptor col, string label) =>
                col.Item().PaddingBottom(4).Row(row =>
                {
                    row.ConstantItem(3).Background(GreenDark);
                    row.AutoItem().PaddingLeft(6).AlignMiddle()
                        .Text(label.ToUpper())
                        .FontSize(7).Bold().FontColor(TextMuted).LetterSpacing(1);
                });

            // ── SHARED: INFO CARD WRAPPER ─────────────────────────────
            void InfoCard(ColumnDescriptor col, Action<ColumnDescriptor> rows) =>
                col.Item().Border(1).BorderColor(BorderCol).Column(rows);

            // ── SHARED: INFO ROW ──────────────────────────────────────
            void InfoRow(ColumnDescriptor col, string label, string value, bool last = false) =>
                col.Item()
                    .Background(last ? Colors.White : RowStripe)
                    .BorderBottom(last ? 0 : 1).BorderColor(BorderCol)
                    .Padding(5).PaddingLeft(10).PaddingRight(10)
                    .Row(row =>
                    {
                        row.ConstantItem(90).AlignMiddle()
                            .Text(label).FontSize(7).FontColor("#9CA3AF").Bold().LetterSpacing(0.5f);
                        row.RelativeItem().AlignMiddle()
                            .Text(value ?? "-").FontSize(8).FontColor(TextMain);
                    });

            // ── SUBSCRIBER SECTION ────────────────────────────────────
            void SubscriberSection(IContainer c, PaymentReceiptData d) =>
                c.Column(col =>
                {
                    SectionLabel(col, "Subscriber");
                    InfoCard(col, rows =>
                    {
                        InfoRow(rows, "FULL NAME", d.FullName);
                        InfoRow(rows, "SUBSCRIBER ID", d.SubscriberId);
                        InfoRow(rows, "TYPE", d.SubscriberType, last: true);
                    });
                });

            // ── PACKAGE SECTION ───────────────────────────────────────
            void PackageSection(IContainer c, PaymentReceiptData d) =>
                c.Column(col =>
                {
                    SectionLabel(col, "Package");
                    InfoCard(col, rows =>
                    {
                        InfoRow(rows, "PACKAGE", d.PackageName);
                        InfoRow(rows, "CYCLE", d.GrowthCycle);
                        InfoRow(rows, "PLAN", $"Installments  • {d.InstallmentLabel} {d.RepaymentPlan}");
                      
                        InfoRow(rows, "REPAYMENT PERIOD", $"{d.PeriodStart} – {d.PeriodEnd}", last: true);
                    });
                });

            // ── PAYMENT SECTION ───────────────────────────────────────
            void PaymentSection(IContainer c, PaymentReceiptData d) =>
                c.Column(col =>
                {
                    SectionLabel(col, "Payment");
                    InfoCard(col, rows =>
                    {
                        InfoRow(rows, "CHANNEL", d.PaymentChannel);
                        InfoRow(rows, "REF", d.PaymentReference);
                        InfoRow(rows, "COLLECTED BY", $"{d.CollectedByName}  •  {d.CollectedByCode}", last: true);
                    });
                });

            // ── AMOUNT BOX ────────────────────────────────────────────
            void AmountBox(IContainer c, PaymentReceiptData d) =>
                c.Background(GreenLight).Border(1.5f).BorderColor(GreenBorder)
                    .Padding(8).PaddingLeft(12).PaddingRight(12).Row(row =>
                    {
                        row.RelativeItem().AlignMiddle().Column(a =>
                        {
                            a.Item().Text("AMOUNT PAID")
                                .FontSize(7).Bold().FontColor(TextMain).LetterSpacing(0.8f);
                            a.Item().PaddingTop(2).Text(d.AmountInWords)
                                .FontSize(7).Italic().FontColor(TextMuted);
                        });
                        row.AutoItem().AlignMiddle()
                            .Text($"₦{d.AmountPaid:N0}")
                            .FontSize(18).Bold().FontColor(GreenDark);
                    });

            // ── SUMMARY TILES ─────────────────────────────────────────
            void SummaryTiles(IContainer c, PaymentReceiptData d) =>
                c.Row(row =>
                {
                    row.RelativeItem()
                        .Background(GreenLight).Border(1.5f).BorderColor(GreenBorder)
                        .Padding(8).Column(t =>
                        {
                            t.Item().AlignCenter().Text("REPAYMENT VALUE")
                                .FontSize(7).Bold().FontColor(GreenDark).LetterSpacing(0.8f);
                            t.Item().PaddingTop(3).AlignCenter()
                                .Text($"₦{d.RepaymentValue:N0}")
                                .FontSize(14).Bold().FontColor(GreenDark);
                        });

                    row.ConstantItem(8);

                    row.RelativeItem()
                        .Background(RedLight).Border(1.5f).BorderColor(RedBorder)
                        .Padding(8).Column(t =>
                        {
                            t.Item().AlignCenter().Text("TOTAL PACKAGE VALUE")
                                .FontSize(7).Bold().FontColor(RedDark).LetterSpacing(0.8f);
                            t.Item().PaddingTop(3).AlignCenter()
                                .Text($"₦{d.TotalPackageValue:N0}")
                                .FontSize(14).Bold().FontColor(RedDark);
                        });
                });

            // ── QR SECTION ────────────────────────────────────────────
            static byte[] GenerateQrPng(string url)
            {
                using var generator = new QRCodeGenerator();
                using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
                using var qrCode = new PngByteQRCode(data);
                return qrCode.GetGraphic(
                    pixelsPerModule: 3,
                    darkColorRgba: new byte[] { 0x0B, 0x6E, 0x5B, 0xFF },
                    lightColorRgba: new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
            }

            void QrSection(IContainer c, PaymentReceiptData d)
            {
                var qrPng = GenerateQrPng(d.QrUrl);

                c.Border(1).BorderColor(BorderCol)
                 .Background("#FAFAFA").Padding(8).Row(row =>
                 {
                     row.AutoItem().AlignMiddle()
                        .Width(56).Height(56)
                        .Image(qrPng, ImageScaling.FitArea);

                     row.ConstantItem(10);

                     row.RelativeItem().AlignMiddle().Column(col =>
                     {
                         col.Item().Text("VERIFY SUBSCRIPTION")
                            .FontSize(7).Bold().FontColor(GreenDark).LetterSpacing(0.8f);

                         col.Item().PaddingTop(3)
                            .Text("Scan to view full subscription details, payment history, and current status.")
                            .FontSize(7).FontColor(TextMuted).LineHeight(1.3f);

                         col.Item().PaddingTop(4)
                            .Background("#E8F5F2")
                            .Padding(3).PaddingLeft(6).PaddingRight(6)
                            .Text($"SUB-{d.SubscriptionId}")
                            .FontSize(7).FontFamily("Courier New").Bold().FontColor(GreenDark);
                     });
                 });
            }

            // ── FOOTER ────────────────────────────────────────────────
            void Footer(IContainer c, PaymentReceiptData d) =>
                c.BorderTop(1).BorderColor(BorderCol)
                    .Padding(7).PaddingLeft(14).PaddingRight(14)
                    .Row(row =>
                    {
                        row.AutoItem().AlignMiddle()
                            .Text(d.ReceiptNumber)
                            .FontSize(7).FontColor("#9CA3AF").FontFamily("Courier New");
                        row.RelativeItem().AlignRight().AlignMiddle()
                            .Text("Keep this receipt as proof of your commitment payment")
                            .FontSize(7).FontColor("#9CA3AF").Italic();
                    });
        }

        public static void GenerateToFile(PaymentReceiptData d, string filePath) =>
            System.IO.File.WriteAllBytes(filePath, Generate(d));
    }
    public static class ReceiptQrUrlBuilder
    {
        public static string Build(
            long subscriptionId,
            long paymentId,
            string baseUrl,
            string hmacSecret,
            TimeSpan? validity = null)
        {
            var expiry = DateTimeOffset.UtcNow
                .Add(validity ?? TimeSpan.FromDays(365))
                .ToUnixTimeSeconds();

            //   var token = ComputeHmac($"{subscriptionId}:{paymentId}:{expiry}", hmacSecret);

            return $"{baseUrl}";
                //.TrimEnd('/')}/subscriptions/verify" +
                // $"?sid={subscriptionId}&pid={paymentId}&exp={expiry}&token={token}";
        }

        private static string ComputeHmac(string data, string secret)
        {
            var key = System.Text.Encoding.UTF8.GetBytes(secret);
            var bytes = System.Text.Encoding.UTF8.GetBytes(data);
            using var hmac = new System.Security.Cryptography.HMACSHA256(key);
            return Convert.ToBase64String(hmac.ComputeHash(bytes))
                          .Replace("+", "-").Replace("/", "_").Replace("=", "");
        }
    }
}

