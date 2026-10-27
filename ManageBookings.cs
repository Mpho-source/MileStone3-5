
using System;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace NextGen__7
{
    public partial class ManageBookings : Form
    {
        private readonly int studentID;
        private readonly string studentNo;
        private readonly string connectionString;
        private ComboBox statusFilter;

        public ManageBookings()
            : this(Login.CurrentStudentID, Login.CurrentStudentNo)
        {
        }

        public ManageBookings(int studentID, string studentNo)
        {
            InitializeComponent();
            if (System.ComponentModel.LicenseManager.UsageMode ==
                System.ComponentModel.LicenseUsageMode.Designtime) return;

            if (studentID <= 0 || string.IsNullOrWhiteSpace(studentNo))
                throw new InvalidOperationException("Invalid student session.");

            this.studentID = studentID;
            this.studentNo = studentNo;

            connectionString = ConfigurationManager.ConnectionStrings[
                "NextGen__7.Properties.Settings.Edu_ConnectConnectionString"].ConnectionString;

            lblStudentNo.Text = "Student No: " + studentNo;

            dgvBookings.ReadOnly = true;
            dgvBookings.MultiSelect = false;
            dgvBookings.SelectionMode =
                DataGridViewSelectionMode.FullRowSelect;
            dgvBookings.AutoGenerateColumns = true;

            btnEditBooking.Click += btnEditBooking_Click;
            btnCancelBooking.Click += btnCancelBooking_Click;

            statusFilter = cmbBookingStatusFilter;
            statusFilter.SelectedIndex = 0;
            btnNavDashboard.Click += (sender, e) => Close();
            btnNavBook.Click += (sender, e) =>
            {
                using (var page = new BookingTutoringSession(studentID, studentNo))
                    page.ShowDialog(this);
            };
            btnNavBack.Click += (sender, e) => Close();
            statusFilter.SelectedIndexChanged += (s, e) =>
                ApplyStatusFilter();
            dgvBookings.SelectionChanged += (s, e) => UpdateActionAvailability();
            dgvBookings.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 ||
                    dgvBookings.Columns[e.ColumnIndex].Name != "Status")
                    return;
                string status = Convert.ToString(e.Value).Trim();
                if (status.Equals("Scheduled", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.BackColor = System.Drawing.Color.FromArgb(221, 243, 229);
                    e.CellStyle.ForeColor = System.Drawing.Color.FromArgb(23, 106, 63);
                }
                else if (status.Equals("Submitted", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.BackColor = System.Drawing.Color.FromArgb(255, 244, 215);
                    e.CellStyle.ForeColor = System.Drawing.Color.FromArgb(135, 83, 16);
                }
                else if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.BackColor = System.Drawing.Color.FromArgb(225, 238, 253);
                    e.CellStyle.ForeColor = System.Drawing.Color.FromArgb(30, 67, 115);
                }
                else if (status.Equals("Verified", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.BackColor = System.Drawing.Color.FromArgb(238, 232, 251);
                    e.CellStyle.ForeColor = System.Drawing.Color.FromArgb(92, 58, 140);
                }
                else if (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.BackColor = System.Drawing.Color.FromArgb(253, 232, 232);
                    e.CellStyle.ForeColor = System.Drawing.Color.FromArgb(150, 40, 51);
                }
            };
            ToolTip rules = new ToolTip();
            rules.SetToolTip(btnEditBooking, "Changes close 48 hours before the session.");
            rules.SetToolTip(btnCancelBooking, "Cancellations close 48 hours before the session.");
            UpdateActionAvailability();
        }

        private void ManageBookings_Load(object sender, EventArgs e)
        {
            LoadBookings();
        }

        private void LoadBookings()
        {
            try
            {
                DataTable table = new DataTable();

                using (OleDbConnection connection =
                    new OleDbConnection(connectionString))
                {
                    connection.Open();

                    string requestIDColumn =
                        BookingData.GetColumn(
                            connection, null,
                            "TUTORING_REQUEST", "RequestID");

                    string query =
                        "SELECT R.[" + requestIDColumn + "] AS BookingKey, " +
                        "C.[CourseCode], C.[CourseName], " +
                        "R.[SupportType], R.[RequestDate], " +
                        "R.[PreferredDate], R.[PreferredStartTime], " +
                        "R.[PreferredEndTime], R.[Status] " +
                        "FROM [TUTORING_REQUEST] AS R " +
                        "LEFT JOIN [COURSE] AS C " +
                        "ON R.[CourseID] = C.[CourseID] " +
                        "WHERE R.[StudentID] = ? " +
                        "ORDER BY R.[PreferredDate] DESC";

                    using (OleDbCommand command =
                        new OleDbCommand(query, connection))
                    {
                        command.Parameters.Add(
                            "@StudentID", OleDbType.Integer).Value =
                            studentID;

                        using (OleDbDataAdapter adapter =
                            new OleDbDataAdapter(command))
                        {
                            adapter.Fill(table);
                        }
                    }
                }

                dgvBookings.DataSource = table;

                dgvBookings.Columns["BookingKey"].Visible = false;
                dgvBookings.Columns["CourseCode"].HeaderText = "Course Code";
                dgvBookings.Columns["CourseName"].HeaderText = "Course Name";
                dgvBookings.Columns["SupportType"].HeaderText = "Support Type";
                dgvBookings.Columns["PreferredDate"].HeaderText = "Session Date";
                dgvBookings.Columns["PreferredStartTime"].HeaderText = "Start Time";
                dgvBookings.Columns["PreferredEndTime"].HeaderText = "End Time";
                dgvBookings.Columns["Status"].HeaderText = "Booking Status";
                dgvBookings.Columns["RequestDate"].HeaderText = "Request Date";

                dgvBookings.Columns["PreferredDate"]
                    .DefaultCellStyle.Format = "dd MMM yyyy";
                dgvBookings.Columns["PreferredStartTime"]
                    .DefaultCellStyle.Format = "HH:mm";
                dgvBookings.Columns["PreferredEndTime"]
                    .DefaultCellStyle.Format = "HH:mm";
                dgvBookings.Columns["RequestDate"]
                    .DefaultCellStyle.Format = "dd MMM yyyy";

                ApplyStatusFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to load bookings.\n\n" + ex.Message,
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ApplyStatusFilter()
        {
            DataTable source = dgvBookings.DataSource as DataTable;
            if (source == null || statusFilter == null)
                return;

            string status = Convert.ToString(statusFilter.SelectedItem);
            source.DefaultView.RowFilter =
                string.IsNullOrEmpty(status) || status == "All bookings"
                    ? "" : "[Status] = '" + status.Replace("'", "''") + "'";

            dgvBookings.ClearSelection();
            UpdateActionAvailability();
        }

        private void UpdateActionAvailability()
        {
            bool allowed = false;
            string message = "Select a booking to see available actions.";

            if (dgvBookings.SelectedRows.Count > 0 &&
                dgvBookings.Columns.Contains("Status") &&
                dgvBookings.Columns.Contains("PreferredDate") &&
                dgvBookings.Columns.Contains("PreferredStartTime"))
            {
                DataGridViewRow row = dgvBookings.SelectedRows[0];
                try
                {
                    string status = Convert.ToString(row.Cells["Status"].Value);
                    DateTime date = Convert.ToDateTime(
                        row.Cells["PreferredDate"].Value).Date;
                    TimeSpan start = BookingData.ReadTime(
                        row.Cells["PreferredStartTime"].Value);
                    DateTime sessionStart = date.Add(start);
                    bool active = status.Equals("Submitted",
                            StringComparison.OrdinalIgnoreCase) ||
                        status.Equals("Scheduled",
                            StringComparison.OrdinalIgnoreCase);

                    allowed = active && DateTime.Now <= sessionStart.AddHours(-48);
                    message = !active
                        ? "This booking is " + status + " and cannot be changed."
                        : allowed
                            ? "You may edit or cancel this booking until " +
                              sessionStart.AddHours(-48).ToString("dd MMM yyyy, HH:mm") + "."
                            : "The 48-hour change deadline has passed for this booking.";
                }
                catch
                {
                    message = "Please select a valid booking to continue.";
                }
            }

            btnEditBooking.Enabled = allowed;
            btnCancelBooking.Enabled = allowed;
            Control[] matches = Controls.Find("lblBookingActionHelp", true);
            if (matches.Length > 0)
                matches[0].Text = message;
        }

        private bool TryGetSelectedBooking(out int requestID)
        {
            requestID = 0;

            if (dgvBookings.SelectedRows.Count == 0)
            {
                MessageBox.Show(
                    "Please select a booking first.",
                    "No Booking Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            object value =
                dgvBookings.SelectedRows[0].Cells["BookingKey"].Value;

            if (value == null || value == DBNull.Value)
                return false;

            requestID = Convert.ToInt32(value);
            return true;
        }

        private void btnEditBooking_Click(object sender, EventArgs e)
        {
            int requestID;

            if (!TryGetSelectedBooking(out requestID))
                return;

            using (EditBooking form =
                new EditBooking(studentID, requestID, connectionString))
            {
                form.ShowDialog(this);
            }

            LoadBookings();
        }

        private void btnCancelBooking_Click(object sender, EventArgs e)
        {
            int requestID;

            if (!TryGetSelectedBooking(out requestID))
                return;

            DialogResult answer = MessageBox.Show(
                "Are you sure you want to cancel this booking?\n\n" +
                "If a tutor has been assigned, their scheduled session " +
                "will also be cancelled.",
                "Confirm Cancellation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (answer != DialogResult.Yes)
                return;

            try
            {
                BookingData.CancelBooking(
                    connectionString, studentID, requestID);

                MessageBox.Show(
                    "Your booking has been cancelled successfully.",
                    "Booking Cancelled",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                LoadBookings();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Cancellation could not be completed.\n\n" +
                    ex.Message,
                    "Cancellation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
