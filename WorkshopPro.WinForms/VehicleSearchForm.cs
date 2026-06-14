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
using WorkshopPro.Infrastructure.Repositories;

namespace WorkshopPro.WinForms
{
    public partial class VehicleSearchForm : Form
    {
        
        private readonly SqliteVehicleRepository _vehicleRepo;
        private readonly SqliteVehicleModelRepository _vehicleModelRepo;
        private readonly SqliteCustomerRepository _customerRepo;

        // These are set when the user confirms — InvoiceForm reads them
        public VehicleEntity SelectedVehicle { get; private set; }
        public CustomerEntity SelectedCustomer { get; private set; }

        // ── Controls ──────────────────────────────────────────────────────────
        private TextBox _txtPlate;
        private Button _btnSearch;
        private Panel _pnlFound;       // shown when vehicle exists
        private Panel _pnlNew;         // shown when vehicle is new
        private Label _lblFoundInfo;
        private ComboBox _cmbManufacturer;
        private ComboBox _cmbModel;
        private TextBox _txtColor;
        private TextBox _txtYear;
        private TextBox _txtCustomerName;
        private TextBox _txtCustomerPhone;
        private Button _btnConfirm;

        public VehicleSearchForm(
            SqliteVehicleRepository vehicleRepo,
            SqliteVehicleModelRepository vehicleModelRepo,
            SqliteCustomerRepository customerRepo)
        {
            InitializeComponent();
            _vehicleRepo = vehicleRepo;
            _vehicleModelRepo = vehicleModelRepo;
            _customerRepo = customerRepo;
            BuildUI();
        }

