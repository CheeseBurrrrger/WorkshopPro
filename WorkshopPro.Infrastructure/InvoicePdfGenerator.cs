using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.UniversalAccessibility.Drawing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using WorkshopPro.Application;

namespace WorkshopPro.Infrastructure
{
    public class InvoicePdfGenerator
    {
        private const string WORKSHOP_NAME = "ED Motor Auto Service";
        private const string WORKSHOP_ADDRESS = "Jl. Raya Danau Kerinci E1C/17\nkota Malang";
        private const string WORKSHOP_EMAIL = "edmotorautoservice@gmail.com";
        private const string FOOTER_NOTE = "*Garansi atas perjanjian kedua belah pihak";

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
            string modelDisplayName = null)
        {
            var document = new PdfDocument();
            document.Info.Title = invoice.InvoiceNo;
            document.Info.Author = WORKSHOP_NAME;

            var page = document.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            var gfx = XGraphics.FromPdfPage(page);

            // ── Fonts ──────────────────────────────────────────────────────────
            var fntHuge = new XFont("Arial", 28, XFontStyleEx.Bold);
            var fntTitle = new XFont("Arial", 14, XFontStyleEx.Bold);
            var fntBold = new XFont("Arial", 10, XFontStyleEx.Bold);
            var fntNormal = new XFont("Arial", 10, XFontStyleEx.Regular);
            var fntSmall = new XFont("Arial", 8, XFontStyleEx.Regular);
            var fntSmallB = new XFont("Arial", 8, XFontStyleEx.Bold);
            var fntSection = new XFont("Arial", 11, XFontStyleEx.Bold);

            // ── Colors ─────────────────────────────────────────────────────────
            var cBlack = XColors.Black;
            var cGray = XColor.FromArgb(100, 100, 100);
            var cLGray = XColor.FromArgb(220, 220, 220);
            var cDark = XColor.FromArgb(30, 30, 30);

            double pageW = page.Width.Point;
            double pageH = page.Height.Point;
            double margin = 45;
            double y = margin;

            // ── helpers ────────────────────────────────────────────────────────
            void HRule(double yy, double thick = 0.5)
            {
                gfx.DrawLine(new XPen(cLGray, thick), margin, yy, pageW - margin, yy);
            }

            void Cell(string text, XFont font, XBrush brush,
                      double x, double yy, double w = 200,
                      XStringFormat fmt = null)
            {
                gfx.DrawString(text ?? "", font, brush,
                    new XRect(x, yy, w, 14),
                    fmt ?? XStringFormats.CenterLeft);
            }

            // ══════════════════════════════════════════════════════════════════
            // SECTION 1 — Title + Date
            // ══════════════════════════════════════════════════════════════════
            gfx.DrawString("Invoice", fntHuge, new XSolidBrush(cDark), margin, y + 4);

            // Workshop name top-right (in lieu of logo)
            gfx.DrawString(WORKSHOP_NAME, fntTitle, new XSolidBrush(cDark),
                new XRect(0, y, pageW - margin, 22), XStringFormats.TopRight);
            gfx.DrawString("Auto Workshop", fntSmall, new XSolidBrush(cGray),
                new XRect(0, y + 20, pageW - margin, 14), XStringFormats.TopRight);

            y += 34;
            // Parse ServiceDate — stored as "yyyy-MM-dd" string
            if (DateTime.TryParse(invoice.ServiceDate, out DateTime svcDate))
                gfx.DrawString(svcDate.ToString("dd MMMM yyyy"), fntNormal,
                    new XSolidBrush(cGray), margin, y);
            else
                gfx.DrawString(invoice.ServiceDate, fntNormal, new XSolidBrush(cGray), margin, y);

            gfx.DrawString($"Invoice No:  {invoice.InvoiceNo}",
                fntBold, new XSolidBrush(cDark),
                new XRect(0, y, pageW - margin, 14), XStringFormats.TopRight);

            y += 22;
            HRule(y);
            y += 12;

            // ══════════════════════════════════════════════════════════════════
            // SECTION 2 — From / To info block
            // ══════════════════════════════════════════════════════════════════
            double col1x = margin;           // left: workshop info
            double col2x = pageW / 2 + 10;  // right: customer info
            double labelW = 80;
            double valueX1 = col1x + labelW + 4;
            double valueX2 = col2x + labelW + 4;
            double rowH = 16;

            // Left column headers
            gfx.DrawString("Invoice from", fntBold, new XSolidBrush(cDark), col1x, y);
            // Right column headers
            gfx.DrawString("Invoice to", fntBold, new XSolidBrush(cDark), col2x, y);
            y += rowH;

            // Workshop info rows
            string[] workshopLines = WORKSHOP_ADDRESS.Split('\n');
            gfx.DrawString(WORKSHOP_NAME, fntNormal, new XSolidBrush(cDark), valueX1, y);
            // Customer name
            gfx.DrawString(customer?.Name ?? "-", fntNormal, new XSolidBrush(cDark), valueX2, y);
            y += rowH;

            gfx.DrawString("Address", fntBold, new XSolidBrush(cDark), col1x, y);
            gfx.DrawString(workshopLines.Length > 0 ? workshopLines[0] : "",
                fntNormal, new XSolidBrush(cGray), valueX1, y);
            gfx.DrawString("Phone", fntBold, new XSolidBrush(cDark), col2x, y);
            gfx.DrawString(customer?.Phone ?? "-", fntNormal, new XSolidBrush(cGray), valueX2, y);
            y += rowH;

            if (workshopLines.Length > 1)
            {
                gfx.DrawString(workshopLines[1], fntNormal, new XSolidBrush(cGray), valueX1, y);
            }
            // Car Brand row
            gfx.DrawString("Car Brand", fntBold, new XSolidBrush(cDark), col2x, y);
            gfx.DrawString(vehicle?.modelDisplayName?.Split(' ').FirstOrDefault() ?? "-",
                fntNormal, new XSolidBrush(cGray), valueX2, y);
            y += rowH;

            gfx.DrawString("Email", fntBold, new XSolidBrush(cDark), col1x, y);
            gfx.DrawString(WORKSHOP_EMAIL, fntNormal, new XSolidBrush(cGray), valueX1, y);
            gfx.DrawString("Car Type", fntBold, new XSolidBrush(cDark), col2x, y);
            gfx.DrawString(modelDisplayName ?? vehicle?.modelDisplayName ?? "-",
                fntNormal, new XSolidBrush(cGray), valueX2, y);
            y += rowH;

            gfx.DrawString("Plate No", fntBold, new XSolidBrush(cDark), col2x, y);
            gfx.DrawString(vehicle?.PlateNumber ?? "-",
                fntBold, new XSolidBrush(cDark), valueX2, y);
            y += rowH + 8;

            HRule(y);
            y += 14;

            // ══════════════════════════════════════════════════════════════════
            // SECTION 3 — Work items table
            // ══════════════════════════════════════════════════════════════════
            gfx.DrawString("Work", fntSection, new XSolidBrush(cDark), margin, y);
            y += 18;

            // Column X positions
            double cItem = margin;
            double cLabour = margin + 230;
            double cParts = margin + 340;
            double cTotal = margin + 430;
            double tableW = pageW - margin * 2;

            // Table header row
            HRule(y);
            y += 4;
            gfx.DrawString("ITEM", fntSmallB, new XSolidBrush(cDark), cItem, y);
            gfx.DrawString("LABOUR", fntSmallB, new XSolidBrush(cDark), cLabour, y);
            gfx.DrawString("PARTS", fntSmallB, new XSolidBrush(cDark), cParts, y);
            gfx.DrawString("TOTAL", fntSmallB, new XSolidBrush(cDark), cTotal, y);
            y += 14;
            HRule(y, 0.8);
            y += 6;

            // Separate items into labor and parts
            var laborItems = invoice.Items.Where(i => i.ItemType == "Labor").ToList();
            var partItems = invoice.Items.Where(i => i.ItemType == "SparePart"
                                                   || i.ItemType == "ManualPart").ToList();

            decimal totalLabour = 0;
            decimal totalParts = 0;

            // Draw all rows — labor shows value in LABOUR column, parts in PARTS column
            // We interleave them in invoice order but track column separately
            foreach (var item in invoice.Items)
            {
                bool isLabor = item.ItemType == "Labor";
                string labourCell = isLabor ? $"Rp{item.Subtotal:N0}" : "-";
                string partsCell = isLabor ? "-" : $"Rp{item.Subtotal:N0}";

                gfx.DrawString(item.Description, fntNormal, new XSolidBrush(cDark),
                    new XRect(cItem, y, cLabour - cItem - 4, 14), XStringFormats.CenterLeft);
                gfx.DrawString(labourCell, fntNormal, new XSolidBrush(cDark), cLabour, y);
                gfx.DrawString(partsCell, fntNormal, new XSolidBrush(cDark), cParts, y);
                gfx.DrawString($"Rp{item.Subtotal:N0}", fntNormal, new XSolidBrush(cDark), cTotal, y);

                if (isLabor) totalLabour += item.Subtotal;
                else totalParts += item.Subtotal;

                y += 18;
            }

            y += 4;
            HRule(y, 0.8);
            y += 10;

            // ── Total row ─────────────────────────────────────────────────────
            gfx.DrawString("Total", fntBold, new XSolidBrush(cDark), cParts, y);
            gfx.DrawString($"Rp{invoice.TotalAmount:N0}",
                fntBold, new XSolidBrush(cDark), cTotal, y);
            y += 20;

            HRule(y);
            y += 20;

            // ══════════════════════════════════════════════════════════════════
            // SECTION 4 — Notes box
            // ══════════════════════════════════════════════════════════════════
            double notesH = 70;
            gfx.DrawRectangle(new XPen(cLGray, 0.8),
                new XRect(margin, y, tableW, notesH));
            gfx.DrawString("Notes", fntSmallB, new XSolidBrush(cDark), margin + 6, y + 6);
            if (!string.IsNullOrWhiteSpace(invoice.Notes))
                gfx.DrawString(invoice.Notes, fntNormal, new XSolidBrush(cGray),
                    new XRect(margin + 6, y + 20, tableW - 12, notesH - 22),
                    XStringFormats.TopLeft);
            y += notesH + 16;

            // ══════════════════════════════════════════════════════════════════
            // SECTION 5 — Footer
            // ══════════════════════════════════════════════════════════════════
            double footerY = pageH - 35;
            HRule(footerY);
            gfx.DrawString(FOOTER_NOTE, fntSmall, new XSolidBrush(cGray), margin, footerY + 8);

            // ── Save ───────────────────────────────────────────────────────────
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
