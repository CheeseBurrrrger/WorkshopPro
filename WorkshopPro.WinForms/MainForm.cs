using System;
using System.Drawing;
using System.Windows.Forms;
using WorkshopPro.Application;
using WorkshopPro.Domain;
using WorkshopPro.Infrastructure;
using WorkshopPro.Infrastructure.Repositories;
namespace WorkshopPro.WinForms
{
    public partial class MainForm : Form
    {
        private readonly SqliteVehicleRepository _vehicleRepo;
        private readonly SqliteVehicleModelRepository _vehicleModelRepo;
        private readonly SqliteCustomerRepository _customerRepo;
        private readonly SqliteSparePartRepository _sparePartRepo;
        private readonly SqliteInvoiceRepository _invoiceRepo;
        private readonly InvoiceService _invoiceService;
        private readonly InventoryService _inventoryService;
        private readonly InvoicePdfGenerator _pdfGenerator;
        private readonly SqliteLaborServiceRepository _laborRepo;
        private readonly SqliteManufacturerRepository _manufacturerRepo;

        public MainForm(
            SqliteVehicleRepository vehicleRepo,
            SqliteVehicleModelRepository vehicleModelRepo,
            SqliteCustomerRepository customerRepo,
            SqliteSparePartRepository sparePartRepo,
            SqliteInvoiceRepository invoiceRepo,
            InvoiceService invoiceService,
            InventoryService inventoryService,
            InvoicePdfGenerator pdfGenerator,
            SqliteLaborServiceRepository laborServiceRepo,
            SqliteManufacturerRepository manufacturerRepo)
        {
            InitializeComponent();
            _vehicleRepo = vehicleRepo;
            _vehicleModelRepo = vehicleModelRepo;
            _customerRepo = customerRepo;
            _sparePartRepo = sparePartRepo;
            _invoiceRepo = invoiceRepo;
            _invoiceService = invoiceService;
            _inventoryService = inventoryService;
            _pdfGenerator = pdfGenerator;
            _laborRepo = laborServiceRepo;
            _manufacturerRepo = manufacturerRepo;
            BuildUI();
        }

        private void BuildUI()
        {
            Text = "WorkshopPro";
            Size = new Size(420, 380);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = Color.FromArgb(245, 245, 248);

            // ── Title ──────────────────────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(30, 30, 60)
            };
            var lblTitle = new Label
            {
                Text = "WorkshopPro",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            var lblSub = new Label
            {
                Text = "Auto Workshop Management",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(180, 200, 230),
                TextAlign = ContentAlignment.BottomCenter,
                Dock = DockStyle.Bottom,
                Height = 22
            };
            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSub });

            // ── Buttons ────────────────────────────────────────────────────────
            var btnInvoices = MakeNavButton("🧾  Invoices",
                "Create, view and print invoices",
                Color.FromArgb(33, 97, 140));
            btnInvoices.Click += (s, e) => OpenInvoices();

            var btnInventory = MakeNavButton("📦  Spare Parts",
                "Manage stock and receive new parts",
                Color.FromArgb(39, 120, 75));
            btnInventory.Click += (s, e) => OpenInventory();

            var btnHistory = MakeNavButton("🔍  Service History",
                "Search past invoices by plate or customer",
                Color.FromArgb(130, 70, 160));
            btnHistory.Click += (s, e) => OpenServiceHistory();

            var pnlButtons = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(30, 20, 30, 20)
            };

            btnInvoices.Location = new Point(30, 20);
            btnInventory.Location = new Point(30, 95);
            btnHistory.Location = new Point(30, 170);

            pnlButtons.Controls.AddRange(new Control[] { btnInvoices, btnInventory, btnHistory });

            Controls.AddRange(new Control[] { pnlButtons, pnlHeader });
        }

        // ── Navigation ─────────────────────────────────────────────────────────

        private void OpenInvoices()
        {
            // Open InvoiceForm directly — no VehicleSearchForm step needed.
            // The admin types the plate inside InvoiceForm itself.
            var form = new InvoiceForm(
                _invoiceService,
                _vehicleRepo,
                _customerRepo,
                _sparePartRepo,
                _laborRepo,
                _vehicleModelRepo,
                _manufacturerRepo,
                _pdfGenerator);
            form.ShowDialog(this);
        }

        private void OpenInventory()
        {
            using (var form = new SparePartForm(_sparePartRepo, _inventoryService))
                form.ShowDialog(this);
        }

        private void OpenServiceHistory()
        {
            var form = new ServiceHistoryForm(
                _invoiceService,
                _vehicleRepo,
                _customerRepo,
                _invoiceRepo,
                _pdfGenerator);
            form.ShowDialog(this);
        }

        // ── Helper ─────────────────────────────────────────────────────────────

        private static Button MakeNavButton(string title, string subtitle, Color color)
        {
            var btn = new Button
            {
                Size = new Size(330, 65),
                FlatStyle = FlatStyle.Flat,
                BackColor = color,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0),
                Cursor = Cursors.Hand,
                Text = title + "\n" + subtitle
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            return btn;
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            ClientSize = new Size(420, 380);
            Name = "MainForm";
            Load += new EventHandler(MainForm_Load);
            ResumeLayout(false);
        }

        private void MainForm_Load(object sender, EventArgs e) { }

    }
}