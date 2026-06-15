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
    public partial class ServiceHistoryForm : Form
    {
        private readonly InvoiceService _invoiceService;
        private readonly IVehicleRepository _vehicleRepo;
        private readonly ICustomerRepository _customerRepo;
        private readonly SqliteInvoiceRepository _invoiceRepo;
        private readonly InvoicePdfGenerator _pdfGenerator;

        // ── Left panel ─────────────────────────────────────────────────────────
        private TextBox _txtPlate;
        private TextBox _txtCustomer;
        private DateTimePicker _dtFrom;
        private DateTimePicker _dtTo;
        private Button _btnSearchPlate;
        private Button _btnSearchCustomer;
        private Button _btnSearchDate;
        private Button _btnShowAll;
        private DataGridView _resultsGrid;
        private Label _lblResultCount;

        // ── Right panel ────────────────────────────────────────────────────────
        private Label _lblDetailHeader;
        private Label _lblVehicleInfo;
        private Label _lblCustomerInfo;
        private DataGridView _detailGrid;
        private Label _lblDetailTotal;
        private Label _lblDetailStatus;
        private Button _btnPrint;

        public ServiceHistoryForm(
            InvoiceService invoiceService,
            IVehicleRepository vehicleRepo,
            ICustomerRepository customerRepo,
            SqliteInvoiceRepository invoiceRepo,
            InvoicePdfGenerator pdfGenerator)
        {
            _invoiceService = invoiceService;
            _vehicleRepo = vehicleRepo;
            _customerRepo = customerRepo;
            _invoiceRepo = invoiceRepo;
            _pdfGenerator = pdfGenerator;

            BuildForm();
            ShowAll(); // load recent invoices on open
        }

        // ══════════════════════════════════════════════════════════════════════
        // UI CONSTRUCTION
        // ══════════════════════════════════════════════════════════════════════

        private void BuildForm()
        {
            Text = "WorkshopPro — Service History";
            Size = new Size(1100, 680);
            MinimumSize = new Size(900, 560);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9f);

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 6
            };

            BuildLeftPanel(split.Panel1);
            BuildRightPanel(split.Panel2);
            Controls.Add(split);

            split.Panel1MinSize = 400;
            split.Panel2MinSize = 320;
            Shown += (s, e) => split.SplitterDistance =
                Math.Min(560, split.Width - split.Panel2MinSize - split.SplitterWidth);
        }

        private void BuildLeftPanel(SplitterPanel panel)
        {
            panel.BackColor = Color.White;

            // ── Header ────────────────────────────────────────────────────────
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(130, 70, 160)
            };
            var lblTitle = new Label
            {
                Text = "SERVICE HISTORY",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0)
            };
            header.Controls.Add(lblTitle);

            // ── Search controls ───────────────────────────────────────────────
            var searchPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 130,
                BackColor = Color.FromArgb(248, 245, 252),
                Padding = new Padding(10, 8, 10, 6)
            };

            // Row 1: plate search
            var row1 = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 32,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            var lbl1 = new Label { Text = "Plate No:", AutoSize = true, Margin = new Padding(0, 7, 4, 0), Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            _txtPlate = new TextBox { Width = 130, CharacterCasing = CharacterCasing.Upper, Margin = new Padding(0, 4, 6, 0) };
            _txtPlate.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; SearchByPlate(); } };
            _btnSearchPlate = MakeSearchButton("Search by Plate", Color.FromArgb(33, 97, 140));
            _btnSearchPlate.Click += (s, e) => SearchByPlate();
            row1.Controls.AddRange(new Control[] { lbl1, _txtPlate, _btnSearchPlate });

            // Row 2: customer name search
            var row2 = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 32,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            var lbl2 = new Label { Text = "Customer:", AutoSize = true, Margin = new Padding(0, 7, 4, 0), Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            _txtCustomer = new TextBox { Width = 180, Margin = new Padding(0, 4, 6, 0) };
            _txtCustomer.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; SearchByCustomer(); } };
            _btnSearchCustomer = MakeSearchButton("Search by Customer", Color.FromArgb(33, 97, 140));
            _btnSearchCustomer.Click += (s, e) => SearchByCustomer();
            row2.Controls.AddRange(new Control[] { lbl2, _txtCustomer, _btnSearchCustomer });

            // Row 3: date range
            var row3 = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 32,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            var lbl3a = new Label { Text = "From:", AutoSize = true, Margin = new Padding(0, 7, 4, 0), Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            _dtFrom = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short, Margin = new Padding(0, 3, 6, 0), Value = DateTime.Today.AddMonths(-1) };
            var lbl3b = new Label { Text = "To:", AutoSize = true, Margin = new Padding(0, 7, 4, 0), Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            _dtTo = new DateTimePicker { Width = 120, Format = DateTimePickerFormat.Short, Margin = new Padding(0, 3, 6, 0), Value = DateTime.Today };
            _btnSearchDate = MakeSearchButton("Search by Date", Color.FromArgb(33, 97, 140));
            _btnSearchDate.Click += (s, e) => SearchByDateRange();
            _btnShowAll = MakeSearchButton("Show All", Color.FromArgb(80, 80, 80));
            _btnShowAll.Click += (s, e) => ShowAll();
            row3.Controls.AddRange(new Control[] { lbl3a, _dtFrom, lbl3b, _dtTo, _btnSearchDate, _btnShowAll });

            searchPanel.Controls.Add(row3);
            searchPanel.Controls.Add(row2);
            searchPanel.Controls.Add(row1);

            // ── Result count label ────────────────────────────────────────────
            _lblResultCount = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                Text = "",
                ForeColor = Color.FromArgb(100, 100, 100),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                Padding = new Padding(10, 4, 0, 0),
                BackColor = Color.White
            };

            // ── Results grid ──────────────────────────────────────────────────
            _resultsGrid = new DataGridView
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
                ColumnHeadersHeight = 30,
                RowTemplate = { Height = 26 }
            };
            _resultsGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(130, 70, 160);
            _resultsGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _resultsGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            _resultsGrid.EnableHeadersVisualStyles = false;
            _resultsGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(240, 220, 255);
            _resultsGrid.DefaultCellStyle.SelectionForeColor = Color.Black;

            _resultsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", Visible = false });
            _resultsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "InvoiceNo", HeaderText = "Invoice No", Width = 115 });
            _resultsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ServiceDate", HeaderText = "Date", Width = 90 });
            _resultsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PlateNumber", HeaderText = "Plate", Width = 85 });
            _resultsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CustomerName", HeaderText = "Customer", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _resultsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TotalAmount",
                HeaderText = "Total (Rp)",
                Width = 100,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight }
            });
            _resultsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", Width = 72 });

            _resultsGrid.SelectionChanged += ResultsGrid_SelectionChanged;

            panel.Controls.Add(_resultsGrid);
            panel.Controls.Add(_lblResultCount);
            panel.Controls.Add(searchPanel);
            panel.Controls.Add(header);
        }

        private void BuildRightPanel(SplitterPanel panel)
        {
            panel.BackColor = Color.FromArgb(248, 245, 252);
            panel.Padding = new Padding(10, 8, 10, 8);

            // Header
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 70 };
            _lblDetailHeader = new Label
            {
                Text = "Select an invoice from the list",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 30, 100),
                Dock = DockStyle.Top,
                Height = 28
            };
            _lblDetailStatus = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                Width = 90,
                Height = 22,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 30)
            };
            _lblVehicleInfo = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(50, 50, 130),
                Dock = DockStyle.Bottom,
                Height = 18
            };
            _lblCustomerInfo = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(20, 100, 50),
                Dock = DockStyle.Bottom,
                Height = 18
            };
            pnlTop.Controls.AddRange(new Control[] { _lblDetailHeader, _lblDetailStatus, _lblVehicleInfo, _lblCustomerInfo });

            // Items detail grid
            _detailGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                Font = new Font("Segoe UI", 9f),
                ColumnHeadersHeight = 28,
                RowTemplate = { Height = 24 }
            };
            _detailGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(130, 70, 160);
            _detailGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _detailGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            _detailGrid.EnableHeadersVisualStyles = false;

            _detailGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ItemType", HeaderText = "Type", Width = 75 });
            _detailGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Description", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _detailGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Qty", HeaderText = "Qty", Width = 42, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            _detailGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitPrice", HeaderText = "Unit Price", Width = 100, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
            _detailGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Subtotal", HeaderText = "Subtotal", Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });

            // Bottom bar
            var bottomBar = new Panel { Dock = DockStyle.Bottom, Height = 46, BackColor = Color.White };
            _lblDetailTotal = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Dock = DockStyle.Left,
                Width = 250,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0)
            };
            _btnPrint = new Button
            {
                Text = "🖨 Print / Open PDF",
                Dock = DockStyle.Right,
                Width = 150,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 100, 160),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            var btnClose = new Button
            {
                Text = "✕ Close",
                Dock = DockStyle.Right,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(255, 44, 44),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();
            //Controls.Add(btnClose);
            _btnPrint.FlatAppearance.BorderSize = 0;
            _btnPrint.Click += BtnPrint_Click;
            bottomBar.Controls.AddRange(new Control[] { _lblDetailTotal, _btnPrint, btnClose });

            panel.Controls.Add(_detailGrid);
            panel.Controls.Add(pnlTop);
            panel.Controls.Add(bottomBar);
        }

        // ══════════════════════════════════════════════════════════════════════
        // SEARCH METHODS
        // ══════════════════════════════════════════════════════════════════════

        private void ShowAll()
        {
            var results = _invoiceService.GetRecentSummaries().ToList();
            PopulateGrid(results, "Showing 200 most recent invoices");
        }

        private void SearchByPlate()
        {
            string plate = _txtPlate.Text.Trim().ToUpper();
            if (string.IsNullOrWhiteSpace(plate))
            {
                MessageBox.Show("Enter a plate number to search.",
                    "WorkshopPro", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var vehicle = _vehicleRepo.GetByPlate(plate);
            if (vehicle == null)
            {
                MessageBox.Show($"No vehicle found with plate '{plate}'.",
                    "WorkshopPro", MessageBoxButtons.OK, MessageBoxIcon.Information);
                PopulateGrid(new List<InvoiceSummaryEntity>(), "No results");
                return;
            }

            var invoices = _invoiceRepo.GetByVehicle(vehicle.Id).ToList();
            var summaries = invoices.Select(inv => new InvoiceSummaryEntity
            {
                Id = inv.Id,
                InvoiceNo = inv.InvoiceNo,
                ServiceDate = inv.ServiceDate,
                PlateNumber = plate,
                CustomerName = _customerRepo.GetById(inv.CustomerId)?.Name ?? "-",
                TotalAmount = inv.TotalAmount,
                Status = inv.Status
            }).ToList();

            PopulateGrid(summaries, $"Found {summaries.Count} invoice(s) for plate {plate}");
        }

        private void SearchByCustomer()
        {
            string term = _txtCustomer.Text.Trim();
            if (string.IsNullOrWhiteSpace(term))
            {
                MessageBox.Show("Enter a customer name or phone to search.",
                    "WorkshopPro", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var results = _invoiceService.Search(term).ToList();
            PopulateGrid(results, $"Found {results.Count} invoice(s) matching '{term}'");
        }

        private void SearchByDateRange()
        {
            if (_dtFrom.Value.Date > _dtTo.Value.Date)
            {
                MessageBox.Show("'From' date must be before or equal to 'To' date.",
                    "WorkshopPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var invoices = _invoiceRepo.GetByDateRange(_dtFrom.Value, _dtTo.Value).ToList();
            var summaries = invoices.Select(inv => new InvoiceSummaryEntity
            {
                Id = inv.Id,
                InvoiceNo = inv.InvoiceNo,
                ServiceDate = inv.ServiceDate,
                PlateNumber = _vehicleRepo.GetById(inv.VehicleId)?.PlateNumber ?? "-",
                CustomerName = _customerRepo.GetById(inv.CustomerId)?.Name ?? "-",
                TotalAmount = inv.TotalAmount,
                Status = inv.Status
            }).ToList();

            decimal grandTotal = summaries.Sum(s => s.TotalAmount);
            PopulateGrid(summaries,
                $"Found {summaries.Count} invoice(s)  |  Total revenue: Rp {grandTotal:N0}");
        }

        private void PopulateGrid(List<InvoiceSummaryEntity> items, string statusText)
        {
            _resultsGrid.Rows.Clear();
            foreach (var inv in items)
            {
                int ri = _resultsGrid.Rows.Add(
                    inv.Id, inv.InvoiceNo, inv.ServiceDate,
                    inv.PlateNumber, inv.CustomerName,
                    $"Rp {inv.TotalAmount:#,0}", inv.Status);

                var row = _resultsGrid.Rows[ri];
                if (inv.Status == "Paid") row.DefaultCellStyle.ForeColor = Color.FromArgb(0, 130, 60);
                else if (inv.Status == "Cancelled") row.DefaultCellStyle.ForeColor = Color.Gray;
            }
            _lblResultCount.Text = "  " + statusText;
            ClearDetail();
        }

        // ══════════════════════════════════════════════════════════════════════
        // DETAIL PANEL
        // ══════════════════════════════════════════════════════════════════════

        private void ResultsGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (_resultsGrid.SelectedRows.Count == 0) return;
            int invoiceId = Convert.ToInt32(_resultsGrid.SelectedRows[0].Cells["Id"].Value);
            LoadDetail(invoiceId);
        }

        private void LoadDetail(int invoiceId)
        {
            var invoice = _invoiceService.GetById(invoiceId);
            if (invoice == null) return;

            var vehicle = _vehicleRepo.GetById(invoice.VehicleId);
            var customer = _customerRepo.GetById(invoice.CustomerId);
            var items = _invoiceService.GetItems(invoiceId).ToList();

            _lblDetailHeader.Text = invoice.InvoiceNo;
            SetStatusBadge(invoice.Status);

            _lblVehicleInfo.Text = vehicle != null
                ? $"Plate: {vehicle.PlateNumber}   Year: {vehicle.Year}   Color: {vehicle.Color}"
                : "";
            _lblCustomerInfo.Text = customer != null
                ? $"Customer: {customer.Name}   {customer.Phone}"
                : "";

            _detailGrid.Rows.Clear();
            foreach (var item in items)
            {
                _detailGrid.Rows.Add(
                    item.ItemType, item.Description, item.Qty,
                    $"Rp {item.UnitPrice:#,0}",
                    $"Rp {item.Subtotal:#,0}");
            }

            decimal total = items.Sum(i => i.Subtotal);
            _lblDetailTotal.Text = $"Total:  Rp {total:#,0}";

            // Store invoice for print
            _detailGrid.Tag = invoice;
            _btnPrint.Enabled = true;
            _btnPrint.Tag = new object[] { invoice, vehicle, customer, items };
        }

        private void ClearDetail()
        {
            _lblDetailHeader.Text = "Select an invoice from the list";
            _lblDetailStatus.Text = "";
            _lblDetailStatus.BackColor = Color.Transparent;
            _lblVehicleInfo.Text = "";
            _lblCustomerInfo.Text = "";
            _detailGrid.Rows.Clear();
            _lblDetailTotal.Text = "";
            _btnPrint.Enabled = false;
        }

        private void SetStatusBadge(string status)
        {
            _lblDetailStatus.Text = "  " + status + "  ";
            _lblDetailStatus.ForeColor = Color.White;
            _lblDetailStatus.BackColor = status == "Paid" ? Color.FromArgb(0, 155, 80)
                                       : status == "Cancelled" ? Color.Gray
                                       : Color.FromArgb(195, 135, 0);
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            if (_btnPrint.Tag == null) return;
            var args = (object[])_btnPrint.Tag;
            var invoice = (InvoiceEntity)args[0];
            var vehicle = (VehicleEntity)args[1];
            var customer = (CustomerEntity)args[2];
            var items = (List<InvoiceItemEntity>)args[3];
            invoice.Items = items;

            try
            {
                string path = _pdfGenerator.Generate(invoice, vehicle, customer);
                InvoicePdfGenerator.OpenPdf(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not generate PDF:\n\n{ex.Message}",
                    "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static Button MakeSearchButton(string text, Color color)
        {
            var btn = new Button
            {
                Text = text,
                Height = 24,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                BackColor = color,
                ForeColor = Color.White,
                Margin = new Padding(4, 3, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

    }
}