        private void BuildUI()
        {
            this.Text = "Vehicle Search";
            this.Size = new Size(500, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // ── Plate search row ──────────────────────────────────────────────
            var lblPlate = new Label { Text = "Plate Number:", Location = new Point(20, 20), AutoSize = true };
            _txtPlate = new TextBox
            {
                Location = new Point(130, 17),
                Width = 200,
                CharacterCasing = CharacterCasing.Upper
            };
            _btnSearch = new Button { Text = "Search", Location = new Point(340, 15), Width = 80 };
            _btnSearch.Click += BtnSearch_Click;

            // ── Found panel ───────────────────────────────────────────────────
            _pnlFound = new Panel { Location = new Point(20, 60), Size = new Size(440, 80), Visible = false };
            _lblFoundInfo = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                Font = new Font("Segoe UI", 10)
            };
            _pnlFound.Controls.Add(_lblFoundInfo);

            // ── New vehicle panel ─────────────────────────────────────────────
            _pnlNew = new Panel { Location = new Point(20, 60), Size = new Size(440, 300), Visible = false };

            var lblNew = new Label
            {
                Text = "New Vehicle — fill in details:",
                Location = new Point(0, 0),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            var lblMfr = new Label { Text = "Manufacturer:", Location = new Point(0, 30), AutoSize = true };
            _cmbManufacturer = new ComboBox
            {
                Location = new Point(130, 27),
                Width = 280,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            var lblModel = new Label { Text = "Model:", Location = new Point(0, 65), AutoSize = true };
            _cmbModel = new ComboBox
            {
                Location = new Point(130, 62),
                Width = 280,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            var lblColor = new Label { Text = "Color:", Location = new Point(0, 100), AutoSize = true };
            _txtColor = new TextBox { Location = new Point(130, 97), Width = 150 };
            var lblYear = new Label { Text = "Year:", Location = new Point(0, 135), AutoSize = true };
            _txtYear = new TextBox { Location = new Point(130, 132), Width = 80 };

            var lblCustName = new Label { Text = "Customer Name:", Location = new Point(0, 175), AutoSize = true };
            _txtCustomerName = new TextBox { Location = new Point(130, 172), Width = 280 };
            var lblCustPhone = new Label { Text = "Phone:", Location = new Point(0, 210), AutoSize = true };
            _txtCustomerPhone = new TextBox { Location = new Point(130, 207), Width = 180 };

            _cmbManufacturer.SelectedIndexChanged += CmbManufacturer_Changed;

            _pnlNew.Controls.AddRange(new Control[] {
                lblNew, lblMfr, _cmbManufacturer, lblModel, _cmbModel,
                lblColor, _txtColor, lblYear, _txtYear,
                lblCustName, _txtCustomerName, lblCustPhone, _txtCustomerPhone });

            // ── Confirm button ────────────────────────────────────────────────
            _btnConfirm = new Button
            {
                Text = "Proceed to Invoice",
                Size = new Size(160, 36),
                Location = new Point(290, 460),
                Visible = false,
                Font = new Font("Segoe UI", 10)
            };
            _btnConfirm.Click += BtnConfirm_Click;

            this.Controls.AddRange(new Control[] {
                lblPlate, _txtPlate, _btnSearch,
                _pnlFound, _pnlNew, _btnConfirm });
        }

        // ── Search logic ──────────────────────────────────────────────────────
        private void BtnSearch_Click(object sender, EventArgs e)
        {
            string plate = _txtPlate.Text.Trim();
            if (string.IsNullOrEmpty(plate))
            {
                MessageBox.Show("Please enter a plate number.", "WorkshopPro",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var vehicle = _vehicleRepo.GetByPlate(plate);

            if (vehicle != null)
            {
                // Vehicle exists — show summary, skip new vehicle form
                SelectedVehicle = vehicle;
                SelectedCustomer = _customerRepo.GetById((int)vehicle.CustomerId);

                _lblFoundInfo.Text =
                    $"✔  Vehicle found\n" +
                    $"    {vehicle.Year} {vehicle.Color}  |  " +
                    $"Customer: {SelectedCustomer?.Name ?? "Unknown"}";

                _pnlNew.Visible = false;
                _pnlFound.Visible = true;
                _btnConfirm.Visible = true;
            }
            else
            {
                // New vehicle — show registration form
                LoadManufacturers();
                _pnlFound.Visible = false;
                _pnlNew.Visible = true;
                _btnConfirm.Visible = true;
            }
        }

        private void LoadManufacturers()
        {
            _cmbManufacturer.DisplayMember = "Name";
            _cmbManufacturer.ValueMember = "Id";
            _cmbManufacturer.DataSource = _vehicleModelRepo.GetAllManufacturers().ToList();
            _cmbManufacturer.SelectedIndex = -1;
        }

        // Cascading dropdown — when manufacturer changes, reload models
        // Java equivalent: addActionListener on the first JComboBox
        private void CmbManufacturer_Changed(object sender, EventArgs e)
        {
            if (_cmbManufacturer.SelectedValue == null) return;
            int mfrId = Convert.ToInt32(_cmbManufacturer.SelectedValue);

            _cmbModel.DisplayMember = "ModelName";
            _cmbModel.ValueMember = "Id";
            _cmbModel.DataSource = _vehicleModelRepo.GetByManufacturer(mfrId).ToList();
        }

        // ── Confirm logic ─────────────────────────────────────────────────────
        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            if (SelectedVehicle != null)
            {
                // Existing vehicle — just proceed
                this.DialogResult = DialogResult.OK;
                return;
            }

            // New vehicle — validate and save
            if (_cmbModel.SelectedValue == null)
            {
                MessageBox.Show("Please select a vehicle model.", "WorkshopPro",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(_txtCustomerName.Text))
            {
                MessageBox.Show("Please enter the customer name.", "WorkshopPro",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!int.TryParse(_txtYear.Text, out int year))
            {
                MessageBox.Show("Please enter a valid year.", "WorkshopPro",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Save customer first, then vehicle
            var customer = new CustomerEntity
            {
                Name = _txtCustomerName.Text.Trim(),
                Phone = _txtCustomerPhone.Text.Trim(),
                Address = ""
            };
            customer.Id = _customerRepo.Insert(customer);

            var vehicle = new VehicleEntity
            {
                PlateNumber = _txtPlate.Text.Trim(),
                CustomerId = customer.Id,
                VehicleModelId = Convert.ToInt32(_cmbModel.SelectedValue),
                Color = _txtColor.Text.Trim(),
                Year = year
            };
            vehicle.Id = _vehicleRepo.Insert(vehicle);

            SelectedVehicle = vehicle;
            SelectedCustomer = customer;

            this.DialogResult = DialogResult.OK;
        }
    }
}
