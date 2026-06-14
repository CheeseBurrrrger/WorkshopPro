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
            this.Text = "WorkshopPro";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            var lblTitle = new Label
            {
                Text = "WorkshopPro",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 70
            };

            var btnNewInvoice = new Button
            {
                Text = "New Invoice",
                Size = new Size(200, 50),
                Location = new Point(90, 100),
                Font = new Font("Segoe UI", 11)
            };
            btnNewInvoice.Click += BtnNewInvoice_Click;

            var btnInventory = new Button
            {
                Text = "Spare Part Inventory",
                Size = new Size(200, 50),
                Location = new Point(90, 165),
                Font = new Font("Segoe UI", 11)
            };
            btnInventory.Click += BtnInventory_Click;

            this.Controls.AddRange(new Control[] { lblTitle, btnNewInvoice, btnInventory });
        }

        private void BtnNewInvoice_Click(object sender, EventArgs e)
        {
            // Opens vehicle search first — plate lookup is always step 1
            var searchForm = new VehicleSearchForm(
                _vehicleRepo, _vehicleModelRepo, _customerRepo);

            if (searchForm.ShowDialog() == DialogResult.OK)
            {
                // VehicleSearchForm sets these on OK
                //var invoiceForm = new InvoiceForm(
                //    searchForm.SelectedVehicle,
                //    searchForm.SelectedCustomer,
                //    _sparePartRepo,
                //    _invoiceService,
                //    _pdfGenerator);
                var invoiceForm = new InvoiceForm(
                    _invoiceService,
                    _vehicleRepo,
                    _customerRepo,
                    _sparePartRepo,
                    _laborRepo,           // you need to add this to MainForm
                    _vehicleModelRepo,
                    _manufacturerRepo,
                    _pdfGenerator);

                invoiceForm.ShowDialog();
            }
        }

        private void BtnInventory_Click(object sender, EventArgs e)
        {
            using (var form = new SparePartForm(_sparePartRepo, _inventoryService))
            {
                form.ShowDialog(this);
            }

        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(282, 253);
            this.Name = "MainForm";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);

        }

        private void MainForm_Load(object sender, System.EventArgs e)
        {

        }
    }
}