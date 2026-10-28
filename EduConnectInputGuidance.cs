using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NextGen__7
{
    internal static class EduConnectInputGuidance
    {
        private const int EmSetCueBanner = 0x1501;
        private const int CbSetCueBanner = 0x1703;
        private static readonly Dictionary<Form, ToolTip> ActiveTips = new Dictionary<Form, ToolTip>();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, string text);

        internal static void Attach(Form form)
        {
            form.Shown += (sender, args) => Apply(form);
            form.Disposed += (sender, args) =>
            {
                ToolTip old;
                if (ActiveTips.TryGetValue(form, out old))
                {
                    old.Dispose();
                    ActiveTips.Remove(form);
                }
            };
        }

        private static void Apply(Form form)
        {
            if (ActiveTips.ContainsKey(form)) return;

            string[] order;
            Dictionary<string, string> guidance = GetGuidance(form.GetType().Name, out order);
            ToolTip tips = new ToolTip
            {
                ShowAlways = true,
                InitialDelay = 350,
                ReshowDelay = 200,
                AutoPopDelay = 12000
            };
            ActiveTips.Add(form, tips);

            foreach (KeyValuePair<string, string> entry in guidance)
            {
                Control[] matches = form.Controls.Find(entry.Key, true);
                if (matches.Length == 0) continue;
                Control control = matches[0];
                string hint = entry.Value;
                control.AccessibleDescription = hint;
                tips.SetToolTip(control, hint);
                TextBox textBox = control as TextBox;
                if (textBox != null && !textBox.ReadOnly)
                {
                    if (textBox.Multiline || IsPasswordField(form.GetType().Name, entry.Key))
                        AddVisiblePlaceholder(textBox, hint);
                    else
                        SendMessage(textBox.Handle, EmSetCueBanner, new IntPtr(1), hint);
                }
                ComboBox combo = control as ComboBox;
                if (combo != null)
                    SendMessage(combo.Handle, CbSetCueBanner, IntPtr.Zero, hint);
            }

            Dictionary<string, int> priorities = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < order.Length; index++)
                priorities[order[index]] = index;
            SetTabOrder(form, priorities);
            EnhanceControls(form);
        }

        private static void AddVisiblePlaceholder(TextBox input, string hint)
        {
            Label placeholder = new Label();
            placeholder.Name = input.Name + "InputHint";
            placeholder.AutoSize = false;
            placeholder.Font = new Font("Segoe UI", 8.5F);
            placeholder.BackColor = input.BackColor;
            placeholder.ForeColor = Color.FromArgb(108, 118, 136);
            placeholder.Text = hint;
            placeholder.TextAlign = ContentAlignment.MiddleLeft;
            placeholder.AutoEllipsis = true;
            placeholder.Cursor = Cursors.IBeam;
            placeholder.TabStop = false;
            input.Parent.Controls.Add(placeholder);

            Action position = () =>
            {
                if (input.IsDisposed || placeholder.IsDisposed) return;
                placeholder.Bounds = new Rectangle(input.Left + 7, input.Top + 4,
                    Math.Max(12, input.Width - 15), Math.Min(28, input.Height - 7));
                placeholder.BringToFront();
            };
            Action show = () =>
            {
                if (input.IsDisposed || placeholder.IsDisposed) return;
                placeholder.Visible = !input.Focused && input.TextLength == 0 && input.Visible;
                if (placeholder.Visible) placeholder.BringToFront();
            };
            placeholder.Click += (sender, args) => input.Focus();
            input.Enter += (sender, args) => show();
            input.Leave += (sender, args) => show();
            input.TextChanged += (sender, args) => show();
            input.LocationChanged += (sender, args) => position();
            input.SizeChanged += (sender, args) => position();
            position();
            show();
        }

        private static void EnhanceControls(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                DataGridView grid = control as DataGridView;
                if (grid != null)
                {
                    EduConnectTheme.StyleGrid(grid);
                    grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
                    grid.ColumnHeadersHeight = 50;
                    grid.CellFormatting += (sender, args) =>
                    {
                        if (args.ColumnIndex < 0 || args.RowIndex < 0) return;
                        string column = grid.Columns[args.ColumnIndex].Name;
                        if (column.IndexOf("status", StringComparison.OrdinalIgnoreCase) < 0 &&
                            column.IndexOf("attendance", StringComparison.OrdinalIgnoreCase) < 0)
                            return;
                        string status = Convert.ToString(args.Value).Trim();
                        Color background, foreground;
                        if (status.Equals("Scheduled", StringComparison.OrdinalIgnoreCase) ||
                            status.Equals("Attended", StringComparison.OrdinalIgnoreCase))
                        { background = Color.FromArgb(226, 244, 235); foreground = Color.FromArgb(19, 105, 68); }
                        else if (status.Equals("Submitted", StringComparison.OrdinalIgnoreCase))
                        { background = Color.FromArgb(255, 245, 219); foreground = Color.FromArgb(126, 81, 15); }
                        else if (status.Equals("Verified", StringComparison.OrdinalIgnoreCase))
                        { background = Color.FromArgb(233, 239, 255); foreground = Color.FromArgb(49, 70, 137); }
                        else if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                        { background = Color.FromArgb(220, 237, 250); foreground = Color.FromArgb(33, 77, 130); }
                        else if (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) ||
                            status.Equals("Absent", StringComparison.OrdinalIgnoreCase))
                        { background = Color.FromArgb(255, 232, 232); foreground = Color.FromArgb(151, 43, 49); }
                        else return;
                        args.CellStyle.BackColor = background;
                        args.CellStyle.ForeColor = foreground;
                        args.CellStyle.SelectionBackColor = background;
                        args.CellStyle.SelectionForeColor = foreground;
                    };
                }
                Button button = control as Button;
                if (button != null)
                    EduConnectTheme.StyleButton(button);
                if (control.HasChildren) EnhanceControls(control);
            }
        }

        private static bool IsPasswordField(string formName, string controlName)
        {
            return formName == "Login" && controlName == "txtPassword"
                || formName == "AccountPasswordsForm"
                || formName == "TutorChangePassword";
        }

        private static int Priority(Control control, Dictionary<string, int> positions)
        {
            int found;
            int best = positions.TryGetValue(control.Name, out found) ? found : int.MaxValue;
            foreach (Control child in control.Controls)
                best = Math.Min(best, Priority(child, positions));
            return best;
        }

        private static void SetTabOrder(Control parent, Dictionary<string, int> positions)
        {
            List<Control> ordered = parent.Controls.Cast<Control>()
                .OrderBy(child => Priority(child, positions))
                .ThenBy(child => child.Top)
                .ThenBy(child => child.Left)
                .ToList();

            for (int index = 0; index < ordered.Count; index++)
            {
                Control control = ordered[index];
                control.TabIndex = index;
                if (control is Label || control is PictureBox || control is Panel)
                    control.TabStop = control is RoundedPanel && positions.ContainsKey(control.Name);
                else if (control is TextBoxBase)
                    control.TabStop = !((TextBoxBase)control).ReadOnly;
                else if (control is Button || control is ComboBox ||
                         control is DateTimePicker || control is DataGridView ||
                         control is NumericUpDown)
                    control.TabStop = true;

                SetTabOrder(control, positions);
            }
        }

        private static Dictionary<string, string> GetGuidance(string name, out string[] order)
        {
            Dictionary<string, string> hints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            switch (name)
            {
                case "Login":
                    order = new[] { "cmbSignInRole", "txtStudentNo", "txtPassword", "btnLogin", "btnAdminSetup" };
                    hints["cmbSignInRole"] = "Choose Student, Staff or Tutor before signing in.";
                    hints["txtStudentNo"] = "Student / staff number or Tutor ID";
                    hints["txtPassword"] = "Password";
                    break;
                case "StudentDashboard":
                    order = new[] { "pnlBook", "pnlManageBooking", "pnlLogout", "btnNavBook", "btnNavManage", "btnChangeStudentPassword", "btnNavLogout" };
                    hints["btnChangeStudentPassword"] = "Change your password using the current password.";
                    break;
                case "BookingTutoringSession":
                    order = new[] { "cmbCourse", "cmbSupportType", "dtpSessionDate", "cmbSessionTime", "btnBookSession", "btnClear", "btnBack", "btnNavDashboard", "btnNavBookings", "btnNavBack" };
                    hints["cmbCourse"] = "Select the course for which you require academic support.";
                    hints["cmbSupportType"] = "Choose individual tutoring, group tutoring, exam preparation or assignment support.";
                    hints["dtpSessionDate"] = "Choose a future date for the requested tutoring session.";
                    hints["cmbSessionTime"] = "Select an available preferred time slot.";
                    break;
                case "ManageBookings":
                    order = new[] { "cmbBookingStatusFilter", "dgvBookings", "btnEditBooking", "btnCancelBooking", "btnNavBook", "btnNavDashboard", "btnNavBack", "btnBack" };
                    hints["cmbBookingStatusFilter"] = "Filter your requests by booking status.";
                    hints["dgvBookings"] = "Select a row to view actions for that booking.";
                    break;
                case "EditBooking":
                    order = new[] { "cmbCourse", "cmbSupportType", "dtpSessionDate", "cmbSessionTime", "btnSaveChanges", "btnBack" };
                    hints["cmbCourse"] = "Choose the course for this booking.";
                    hints["cmbSupportType"] = "Select the required type of tutoring support.";
                    hints["dtpSessionDate"] = "Choose a future date. Changes must meet the 48-hour rule.";
                    hints["cmbSessionTime"] = "Choose a preferred time slot. Your tutor may change after saving.";
                    break;
                case "AdminDashboard":
                    order = new[] { "btnTutors", "btnRequests", "btnReports", "btnManageAccounts", "btnChangeStaffPassword", "btnSignOut" };
                    break;
                case "AdminRequests":
                    order = new[] { "requests", "btnVerify", "cmbMatchTutor", "btnAssign", "btnRefresh", "btnBack" };
                    hints["requests"] = "Select a Submitted request to verify it, or a Verified request to assign a tutor.";
                    hints["cmbMatchTutor"] = "Only qualified tutors available at the requested time will appear.";
                    break;
                case "ManagaeTutors":
                    order = new[] { "txtSearch", "gridTutors", "btnNew", "txtFirstName", "txtLastName", "txtEmail", "btnSave", "cmbCourse", "btnAssignCourse", "btnAvailability", "btnCreateTutorLogin", "btnResetTutorLogin", "btnRefresh", "btnCancel", "btnBack" };
                    hints["txtSearch"] = "Search by name or email";
                    hints["gridTutors"] = "Select a tutor to edit their details, courses, availability or sign-in.";
                    hints["txtFirstName"] = "First name";
                    hints["txtLastName"] = "Last name";
                    hints["txtEmail"] = "Email e.g. tutor@example.com";
                    hints["cmbCourse"] = "Select a course to assign to this tutor after saving their profile.";
                    break;
                case "TutorAvailability":
                    order = new[] { "dtpDate", "dtpStart", "dtpEnd", "btnAdd", "gridSlots", "btnDisable", "btnRefresh", "btnBack" };
                    hints["dtpDate"] = "Choose the date on which the tutor is available.";
                    hints["dtpStart"] = "Choose the start of the tutor's available period.";
                    hints["dtpEnd"] = "End time must be later than start time and cannot overlap another active slot.";
                    hints["gridSlots"] = "Select an existing slot before marking it unavailable.";
                    break;
                case "TutorSessions":
                    order = new[] { "gridSessions", "cmbAttendance", "txtFeedback", "btnRecord", "btnRefresh", "btnChangePassword", "btnLogout" };
                    hints["gridSessions"] = "Select a session that has ended to record attendance.";
                    hints["cmbAttendance"] = "Choose Attended or Absent.";
                    hints["txtFeedback"] = "Add session feedback (maximum 240 characters)";
                    break;
                case "Reports":
                    order = new[] { "cmbReport", "fromDate", "toDate", "numYear", "cmbStudentChoice", "btnGenerate", "grid", "btnToggleView", "btnPreview", "btnPrint", "btnBack" };
                    hints["cmbReport"] = "Choose one of the three approved tutoring reports.";
                    hints["fromDate"] = "Set the starting date for the tutor-workload report.";
                    hints["toDate"] = "Set an end date not earlier than the start date.";
                    hints["numYear"] = "Select the year for GPA and tutoring attendance trends.";
                    hints["cmbStudentChoice"] = "Choose one student or the complete cohort.";
                    hints["grid"] = "View the generated report results.";
                    hints["btnPrint"] = "Choose where to save a downloadable PDF copy of this report.";
                    hints["btnPreview"] = "Preview the report before saving or printing.";
                    break;
                case "AccountPasswordsForm":
                    order = new[] { "cmbRole", "txtAccount", "txtPassword", "txtConfirm", "txtSetupKey", "btnSave", "btnClose" };
                    hints["cmbRole"] = "Choose Student or Staff.";
                    hints["txtAccount"] = "Enter an existing student or staff number.";
                    hints["txtPassword"] = "New password (12+ characters)";
                    hints["txtConfirm"] = "Confirm new password";
                    hints["txtSetupKey"] = "Administrator setup key";
                    break;
                case "TutorChangePassword":
                    order = new[] { "txtCurrent", "txtNew", "txtConfirm", "btnSave", "btnCancel" };
                    hints["txtCurrent"] = "Current password";
                    hints["txtNew"] = "New password (12+ characters)";
                    hints["txtConfirm"] = "Confirm new password";
                    break;
                default:
                    order = new string[0];
                    break;
            }
            return hints;
        }
    }
}
