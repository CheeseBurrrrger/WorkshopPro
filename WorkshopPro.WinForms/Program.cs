using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WorkshopPro.Infrastructure;
using WorkshopPro.Infrastructure.Repositories;
using WorkshopPro.Domain;
using PdfSharp.Fonts;




namespace WorkshopPro.WinForms
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]

        static void Main()
        {

            System.Windows.Forms.Application.EnableVisualStyles();
            GlobalFontSettings.FontResolver = new WindowsFontResolver();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            // ── Paths ─────────────────────────────────────────────────────────
            // AppDomain.CurrentDomain.BaseDirectory = the folder where the .exe lives.
            // Using this keeps the app fully portable (flash drive friendly).
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string dataDir = Path.Combine(appDir, "Data");
            string dbPath = Path.Combine(dataDir, "workshop.db");

            // ── Phase 1: Database Initialisation ─────────────────────────────
            try
            {
                //var dbInit = new DatabaseInitializer(dbPath);
                //dbInit.Initialize();

                //var seedLoader = new SeedLoader(dbPath, dataDir);
                //seedLoader.SeedAll();
                new DatabaseInitializer(dbPath).Initialize();
                new SeedLoader(dbPath, dataDir).SeedAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to initialize database:\n\n{ex.Message}\n\nPath: {dbPath}",
                    "WorkshopPro — Startup Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            var vehicleModelRepo = new SqliteVehicleModelRepository(dbPath);
            var vehicleRepo = new SqliteVehicleRepository(dbPath);
            var customerRepo = new SqliteCustomerRepository(dbPath);
            var sparePartRepo = new SqliteSparePartRepository(dbPath);
            var invoiceRepo = new SqliteInvoiceRepository(dbPath);
            var invoiceService = new InvoiceService(invoiceRepo, sparePartRepo);
            var inventoryService = new InventoryService(sparePartRepo);
            string invoiceOutputDir = Path.Combine(dataDir, "Invoices");
            var invoicePdfGenerator = new InvoicePdfGenerator(invoiceOutputDir);
            var laborRepo = new SqliteLaborServiceRepository(dbPath);
            var manufacturerRepo = new SqliteManufacturerRepository(dbPath);

            System.Windows.Forms.Application.Run(new MainForm(
                vehicleRepo, vehicleModelRepo, customerRepo,
                sparePartRepo, invoiceRepo,
                invoiceService, inventoryService, invoicePdfGenerator, laborRepo, manufacturerRepo));
        }
    }
}
