
using System;
using System.Collections.Generic;
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
            if (System.ComponentModel.LicenseManager.UsageMode ==
                System.ComponentModel.LicenseUsageMode.Designtime) return;

            if (studentID <= 0 || string.IsNullOrWhiteSpace(studentNo))
                throw new InvalidOperationException("Invalid student session.");

            this.studentID = studentID;
            this.studentNo = studentNo;

            ConnectionStringSettings settings =
                ConfigurationManager.ConnectionStrings[
                    "NextGen__7.Properties.Settings.Edu_ConnectConnectionString"];

            if (settings == null)
                throw new InvalidOperationException(
                    "Database connection string not found.");

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
                cmbCourse, "Select the course you need tutoring for.");

            toolTip1.SetToolTip(
                cmbSupportType, "Select the type of tutoring support.");

            toolTip1.SetToolTip(
                dtpSessionDate, "Select your preferred tutoring date.");

            toolTip1.SetToolTip(
                cmbSessionTime, "Select your preferred tutoring time.");

            btnNavDashboard.Click += (sender, e) => Close();
            btnNavBookings.Click += (sender, e) =>
            {
                using (var page = new ManageBookings(studentID, studentNo))
                    page.ShowDialog(this);
            };
            btnNavBack.Click += (sender, e) => Close();
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
                using (OleDbDataAdapter adapter =
                    new OleDbDataAdapter(
                        "SELECT [CourseID], [CourseCode], [CourseName] " +
                        "FROM [COURSE] ORDER BY [CourseCode]",
                        connection))
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
            object sender, EventArgs e)
        {
            if (cmbCourse.SelectedIndex >= 0 &&
                cmbCourse.SelectedItem is DataRowView)
            {
                DataRowView row =
                    (DataRowView)cmbCourse.SelectedItem;

                lblSelectedCourse.Text =
                    "Selected course: " +
                    Convert.ToString(row["CourseCode"]);

                lblSelectedCourse.ForeColor = EduConnectTheme.Navy;
                lblSelectedCourse.BackColor = Color.FromArgb(225, 237, 252);
            }
            else
            {
                lblSelectedCourse.Text = "No course selected";
                lblSelectedCourse.ForeColor = EduConnectTheme.Navy;
                lblSelectedCourse.BackColor = Color.FromArgb(225, 237, 252);
            }
        }

        private static string GetColumnName(
            OleDbConnection connection,
            OleDbTransaction transaction,
            string tableName,
            string columnName)
        {
            string query =
                "SELECT * FROM [" + tableName + "] WHERE 1 = 0";

            using (OleDbCommand command =
                new OleDbCommand(query, connection, transaction))
            using (OleDbDataReader reader = command.ExecuteReader())
            {
                DataTable schema = reader.GetSchemaTable();

                foreach (DataRow row in schema.Rows)
                {
                    string name = Convert.ToString(row["ColumnName"]);

                    if (name.Trim('\uFEFF', ' ')
                        .Equals(columnName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return name;
                    }
                }
            }

            throw new InvalidOperationException(
                columnName + " was not found in " + tableName + ".");
        }

        private static int GetNextID(
            OleDbConnection connection,
            OleDbTransaction transaction,
            string tableName,
            string columnName)
        {
            string query =
                "SELECT MAX([" + columnName + "]) FROM [" +
                tableName + "]";

            using (OleDbCommand command =
                new OleDbCommand(query, connection, transaction))
            {
                object result = command.ExecuteScalar();

                if (result == null || result == DBNull.Value)
                    return 1;

                return checked(Convert.ToInt32(result) + 1);
            }
        }

        private static TimeSpan GetTime(object value)
        {
            if (value == null || value == DBNull.Value)
                throw new InvalidOperationException(
                    "A required time value is missing.");

            if (value is DateTime)
                return ((DateTime)value).TimeOfDay;

            if (value is TimeSpan)
                return (TimeSpan)value;

            string text = Convert.ToString(value).Trim();

            DateTime parsedDate;

            if (DateTime.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out parsedDate))
            {
                return parsedDate.TimeOfDay;
            }

            TimeSpan parsedTime;

            if (TimeSpan.TryParse(
                text,
                CultureInfo.InvariantCulture,
                out parsedTime))
            {
                return parsedTime;
            }

            throw new InvalidOperationException(
                "An invalid time value was found in the database.");
        }

        private bool HasStudentConflict(
            OleDbConnection connection,
            OleDbTransaction transaction,
            DateTime sessionDate,
            TimeSpan start,
            TimeSpan end)
        {
            string query =
                "SELECT [PreferredStartTime], [PreferredEndTime] " +
                "FROM [TUTORING_REQUEST] " +
                "WHERE [StudentID] = ? " +
                "AND [PreferredDate] >= ? " +
                "AND [PreferredDate] < ? " +
                "AND [Status] IN ('Submitted', 'Scheduled')";

            using (OleDbCommand command =
                new OleDbCommand(query, connection, transaction))
            {
                command.Parameters.Add(
                    "@StudentID", OleDbType.Integer).Value = studentID;

                command.Parameters.Add(
                    "@DateStart", OleDbType.Date).Value = sessionDate.Date;

                command.Parameters.Add(
                    "@DateEnd", OleDbType.Date).Value =
                    sessionDate.Date.AddDays(1);

                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.IsDBNull(0) || reader.IsDBNull(1))
                            continue;

                        TimeSpan existingStart = GetTime(reader[0]);
                        TimeSpan existingEnd = GetTime(reader[1]);

                        if (start < existingEnd && end > existingStart)
                            return true;
                    }
                }
            }

            return false;
        }

        private List<int> GetQualifiedTutors(
            OleDbConnection connection,
            OleDbTransaction transaction,
            int courseID)
        {
            List<int> tutors = new List<int>();

            string query =
                "SELECT [TutorID] FROM [TUTOR_COURSE] " +
                "WHERE [CourseID] = ? ORDER BY [TutorID]";

            using (OleDbCommand command =
                new OleDbCommand(query, connection, transaction))
            {
                command.Parameters.Add(
                    "@CourseID", OleDbType.Integer).Value = courseID;

                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(0))
                            tutors.Add(Convert.ToInt32(reader[0]));
                    }
                }
            }

            return tutors;
        }

        private bool IsTutorAvailable(
            OleDbConnection connection,
            OleDbTransaction transaction,
            int tutorID,
            DateTime sessionDate,
            TimeSpan start,
            TimeSpan end)
        {
            string query =
                "SELECT [StartTime], [EndTime], [IsAvailable] " +
                "FROM [TUTOR_AVAILABILITY] " +
                "WHERE [TutorID] = ? " +
                "AND [AvailableDate] >= ? " +
                "AND [AvailableDate] < ?";

            using (OleDbCommand command =
                new OleDbCommand(query, connection, transaction))
            {
                command.Parameters.Add(
                    "@TutorID", OleDbType.Integer).Value = tutorID;

                command.Parameters.Add(
                    "@DateStart", OleDbType.Date).Value = sessionDate.Date;

                command.Parameters.Add(
                    "@DateEnd", OleDbType.Date).Value =
                    sessionDate.Date.AddDays(1);

                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.IsDBNull(0) ||
                            reader.IsDBNull(1) ||
                            reader.IsDBNull(2))
                            continue;

                        string value =
                            Convert.ToString(reader[2]).Trim();

                        bool available =
                            value.Equals("True",
                                StringComparison.OrdinalIgnoreCase) ||
                            value.Equals("Yes",
                                StringComparison.OrdinalIgnoreCase) ||
                            value == "1";

                        if (!available)
                            continue;

                        TimeSpan availableStart = GetTime(reader[0]);
                        TimeSpan availableEnd = GetTime(reader[1]);

                        if (availableStart <= start &&
                            availableEnd >= end)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool HasTutorConflict(
            OleDbConnection connection,
            OleDbTransaction transaction,
            int tutorID,
            DateTime sessionDate,
            TimeSpan start,
            TimeSpan end)
        {
            string query =
                "SELECT [StartTime], [EndTime], [SessionStatus] " +
                "FROM [TUTORING_SESSION] " +
                "WHERE [TutorID] = ? " +
                "AND [SessionDate] >= ? " +
                "AND [SessionDate] < ?";

            using (OleDbCommand command =
                new OleDbCommand(query, connection, transaction))
            {
                command.Parameters.Add(
                    "@TutorID", OleDbType.Integer).Value = tutorID;

                command.Parameters.Add(
                    "@DateStart", OleDbType.Date).Value = sessionDate.Date;

                command.Parameters.Add(
                    "@DateEnd", OleDbType.Date).Value =
                    sessionDate.Date.AddDays(1);

                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.IsDBNull(0) || reader.IsDBNull(1))
                            continue;

                        string status = reader.IsDBNull(2)
                            ? ""
                            : Convert.ToString(reader[2]).Trim();

                        if (status.Equals("Cancelled",
                                StringComparison.OrdinalIgnoreCase) ||
                            status.Equals("Canceled",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        TimeSpan existingStart = GetTime(reader[0]);
                        TimeSpan existingEnd = GetTime(reader[1]);

                        if (start < existingEnd && end > existingStart)
                            return true;
                    }
                }
            }

            return false;
        }

        private int FindAvailableTutor(
            OleDbConnection connection,
            OleDbTransaction transaction,
            int courseID,
            DateTime sessionDate,
            TimeSpan start,
            TimeSpan end)
        {
            List<int> qualifiedTutors =
                GetQualifiedTutors(connection, transaction, courseID);

            foreach (int tutorID in qualifiedTutors)
            {
                if (!IsTutorAvailable(
                    connection, transaction,
                    tutorID, sessionDate, start, end))
                    continue;

                if (HasTutorConflict(
                    connection, transaction,
                    tutorID, sessionDate, start, end))
                    continue;

                return tutorID;
            }

            return 0;
        }

        private string GetTutorName(
            OleDbConnection connection,
            OleDbTransaction transaction,
            int tutorID)
        {
            string idColumn =
                GetColumnName(connection, transaction,
                    "TUTOR", "TutorID");

            string query =
                "SELECT [FirstName], [LastName] " +
                "FROM [TUTOR] WHERE [" + idColumn + "] = ?";

            using (OleDbCommand command =
                new OleDbCommand(query, connection, transaction))
            {
                command.Parameters.Add(
                    "@TutorID", OleDbType.Integer).Value = tutorID;

                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string firstName =
                            Convert.ToString(reader["FirstName"]).Trim();

                        string lastName =
                            Convert.ToString(reader["LastName"]).Trim();

                        string fullName =
                            (firstName + " " + lastName).Trim();

                        if (!string.IsNullOrWhiteSpace(fullName))
                            return fullName;
                    }
                }
            }

            throw new InvalidOperationException(
                "The selected tutor could not be found.");
        }

        private void InsertRequest(
            OleDbConnection connection,
            OleDbTransaction transaction,
            string requestIdColumn,
            int requestID,
            int courseID,
            string supportType,
            DateTime date,
            DateTime start,
            DateTime end,
            string status)
        {
            string query =
                "INSERT INTO [TUTORING_REQUEST] " +
                "([" + requestIdColumn + "], [StudentID], " +
                "[CourseID], [SupportType], [PreferredDate], " +
                "[PreferredStartTime], [PreferredEndTime], " +
                "[RequestDate], [Status]) " +
                "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)";

            using (OleDbCommand command =
                new OleDbCommand(query, connection, transaction))
            {
                command.Parameters.Add(
                    "@RequestID", OleDbType.Integer).Value = requestID;

                command.Parameters.Add(
                    "@StudentID", OleDbType.Integer).Value = studentID;

                command.Parameters.Add(
                    "@CourseID", OleDbType.VarWChar).Value =
                    courseID.ToString(CultureInfo.InvariantCulture);

                command.Parameters.Add(
                    "@SupportType", OleDbType.VarWChar).Value = supportType;

                command.Parameters.Add(
                    "@PreferredDate", OleDbType.Date).Value = date;

                command.Parameters.Add(
                    "@PreferredStartTime", OleDbType.Date).Value = start;

                command.Parameters.Add(
                    "@PreferredEndTime", OleDbType.Date).Value = end;

                command.Parameters.Add(
                    "@RequestDate", OleDbType.Date).Value = DateTime.Now;

                command.Parameters.Add(
                    "@Status", OleDbType.VarWChar).Value = status;

                if (command.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException(
                        "The tutoring request could not be saved.");
            }
        }

        private void InsertSession(
            OleDbConnection connection,
            OleDbTransaction transaction,
            string sessionIdColumn,
            int sessionID,
            int requestID,
            int tutorID,
            DateTime date,
            DateTime start,
            DateTime end)
        {
            string query =
                "INSERT INTO [TUTORING_SESSION] " +
                "([" + sessionIdColumn + "], [RequestID], " +
                "[TutorID], [SessionDate], [StartTime], " +
                "[EndTime], [SessionStatus]) " +
                "VALUES (?, ?, ?, ?, ?, ?, ?)";

            using (OleDbCommand command =
                new OleDbCommand(query, connection, transaction))
            {
                command.Parameters.Add(
                    "@SessionID", OleDbType.Integer).Value = sessionID;

                command.Parameters.Add(
                    "@RequestID", OleDbType.Integer).Value = requestID;

                command.Parameters.Add(
                    "@TutorID", OleDbType.Integer).Value = tutorID;

                command.Parameters.Add(
                    "@SessionDate", OleDbType.Date).Value = date;

                command.Parameters.Add(
                    "@StartTime", OleDbType.Date).Value = start;

                command.Parameters.Add(
                    "@EndTime", OleDbType.Date).Value = end;

                command.Parameters.Add(
                    "@SessionStatus", OleDbType.VarWChar).Value =
                    "Scheduled";

                if (command.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException(
                        "The tutoring session could not be created.");
            }
        }

        private void btnBookSession_Click(object sender, EventArgs e)
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
                    times[0].Trim(), @"hh\:mm",
                    CultureInfo.InvariantCulture, out startTime) ||
                !TimeSpan.TryParseExact(
                    times[1].Trim(), @"hh\:mm",
                    CultureInfo.InvariantCulture, out endTime))
            {
                MessageBox.Show(
                    "The selected time slot is invalid.",
                    "Invalid Time",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            DateTime preferredDate = dtpSessionDate.Value.Date;
            DateTime preferredStart = preferredDate.Add(startTime);
            DateTime preferredEnd = preferredDate.Add(endTime);

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
                courseID = Convert.ToInt32(cmbCourse.SelectedValue);
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
                Convert.ToString(cmbSupportType.SelectedItem).Trim();

            DialogResult confirmation = MessageBox.Show(
                "Course: " + cmbCourse.Text +
                "\nSupport Type: " + supportType +
                "\nDate: " + preferredDate.ToString("dd MMM yyyy") +
                "\nTime: " + selectedSlot +
                "\n\nDo you want to book this tutoring session?",
                "Confirm Booking",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmation != DialogResult.Yes)
                return;

            btnBookSession.Enabled = false;

            bool bookingSaved = false;
            bool tutorMatched = false;
            string tutorName = "";

            try
            {
                using (OleDbConnection connection =
                    new OleDbConnection(connectionString))
                {
                    connection.Open();

                    using (OleDbTransaction transaction =
                        connection.BeginTransaction())
                    {
                        try
                        {
                            if (HasStudentConflict(
                                connection,
                                transaction,
                                preferredDate,
                                startTime,
                                endTime))
                            {
                                throw new InvalidOperationException(
                                    "You already have an active tutoring " +
                                    "booking that overlaps this time slot. " +
                                    "Please choose another date or time.");
                            }

                            string requestIdColumn =
                                GetColumnName(
                                    connection,
                                    transaction,
                                    "TUTORING_REQUEST",
                                    "RequestID");

                            string sessionIdColumn =
                                GetColumnName(
                                    connection,
                                    transaction,
                                    "TUTORING_SESSION",
                                    "SessionID");

                            int tutorID = FindAvailableTutor(
                                connection,
                                transaction,
                                courseID,
                                preferredDate,
                                startTime,
                                endTime);

                            int newRequestID = GetNextID(
                                connection,
                                transaction,
                                "TUTORING_REQUEST",
                                requestIdColumn);

                            if (tutorID > 0)
                            {
                                tutorName = GetTutorName(
                                    connection, transaction, tutorID);

                                int newSessionID = GetNextID(
                                    connection,
                                    transaction,
                                    "TUTORING_SESSION",
                                    sessionIdColumn);

                                InsertRequest(
                                    connection,
                                    transaction,
                                    requestIdColumn,
                                    newRequestID,
                                    courseID,
                                    supportType,
                                    preferredDate,
                                    preferredStart,
                                    preferredEnd,
                                    "Scheduled");

                                InsertSession(
                                    connection,
                                    transaction,
                                    sessionIdColumn,
                                    newSessionID,
                                    newRequestID,
                                    tutorID,
                                    preferredDate,
                                    preferredStart,
                                    preferredEnd);

                                tutorMatched = true;
                            }
                            else
                            {
                                InsertRequest(
                                    connection,
                                    transaction,
                                    requestIdColumn,
                                    newRequestID,
                                    courseID,
                                    supportType,
                                    preferredDate,
                                    preferredStart,
                                    preferredEnd,
                                    "Submitted");
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

                string message =
                    "Course: " + cmbCourse.Text +
                    "\nSupport Type: " + supportType +
                    "\nDate: " +
                    preferredDate.ToString("dd MMM yyyy") +
                    "\nTime: " + selectedSlot;

                if (tutorMatched)
                {
                    MessageBox.Show(
                        "Your tutoring session has been scheduled successfully!" +
                        "\n\n" + message +
                        "\nAssigned Tutor: " + tutorName +
                        "\nStatus: Scheduled" +
                        "\n\nYour tutor has been assigned and " +
                        "your session is confirmed.",
                        "Booking Successful",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(
                        "Your tutoring request has been submitted successfully!" +
                        "\n\n" + message +
                        "\nStatus: Submitted" +
                        "\n\nNo qualified tutor is currently available " +
                        "for your selected date and time." +
                        "\nYour request is awaiting tutor assignment.",
                        "Request Submitted",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Booking could not be completed.\n\n" + ex.Message,
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnBookSession.Enabled = true;
            }

            if (bookingSaved)
                OpenManageBookings();
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
            lblSelectedCourse.ForeColor = EduConnectTheme.Navy;
            lblSelectedCourse.BackColor = Color.FromArgb(225, 237, 252);
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
            object sender, EventArgs e)
        {
        }
    }
}
