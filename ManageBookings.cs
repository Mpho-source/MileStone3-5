
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

        public ManageBookings()
            : this(Login.CurrentStudentID, Login.CurrentStudentNo)
        {
        }

        public ManageBookings(int studentID, string studentNo)
        {
            InitializeComponent();

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

                dgvBookings.ClearSelection();

                btnEditBooking.Enabled = table.Rows.Count > 0;
                btnCancelBooking.Enabled = table.Rows.Count > 0;
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
