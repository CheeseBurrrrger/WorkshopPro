using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WorkshopPro.Application;
using WorkshopPro.Domain;
using WorkshopPro.Infrastructure.Repositories;

namespace WorkshopPro.WinForms
{
    public class SparePartForm: Form
    {
        private readonly SqliteSparePartRepository _repo;
        private readonly InventoryService _inventoryService;

        private List<SparePartEntity> _allParts = new List<SparePartEntity>();
        private SparePartEntity _selectedPart = null;
        private bool _isNewPartMode = false;


        private TextBox _txtSearch;
        private ComboBox _cmbCategoryFilter;
        private DataGridView _grid;
        private Button _btnAddNew;


        private TabControl _tabs;

        // Details tab
        private TextBox _txtPartCode;
        private TextBox _txtPartName;
        private ComboBox _cmbCategory;
        private TextBox _txtUnit;
        private TextBox _txtPriceBuy;
        private TextBox _txtPriceSell;
        private Label _lblStockQty;
        private Button _btnSave;
        private Button _btnCancelNew;

        // Stock-in panel
        private TextBox _txtStockInQty;
        private TextBox _txtStockInRef;
        private Button _btnReceive;

        // Movement history tab
        private DataGridView _gridHistory;

        // ── Fixed category list ───────────────────────────────────────────────
        private static readonly string[] Categories = new[]
        {
            "Engine", "Transmission", "Brake", "Suspension", "Electrical",
            "Body", "Cooling", "Exhaust", "Filter", "Fluid", "Tire"
        };

        // ─────────────────────────────────────────────────────────────────────
        public SparePartForm(SqliteSparePartRepository repo, InventoryService inventoryService)
        {
            _repo = repo;
            _inventoryService = inventoryService;
            BuildUI();
            LoadParts();
        }

        // ═════════════════════════════════════════════════════════════════════
        // UI CONSTRUCTION
        // ═════════════════════════════════════════════════════════════════════

        private void BuildUI()
        {
            Text = "WorkshopPro — Spare Part Inventory";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1000, 640);
            MinimumSize = new Size(900, 580);
            Font = new Font("Segoe UI", 9);

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
            var btnClose = new Button
            {
                Text = "✕ Close",
                Dock = DockStyle.Bottom,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();
            Controls.Add(btnClose);
        }

        // ── LEFT PANEL ────────────────────────────────────────────────────────

        private void BuildLeftPanel(SplitterPanel panel)
        {
            // Toolbar row
            var toolRow = new Panel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(6, 6, 6, 0) };

            _txtSearch = new TextBox
            {
                Width = 180,
                Location = new Point(6, 10)
            };
            _txtSearch.TextChanged += (s, e) => ApplyFilter();

            _cmbCategoryFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 130,
                Location = new Point(194, 10)
            };
            _cmbCategoryFilter.Items.Add("All Categories");
            foreach (var c in Categories) _cmbCategoryFilter.Items.Add(c);
            _cmbCategoryFilter.SelectedIndex = 0;
            _cmbCategoryFilter.SelectedIndexChanged += (s, e) => ApplyFilter();

