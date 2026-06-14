using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.UniversalAccessibility.Drawing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using WorkshopPro.Application;

namespace WorkshopPro.Infrastructure
{
    public class InvoicePdfGenerator
    {
        private readonly string _outputFolder;

        public InvoicePdfGenerator(string outputFolder)
        {
            _outputFolder = outputFolder;
            if (!Directory.Exists(_outputFolder))
                Directory.CreateDirectory(_outputFolder);
        }

        public string Generate(
            InvoiceEntity invoice,
            VehicleEntity vehicle,
            CustomerEntity customer,
            string workshopName = "WorkshopPro Auto Service")
        {
            var document = new PdfDocument();
            document.Info.Title = invoice.InvoiceNo;
            document.Info.Author = workshopName;

            var page = document.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            var gfx = XGraphics.FromPdfPage(page);

            // ── Fonts ──────────────────────────────────────────────────────
            var fontTitle = new XFont("Arial", 20, XFontStyleEx.Bold);
            var fontHeading = new XFont("Arial", 11, XFontStyleEx.Bold);
            var fontNormal = new XFont("Arial", 10, XFontStyleEx.Regular);
            var fontSmall = new XFont("Arial", 8, XFontStyleEx.Regular);
            var fontBold = new XFont("Arial", 10, XFontStyleEx.Bold);

            // ── Colors ─────────────────────────────────────────────────────
            var colorPrimary = XColor.FromArgb(33, 97, 140);
            var colorLight = XColor.FromArgb(214, 234, 248);
            var colorBlack = XColors.Black;
            var colorGray = XColor.FromArgb(120, 120, 120);

            double pageW = page.Width.Point;
            double margin = 50;
            double y = margin;

            // ── Header bar ─────────────────────────────────────────────────
            gfx.DrawRectangle(new XSolidBrush(colorPrimary), new XRect(0, 0, pageW, 70));
            gfx.DrawString(workshopName, fontTitle, XBrushes.White,
                new XRect(margin, 0, pageW - margin * 2, 70), XStringFormats.CenterLeft);
            gfx.DrawString("INVOICE", fontTitle, XBrushes.White,
                new XRect(0, 0, pageW - margin, 70), XStringFormats.CenterRight);

            y = 90;

            // ── Invoice meta (right) ────────────────────────────────────────
            double col2 = pageW / 2 + 20;

            // FIX: ServiceDate is now a string ("2024-01-15"), not a DateTime.
            // Parse it to DateTime only for display formatting, with a safe fallback.
            string serviceDateDisplay = invoice.ServiceDate ?? "";
            if (DateTime.TryParse(invoice.ServiceDate, out DateTime parsedDate))
                serviceDateDisplay = parsedDate.ToString("dd MMM yyyy");

            gfx.DrawString("Invoice No :", fontBold, new XSolidBrush(colorBlack), col2, y);
            gfx.DrawString(invoice.InvoiceNo, fontNormal, new XSolidBrush(colorBlack), col2 + 90, y);

            gfx.DrawString("Date :", fontBold, new XSolidBrush(colorBlack), col2, y + 18);
            gfx.DrawString(serviceDateDisplay, fontNormal, new XSolidBrush(colorBlack), col2 + 90, y + 18);

            gfx.DrawString("Status :", fontBold, new XSolidBrush(colorBlack), col2, y + 36);
            gfx.DrawString(invoice.Status, fontNormal, new XSolidBrush(colorBlack), col2 + 90, y + 36);

            // ── Customer info (left) ────────────────────────────────────────
            gfx.DrawString("Bill To", fontHeading, new XSolidBrush(colorPrimary), margin, y);
            gfx.DrawString(customer?.Name ?? "-", fontBold, new XSolidBrush(colorBlack), margin, y + 18);
            gfx.DrawString($"Phone  : {customer?.Phone ?? "-"}", fontNormal, new XSolidBrush(colorGray), margin, y + 34);
            gfx.DrawString($"Address: {customer?.Address ?? "-"}", fontNormal, new XSolidBrush(colorGray), margin, y + 50);

            y += 80;

            // ── Vehicle box ─────────────────────────────────────────────────
            gfx.DrawRectangle(new XSolidBrush(colorLight), new XRect(margin, y, pageW - margin * 2, 40));
            gfx.DrawString(
                $"Vehicle:  {vehicle.PlateNumber}   |   Year: {vehicle.Year}   |   Color: {vehicle.Color}",
                fontNormal, new XSolidBrush(colorBlack),
                new XRect(margin + 8, y, pageW - margin * 2 - 8, 40),
                XStringFormats.CenterLeft);

            y += 55;

            // ── Line items header ───────────────────────────────────────────
            double[] colX = { margin, margin + 260, margin + 330, margin + 390, margin + 450 };
            string[] headers = { "Description", "Type", "Qty", "Unit Price", "Subtotal" };

            gfx.DrawRectangle(new XSolidBrush(colorPrimary), new XRect(margin, y, pageW - margin * 2, 22));
            for (int i = 0; i < headers.Length; i++)
                gfx.DrawString(headers[i], fontBold, XBrushes.White, colX[i] + 4, y + 15);
            y += 26;

            // ── Line items rows ─────────────────────────────────────────────
            bool shadeRow = false;
            foreach (var item in invoice.Items)
            {
                if (shadeRow)
                    gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(240, 240, 240)),
                        new XRect(margin, y - 4, pageW - margin * 2, 20));

