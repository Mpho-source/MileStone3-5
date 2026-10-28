using System;
using System.Drawing;
using System.Windows.Forms;

namespace NextGen__7
{
    public static class EduConnectTheme
    {
        public static readonly Color Background = Color.FromArgb(230, 237, 249);
        public static readonly Color Navy = Color.FromArgb(31, 56, 100);
        public static readonly Color NavyDark = Color.FromArgb(22, 42, 79);
        public static readonly Color Blue = Color.FromArgb(35, 93, 177);
        public static readonly Color Ink = Color.FromArgb(36, 52, 79);
        public static readonly Color Muted = Color.FromArgb(83, 104, 132);
        public static readonly Color Edge = Color.FromArgb(113, 145, 187);
        public static readonly Color SoftBlue = Color.FromArgb(243, 248, 255);
        public static readonly Color HoverBlue = Color.FromArgb(222, 235, 252);
        public static readonly Color Danger = Color.FromArgb(164, 47, 65);
        public static readonly Color Success = Color.FromArgb(21, 112, 82);
        public static readonly Color Warning = Color.FromArgb(151, 92, 18);

        public static void Apply(Form form)
        {
            form.SuspendLayout();
            form.Font = new Font("Segoe UI", 10F);
            form.BackColor = Background;
            form.ForeColor = Ink;
            form.StartPosition = FormStartPosition.CenterScreen;
            StyleChildren(form.Controls);
            form.ResumeLayout(false);
        }

        public static void StyleChildren(Control.ControlCollection controls)
        {
            foreach (Control c in controls)
            {
                RoundedPanel rounded = c as RoundedPanel;
                if (rounded != null)
                {
                    rounded.NormalColor = SoftBlue;
                    rounded.HoverColor = HoverBlue;
                    rounded.BorderColor = Edge;
                    rounded.CornerRadius = 16;
                    rounded.Cursor = Cursors.Hand;
                    rounded.TabStop = true;
                }
                else if (c is Button)
                    StyleButton((Button)c);
                else if (c is TextBox)
                {
                    TextBox t = (TextBox)c;
                    t.Font = new Font("Segoe UI", 10.5F);
                    t.BorderStyle = BorderStyle.FixedSingle;
                    t.BackColor = Color.White;
                    t.ForeColor = Ink;
                }
                else if (c is RichTextBox)
                {
                    RichTextBox t = (RichTextBox)c;
                    t.Font = new Font("Segoe UI", 10.5F);
                    t.BorderStyle = BorderStyle.FixedSingle;
                    t.BackColor = t.ReadOnly ? Color.FromArgb(236, 242, 250) : Color.White;
                    t.ForeColor = Ink;
                }
                else if (c is ComboBox)
                {
                    ComboBox t = (ComboBox)c;
                    t.Font = new Font("Segoe UI", 10.5F);
                    t.FlatStyle = FlatStyle.Standard;
                    t.BackColor = Color.White;
                    t.ForeColor = Ink;
                }
                else if (c is DateTimePicker)
                {
                    DateTimePicker t = (DateTimePicker)c;
                    t.Font = new Font("Segoe UI", 10.5F);
                    t.CalendarForeColor = Ink;
                    t.CalendarTitleBackColor = Navy;
                }
                else if (c is DataGridView)
                    StyleGrid((DataGridView)c);
                else if (c is Label)
                {
                    Label label = (Label)c;
                    if (label.Parent != null &&
                        label.Parent.BackColor.GetBrightness() < 0.45F)
                        label.ForeColor = Color.White;
                    else if (label.Font.Bold)
                        label.ForeColor = Navy;
                    else if (label.ForeColor == SystemColors.GrayText ||
                             label.ForeColor == SystemColors.ControlDarkDark)
                        label.ForeColor = Muted;
                }

                if (c.HasChildren)
                    StyleChildren(c.Controls);
            }
        }

        public static void StyleButton(Button button)
        {
            string name = button.Name ?? "";
            bool danger = name.IndexOf("CancelBooking", StringComparison.OrdinalIgnoreCase) >= 0 ||
                          name.IndexOf("Logout", StringComparison.OrdinalIgnoreCase) >= 0;
            bool secondary = name.IndexOf("Back", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             name.IndexOf("Clear", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             name.IndexOf("Refresh", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             name.IndexOf("Preview", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             name.IndexOf("New", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             button.Text.Trim().Equals("Cancel", StringComparison.OrdinalIgnoreCase);
            bool success = !danger && !secondary &&
                           (name.IndexOf("Save", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("Add", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("Record", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("Assign", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("Verify", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("Print", StringComparison.OrdinalIgnoreCase) >= 0);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = secondary ? 2 : 1;
            button.FlatAppearance.BorderColor = secondary ? Edge : danger ? Danger : success ? Success : Blue;
            button.BackColor = danger ? Danger : secondary ? SoftBlue : success ? Success : Blue;
            button.ForeColor = secondary ? Navy : Color.White;
            button.FlatAppearance.MouseOverBackColor = danger
                ? Color.FromArgb(131, 36, 52)
                : secondary ? HoverBlue : success ? Color.FromArgb(13, 89, 64) : Color.FromArgb(26, 73, 148);
            button.FlatAppearance.MouseDownBackColor = NavyDark;
            button.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
            button.MinimumSize = new Size(Math.Min(button.Width, 110), Math.Min(button.Height, 39));
            button.AccessibleName = button.Text;
            button.TabStop = true;
        }

        public static void StyleGrid(DataGridView grid)
        {
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.FixedSingle;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = Color.FromArgb(212, 226, 243);
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Navy;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Navy;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font =
                new Font("Segoe UI", 10F, FontStyle.Bold);
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 45;
            grid.RowTemplate.Height = 44;
            grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.DefaultCellStyle.Padding = new Padding(6, 3, 6, 3);
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
            grid.DefaultCellStyle.ForeColor = Ink;
            grid.DefaultCellStyle.BackColor = Color.White;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(208, 229, 252);
            grid.DefaultCellStyle.SelectionForeColor = NavyDark;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(241, 247, 255);
            grid.AlternatingRowsDefaultCellStyle.ForeColor = Ink;
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.ReadOnly = true;
        }
    }
}
