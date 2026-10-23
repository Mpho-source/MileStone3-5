using System;
using System.Drawing;
using System.Windows.Forms;

namespace NextGen__7
{
    public static class EduConnectTheme
    {
        private static readonly Color Navy = Color.FromArgb(24, 43, 78);
        private static readonly Color Blue = Color.FromArgb(38, 92, 174);
        private static readonly Color Pale = Color.FromArgb(244, 247, 252);
        private static readonly Color Ink = Color.FromArgb(34, 48, 68);
        private static readonly Color Muted = Color.FromArgb(96, 111, 133);
        private static readonly Color Border = Color.FromArgb(210, 220, 233);
        private static readonly Color Danger = Color.FromArgb(169, 48, 56);

        public static void Apply(Form form)
        {
            form.SuspendLayout();
            form.Font = new Font("Segoe UI", 10F);
            form.BackColor = Pale;
            form.ForeColor = Ink;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.AutoScaleMode = AutoScaleMode.Dpi;
            form.KeyPreview = true;
            form.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape && form.Modal)
                {
                    e.Handled = true;
                    form.Close();
                }
            };
            StyleChildren(form.Controls);
            form.ResumeLayout(true);
        }

        private static void StyleChildren(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                if (control is Button)
                    StyleButton((Button)control);
                else if (control is TextBox)
                    StyleTextBox((TextBox)control);
                else if (control is RichTextBox)
                    StyleRichTextBox((RichTextBox)control);
                else if (control is ComboBox)
                    StyleComboBox((ComboBox)control);
                else if (control is DateTimePicker)
                    StyleDatePicker((DateTimePicker)control);
                else if (control is DataGridView)
                    StyleGrid((DataGridView)control);
                else if (control is Label)
                    StyleLabel((Label)control);
                else if (control is Panel)
                    StylePanel((Panel)control);
                if (control.HasChildren)
                    StyleChildren(control.Controls);
            }
        }

        private static void StyleButton(Button button)
        {
            bool destructive = button.Name.IndexOf("CancelBooking", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               button.Name.IndexOf("Logout", StringComparison.OrdinalIgnoreCase) >= 0;
            bool secondary = button.Name.IndexOf("Back", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             button.Name.IndexOf("Clear", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             (button.Text.Trim().Equals("Cancel", StringComparison.OrdinalIgnoreCase));
            Color baseColor = destructive ? Danger : secondary ? Color.White : Blue;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = secondary ? 1 : 0;
            button.FlatAppearance.BorderColor = Border;
            button.FlatAppearance.MouseOverBackColor = destructive
                ? Color.FromArgb(140, 34, 43)
                : secondary ? Color.FromArgb(230, 237, 247)
                : Color.FromArgb(25, 73, 149);
            button.BackColor = baseColor;
            button.ForeColor = secondary ? Navy : Color.White;
            button.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
            button.MinimumSize = new Size(95, 35);
            button.AccessibleName = button.Text;
            button.TabStop = true;
        }

        private static void StyleTextBox(TextBox text)
        {
            text.Font = new Font("Segoe UI", 10.5F);
            text.BackColor = Color.White;
            text.ForeColor = Ink;
            text.BorderStyle = BorderStyle.FixedSingle;
        }

        private static void StyleRichTextBox(RichTextBox text)
        {
            text.Font = new Font("Segoe UI", 10.5F);
            text.BackColor = text.ReadOnly ? Color.FromArgb(235, 241, 248) : Color.White;
            text.ForeColor = Ink;
            text.BorderStyle = BorderStyle.FixedSingle;
        }

        private static void StyleComboBox(ComboBox combo)
        {
            combo.Font = new Font("Segoe UI", 10F);
            combo.BackColor = Color.White;
            combo.ForeColor = Ink;
            combo.FlatStyle = FlatStyle.Flat;
        }

        private static void StyleDatePicker(DateTimePicker picker)
        {
            picker.Font = new Font("Segoe UI", 10F);
            picker.CalendarForeColor = Ink;
            picker.CalendarTitleBackColor = Navy;
        }

        private static void StyleLabel(Label label)
        {
            if (label.Parent is Panel && label.Parent.Height <= 120 &&
                label.Parent.BackColor.GetBrightness() < 0.45f)
            {
                label.ForeColor = Color.White;
                return;
            }
            if (label.Font.Size >= 17)
            {
                label.Font = new Font("Segoe UI", label.Font.Size, FontStyle.Bold);
                label.ForeColor = Navy;
            }
            else if (label.Font.Bold)
                label.ForeColor = Navy;
            else if (label.ForeColor == SystemColors.ControlDarkDark ||
                     label.ForeColor == SystemColors.GrayText)
                label.ForeColor = Muted;
        }

        private static void StylePanel(Panel panel)
        {
            if (panel.Height <= 115 && panel.Width >= 450 &&
                panel.Dock != DockStyle.Fill)
                panel.BackColor = Navy;
        }

        public static void StyleGrid(DataGridView grid)
        {
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = Color.FromArgb(233, 238, 246);
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Navy;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font =
                new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            grid.ColumnHeadersHeight = 44;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.RowTemplate.Height = 38;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
            grid.DefaultCellStyle.ForeColor = Ink;
            grid.DefaultCellStyle.BackColor = Color.White;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(217, 231, 250);
            grid.DefaultCellStyle.SelectionForeColor = Navy;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 250, 254);
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.ReadOnly = true;
        }

        public static void AddBookingGuidance(Form form, Control anchor)
        {
            Label guidance = new Label();
            guidance.Name = "lblBookingGuidance";
            guidance.Text = "Choose your course, support type, date and time. " +
                "You can change or cancel a booking until 48 hours before it starts.";
            guidance.Font = new Font("Segoe UI", 9F);
            guidance.ForeColor = Muted;
            guidance.AutoSize = false;
            guidance.Size = new Size(Math.Max(400, anchor.Width), 42);
            guidance.Location = new Point(anchor.Left, anchor.Bottom + 8);
            guidance.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            form.Controls.Add(guidance);
            guidance.BringToFront();
        }
    }
}