            _btnAddNew = new Button
            {
                Text = "+ New Part",
                Width = 90,
                Height = 26,
                Location = new Point(332, 8),
                BackColor = Color.FromArgb(33, 97, 140),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnAddNew.FlatAppearance.BorderSize = 0;
            _btnAddNew.Click += BtnAddNew_Click;

            toolRow.Controls.AddRange(new Control[] { _txtSearch, _cmbCategoryFilter, _btnAddNew });

            // Grid
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(220, 220, 220),
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(33, 97, 140),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    Padding = new Padding(4, 0, 0, 0)
                },
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 32,
                RowTemplate = { Height = 26 }
            };
            _grid.EnableHeadersVisualStyles = false;
            _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(214, 234, 248);
            _grid.DefaultCellStyle.SelectionForeColor = Color.Black;

            // Columns
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCode", HeaderText = "Code", FillWeight = 18 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Part Name", FillWeight = 32 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCategory", HeaderText = "Category", FillWeight = 18 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUnit", HeaderText = "Unit", FillWeight = 10 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStock", HeaderText = "Stock", FillWeight = 10 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSell", HeaderText = "Sell Price", FillWeight = 14 });

            _grid.CellFormatting += Grid_CellFormatting;
            _grid.SelectionChanged += Grid_SelectionChanged;

            panel.Controls.Add(_grid);
            panel.Controls.Add(toolRow);
        }

        // ── RIGHT PANEL ───────────────────────────────────────────────────────

        private void BuildRightPanel(SplitterPanel panel)
        {
            _tabs = new TabControl { Dock = DockStyle.Fill };

            var tabDetails = new TabPage("Part Details");
            var tabHistory = new TabPage("Movement History");

            BuildDetailsTab(tabDetails);
            BuildHistoryTab(tabHistory);

            _tabs.TabPages.Add(tabDetails);
            _tabs.TabPages.Add(tabHistory);

            panel.Controls.Add(_tabs);
        }

        private void BuildDetailsTab(TabPage tab)
        {
            tab.Padding = new Padding(10);

            // ── Part fields ───────────────────────────────────────────────────
            int lx = 10, fx = 110, fy = 16, gap = 32;

            Label Lbl(string t, int y) => new Label
            {
                Text = t,
                Location = new Point(lx, y + 3),
                AutoSize = true,
                ForeColor = Color.FromArgb(60, 60, 60)
            };

            _txtPartCode = Field(fx, fy); tab.Controls.Add(Lbl("Part Code", fy)); tab.Controls.Add(_txtPartCode); fy += gap;
            _txtPartName = Field(fx, fy, wide: true); tab.Controls.Add(Lbl("Part Name", fy)); tab.Controls.Add(_txtPartName); fy += gap;

            _cmbCategory = new ComboBox
            {
                Location = new Point(fx, fy),
                Width = 160,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (var c in Categories) _cmbCategory.Items.Add(c);
            tab.Controls.Add(Lbl("Category", fy));
            tab.Controls.Add(_cmbCategory);
            fy += gap;

            _txtUnit = Field(fx, fy, 80); tab.Controls.Add(Lbl("Unit", fy)); tab.Controls.Add(_txtUnit); fy += gap;
            _txtPriceBuy = Field(fx, fy, 100); tab.Controls.Add(Lbl("Buy Price", fy)); tab.Controls.Add(_txtPriceBuy); fy += gap;
            _txtPriceSell = Field(fx, fy, 100); tab.Controls.Add(Lbl("Sell Price", fy)); tab.Controls.Add(_txtPriceSell); fy += gap;

            _lblStockQty = new Label
            {
                Text = "Stock: —",
                Location = new Point(fx, fy),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            tab.Controls.Add(Lbl("Current Stock", fy));
            tab.Controls.Add(_lblStockQty);
            fy += gap + 8;

            // Save / Cancel buttons
            _btnSave = new Button
            {
                Text = "Save Changes",
                Location = new Point(fx, fy),
                Size = new Size(110, 28),
                BackColor = Color.FromArgb(30, 132, 73),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.Click += BtnSave_Click;
            tab.Controls.Add(_btnSave);

            _btnCancelNew = new Button
            {
                Text = "Cancel",
                Location = new Point(fx + 118, fy),
                Size = new Size(70, 28),
                Visible = false
            };
            _btnCancelNew.Click += (s, e) => { _isNewPartMode = false; LoadSelectedPartIntoPanel(_selectedPart); };
            tab.Controls.Add(_btnCancelNew);

            fy += gap + 16;

            // ── Separator ─────────────────────────────────────────────────────
            var sep = new Label
            {
                Text = "── Stock In ─────────────────────",
                Location = new Point(lx, fy),
                AutoSize = true,
                ForeColor = Color.FromArgb(46, 134, 193),
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            tab.Controls.Add(sep);
            fy += 22;

            // Stock-in fields
            _txtStockInQty = Field(fx, fy, 70);
            tab.Controls.Add(Lbl("Qty to Add", fy));
            tab.Controls.Add(_txtStockInQty);
            fy += gap;

            _txtStockInRef = Field(fx, fy, wide: true);
            tab.Controls.Add(Lbl("Reference", fy));
            tab.Controls.Add(_txtStockInRef);
            fy += gap;

            _btnReceive = new Button
            {
                Text = "Receive Stock ↑",
                Location = new Point(fx, fy),
                Size = new Size(130, 28),
                BackColor = Color.FromArgb(46, 134, 193),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            _btnReceive.FlatAppearance.BorderSize = 0;
            _btnReceive.Click += BtnReceive_Click;
            tab.Controls.Add(_btnReceive);

            // Start disabled until a part is selected
            SetDetailsPanelEnabled(false);
        }

        private TextBox Field(int x, int y, int w = 160, bool wide = false)
            => new TextBox { Location = new Point(x, y), Width = wide ? 220 : w };

        private void BuildHistoryTab(TabPage tab)
        {
            _gridHistory = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(220, 220, 220),
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(33, 97, 140),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold)
                },
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 32,
                RowTemplate = { Height = 26 }
            };
            _gridHistory.EnableHeadersVisualStyles = false;
            _gridHistory.DefaultCellStyle.SelectionBackColor = Color.FromArgb(214, 234, 248);
            _gridHistory.DefaultCellStyle.SelectionForeColor = Color.Black;

            _gridHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate", HeaderText = "Date / Time", FillWeight = 28 });
            _gridHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "colType", HeaderText = "Type", FillWeight = 12 });
            _gridHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty", HeaderText = "Qty", FillWeight = 10 });
            _gridHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRef", HeaderText = "Reference", FillWeight = 25 });
            _gridHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNotes", HeaderText = "Notes", FillWeight = 25 });

            _gridHistory.CellFormatting += GridHistory_CellFormatting;

            tab.Controls.Add(_gridHistory);
        }

        // ═════════════════════════════════════════════════════════════════════
        // DATA LOADING
        // ═════════════════════════════════════════════════════════════════════

        private void LoadParts()
        {
            _allParts = _repo.GetAll().ToList();
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string search = _txtSearch.Text.Trim().ToLowerInvariant();
            string category = _cmbCategoryFilter.SelectedIndex <= 0
                ? null
                : _cmbCategoryFilter.SelectedItem.ToString();

            var filtered = _allParts
                .Where(p =>
                    (string.IsNullOrEmpty(search) ||
                     p.PartName.ToLowerInvariant().Contains(search) ||
                     p.PartCode.ToLowerInvariant().Contains(search))
                    &&
                    (category == null || p.Category == category))
                .ToList();

            RefreshGrid(filtered);
        }

        private void RefreshGrid(List<SparePartEntity> parts)
        {
            int selectedId = _selectedPart?.Id ?? -1;

            _grid.SuspendLayout();
            _grid.Rows.Clear();

            foreach (var p in parts)
            {
                int row = _grid.Rows.Add(
                    p.PartCode,
                    p.PartName,
                    p.Category,
                    p.Unit,
                    p.StockQty,
                    p.PriceSell.ToString("N0"));

                _grid.Rows[row].Tag = p;

                // Re-select the previously selected part
                if (p.Id == selectedId)
                    _grid.Rows[row].Selected = true;
            }

            _grid.ResumeLayout();
        }

        private void LoadMovementHistory(int sparePartId)
        {
            _gridHistory.Rows.Clear();
            var movements = _repo.GetMovements(sparePartId).ToList();
            foreach (var m in movements)
            {
                _gridHistory.Rows.Add(
                    m.CreatedAt,
                    m.MovementType,
                    m.Qty,
                    m.Reference,
                    m.Notes);
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // EVENTS — GRID
        // ═════════════════════════════════════════════════════════════════════

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            if (_grid.SelectedRows.Count == 0) return;
            var part = _grid.SelectedRows[0].Tag as SparePartEntity;
            if (part == null) return;

            _selectedPart = part;
            _isNewPartMode = false;
            LoadSelectedPartIntoPanel(part);
            LoadMovementHistory(part.Id);
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (_grid.Columns[e.ColumnIndex].Name == "colStock" && e.Value != null)
            {
                if (int.TryParse(e.Value.ToString(), out int qty) && qty <= 0)
                {
                    e.CellStyle.ForeColor = Color.Red;
                    e.CellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                }
            }
        }

        private void GridHistory_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (_gridHistory.Columns[e.ColumnIndex].Name == "colType" && e.Value != null)
            {
                e.CellStyle.ForeColor = e.Value.ToString() == "IN"
                    ? Color.FromArgb(30, 132, 73)
                    : Color.FromArgb(192, 57, 43);
                e.CellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // EVENTS — BUTTONS
        // ═════════════════════════════════════════════════════════════════════

        private void BtnAddNew_Click(object sender, EventArgs e)
        {
            _isNewPartMode = true;
            _selectedPart = null;
            _grid.ClearSelection();
            ClearDetailsPanel();
            SetDetailsPanelEnabled(true);
            _btnReceive.Enabled = false;        // no stock-in until part is saved
            _btnCancelNew.Visible = true;
            _btnSave.Text = "Create Part";
            _tabs.SelectedIndex = 0;
            _txtPartCode.Focus();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!ValidateFields()) return;

            decimal.TryParse(_txtPriceBuy.Text, out decimal priceBuy);
            decimal.TryParse(_txtPriceSell.Text, out decimal priceSell);

            if (_isNewPartMode)
            {
                var newPart = new SparePartEntity
                {
                    PartCode = _txtPartCode.Text.Trim().ToUpperInvariant(),
                    PartName = _txtPartName.Text.Trim(),
                    Category = _cmbCategory.SelectedItem?.ToString() ?? "",
                    Unit = _txtUnit.Text.Trim(),
                    StockQty = 0,
                    PriceBuy = priceBuy,
                    PriceSell = priceSell
                };

                try
                {
                    int newId = _repo.Insert(newPart);
                    newPart.Id = newId;
                    _selectedPart = newPart;
                    _isNewPartMode = false;
                    _btnSave.Text = "Save Changes";
                    _btnCancelNew.Visible = false;
                    _btnReceive.Enabled = true;

                    MessageBox.Show($"Part '{newPart.PartName}' created successfully.",
                        "WorkshopPro", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to create part:\n{ex.Message}",
                        "WorkshopPro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else
            {
                if (_selectedPart == null) return;

                _selectedPart.PartCode = _txtPartCode.Text.Trim().ToUpperInvariant();
                _selectedPart.PartName = _txtPartName.Text.Trim();
                _selectedPart.Category = _cmbCategory.SelectedItem?.ToString() ?? "";
                _selectedPart.Unit = _txtUnit.Text.Trim();
                _selectedPart.PriceBuy = priceBuy;
                _selectedPart.PriceSell = priceSell;

                try
                {
                    _repo.Update(_selectedPart);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save changes:\n{ex.Message}",
                        "WorkshopPro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            LoadParts();
        }

        private void BtnReceive_Click(object sender, EventArgs e)
        {
            if (_selectedPart == null) return;

            if (!int.TryParse(_txtStockInQty.Text.Trim(), out int qty) || qty <= 0)
            {
                MessageBox.Show("Please enter a valid positive quantity.",
                    "WorkshopPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtStockInQty.Focus();
                return;
            }

            string reference = _txtStockInRef.Text.Trim();
            if (string.IsNullOrEmpty(reference))
            {
                MessageBox.Show("Please enter a reference (e.g. PO number or supplier name).",
                    "WorkshopPro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtStockInRef.Focus();
                return;
            }

            try
            {
                _inventoryService.ReceiveStock(_selectedPart.Id, qty, reference);

                // Refresh local part data
                _selectedPart = _repo.GetById(_selectedPart.Id);
                _lblStockQty.Text = $"Stock: {_selectedPart.StockQty}";
                _lblStockQty.ForeColor = _selectedPart.StockQty > 0
                    ? Color.FromArgb(30, 132, 73)
                    : Color.Red;

                _txtStockInQty.Clear();
                _txtStockInRef.Clear();

                LoadParts();
                LoadMovementHistory(_selectedPart.Id);

                MessageBox.Show(
                    $"Received {qty} unit(s) of '{_selectedPart.PartName}'.\nNew stock: {_selectedPart.StockQty}",
                    "WorkshopPro", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Stock receive failed:\n{ex.Message}",
                    "WorkshopPro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // HELPERS
        // ═════════════════════════════════════════════════════════════════════

        private void LoadSelectedPartIntoPanel(SparePartEntity part)
        {
            if (part == null) return;

            _txtPartCode.Text = part.PartCode;
            _txtPartName.Text = part.PartName;
            _txtUnit.Text = part.Unit;
            _txtPriceBuy.Text = part.PriceBuy.ToString("N2");
            _txtPriceSell.Text = part.PriceSell.ToString("N2");

            int catIdx = Array.IndexOf(Categories, part.Category);
            _cmbCategory.SelectedIndex = catIdx >= 0 ? catIdx : 0;

            _lblStockQty.Text = $"Stock: {part.StockQty}";
            _lblStockQty.ForeColor = part.StockQty > 0
                ? Color.FromArgb(30, 132, 73)
                : Color.Red;

            _btnSave.Text = "Save Changes";
            _btnCancelNew.Visible = false;
            _btnReceive.Enabled = true;

            SetDetailsPanelEnabled(true);
        }

        private void ClearDetailsPanel()
        {
            _txtPartCode.Clear();
            _txtPartName.Clear();
            _txtUnit.Clear();
            _txtPriceBuy.Clear();
            _txtPriceSell.Clear();
            _cmbCategory.SelectedIndex = -1;
            _lblStockQty.Text = "Stock: —";
            _lblStockQty.ForeColor = Color.FromArgb(60, 60, 60);
            _gridHistory.Rows.Clear();
        }

        private void SetDetailsPanelEnabled(bool enabled)
        {
            _txtPartCode.Enabled = enabled;
            _txtPartName.Enabled = enabled;
            _cmbCategory.Enabled = enabled;
            _txtUnit.Enabled = enabled;
            _txtPriceBuy.Enabled = enabled;
            _txtPriceSell.Enabled = enabled;
            _btnSave.Enabled = enabled;
            _txtStockInQty.Enabled = enabled;
            _txtStockInRef.Enabled = enabled;
        }

        private bool ValidateFields()
        {
            if (string.IsNullOrWhiteSpace(_txtPartCode.Text))
            {
                MessageBox.Show("Part Code is required.", "WorkshopPro",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtPartCode.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(_txtPartName.Text))
            {
                MessageBox.Show("Part Name is required.", "WorkshopPro",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtPartName.Focus();
                return false;
            }
            if (_cmbCategory.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a category.", "WorkshopPro",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _cmbCategory.Focus();
                return false;
            }
            if (!decimal.TryParse(_txtPriceBuy.Text, out _) ||
                !decimal.TryParse(_txtPriceSell.Text, out _))
            {
                MessageBox.Show("Buy Price and Sell Price must be valid numbers.", "WorkshopPro",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // SparePartForm
            // 
            this.ClientSize = new System.Drawing.Size(282, 253);
            this.Name = "SparePartForm";
            this.Load += new System.EventHandler(this.SparePartForm_Load);
            this.ResumeLayout(false);

        }

        private void SparePartForm_Load(object sender, EventArgs e)
        {

        }
    }
}
