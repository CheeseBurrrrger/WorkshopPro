using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WorkshopPro.Application;
using WorkshopPro.Domain;
using WorkshopPro.Infrastructure;
using WorkshopPro.Infrastructure.Repositories;

namespace WorkshopPro.WinForms
{
    public partial class InvoiceForm : Form
    {
        private readonly InvoiceService _invoiceService;
        private readonly IVehicleRepository _vehicleRepo;
        private readonly ICustomerRepository _customerRepo;
        private readonly ISparePartRepository _sparePartRepo;
        private readonly ILaborServiceRepository _laborRepo;
        private readonly IVehicleModelRepository _vehicleModelRepo;
        private readonly IManufacturerRepository _manufacturerRepo;
        private readonly InvoicePdfGenerator _pdfGenerator;


        // ── State ──────────────────────────────────────────────────────────
        private int _currentInvoiceId;
        private bool _invoiceIsDraft;
        private VehicleEntity _currentVehicle;
        private CustomerEntity _currentCustomer;
        private List<InvoiceItemEntity> _currentItems = new List<InvoiceItemEntity>();
        private readonly PartSearchPopup _partPopup;

        // ── Left panel controls ────────────────────────────────────────────
        private TextBox _searchBox;
        private DataGridView _invoiceListGrid;
        private Button _btnNew;

        // ── Right panel controls ───────────────────────────────────────────
        private Panel _detailPanel;
        private Label _lblInvoiceNo;
        private Label _lblStatus;

        private TextBox _txtPlate;
        private Button _btnLookup;
        private Label _lblVehicleInfo;
        private Label _lblCustomerInfo;

        private Panel _newVehiclePanel;
        private TextBox _txtCustomerName;
        private TextBox _txtCustomerPhone;
        private ComboBox _cmbManufacturer;
        private ComboBox _cmbModel;
        private TextBox _txtColor;
        private TextBox _txtYear;
        private Button _btnRegisterVehicle;

        private DataGridView _itemsGrid;
        private TextBox _txtPartSearch;
        private Label _lblPartSearchHint;
        private ComboBox _cmbLaborService;
        private TextBox _txtLaborPrice;
        private Button _btnAddLabor;
        private Button _btnRemoveItem;

        private Label _lblTotal;
        private Button _btnSaveDraft;
        private Button _btnMarkPaid;
        private Button _btnCancel;
        private Button _btnPrint;
        private VehicleEntity selectedVehicle;
        private CustomerEntity selectedCustomer;
        private InvoicePdfGenerator pdfGenerator;

        public InvoiceForm(
            InvoiceService invoiceService,
            IVehicleRepository vehicleRepo,
            ICustomerRepository customerRepo,
            ISparePartRepository sparePartRepo,
            ILaborServiceRepository laborRepo,
            IVehicleModelRepository vehicleModelRepo,
            IManufacturerRepository manufacturerRepo,
            InvoicePdfGenerator pdfGenerator)
        {
            _invoiceService = invoiceService;
            _vehicleRepo = vehicleRepo;
            _customerRepo = customerRepo;
            _sparePartRepo = sparePartRepo;
            _laborRepo = laborRepo;
            _vehicleModelRepo = vehicleModelRepo;
            _manufacturerRepo = manufacturerRepo;
            _pdfGenerator = pdfGenerator;       


            _partPopup = new PartSearchPopup();
            _partPopup.PartSelected += OnPartSelected;

            InitializeComponent();
            LoadInvoiceList();
            LoadLaborServices();
        }

        public InvoiceForm(VehicleEntity selectedVehicle, CustomerEntity selectedCustomer, SqliteSparePartRepository sparePartRepo, InvoiceService invoiceService, InvoicePdfGenerator pdfGenerator)
        {
            this.selectedVehicle = selectedVehicle;
            this.selectedCustomer = selectedCustomer;
            _sparePartRepo = sparePartRepo;
            _invoiceService = invoiceService;
            this.pdfGenerator = pdfGenerator;
        }

        // ══════════════════════════════════════════════════════════════════
        // UI CONSTRUCTION
        // FIX: the old InitializeComponent only set ClientSize and wired Load.
        // It never built the SplitContainer or called BuildLeftPanel/BuildRightPanel
        // so the form appeared as a blank 282x253 window.
        // ══════════════════════════════════════════════════════════════════

        private void InitializeComponent()
        {
            Text = "WorkshopPro — Invoices";
            Size = new Size(1200, 700);
            MinimumSize = new Size(960, 600);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9f);

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                FixedPanel = FixedPanel.Panel1,
                SplitterWidth = 6
            };

