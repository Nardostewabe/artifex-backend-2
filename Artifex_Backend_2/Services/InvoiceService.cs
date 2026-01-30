using Artifex_Backend_2.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Artifex_Backend_2.Services
{
    public interface IInvoiceService
    {
        byte[] GenerateInvoice(Payment payment, User user);
    }

    public class InvoiceService : IInvoiceService
    {
        public InvoiceService()
        {
            // QuestPDF Community License
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerateInvoice(Payment payment, User user)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Helvetica"));

                    // 1. Header
                    page.Header().Element(ComposeHeader);

                    // 2. Content
                    page.Content().PaddingVertical(20).Column(column =>
                    {
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Element(e => ComposeUserAddress(e, user));
                            row.RelativeItem().Element(e => ComposeInvoiceInfo(e, payment));
                        });

                        column.Item().PaddingTop(25).Element(e => ComposePaymentTable(e, payment));

                        column.Item().PaddingTop(25).Column(c =>
                        {
                            c.Item().Text($"Payment Method: {payment.PaymentMethod ?? "Chapa Online Payment"}");
                            c.Item().Text($"Transaction Ref: {payment.TxRef}");

                            var statusColor = payment.Status == "Success" ? Colors.Green.Medium : Colors.Red.Medium;
                            c.Item().PaddingTop(5).Text(text =>
                            {
                                text.Span("Status: ").SemiBold();
                                text.Span(payment.Status).FontColor(statusColor).Bold();
                            });
                        });
                    });

                    // 3. Footer
                    page.Footer().AlignCenter().Column(c =>
                    {
                        c.Item().Text("Thank you for shopping with Artifex!");
                        c.Item().Text($"Generated on {DateTime.Now:F}").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            });

            return document.GeneratePdf();
        }

        void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("Artifex").FontSize(24).SemiBold().FontColor(Colors.Blue.Medium);
                    column.Item().Text("Ethiopian Marketplace");
                    column.Item().Text("Addis Ababa, Ethiopia");
                    column.Item().Text("support@artifex.com");
                });
            });
        }

        void ComposeUserAddress(IContainer container, User user)
        {
            container.Column(column =>
            {
                column.Item().Text("Bill To:").FontSize(14).SemiBold();
                column.Item().Text(user.Username ?? "Valued Customer");
                column.Item().Text(user.Email);
            });
        }

        void ComposeInvoiceInfo(IContainer container, Payment payment)
        {
            container.Column(column =>
            {
                column.Item().AlignRight().Text("INVOICE").FontSize(20).SemiBold().FontColor(Colors.Grey.Darken2);
                column.Item().AlignRight().Text($"Invoice #: INV-{payment.Id}");
                column.Item().AlignRight().Text($"Date: {payment.CreatedAt:d}");
            });
        }

        void ComposePaymentTable(IContainer container, Payment payment)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Description").SemiBold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Amount").SemiBold();

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.DefaultTextStyle(x => x.SemiBold())
                                        .PaddingVertical(5)
                                        .BorderBottom(1)
                                        .BorderColor(Colors.Grey.Lighten2); // ✅ Fixed here
                    }
                });

                table.Cell().Element(CellStyle).Text("Partial Payment (50% Deposit)");
                table.Cell().Element(CellStyle).AlignRight().Text($"ETB {payment.Amount:N2}");

                static IContainer CellStyle(IContainer container)
                {
                    return container.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2); // ✅ And here
                }

                table.Cell().ColumnSpan(2).PaddingTop(10).AlignRight().Text(text =>
                {
                    text.Span("Total Paid: ").FontSize(12);
                    text.Span($"ETB {payment.Amount:N2}").FontSize(14).Bold();
                });
            });
        }
    }
}