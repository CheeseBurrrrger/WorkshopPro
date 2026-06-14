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

namespace WorkshopPro.WinForms
{
    public partial class PartSearchPopup : Form
    {
        public event Action<SparePartEntity> PartSelected;

        private readonly ListBox _listBox;
        private List<SparePartEntity> _currentResults = new List<SparePartEntity>();

        public PartSearchPopup()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            Width = 460;
            Height = 0;

            _listBox = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9f),
                ItemHeight = 26,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            _listBox.DrawItem += ListBox_DrawItem;
            _listBox.MouseClick += ListBox_MouseClick;
            _listBox.KeyDown += ListBox_KeyDown;

            Controls.Add(_listBox);
        }

        /// <summary>
        /// Updates the popup with new results and positions it below the anchor control.
        /// Call this after every TextChanged event on the search box.
        /// </summary>
        public void ShowResults(IEnumerable<SparePartEntity> results, Control anchor)
        {
            _currentResults = new List<SparePartEntity>(results);
            _listBox.Items.Clear();

            if (_currentResults.Count == 0) { Hide(); return; }

            foreach (var part in _currentResults)
                _listBox.Items.Add(part);

            int visibleRows = Math.Min(_currentResults.Count, 6);
            Height = visibleRows * _listBox.ItemHeight + 4;
            Width = Math.Max(anchor.Width, 460);

            // PointToScreen converts control-local coordinates to screen coordinates.
            // We want the popup to start at the bottom-left corner of the anchor.
            // Java equivalent: component.getLocationOnScreen()
            var screenPos = anchor.PointToScreen(new Point(0, anchor.Height));
            Location = screenPos;

            if (!Visible)
                Show(anchor.FindForm());
        }

        // ── Custom row drawing ─────────────────────────────────────────────
        // We draw each row manually so we can show code + name + stock + price
        // all on one line, with different fonts/colors per segment.

        private void ListBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _currentResults.Count) return;
            var part = _currentResults[e.Index];
            bool selected = (e.State & DrawItemState.Selected) != 0;

            e.Graphics.FillRectangle(
                selected ? SystemBrushes.Highlight : SystemBrushes.Window,
                e.Bounds);

            var fontCode = new Font("Segoe UI", 8f);
            var fontName = new Font("Segoe UI", 9f, FontStyle.Bold);
            var fontInfo = new Font("Segoe UI", 8f);
            var brushText = selected ? SystemBrushes.HighlightText : SystemBrushes.WindowText;
            var brushGray = selected ? SystemBrushes.HighlightText : Brushes.Gray;

            // Part code on the left in gray
            string code = $"[{part.PartCode}]";
            float codeWidth = e.Graphics.MeasureString(code, fontCode).Width;
            e.Graphics.DrawString(code, fontCode, brushGray, e.Bounds.X + 4, e.Bounds.Y + 5);

            // Part name in bold after the code
            e.Graphics.DrawString(part.PartName, fontName, brushText,
                e.Bounds.X + 4 + codeWidth + 4, e.Bounds.Y + 4);

            // Stock + price right-aligned
            string info = $"Stok: {part.StockQty}  Rp {part.PriceSell:#,0}";
            float infoW = e.Graphics.MeasureString(info, fontInfo).Width;
            e.Graphics.DrawString(info, fontInfo, brushGray,
                e.Bounds.Right - infoW - 6, e.Bounds.Y + 6);
        }

        private void ListBox_MouseClick(object sender, MouseEventArgs e)
        {
            int index = _listBox.IndexFromPoint(e.Location);
            if (index >= 0 && index < _currentResults.Count)
                SelectPart(_currentResults[index]);
        }

        private void ListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && _listBox.SelectedIndex >= 0)
                SelectPart(_currentResults[_listBox.SelectedIndex]);
            else if (e.KeyCode == Keys.Escape)
                Hide();
        }

        private void SelectPart(SparePartEntity part)
        {
            Hide();
            PartSelected?.Invoke(part);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Hide();
        }
    }
}