                gfx.DrawString(item.Description, fontNormal, new XSolidBrush(colorBlack), colX[0] + 4, y + 10);
                gfx.DrawString(item.ItemType, fontNormal, new XSolidBrush(colorBlack), colX[1] + 4, y + 10);
                gfx.DrawString(item.Qty.ToString(), fontNormal, new XSolidBrush(colorBlack), colX[2] + 4, y + 10);
                gfx.DrawString($"Rp {item.UnitPrice:N0}", fontNormal, new XSolidBrush(colorBlack), colX[3] + 4, y + 10);
                gfx.DrawString($"Rp {item.Subtotal:N0}", fontNormal, new XSolidBrush(colorBlack), colX[4] + 4, y + 10);

                y += 22;
                shadeRow = !shadeRow;
            }

            // ── Divider + Total ─────────────────────────────────────────────
            gfx.DrawLine(new XPen(colorPrimary, 1), margin, y + 5, pageW - margin, y + 5);
            y += 15;
            gfx.DrawString("TOTAL", fontHeading, new XSolidBrush(colorPrimary), pageW - margin - 180, y + 14);
            gfx.DrawString($"Rp {invoice.TotalAmount:N0}",
                new XFont("Arial", 13, XFontStyleEx.Bold),
                new XSolidBrush(colorPrimary),
                pageW - margin - 90, y + 14);
            y += 50;

            // ── Notes ───────────────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(invoice.Notes))
            {
                gfx.DrawString("Notes:", fontBold, new XSolidBrush(colorGray), margin, y);
                gfx.DrawString(invoice.Notes, fontNormal, new XSolidBrush(colorGray), margin, y + 16);
                y += 36;
            }

            // ── Footer ──────────────────────────────────────────────────────
            double footerY = page.Height.Point - 40;
            gfx.DrawLine(new XPen(colorLight, 1), margin, footerY, pageW - margin, footerY);
            gfx.DrawString("Thank you for your business!", fontSmall, new XSolidBrush(colorGray),
                new XRect(0, footerY + 8, pageW, 20), XStringFormats.TopCenter);

            // ── Save ────────────────────────────────────────────────────────
            string fileName = $"{invoice.InvoiceNo.Replace("-", "_")}.pdf";
            string filePath = Path.Combine(_outputFolder, fileName);
            document.Save(filePath);
            return filePath;
        }

        public static void OpenPdf(string filePath)
        {
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
    }
}