            BuildLeftPanel(split.Panel1);
            BuildRightPanel(split.Panel2);

            Controls.Add(split);

            // Set these AFTER adding to Controls
            split.Panel1MinSize = 360;
            split.Panel2MinSize = 340;

            this.Shown += (s, e) =>
            {
                int dist = Math.Max(split.Panel1MinSize,
                               Math.Min(480, split.Width - split.Panel2MinSize - split.SplitterWidth));
                split.SplitterDistance = dist;
            };
        }

        // ── Left panel: invoice list ───────────────────────────────────────

        private void BuildLeftPanel(SplitterPanel panel)
        {
            panel.BackColor = Color.White;

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Color.FromArgb(30, 30, 60)
            };

            var lblTitle = new Label
            {
                Text = "INVOICES",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                Width = 160,
                Padding = new Padding(10, 0, 0, 0)
            };

            _btnNew = new Button
            {
                Text = "+ New",
                Dock = DockStyle.Right,
                Width = 80,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 180, 120),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnNew.FlatAppearance.BorderSize = 0;
            _btnNew.Click += BtnNew_Click;
            header.Controls.AddRange(new Control[] { lblTitle, _btnNew });

            var searchPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.FromArgb(240, 240, 245),
                Padding = new Padding(8, 6, 8, 6)
            };
            _searchBox = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f) };
            _searchBox.TextChanged += SearchBox_TextChanged;
            searchPanel.Controls.Add(_searchBox);

            _invoiceListGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                GridColor = Color.FromArgb(230, 230, 235),
                Font = new Font("Segoe UI", 9f),
                ColumnHeadersHeight = 32,
                RowTemplate = { Height = 28 }
            };
            _invoiceListGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 245);
            _invoiceListGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            _invoiceListGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 235, 255);
            _invoiceListGrid.DefaultCellStyle.SelectionForeColor = Color.Black;

            _invoiceListGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", Visible = false });
            _invoiceListGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "InvoiceNo", HeaderText = "Invoice No", Width = 110 });
            _invoiceListGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ServiceDate", HeaderText = "Date", Width = 85 });
            _invoiceListGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PlateNumber", HeaderText = "Plate", Width = 80 });
            _invoiceListGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TotalAmount",
                HeaderText = "Total (Rp)",
                Width = 90,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight }
            });
            _invoiceListGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", Width = 70 });

            _invoiceListGrid.CellDoubleClick += InvoiceListGrid_CellDoubleClick;
            _invoiceListGrid.SelectionChanged += InvoiceListGrid_SelectionChanged;

            panel.Controls.Add(_invoiceListGrid);
            panel.Controls.Add(searchPanel);
            panel.Controls.Add(header);
        }

        // ── Right panel: invoice detail ────────────────────────────────────

        private void BuildRightPanel(SplitterPanel panel)
        {
            panel.BackColor = Color.FromArgb(245, 245, 248);
            panel.Padding = new Padding(12, 8, 12, 8);

            _detailPanel = new Panel { Dock = DockStyle.Fill, Visible = false };

            // Top strip: invoice number + status badge
            var topStrip = new Panel { Dock = DockStyle.Top, Height = 36 };
            _lblInvoiceNo = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 60),
                Dock = DockStyle.Left,
                Width = 200,
                TextAlign = ContentAlignment.MiddleLeft
            };
            _lblStatus = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Dock = DockStyle.Left,
                Width = 80,
                TextAlign = ContentAlignment.MiddleCenter
            };
            topStrip.Controls.AddRange(new Control[] { _lblInvoiceNo, _lblStatus });

            // Vehicle lookup group
            var vehicleGroup = new GroupBox
            {
                Text = "Vehicle",
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(8, 4, 8, 4),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            var vehicleRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            _txtPlate = new TextBox
            {
                Width = 120,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                CharacterCasing = CharacterCasing.Upper,
                Margin = new Padding(0, 4, 6, 0)
            };
            _txtPlate.KeyDown += TxtPlate_KeyDown;

            _btnLookup = new Button
            {
                Text = "Lookup",
                Width = 72,
                Height = 26,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 30, 60),
                ForeColor = Color.White,
                Margin = new Padding(0, 3, 12, 0),
                Cursor = Cursors.Hand
            };
            _btnLookup.FlatAppearance.BorderSize = 0;
            _btnLookup.Click += BtnLookup_Click;

            _lblVehicleInfo = new Label
            {
                Text = "Enter a plate number to begin",
                ForeColor = Color.Gray,
                AutoSize = true,
                Margin = new Padding(0, 8, 0, 0)
            };
            _lblCustomerInfo = new Label
            {
                Text = "",
                ForeColor = Color.FromArgb(30, 100, 30),
                AutoSize = true,
                Margin = new Padding(12, 8, 0, 0)
            };
            vehicleRow.Controls.AddRange(new Control[] { _txtPlate, _btnLookup, _lblVehicleInfo, _lblCustomerInfo });
            vehicleGroup.Controls.Add(vehicleRow);

            // New vehicle registration panel (hidden until plate not found)
            _newVehiclePanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 110,          // ← was 100
                BackColor = Color.FromArgb(255, 250, 235),
                Padding = new Padding(6, 4, 6, 4),
                Visible = false
            };
            BuildNewVehiclePanel();

            // Items group
            var itemsGroup = new GroupBox
            {
                Text = "Items",
                Dock = DockStyle.Fill,
                Padding = new Padding(8, 4, 8, 4),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            _itemsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BorderStyle = BorderStyle.None,
                BackgroundColor = Color.White,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9f),
                ColumnHeadersHeight = 28,
                RowTemplate = { Height = 26 }
            };
            _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ItemId", Visible = false });
            _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ItemType", HeaderText = "Type", Width = 70 });
            _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Description", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Qty", HeaderText = "Qty", Width = 45, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice", HeaderText = "Unit Price", Width = 100, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Subtotal", HeaderText = "Subtotal", Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            itemsGroup.Controls.Add(_itemsGrid);

            // Add item toolbar
            var addItemBar = new Panel { Dock = DockStyle.Bottom, Height = 80, BackColor = Color.FromArgb(240, 240, 245), Padding = new Padding(6) };
            BuildAddItemBar(addItemBar);
            itemsGroup.Controls.Add(addItemBar);

            // Bottom action bar
            var bottomBar = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Color.White, Padding = new Padding(6) };
            BuildBottomBar(bottomBar);

            _detailPanel.Controls.Add(itemsGroup);
            _detailPanel.Controls.Add(_newVehiclePanel);
            _detailPanel.Controls.Add(vehicleGroup);
            _detailPanel.Controls.Add(topStrip);
            _detailPanel.Controls.Add(bottomBar);

            var placeholder = new Label
            {
                Name = "Placeholder",
                Text = "Select an invoice from the list\nor click '+ New' to create one.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 11f)
            };

            panel.Controls.Add(_detailPanel);
            panel.Controls.Add(placeholder);
        }

        private void BuildNewVehiclePanel()
        {
            // Warning header
            var lbl = new Label
            {
                Text = "⚠  Plate not found — register this vehicle before creating the invoice:",
                Dock = DockStyle.Top,
                Height = 24,
                ForeColor = Color.FromArgb(180, 90, 0),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Padding = new Padding(4, 4, 0, 0)
            };

            // Use a TableLayoutPanel — gives us a proper label/field grid
            // Java equivalent: GridBagLayout or GroupLayout
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                RowCount = 2,
                Padding = new Padding(4, 2, 4, 2)
            };

            // Column widths: label narrow, field wider, repeat
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110)); // label: Customer Name
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155)); // field: customer name
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 75));  // label: Phone
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130)); // field: phone
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));  // label: Color
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));  // field: color
                                                                             // Row 2 will share columns differently via spanning — handled below

            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // row 0: customer info
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // row 1: vehicle model

            // ── Row 0: Customer Name | Phone | Color | Year ───────────────────────
            _txtCustomerName = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 3, 6, 0) };
            SetPlaceholder(_txtCustomerName, "e.g. Budi Santoso");

            _txtCustomerPhone = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 3, 6, 0) };
            SetPlaceholder(_txtCustomerPhone, "e.g. 08123456789");

            _txtColor = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 3, 6, 0) };
            SetPlaceholder(_txtColor, "e.g. Silver");

            _txtYear = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 3, 0, 0) };
            SetPlaceholder(_txtYear, "e.g. 2019");

            table.Controls.Add(MakeFieldLabel("Customer Name *"), 0, 0);
            table.Controls.Add(_txtCustomerName, 1, 0);
            table.Controls.Add(MakeFieldLabel("Phone"), 2, 0);
            table.Controls.Add(_txtCustomerPhone, 3, 0);
            table.Controls.Add(MakeFieldLabel("Color"), 4, 0);
            table.Controls.Add(_txtColor, 5, 0);

            // Year doesn't fit in row 0 columns — add it as an extra column pair
            // Simplest fix: widen the table to 8 columns by adding Year after Color
            table.ColumnCount = 8;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 45));  // label: Year
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 75));  // field: year
            table.Controls.Add(MakeFieldLabel("Year *"), 6, 0);
            table.Controls.Add(_txtYear, 7, 0);

            // ── Row 1: Manufacturer | Model | Register button ─────────────────────
            _cmbManufacturer = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 3, 6, 0)
            };
            _cmbModel = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 3, 6, 0)
            };
            _btnRegisterVehicle = MakeButton("Register & Continue ➜", Color.FromArgb(0, 120, 200), 165, 24);
            _btnRegisterVehicle.Dock = DockStyle.Fill;
            _btnRegisterVehicle.Margin = new Padding(0, 3, 0, 0);
            _btnRegisterVehicle.Click += BtnRegisterVehicle_Click;

            _cmbManufacturer.SelectedIndexChanged += CmbManufacturer_SelectedIndexChanged;
            _cmbManufacturer.DisplayMember = "Name";
            _cmbManufacturer.ValueMember = "Id";
            _cmbManufacturer.DataSource = _manufacturerRepo.GetAll().OrderBy(m => m.Name).ToList();
            _cmbManufacturer.SelectedIndex = -1;

            // Manufacturer label + field span columns 0-1
            table.Controls.Add(MakeFieldLabel("Manufacturer *"), 0, 1);
            table.Controls.Add(_cmbManufacturer, 1, 1);

            // Model label + field span columns 2-4 (wider — model names are long)
            table.Controls.Add(MakeFieldLabel("Model *"), 2, 1);

            // Span model combobox across 3 columns (Phone + Color + Year label area)
            table.Controls.Add(_cmbModel, 3, 1);
            table.SetColumnSpan(_cmbModel, 3);

            // Register button spans last 2 columns
            table.Controls.Add(_btnRegisterVehicle, 6, 1);
            table.SetColumnSpan(_btnRegisterVehicle, 2);

            _newVehiclePanel.Controls.Add(table);
            _newVehiclePanel.Controls.Add(lbl);
        }

        // Small bold label for field names inside the registration panel
        private static Label MakeFieldLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                Padding = new Padding(0, 0, 6, 0)
            };
        }

        private void BuildAddItemBar(Panel bar)
        {
            var row1 = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 30, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            var lblPart = new Label { Text = "Part:", AutoSize = true, Margin = new Padding(0, 7, 4, 0) };
            _txtPartSearch = new TextBox { Width = 280, Margin = new Padding(0, 4, 6, 0) };
            _txtPartSearch.TextChanged += TxtPartSearch_TextChanged;
            _txtPartSearch.KeyDown += TxtPartSearch_KeyDown;
            _lblPartSearchHint = new Label { Text = "Type 2+ letters to search parts", ForeColor = Color.Gray, AutoSize = true, Margin = new Padding(0, 7, 0, 0), Font = new Font("Segoe UI", 8f) };
            row1.Controls.AddRange(new Control[] { lblPart, _txtPartSearch, _lblPartSearchHint });

            var row2 = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 30, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            var lblLabor = new Label { Text = "Labor:", AutoSize = true, Margin = new Padding(0, 7, 4, 0) };
            _cmbLaborService = new ComboBox { Width = 260, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 4, 6, 0) };
            _cmbLaborService.DisplayMember = "Name";
            _cmbLaborService.ValueMember = "Id";
            _txtLaborPrice = MakeTextBox("Price (Rp)", 100);
            _btnAddLabor = new Button { Text = "Add Labor", Width = 90, Height = 24, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(80, 80, 160), ForeColor = Color.White, Margin = new Padding(4, 3, 0, 0), Cursor = Cursors.Hand };
            _btnAddLabor.FlatAppearance.BorderSize = 0;
            _btnAddLabor.Click += BtnAddLabor_Click;
            _btnRemoveItem = new Button { Text = "Remove", Width = 75, Height = 24, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(200, 50, 50), ForeColor = Color.White, Margin = new Padding(10, 3, 0, 0), Cursor = Cursors.Hand };
            _btnRemoveItem.FlatAppearance.BorderSize = 0;
            _btnRemoveItem.Click += BtnRemoveItem_Click;
            row2.Controls.AddRange(new Control[] { lblLabor, _cmbLaborService, _txtLaborPrice, _btnAddLabor, _btnRemoveItem });

            bar.Controls.Add(row2);
            bar.Controls.Add(row1);
        }

        private void BuildBottomBar(Panel bar)
        {
            _lblTotal = new Label
            {
                Text = "Total:  Rp 0",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Dock = DockStyle.Left,
                Width = 260,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Use a simple FlowLayoutPanel docked Fill on the right
            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0, 4, 0, 0)
            };

            var btnClose = MakeButton("✕ Close", Color.FromArgb(80, 80, 80), 85, 34);
            _btnPrint = MakeButton("🖨 Print PDF", Color.FromArgb(50, 100, 160), 110, 34);
            _btnCancel = MakeButton("Cancel Inv.", Color.FromArgb(190, 50, 50), 100, 34);
            _btnMarkPaid = MakeButton("✔ Mark Paid", Color.FromArgb(0, 155, 80), 105, 34);

            foreach (var b in new[] { btnClose, _btnPrint, _btnCancel, _btnMarkPaid })
                b.Margin = new Padding(4, 0, 4, 0);

            btnClose.Click += (s, e) => Close();
            _btnPrint.Click += BtnPrint_Click;
            _btnCancel.Click += BtnCancelInvoice_Click;
            _btnMarkPaid.Click += BtnMarkPaid_Click;

            // RightToLeft flow — add in reverse visual order
            btnPanel.Controls.AddRange(new Control[] { btnClose, _btnPrint, _btnCancel, _btnMarkPaid });

            bar.Controls.Add(btnPanel);
            bar.Controls.Add(_lblTotal);   // _lblTotal docked Left, added after so it doesn't consume Fill
        }

        // ══════════════════════════════════════════════════════════════════
        // DATA LOADING
        // ══════════════════════════════════════════════════════════════════

        private void LoadInvoiceList(string searchTerm = null)
        {
            var invoices = string.IsNullOrWhiteSpace(searchTerm)
                ? _invoiceService.GetRecentSummaries()
                : _invoiceService.Search(searchTerm);

            _invoiceListGrid.Rows.Clear();
            foreach (var inv in invoices)
            {
                int rowIndex = _invoiceListGrid.Rows.Add(
                    inv.Id, inv.InvoiceNo, inv.ServiceDate,
                    inv.PlateNumber, $"Rp {inv.TotalAmount:#,0}", inv.Status);

                var row = _invoiceListGrid.Rows[rowIndex];
                if (inv.Status == "Paid") row.DefaultCellStyle.ForeColor = Color.FromArgb(0, 130, 60);
                else if (inv.Status == "Cancelled") row.DefaultCellStyle.ForeColor = Color.Gray;
                else row.DefaultCellStyle.ForeColor = Color.Black;
            }
        }

        private void LoadInvoiceDetail(int invoiceId)
        {
            _currentInvoiceId = invoiceId;
            var invoice = _invoiceService.GetById(invoiceId);
            _invoiceIsDraft = invoice.Status == "Draft";

            _lblInvoiceNo.Text = invoice.InvoiceNo;
            SetStatusLabel(invoice.Status);

            _currentVehicle = _vehicleRepo.GetById(invoice.VehicleId);
            _currentCustomer = _customerRepo.GetById(invoice.CustomerId);
            _txtPlate.Text = _currentVehicle?.PlateNumber ?? "";
            UpdateVehicleInfoLabels();
            _newVehiclePanel.Visible = false;

            RefreshItemsGrid();
            SetEditMode(_invoiceIsDraft);

            _detailPanel.Visible = true;
            HidePlaceholder();
        }

        private void LoadLaborServices()
        {
            _cmbLaborService.DataSource = _laborRepo.GetAll().ToList();
        }

        private void RefreshItemsGrid()
        {
            _currentItems = _invoiceService.GetItems(_currentInvoiceId).ToList();
            _itemsGrid.Rows.Clear();
            foreach (var item in _currentItems)
            {
                _itemsGrid.Rows.Add(
                    item.Id, item.ItemType, item.Description,
                    item.Qty, $"Rp {item.UnitPrice:#,0}", $"Rp {item.Subtotal:#,0}");
            }
            UpdateTotalLabel();
        }

        private void UpdateTotalLabel()
        {
            decimal total = _currentItems.Sum(i => i.Subtotal);
            _lblTotal.Text = $"Total: Rp {total:#,0}";
        }

        // ══════════════════════════════════════════════════════════════════
        // EVENT HANDLERS — LEFT PANEL
        // ══════════════════════════════════════════════════════════════════

        private void BtnNew_Click(object sender, EventArgs e)
        {
            _currentInvoiceId = 0;
            _currentVehicle = null;
            _currentCustomer = null;
            _currentItems.Clear();
            _itemsGrid.Rows.Clear();
            _lblInvoiceNo.Text = "(new invoice)";
            SetStatusLabel("Draft");
            _txtPlate.Text = "";
            _lblVehicleInfo.Text = "Enter plate number and click Lookup";
            _lblVehicleInfo.ForeColor = Color.Gray;
            _lblCustomerInfo.Text = "";
            _newVehiclePanel.Visible = false;
            _lblTotal.Text = "Total: Rp 0";
            SetEditMode(true);
            _detailPanel.Visible = true;
            HidePlaceholder();
            _txtPlate.Focus();
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            LoadInvoiceList(_searchBox.Text.Trim());
        }

        private void InvoiceListGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int id = Convert.ToInt32(_invoiceListGrid.Rows[e.RowIndex].Cells["Id"].Value);
            LoadInvoiceDetail(id);
        }

        private void InvoiceListGrid_SelectionChanged(object sender, EventArgs e) { }

        // ══════════════════════════════════════════════════════════════════
        // EVENT HANDLERS — VEHICLE LOOKUP
        // ══════════════════════════════════════════════════════════════════

        private void TxtPlate_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) BtnLookup_Click(sender, e);
        }

        private void BtnLookup_Click(object sender, EventArgs e)
        {
            string plate = _txtPlate.Text.Trim().ToUpper();
            if (string.IsNullOrWhiteSpace(plate)) { MessageBox.Show("Enter a plate number first."); return; }

            _currentVehicle = _vehicleRepo.GetByPlate(plate);

            if (_currentVehicle != null)
            {
                _currentCustomer = _customerRepo.GetById((int)_currentVehicle.CustomerId);
                UpdateVehicleInfoLabels();
                _newVehiclePanel.Visible = false;
                if (_currentInvoiceId == 0) CreateNewInvoiceRecord();
            }
            else
            {
                _lblVehicleInfo.Text = $"'{plate}' not found — register new vehicle:";
                _lblVehicleInfo.ForeColor = Color.DarkOrange;
                _lblCustomerInfo.Text = "";
                _newVehiclePanel.Visible = true;
                var grp = _newVehiclePanel.Parent as GroupBox;
                if (grp != null) grp.Height = 160;
            }
        }

        private void CmbManufacturer_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_cmbManufacturer.SelectedItem == null) return;
            int manufacturerId = ((ManufacturerEntity)_cmbManufacturer.SelectedItem).Id;
            var models = _vehicleModelRepo.GetByManufacturer(manufacturerId)
                                          .OrderBy(m => m.ModelName).ToList();
            _cmbModel.DataSource = models.Select(m => new
            {
                m.Id,
                DisplayText = $"{m.ModelName} {m.EngineCC}cc {m.FuelType} {m.TransmissionType}"
            }).ToList();
            _cmbModel.DisplayMember = "DisplayText";
            _cmbModel.ValueMember = "Id";
        }

        private void BtnRegisterVehicle_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtCustomerName.Text)) { MessageBox.Show("Customer name is required."); return; }
            if (_cmbModel.SelectedValue == null) { MessageBox.Show("Select a vehicle model."); return; }
            if (!int.TryParse(_txtYear.Text, out int year) || year < 1950 || year > DateTime.Now.Year + 1)
            { MessageBox.Show("Enter a valid year."); return; }

            _currentCustomer = _customerRepo.GetOrCreate(
                _txtCustomerName.Text.Trim(),
                _txtCustomerPhone.Text.Trim(), "");

            var vehicle = new VehicleEntity
            {
                PlateNumber = _txtPlate.Text.Trim().ToUpper(),
                CustomerId = _currentCustomer.Id,
                VehicleModelId = Convert.ToInt32(_cmbModel.SelectedValue),
                Color = _txtColor.Text.Trim(),
                Year = year
            };
            vehicle.Id = _vehicleRepo.Insert(vehicle);
            _currentVehicle = vehicle;

            UpdateVehicleInfoLabels();
            _newVehiclePanel.Visible = false;
            CreateNewInvoiceRecord();
        }

        private void CreateNewInvoiceRecord()
        {
            var invoice = _invoiceService.CreateInvoice(_currentVehicle.Id, _currentCustomer.Id);
            _currentInvoiceId = invoice.Id;
            _lblInvoiceNo.Text = invoice.InvoiceNo;
            SetEditMode(true);
            LoadInvoiceList();
        }

        // ══════════════════════════════════════════════════════════════════
        // EVENT HANDLERS — LINE ITEMS
        // ══════════════════════════════════════════════════════════════════

        private void TxtPartSearch_TextChanged(object sender, EventArgs e)
        {
            string term = _txtPartSearch.Text.Trim();
            if (term.Length < 2) { _partPopup.Hide(); return; }
            var results = _sparePartRepo.Search(term).ToList();
            _partPopup.ShowResults(results, _txtPartSearch);
        }

        private void TxtPartSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) _partPopup.Hide();
        }

        private void OnPartSelected(SparePartEntity part)
        {
            if (_currentInvoiceId == 0) { MessageBox.Show("Complete vehicle lookup first."); return; }

            string qtyInput = Microsoft.VisualBasic.Interaction.InputBox(
                $"Add: {part.PartName}\nStock: {part.StockQty} {part.Unit}\nUnit price: Rp {part.PriceSell:#,0}\n\nEnter quantity:",
                "Add Spare Part", "1");

            if (string.IsNullOrWhiteSpace(qtyInput)) return;
            if (!int.TryParse(qtyInput, out int qty) || qty <= 0) { MessageBox.Show("Invalid quantity."); return; }

            try
            {
                _invoiceService.AddSparePartItem(_currentInvoiceId, part.Id, qty, part.PriceSell);
                RefreshItemsGrid();
                LoadInvoiceList();
                _txtPartSearch.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnAddLabor_Click(object sender, EventArgs e)
        {
            if (_currentInvoiceId == 0) { /* same check */ return; }

            if (_cmbLaborService.SelectedItem == null || _cmbLaborService.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a labor service from the dropdown.",
                    "WorkshopPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string desc = ((LaborServiceEntity)_cmbLaborService.SelectedItem).Name;

            string priceText = GetTextOrNull(_txtLaborPrice);
            if (priceText == null)
            {
                MessageBox.Show("Please enter the labor price.\n\nExample: 75000",
                    "WorkshopPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtLaborPrice.Focus(); return;
            }

            string cleaned = new string(priceText.Where(char.IsDigit).ToArray());
            if (!decimal.TryParse(cleaned, out decimal price) || price <= 0)
            {
                MessageBox.Show("Price must be a number greater than zero.",
                    "WorkshopPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtLaborPrice.Focus(); return;
            }

            try
            {
                _invoiceService.AddLaborItem(_currentInvoiceId, desc, price);
                RefreshItemsGrid();
                LoadInvoiceList();
                _cmbLaborService.SelectedIndex = -1;
                _txtLaborPrice.Clear();
                SetPlaceholder(_txtLaborPrice, "Price e.g. 75000");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Cannot Add Item", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnRemoveItem_Click(object sender, EventArgs e)
        {
            if (_itemsGrid.CurrentRow == null) return;
            int itemId = Convert.ToInt32(_itemsGrid.CurrentRow.Cells["ItemId"].Value);
            var item = _currentItems.FirstOrDefault(i => i.Id == itemId);
            if (item == null) return;

            if (MessageBox.Show($"Remove '{item.Description}'?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            _invoiceService.RemoveItem(_currentInvoiceId, item);
            RefreshItemsGrid();
            LoadInvoiceList();
        }

        // ══════════════════════════════════════════════════════════════════
        // EVENT HANDLERS — STATUS ACTIONS
        // ══════════════════════════════════════════════════════════════════

        private void BtnSaveDraft_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Invoice saved as Draft.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadInvoiceList();
        }

        private void BtnMarkPaid_Click(object sender, EventArgs e)
        {
            if (_currentInvoiceId == 0) return;
            if (MessageBox.Show("Mark this invoice as Paid? This cannot be undone.",
                "Confirm Payment", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                _invoiceService.MarkAsPaid(_currentInvoiceId);
                LoadInvoiceDetail(_currentInvoiceId);
                LoadInvoiceList();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private void BtnCancelInvoice_Click(object sender, EventArgs e)
        {
            if (_currentInvoiceId == 0) return;
            if (MessageBox.Show("Cancel this invoice? Stock will be restored.",
                "Confirm Cancellation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                _invoiceService.CancelInvoice(_currentInvoiceId);
                LoadInvoiceDetail(_currentInvoiceId);
                LoadInvoiceList();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            if (_currentInvoiceId == 0) return;
            //TODO Phase 4 PDF integration:
            var invoice = _invoiceService.GetById(_currentInvoiceId);
            var vehicle = _vehicleRepo.GetById(invoice.VehicleId);
            var customer = _customerRepo.GetById(invoice.CustomerId);
            invoice.Items = _invoiceService.GetItems(_currentInvoiceId).ToList();
            string path = _pdfGenerator.Generate(invoice, vehicle, customer);
            InvoicePdfGenerator.OpenPdf(path);
            //MessageBox.Show("Wire up InvoicePdfGenerator here (Phase 4 integration).", "Print");
        }

        // ══════════════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════════════

        private void UpdateVehicleInfoLabels()
        {
            if (_currentVehicle == null) return;
            _lblVehicleInfo.Text = $"Plate: {_currentVehicle.PlateNumber}  |  Year: {_currentVehicle.Year}  |  Color: {_currentVehicle.Color}";
            _lblVehicleInfo.ForeColor = Color.FromArgb(20, 60, 120);
            _lblCustomerInfo.Text = $"Customer: {_currentCustomer?.Name ?? "Unknown"}  {_currentCustomer?.Phone}";
        }

        private void SetStatusLabel(string status)
        {
            _lblStatus.Text = status;
            _lblStatus.ForeColor = Color.White;
            if (status == "Paid") _lblStatus.BackColor = Color.FromArgb(0, 180, 90);
            else if (status == "Cancelled") _lblStatus.BackColor = Color.Gray;
            else _lblStatus.BackColor = Color.FromArgb(200, 140, 0);
        }

        private void SetEditMode(bool editable)
        {
            bool isDraft = editable;
            // Item editing only on Draft
            _txtPartSearch.Enabled = isDraft;
            _cmbLaborService.Enabled = isDraft;
            _txtLaborPrice.Enabled = isDraft;
            _btnAddLabor.Enabled = isDraft;
            _btnRemoveItem.Enabled = isDraft;
            // Status buttons
            _btnMarkPaid.Enabled = isDraft;
            _btnCancel.Enabled = isDraft;
            // Plate lookup only on a brand-new invoice (no ID yet)
            _txtPlate.Enabled = _currentInvoiceId == 0;
            _btnLookup.Enabled = _currentInvoiceId == 0;
            // Print always available when invoice exists
            _btnPrint.Enabled = _currentInvoiceId != 0;
        }

        private void HidePlaceholder()
        {
            var ph = Controls.Find("Placeholder", true).FirstOrDefault();
            if (ph != null) ph.Visible = false;
        }

        private static TextBox MakeTextBox(string placeholder, int width) =>
            new TextBox { Width = width, Margin = new Padding(0, 4, 6, 0) };

        private static Button MakeActionButton(string text, Color backColor)
        {
            var btn = new Button
            {
                Text = text,
                Width = 100,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = Color.White,
                Margin = new Padding(6, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }
        private string GetTextOrNull(TextBox tb)
        {
            if (tb.ForeColor == Color.Gray) return null;
            string v = tb.Text.Trim();
            return string.IsNullOrWhiteSpace(v) ? null : v;
        }
        private static void SetPlaceholder(TextBox tb, string hint)
        {
            tb.Text = hint;
            tb.ForeColor = Color.Gray;

            tb.GotFocus += (s, e) =>
            {
                if (tb.ForeColor == Color.Gray) { tb.Text = ""; tb.ForeColor = Color.Black; }
            };
            tb.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(tb.Text)) { tb.Text = hint; tb.ForeColor = Color.Gray; }
            };
        }

        private static TextBox MakeHintTextBox(string hint, int width)
        {
            var tb = new TextBox { Width = width, Margin = new Padding(0, 4, 6, 0) };
            SetPlaceholder(tb, hint);
            return tb;
        }

        private static Button MakeButton(string text, Color backColor, int width, int height)
        {
            var btn = new Button
            {
                Text = text,
                Width = width,
                Height = height,
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        // ── Required by WinForms partial-class pattern ─────────────────────────
        //private void InitializeComponent()
        //{
        //    this.SuspendLayout();
        //    this.ClientSize = new Size(1200, 720);
        //    this.Name = "InvoiceForm";
        //    this.ResumeLayout(false);
        //}
    }
}
