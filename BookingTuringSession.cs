
using System;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace NextGen__7
{
    public partial class BookingTutoringSession : Form
    {
        private readonly int studentID;
        private readonly string studentNo;
        private readonly string connectionString;

        public BookingTutoringSession()
            : this(Login.CurrentStudentID, Login.CurrentStudentNo)
        {
        }

        public BookingTutoringSession(int studentID, string studentNo)
        {
            InitializeComponent();

            if (studentID <= 0 || string.IsNullOrWhiteSpace(studentNo))
            {
                throw new InvalidOperationException(
                    "Invalid student session.");
            }

            this.studentID = studentID;
            this.studentNo = studentNo;

            ConnectionStringSettings settings =
                ConfigurationManager.ConnectionStrings[
                    "NextGen__7.Properties.Settings.Edu_ConnectConnectionString"];

            if (settings == null)
            {
                throw new InvalidOperationException(
                    "Database connection string not found.");
            }

            connectionString = settings.ConnectionString;

            txtStudentID.Text = studentNo;
            txtStudentID.ReadOnly = true;
            txtStudentID.TabStop = false;

            cmbCourse.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSupportType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSessionTime.DropDownStyle = ComboBoxStyle.DropDownList;

            dtpSessionDate.ShowCheckBox = true;
            dtpSessionDate.MinDate = DateTime.Today;
            dtpSessionDate.Checked = false;

            toolTip1.SetToolTip(
                cmbCourse,
                "Select the course you need tutoring for.");

            toolTip1.SetToolTip(
                cmbSupportType,
                "Select the type of tutoring support you need.");

            toolTip1.SetToolTip(
                dtpSessionDate,
                "Select your preferred tutoring date.");

            toolTip1.SetToolTip(
                cmbSessionTime,
                "Select your preferred tutoring time.");
        }

        private void BookingTutoringSession_Load(object sender, EventArgs e)
        {
            try
            {
                LoadCourses();
                ClearFields();

                if (cmbCourse.Items.Count == 0)
                {
                    btnBookSession.Enabled = false;

                    MessageBox.Show(
                        "No courses are available for booking.",
                        "No Courses",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                btnBookSession.Enabled = false;

                MessageBox.Show(
                    "Unable to load courses.\n\n" + ex.Message,
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void LoadCourses()
        {
            DataTable courses = new DataTable();

            using (OleDbConnection connection =
                new OleDbConnection(connectionString))
            {
                string query =
                    "SELECT [CourseID], [CourseCode], [CourseName] " +
                    "FROM [COURSE] ORDER BY [CourseCode]";

                using (OleDbDataAdapter adapter =
                    new OleDbDataAdapter(query, connection))
                {
                    adapter.Fill(courses);
                }
            }

            courses.Columns.Add("DisplayName", typeof(string));

            foreach (DataRow row in courses.Rows)
            {
                row["DisplayName"] =
                    Convert.ToString(row["CourseCode"]) +
                    " - " +
                    Convert.ToString(row["CourseName"]);
            }

            cmbCourse.DataSource = null;
            cmbCourse.DisplayMember = "DisplayName";
            cmbCourse.ValueMember = "CourseID";
            cmbCourse.DataSource = courses;
            cmbCourse.SelectedIndex = -1;
        }

        private void cmbCourse_SelectedIndexChanged(
            object sender,
            EventArgs e)
        {
            if (cmbCourse.SelectedIndex >= 0 &&
                cmbCourse.SelectedItem is DataRowView)
            {
                DataRowView row =
                    (DataRowView)cmbCourse.SelectedItem;

                lblSelectedCourse.Text =
                    "Selected course: " +
                    Convert.ToString(row["CourseCode"]);

                lblSelectedCourse.ForeColor = Color.Green;
                lblSelectedCourse.BackColor = Color.LightGreen;
            }
            else
            {
                lblSelectedCourse.Text = "No course selected";
                lblSelectedCourse.ForeColor = Color.Black;
                lblSelectedCourse.BackColor = SystemColors.Control;
            }
        }

        private static string GetRequestIdColumn(
            OleDbConnection connection,
            OleDbTransaction transaction)
        {
            using (OleDbCommand command =
                new OleDbCommand(
                    "SELECT * FROM [TUTORING_REQUEST] WHERE 1 = 0",
                    connection,
                    transaction))
            using (OleDbDataReader reader =
                command.ExecuteReader())
            {
                DataTable schema = reader.GetSchemaTable();

                foreach (DataRow row in schema.Rows)
                {
                    string name =
                        Convert.ToString(row["ColumnName"]);

                    if (name.Trim('\uFEFF', ' ')
                        .Equals(
                            "RequestID",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return name;
                    }
                }
            }

            throw new InvalidOperationException(
                "RequestID column could not be found.");
        }

        private bool HasStudentConflict(
            OleDbConnection connection,
            OleDbTransaction transaction,
            DateTime sessionDate,
            DateTime start,
            DateTime end)
        {
            string query =
                "SELECT [PreferredStartTime], [PreferredEndTime] " +
                "FROM [TUTORING_REQUEST] " +
                "WHERE [StudentID] = ? " +
                "AND [PreferredDate] >= ? " +
                "AND [PreferredDate] < ? " +
                "AND [Status] IN ('Submitted', 'Scheduled')";

            using (OleDbCommand command =
                new OleDbCommand(
                    query,
                    connection,
                    transaction))
            {
                command.Parameters.Add(
                    "@StudentID",
                    OleDbType.Integer).Value = studentID;

                command.Parameters.Add(
                    "@DateStart",
                    OleDbType.Date).Value = sessionDate.Date;

                command.Parameters.Add(
                    "@DateEnd",
                    OleDbType.Date).Value =
                        sessionDate.Date.AddDays(1);

                using (OleDbDataReader reader =
                    command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.IsDBNull(0) ||
                            reader.IsDBNull(1))
                        {
                            continue;
                        }

                        TimeSpan existingStart =
                            Convert.ToDateTime(
                                reader.GetValue(0)).TimeOfDay;

                        TimeSpan existingEnd =
                            Convert.ToDateTime(
                                reader.GetValue(1)).TimeOfDay;

                        if (start.TimeOfDay < existingEnd &&
                            end.TimeOfDay > existingStart)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool HasRecordedTutorAvailability(
            OleDbConnection connection,
            OleDbTransaction transaction,
            DateTime sessionDate,
            DateTime start,
            DateTime end)
        {
            string query =
                "SELECT [AvailableDate], [StartTime], " +
                "[EndTime], [IsAvailable] " +
                "FROM [TUTOR_AVAILABILITY] " +
                "WHERE [AvailableDate] >= ? " +
                "AND [AvailableDate] < ?";

            using (OleDbCommand command =
                new OleDbCommand(
                    query,
                    connection,
                    transaction))
            {
                command.Parameters.Add(
                    "@DateStart",
                    OleDbType.Date).Value = sessionDate.Date;

                command.Parameters.Add(
                    "@DateEnd",
                    OleDbType.Date).Value =
                        sessionDate.Date.AddDays(1);

                using (OleDbDataReader reader =
                    command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.IsDBNull(1) ||
                            reader.IsDBNull(2) ||
                            reader.IsDBNull(3))
                        {
                            continue;
                        }

                        string availability =
                            Convert.ToString(
                                reader["IsAvailable"]).Trim();

                        bool isAvailable =
                            availability.Equals(
                                "Yes",
                                StringComparison.OrdinalIgnoreCase) ||
                            availability.Equals(
                                "True",
                                StringComparison.OrdinalIgnoreCase) ||
                            availability.Equals(
                                "Available",
                                StringComparison.OrdinalIgnoreCase) ||
                            availability == "1";

                        if (!isAvailable)
                        {
                            continue;
                        }

                        TimeSpan availableStart =
                            Convert.ToDateTime(
                                reader["StartTime"]).TimeOfDay;

                        TimeSpan availableEnd =
                            Convert.ToDateTime(
                                reader["EndTime"]).TimeOfDay;

                        if (availableStart <= start.TimeOfDay &&
                            availableEnd >= end.TimeOfDay)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void btnBookSession_Click(
            object sender,
            EventArgs e)
        {
            if (studentID != Login.CurrentStudentID ||
                studentNo != Login.CurrentStudentNo)
            {
                MessageBox.Show(
                    "Your student session is no longer valid. " +
                    "Please log in again.",
                    "Session Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                Close();
                return;
            }

            if (cmbCourse.SelectedIndex < 0)
            {
                MessageBox.Show(
                    "Please select a course.",
                    "Missing Course",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                cmbCourse.Focus();
                return;
            }

            if (cmbSupportType.SelectedIndex < 0)
            {
                MessageBox.Show(
                    "Please select a support type.",
                    "Missing Support Type",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                cmbSupportType.Focus();
                return;
            }

            if (!dtpSessionDate.Checked)
            {
                MessageBox.Show(
                    "Please select a tutoring session date.",
                    "Missing Date",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                dtpSessionDate.Focus();
                return;
            }

            if (cmbSessionTime.SelectedIndex < 0)
            {
                MessageBox.Show(
                    "Please select a tutoring time slot.",
                    "Missing Time",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                cmbSessionTime.Focus();
                return;
            }

            string selectedSlot =
                Convert.ToString(cmbSessionTime.SelectedItem);

            string[] times = selectedSlot.Split(
                new string[] { " - " },
                StringSplitOptions.None);

            TimeSpan startTime;
            TimeSpan endTime;

            if (times.Length != 2 ||
                !TimeSpan.TryParseExact(
                    times[0].Trim(),
                    @"hh\:mm",
                    CultureInfo.InvariantCulture,
                    out startTime) ||
                !TimeSpan.TryParseExact(
                    times[1].Trim(),
                    @"hh\:mm",
                    CultureInfo.InvariantCulture,
                    out endTime))
            {
                MessageBox.Show(
                    "The selected time slot is invalid.",
                    "Invalid Time",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            DateTime preferredDate =
                dtpSessionDate.Value.Date;

            DateTime preferredStart =
                preferredDate.Add(startTime);

            DateTime preferredEnd =
                preferredDate.Add(endTime);

            if (preferredStart <= DateTime.Now)
            {
                MessageBox.Show(
                    "Please select a future session date and time.",
                    "Invalid Date",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (preferredEnd <= preferredStart)
            {
                MessageBox.Show(
                    "The session end time must be after the start time.",
                    "Invalid Time",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            int courseID;

            try
            {
                courseID =
                    Convert.ToInt32(cmbCourse.SelectedValue);
            }
            catch
            {
                MessageBox.Show(
                    "Please select a valid course.",
                    "Invalid Course",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            string supportType =
                Convert.ToString(
                    cmbSupportType.SelectedItem).Trim();

            DialogResult confirmation =
                MessageBox.Show(
                    "Course: " + cmbCourse.Text +
                    "\nSupport Type: " + supportType +
                    "\nDate: " +
                    preferredDate.ToString("dd MMM yyyy") +
                    "\nTime: " + selectedSlot +
                    "\n\nDo you want to submit this tutoring request?",
                    "Confirm Booking",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

            if (confirmation != DialogResult.Yes)
            {
                return;
            }

            btnBookSession.Enabled = false;
            bool bookingSaved = false;

            try
            {
                int newRequestID = 0;
                bool recordedTutorAvailability = false;

                using (OleDbConnection connection =
                    new OleDbConnection(connectionString))
                {
                    connection.Open();

                    using (OleDbTransaction transaction =
                        connection.BeginTransaction())
                    {
                        try
                        {
                            bool studentConflict =
                                HasStudentConflict(
                                    connection,
                                    transaction,
                                    preferredDate,
                                    preferredStart,
                                    preferredEnd);

                            if (studentConflict)
                            {
                                transaction.Rollback();

                                MessageBox.Show(
                                    "You already have an active tutoring " +
                                    "request that overlaps this time slot." +
                                    "\n\nPlease select another date or time.",
                                    "Booking Conflict",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);

                                return;
                            }

                            recordedTutorAvailability =
                                HasRecordedTutorAvailability(
                                    connection,
                                    transaction,
                                    preferredDate,
                                    preferredStart,
                                    preferredEnd);

                            string idColumn =
                                GetRequestIdColumn(
                                    connection,
                                    transaction);

                            string idQuery =
                                "SELECT MAX([" + idColumn + "]) " +
                                "FROM [TUTORING_REQUEST]";

                            using (OleDbCommand idCommand =
                                new OleDbCommand(
                                    idQuery,
                                    connection,
                                    transaction))
                            {
                                object result =
                                    idCommand.ExecuteScalar();

                                if (result == null ||
                                    result == DBNull.Value)
                                {
                                    newRequestID = 1;
                                }
                                else
                                {
                                    newRequestID = checked(
                                        Convert.ToInt32(result) + 1);
                                }
                            }

                            string insertQuery =
                                "INSERT INTO [TUTORING_REQUEST] " +
                                "([" + idColumn + "], " +
                                "[StudentID], [CourseID], " +
                                "[SupportType], [PreferredDate], " +
                                "[PreferredStartTime], [PreferredEndTime], " +
                                "[RequestDate], [Status]) " +
                                "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)";

                            using (OleDbCommand command =
                                new OleDbCommand(
                                    insertQuery,
                                    connection,
                                    transaction))
                            {
                                command.Parameters.Add(
                                    "@RequestID",
                                    OleDbType.Integer).Value =
                                        newRequestID;

                                command.Parameters.Add(
                                    "@StudentID",
                                    OleDbType.Integer).Value =
                                        studentID;

                                command.Parameters.Add(
                                    "@CourseID",
                                    OleDbType.Integer).Value =
                                        courseID;

                                command.Parameters.Add(
                                    "@SupportType",
                                    OleDbType.VarWChar).Value =
                                        supportType;

                                command.Parameters.Add(
                                    "@PreferredDate",
                                    OleDbType.Date).Value =
                                        preferredDate;

                                command.Parameters.Add(
                                    "@PreferredStartTime",
                                    OleDbType.Date).Value =
                                        preferredStart;

                                command.Parameters.Add(
                                    "@PreferredEndTime",
                                    OleDbType.Date).Value =
                                        preferredEnd;

                                command.Parameters.Add(
                                    "@RequestDate",
                                    OleDbType.Date).Value =
                                        DateTime.Now;

                                command.Parameters.Add(
                                    "@Status",
                                    OleDbType.VarWChar).Value =
                                        "Submitted";

                                int affectedRows =
                                    command.ExecuteNonQuery();

                                if (affectedRows != 1)
                                {
                                    throw new InvalidOperationException(
                                        "The tutoring request was not saved.");
                                }
                            }

                            transaction.Commit();
                            bookingSaved = true;
                        }
                        catch
                        {
                            try
                            {
                                transaction.Rollback();
                            }
                            catch
                            {
                            }

                            throw;
                        }
                    }
                }

                string availabilityMessage;

                if (recordedTutorAvailability)
                {
                    availabilityMessage =
                        "\n\nA tutor has recorded availability " +
                        "for this time slot." +
                        "\nCourse suitability and final tutor " +
                        "assignment still need to be verified.";
                }
                else
                {
                    availabilityMessage =
                        "\n\nNo tutor availability is currently " +
                        "recorded for this time slot." +
                        "\nYour request is awaiting review.";
                }

                MessageBox.Show(
                    "Your tutoring request has been submitted successfully!" +
                    "\n\nCourse: " + cmbCourse.Text +
                    "\nSupport Type: " + supportType +
                    "\nDate: " +
                    preferredDate.ToString("dd MMM yyyy") +
                    "\nTime: " + selectedSlot +
                    "\nStatus: Submitted" +
                    availabilityMessage,
                    "Booking Successful",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Booking could not be completed.\n\n" +
                    ex.Message,
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnBookSession.Enabled = true;
            }

            if (bookingSaved)
            {
                OpenManageBookings();
            }
        }

        private void OpenManageBookings()
        {
            Hide();

            try
            {
                using (ManageBookings form =
                    new ManageBookings(studentID, studentNo))
                {
                    form.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Your booking was saved, but Manage Bookings " +
                    "could not be opened.\n\n" + ex.Message,
                    "Navigation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                Close();
            }
        }

        private void ClearFields()
        {
            cmbCourse.SelectedIndex = -1;
            cmbSupportType.SelectedIndex = -1;
            cmbSessionTime.SelectedIndex = -1;

            dtpSessionDate.Value = DateTime.Today;
            dtpSessionDate.Checked = false;

            lblSelectedCourse.Text = "No course selected";
            lblSelectedCourse.ForeColor = Color.Black;
            lblSelectedCourse.BackColor = SystemColors.Control;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ClearFields();
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void label1_Click(object sender, EventArgs e)
        {
        }

        private void label2_Click(object sender, EventArgs e)
        {
        }

        private void label3_Click(object sender, EventArgs e)
        {
        }

        private void label4_Click(object sender, EventArgs e)
        {
        }

        private void label6_Click(object sender, EventArgs e)
        {
        }

        private void dtpSessionDate_ValueChanged(
            object sender,
            EventArgs e)
        {
        }
    }
}
