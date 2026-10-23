
using System;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
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
            {
                throw new InvalidOperationException(
                    "No valid student session was provided.");
            }

            this.studentID = studentID;
            this.studentNo = studentNo;

            ConnectionStringSettings settings =
                ConfigurationManager.ConnectionStrings[
                    "NextGen__7.Properties.Settings.Edu_ConnectConnectionString"];

            if (settings == null)
            {
                throw new InvalidOperationException(
                    "The database connection is not configured.");
            }

            connectionString = settings.ConnectionString;

            lblStudentNo.Text = "Student No: " + studentNo;

            dgvBookings.ReadOnly = true;
            dgvBookings.MultiSelect = false;
            dgvBookings.SelectionMode =
                DataGridViewSelectionMode.FullRowSelect;
            dgvBookings.AllowUserToAddRows = false;
            dgvBookings.AllowUserToDeleteRows = false;
            dgvBookings.AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill;
            dgvBookings.RowHeadersVisible = false;
            dgvBookings.AutoGenerateColumns = true;

            Load += ManageBookings_Load;

            btnEditBooking.Click += btnEditBooking_Click;
            btnCancelBooking.Click += btnCancelBooking_Click;
        }

        private void ManageBookings_Load(object sender, EventArgs e)
        {
            LoadBookings();
        }

        private bool SessionIsValid()
        {
            return studentID == Login.CurrentStudentID &&
                   studentNo == Login.CurrentStudentNo;
        }

        private void LoadBookings()
        {
            if (!SessionIsValid())
            {
                MessageBox.Show(
                    "Your login session is no longer valid.",
                    "Session Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Close();
                return;
            }

            string query =
                "SELECT R.[RequestID], C.[CourseCode], " +
                "R.[PreferredDate], R.[PreferredStartTime], " +
                "R.[PreferredEndTime], R.[Status] " +
                "FROM [TUTORING_REQUEST] AS R " +
                "INNER JOIN [COURSE] AS C " +
                "ON R.[CourseID] = C.[CourseID] " +
                "WHERE R.[StudentID] = ? " +
                "ORDER BY R.[PreferredDate] DESC";

            try
            {
                DataTable table = new DataTable();

                using (OleDbConnection connection =
                    new OleDbConnection(connectionString))
                {
                    using (OleDbCommand command =
                        new OleDbCommand(query, connection))
                    {
                        command.Parameters.Add(
                            "@StudentID", OleDbType.Integer).Value = studentID;

                        using (OleDbDataAdapter adapter =
                            new OleDbDataAdapter(command))
                        {
                            adapter.Fill(table);
                        }
                    }
                }

                dgvBookings.DataSource = table;

                SetHeader("RequestID", "Request ID");
                SetHeader("CourseCode", "Course");
                SetHeader("PreferredDate", "Preferred Date");
                SetHeader("PreferredStartTime", "Start Time");
                SetHeader("PreferredEndTime", "End Time");
                SetHeader("Status", "Status");

                SetFormat("PreferredDate", "dd MMM yyyy");
                SetFormat("PreferredStartTime", "HH:mm");
                SetFormat("PreferredEndTime", "HH:mm");

                dgvBookings.ClearSelection();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to load your bookings.\n\n" + ex.Message,
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void SetHeader(string columnName, string header)
        {
            if (dgvBookings.Columns.Contains(columnName))
            {
                dgvBookings.Columns[columnName].HeaderText = header;
            }
        }

        private void SetFormat(string columnName, string format)
        {
            if (dgvBookings.Columns.Contains(columnName))
            {
                dgvBookings.Columns[columnName]
                    .DefaultCellStyle.Format = format;
            }
        }

        private bool TryGetSelectedRequest(
            out int requestID, out string status)
        {
            requestID = 0;
            status = null;

            if (dgvBookings.SelectedRows.Count == 0)
            {
                MessageBox.Show(
                    "Please select a booking first.",
                    "No Booking Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            DataGridViewRow row = dgvBookings.SelectedRows[0];

            if (row.IsNewRow ||
                row.Cells["RequestID"].Value == DBNull.Value)
            {
                return false;
            }

            requestID = Convert.ToInt32(
                row.Cells["RequestID"].Value);

            status = Convert.ToString(
                row.Cells["Status"].Value);

            return true;
        }

        private void btnEditBooking_Click(object sender, EventArgs e)
        {
            if (!SessionIsValid())
            {
                MessageBox.Show("Please log in again.");
                Close();
                return;
            }

            int requestID;
            string status;

            if (!TryGetSelectedRequest(out requestID, out status))
                return;

            if (!string.Equals(
                status, "Submitted",
                StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    "Only submitted requests can be edited.",
                    "Editing Not Allowed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            DateTime originalDate;
            DateTime originalStart;
            DateTime originalEnd;

            try
            {
                using (OleDbConnection connection =
                    new OleDbConnection(connectionString))
                {
                    connection.Open();

                    string query =
                        "SELECT [PreferredDate], [PreferredStartTime], " +
                        "[PreferredEndTime] FROM [TUTORING_REQUEST] " +
                        "WHERE [RequestID] = ? AND [StudentID] = ? " +
                        "AND [Status] = ?";

                    using (OleDbCommand command =
                        new OleDbCommand(query, connection))
                    {
                        command.Parameters.Add(
                            "@RequestID", OleDbType.Integer).Value = requestID;
                        command.Parameters.Add(
                            "@StudentID", OleDbType.Integer).Value = studentID;
                        command.Parameters.Add(
                            "@Status", OleDbType.VarWChar).Value = "Submitted";

                        using (OleDbDataReader reader =
                            command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                MessageBox.Show(
                                    "This request is no longer available for editing.");
                                LoadBookings();
                                return;
                            }

                            originalDate =
                                Convert.ToDateTime(reader["PreferredDate"]);

                            originalStart =
                                Convert.ToDateTime(reader["PreferredStartTime"]);

                            originalEnd =
                                Convert.ToDateTime(reader["PreferredEndTime"]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to retrieve the booking.\n\n" + ex.Message,
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            using (Form editForm = new Form())
            {
                editForm.Text = "Edit Booking";
                editForm.Size = new Size(420, 340);
                editForm.StartPosition =
                    FormStartPosition.CenterParent;
                editForm.FormBorderStyle =
                    FormBorderStyle.FixedDialog;
                editForm.MaximizeBox = false;
                editForm.MinimizeBox = false;

                Label lblDate = new Label();
                lblDate.Text = "Preferred Date";
                lblDate.Location = new Point(25, 25);
                lblDate.AutoSize = true;

                DateTimePicker dtpDate = new DateTimePicker();
                dtpDate.Location = new Point(25, 50);
                dtpDate.Width = 345;
                dtpDate.Format = DateTimePickerFormat.Short;
                dtpDate.MinDate = DateTime.Today;
                dtpDate.Value = originalDate.Date < DateTime.Today
                    ? DateTime.Today
                    : originalDate.Date;

                Label lblStart = new Label();
                lblStart.Text = "Start Time";
                lblStart.Location = new Point(25, 95);
                lblStart.AutoSize = true;

                DateTimePicker dtpStart = new DateTimePicker();
                dtpStart.Location = new Point(25, 120);
                dtpStart.Width = 345;
                dtpStart.Format = DateTimePickerFormat.Custom;
                dtpStart.CustomFormat = "HH:mm";
                dtpStart.ShowUpDown = true;
                dtpStart.Value = originalStart;

                Label lblEnd = new Label();
                lblEnd.Text = "End Time";
                lblEnd.Location = new Point(25, 165);
                lblEnd.AutoSize = true;

                DateTimePicker dtpEnd = new DateTimePicker();
                dtpEnd.Location = new Point(25, 190);
                dtpEnd.Width = 345;
                dtpEnd.Format = DateTimePickerFormat.Custom;
                dtpEnd.CustomFormat = "HH:mm";
                dtpEnd.ShowUpDown = true;
                dtpEnd.Value = originalEnd;

                Button btnSave = new Button();
                btnSave.Text = "Save Changes";
                btnSave.Location = new Point(80, 245);
                btnSave.Size = new Size(115, 35);
                btnSave.DialogResult = DialogResult.OK;

                Button btnCancel = new Button();
                btnCancel.Text = "Cancel";
                btnCancel.Location = new Point(215, 245);
                btnCancel.Size = new Size(100, 35);
                btnCancel.DialogResult = DialogResult.Cancel;

                editForm.Controls.Add(lblDate);
                editForm.Controls.Add(dtpDate);
                editForm.Controls.Add(lblStart);
                editForm.Controls.Add(dtpStart);
                editForm.Controls.Add(lblEnd);
                editForm.Controls.Add(dtpEnd);
                editForm.Controls.Add(btnSave);
                editForm.Controls.Add(btnCancel);

                editForm.AcceptButton = btnSave;
                editForm.CancelButton = btnCancel;

                btnSave.Click += (s, args) =>
                {
                    TimeSpan start = dtpStart.Value.TimeOfDay;
                    TimeSpan end = dtpEnd.Value.TimeOfDay;

                    if (end <= start)
                    {
                        MessageBox.Show(
                            "The end time must be after the start time.",
                            "Invalid Time",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        editForm.DialogResult = DialogResult.None;
                        return;
                    }

                    if (dtpDate.Value.Date == DateTime.Today &&
                        start <= DateTime.Now.TimeOfDay)
                    {
                        MessageBox.Show(
                            "Please select a future session time.",
                            "Invalid Time",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        editForm.DialogResult = DialogResult.None;
                    }
                };

                if (editForm.ShowDialog(this) != DialogResult.OK)
                    return;

                DateTime newDate = dtpDate.Value.Date;
                DateTime newStart =
                    newDate.Add(dtpStart.Value.TimeOfDay);
                DateTime newEnd =
                    newDate.Add(dtpEnd.Value.TimeOfDay);

                try
                {
                    using (OleDbConnection connection =
                        new OleDbConnection(connectionString))
                    {
                        connection.Open();

                        string query =
                            "UPDATE [TUTORING_REQUEST] " +
                            "SET [PreferredDate] = ?, " +
                            "[PreferredStartTime] = ?, " +
                            "[PreferredEndTime] = ? " +
                            "WHERE [RequestID] = ? " +
                            "AND [StudentID] = ? " +
                            "AND [Status] = ?";

                        using (OleDbCommand command =
                            new OleDbCommand(query, connection))
                        {
                            command.Parameters.Add(
                                "@Date", OleDbType.Date).Value = newDate;

                            command.Parameters.Add(
                                "@Start", OleDbType.Date).Value = newStart;

                            command.Parameters.Add(
                                "@End", OleDbType.Date).Value = newEnd;

                            command.Parameters.Add(
                                "@RequestID", OleDbType.Integer).Value =
                                requestID;

                            command.Parameters.Add(
                                "@StudentID", OleDbType.Integer).Value =
                                studentID;

                            command.Parameters.Add(
                                "@Status", OleDbType.VarWChar).Value =
                                "Submitted";

                            int affected = command.ExecuteNonQuery();

                            if (affected == 0)
                            {
                                MessageBox.Show(
                                    "This request could not be updated. " +
                                    "Its status may have changed.");
                            }
                            else
                            {
                                MessageBox.Show(
                                    "Your booking has been updated successfully!",
                                    "Booking Updated",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                            }
                        }
                    }

                    LoadBookings();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Unable to update the booking.\n\n" + ex.Message,
                        "Database Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private void btnCancelBooking_Click(object sender, EventArgs e)
        {
            if (!SessionIsValid())
            {
                MessageBox.Show("Please log in again.");
                Close();
                return;
            }

            int requestID;
            string status;

            if (!TryGetSelectedRequest(out requestID, out status))
                return;

            if (!string.Equals(
                status, "Submitted",
                StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    "Only submitted requests can be cancelled here.",
                    "Cancellation Not Allowed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            DialogResult confirmation = MessageBox.Show(
                "Are you sure you want to cancel this tutoring request?",
                "Confirm Cancellation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmation != DialogResult.Yes)
                return;

            try
            {
                using (OleDbConnection connection =
                    new OleDbConnection(connectionString))
                {
                    connection.Open();

                    string query =
                        "UPDATE [TUTORING_REQUEST] " +
                        "SET [Status] = ? " +
                        "WHERE [RequestID] = ? " +
                        "AND [StudentID] = ? " +
                        "AND [Status] = ?";

                    using (OleDbCommand command =
                        new OleDbCommand(query, connection))
                    {
                        command.Parameters.Add(
                            "@NewStatus", OleDbType.VarWChar).Value =
                            "Cancelled";

                        command.Parameters.Add(
                            "@RequestID", OleDbType.Integer).Value =
                            requestID;

                        command.Parameters.Add(
                            "@StudentID", OleDbType.Integer).Value =
                            studentID;

                        command.Parameters.Add(
                            "@OldStatus", OleDbType.VarWChar).Value =
                            "Submitted";

                        int affected = command.ExecuteNonQuery();

                        if (affected == 0)
                        {
                            MessageBox.Show(
                                "The request could not be cancelled. " +
                                "Its status may have changed.");
                        }
                        else
                        {
                            MessageBox.Show(
                                "Your tutoring request has been cancelled.",
                                "Booking Cancelled",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                    }
                }

                LoadBookings();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to cancel the booking.\n\n" + ex.Message,
                    "Database Error",
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
