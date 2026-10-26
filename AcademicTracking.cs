using System;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace NextGen__7
{
    public partial class AcademicTracking : Form
    {
        private readonly string connectionString;
        private DataTable students;
        private DataTable sessions;
        private DataTable requests;
        private string sessionIdColumn;

        public AcademicTracking() : this(null) { }

        public AcademicTracking(string connection)
        {
            InitializeComponent();
            if (System.ComponentModel.LicenseManager.UsageMode ==
                System.ComponentModel.LicenseUsageMode.Designtime) return;
            if (string.IsNullOrEmpty(connection))
            {
                var cfg = ConfigurationManager.ConnectionStrings[
                    "NextGen__7.Properties.Settings.Edu_ConnectConnectionString"];
                if (cfg == null) throw new InvalidOperationException("Database not configured.");
                connection = cfg.ConnectionString;
            }
            connectionString = connection;
            numYear.Value = Math.Max(2020, Math.Min(2100, DateTime.Today.Year));
            cmbTerm.SelectedIndex = 0;
            btnSetup.Click += (s, e) => SetupSchema();
            btnSaveGpa.Click += (s, e) => SaveGpa();
            btnSaveAttendance.Click += (s, e) => SaveAttendance();
            btnBack.Click += (s, e) => Close();
            cmbStudent.SelectedIndexChanged += (s, e) => LoadSessions();
            Load += (s, e) => LoadStudents();
        }

        public static bool HasTable(OleDbConnection db, string name)
        {
            DataTable schema = db.GetSchema("Tables");
            foreach (DataRow row in schema.Rows)
                if (Convert.ToString(row["TABLE_NAME"]).Equals(
                    name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public static bool HasColumn(OleDbConnection db, string table, string logical)
        {
            using (var cmd = new OleDbCommand("SELECT * FROM [" + table + "] WHERE 1=0", db))
            using (var reader = cmd.ExecuteReader(System.Data.CommandBehavior.SchemaOnly))
                foreach (DataRow row in reader.GetSchemaTable().Rows)
                    if (Convert.ToString(row["ColumnName"]).Trim('\uFEFF', ' ').Equals(
                        logical, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static string Column(DataTable table, string logical)
        {
            foreach (DataColumn col in table.Columns)
                if (col.ColumnName.Trim('\uFEFF', ' ').Equals(logical,
                    StringComparison.OrdinalIgnoreCase)) return col.ColumnName;
            throw new InvalidOperationException("Missing " + logical + " in " + table.TableName);
        }

        private DataTable Read(OleDbConnection db, string table)
        {
            var data = new DataTable(table);
            using (var adapter = new OleDbDataAdapter("SELECT * FROM [" + table + "]", db))
                adapter.Fill(data);
            return data;
        }

        private void LoadStudents()
        {
            try
            {
                using (var db = new OleDbConnection(connectionString))
                {
                    db.Open();
                    students = Read(db, "STUDENT");
                    string id = Column(students, "StudentID");
                    students.Columns.Add("Display", typeof(string));
                    foreach (DataRow row in students.Rows)
                        row["Display"] = Convert.ToString(row["StudentNo"]) + " - " +
                            Convert.ToString(row["FirstName"]) + " " +
                            Convert.ToString(row["LastName"]);
                    cmbStudent.DisplayMember = "Display";
                    cmbStudent.ValueMember = id;
                    cmbStudent.DataSource = students;
                    lblStatus.Text = HasTable(db, "STUDENTACADEMICRECORD") &&
                                     HasColumn(db, "TUTORING_SESSION", "AttendanceStatus")
                        ? "Academic tracking is ready."
                        : "Choose Set up tracking to add the required fields.";
                }
                if (students.Rows.Count > 0) cmbStudent.SelectedIndex = 0;
            }
            catch (Exception ex) { lblStatus.Text = "Load failed: " + ex.Message; }
        }

        private void SetupSchema()
        {
            if (MessageBox.Show(
                "Create the academic-record table and an AttendanceStatus field " +
                "in TUTORING_SESSION? Existing records are not deleted.",
                "Set up academic tracking", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                using (var db = new OleDbConnection(connectionString))
                {
                    db.Open();
                    if (!HasTable(db, "STUDENTACADEMICRECORD"))
                    {
                        using (var cmd = new OleDbCommand(
                            "CREATE TABLE [STUDENTACADEMICRECORD] (" +
                            "[AcademicRecordID] COUNTER PRIMARY KEY, " +
                            "[StudentNo] TEXT(50), [AcademicPeriod] TEXT(12), " +
                            "[GPA] DOUBLE, [RecordDate] DATETIME)", db))
                            cmd.ExecuteNonQuery();
                    }
                    if (!HasColumn(db, "TUTORING_SESSION", "AttendanceStatus"))
                    {
                        using (var cmd = new OleDbCommand(
                            "ALTER TABLE [TUTORING_SESSION] ADD COLUMN [AttendanceStatus] TEXT(20)", db))
                            cmd.ExecuteNonQuery();
                    }
                }
                lblStatus.Text = "Academic tracking enabled. Enter real GPAs and attendance below.";
                LoadSessions();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database setup failed. Make sure Access is not locking the " +
                    "table.\n\n" + ex.Message, "Schema error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int StudentID()
        {
            return cmbStudent.SelectedValue is DataRowView ||
                cmbStudent.SelectedValue == null ? 0 :
                Convert.ToInt32(cmbStudent.SelectedValue);
        }

        private void SaveGpa()
        {
            int studentId = StudentID();
            DataRowView selected = cmbStudent.SelectedItem as DataRowView;
            string studentNo = selected == null ? "" : Convert.ToString(selected["StudentNo"]).Trim();
            double gpa;
            if (studentId <= 0 || studentNo.Length == 0)
            { MessageBox.Show("Select a student."); return; }
            if (cmbTerm.SelectedIndex < 0)
            { MessageBox.Show("Select an academic term."); return; }
            if (!double.TryParse(txtGpa.Text.Trim(), out gpa) || gpa < 0 || gpa > 100)
            { MessageBox.Show("Enter a GPA percentage between 0 and 100."); return; }

            string period = ((int)numYear.Value) + "-T" + (cmbTerm.SelectedIndex + 1);
            try
            {
                using (var db = new OleDbConnection(connectionString))
                {
                    db.Open();
                    if (!HasTable(db, "STUDENTACADEMICRECORD"))
                    { MessageBox.Show("Click Set up tracking first."); return; }

                    bool studentNumberKey = HasColumn(db, "STUDENTACADEMICRECORD", "StudentNo");
                    bool legacyIdKey = HasColumn(db, "STUDENTACADEMICRECORD", "StudentID");
                    if (!studentNumberKey && !legacyIdKey)
                        throw new InvalidOperationException(
                            "Academic records have neither StudentNo nor StudentID.");
                    string key = studentNumberKey ? "StudentNo" : "StudentID";
                    OleDbType type = studentNumberKey ? OleDbType.VarWChar : OleDbType.Integer;
                    object id = studentNumberKey ? (object)studentNo : studentId;

                    using (var check = new OleDbCommand(
                        "SELECT COUNT(*) FROM [STUDENTACADEMICRECORD] WHERE [" +
                        key + "]=? AND [AcademicPeriod]=?", db))
                    {
                        check.Parameters.Add("@Student", type).Value = id;
                        check.Parameters.Add("@Period", OleDbType.VarWChar).Value = period;
                        bool exists = Convert.ToInt32(check.ExecuteScalar()) > 0;
                        string sql = exists
                            ? "UPDATE [STUDENTACADEMICRECORD] SET [GPA]=?, [RecordDate]=? " +
                              "WHERE [" + key + "]=? AND [AcademicPeriod]=?"
                            : "INSERT INTO [STUDENTACADEMICRECORD] ([" +
                              key + "],[AcademicPeriod],[GPA],[RecordDate]) VALUES (?,?,?,?)";
                        using (var cmd = new OleDbCommand(sql, db))
                        {
                            if (exists)
                            {
                                cmd.Parameters.Add("@GPA", OleDbType.Double).Value = gpa;
                                cmd.Parameters.Add("@Date", OleDbType.Date).Value = DateTime.Now;
                                cmd.Parameters.Add("@Student", type).Value = id;
                                cmd.Parameters.Add("@Period", OleDbType.VarWChar).Value = period;
                            }
                            else
                            {
                                cmd.Parameters.Add("@Student", type).Value = id;
                                cmd.Parameters.Add("@Period", OleDbType.VarWChar).Value = period;
                                cmd.Parameters.Add("@GPA", OleDbType.Double).Value = gpa;
                                cmd.Parameters.Add("@Date", OleDbType.Date).Value = DateTime.Now;
                            }
                            if (cmd.ExecuteNonQuery() != 1)
                                throw new InvalidOperationException("No academic record was saved.");
                        }
                    }
                }
                lblStatus.Text = "GPA saved for " + studentNo + " in " + period + ".";
            }
            catch (Exception ex)
            {
                MessageBox.Show("GPA save failed.\n\n" + ex.Message,
                    "Database error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadSessions()
        {
            int studentId = StudentID();
            if (studentId <= 0) return;
            try
            {
                using (var db = new OleDbConnection(connectionString))
                {
                    db.Open();
                    if (!HasColumn(db, "TUTORING_SESSION", "AttendanceStatus"))
                    {
                        dgvSessions.DataSource = null; return;
                    }
                    requests = Read(db, "TUTORING_REQUEST");
                    sessions = Read(db, "TUTORING_SESSION");
                    sessionIdColumn = Column(sessions, "SessionID");
                    string reqId = Column(requests, "RequestID");
                    var ids = new System.Collections.Generic.HashSet<int>();
                    foreach (DataRow row in requests.Rows)
                        if (row["StudentID"] != DBNull.Value &&
                            Convert.ToInt32(row["StudentID"]) == studentId)
                            ids.Add(Convert.ToInt32(row[reqId]));
                    string sessionReq = Column(sessions, "RequestID");
                    var visible = new DataTable();
                    visible.Columns.Add("Session ID", typeof(int));
                    visible.Columns.Add("Session date", typeof(DateTime));
                    visible.Columns.Add("Session status", typeof(string));
                    visible.Columns.Add("Attendance", typeof(string));
                    foreach (DataRow row in sessions.Rows)
                        if (row[sessionReq] != DBNull.Value &&
                            ids.Contains(Convert.ToInt32(row[sessionReq])))
                            visible.Rows.Add(Convert.ToInt32(row[sessionIdColumn]),
                                Convert.ToDateTime(row["SessionDate"]),
                                Convert.ToString(row["SessionStatus"]),
                                Convert.ToString(row["AttendanceStatus"]));
                    dgvSessions.DataSource = visible;
                    dgvSessions.ClearSelection();
                }
            }
            catch (Exception ex) { lblStatus.Text = "Session lookup failed: " + ex.Message; }
        }

        private void SaveAttendance()
        {
            if (dgvSessions.SelectedRows.Count != 1)
            { MessageBox.Show("Select a tutoring session in the table."); return; }
            if (cmbAttendance.SelectedIndex < 0)
            { MessageBox.Show("Select Attended or Absent."); return; }
            var selected = dgvSessions.SelectedRows[0];
            int sessionID = Convert.ToInt32(selected.Cells["Session ID"].Value);
            DateTime sessionDay = Convert.ToDateTime(selected.Cells["Session date"].Value).Date;
            string sessionStatus = Convert.ToString(selected.Cells["Session status"].Value).Trim();
            if (sessionDay > DateTime.Today)
            {
                MessageBox.Show("Attendance cannot be recorded for a future session."); return;
            }
            if (!sessionStatus.Equals("Scheduled", StringComparison.OrdinalIgnoreCase) &&
                !sessionStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Attendance can only be recorded for a scheduled or completed session.");
                return;
            }
            try
            {
                using (var db = new OleDbConnection(connectionString))
                {
                    db.Open();
                    if (!HasColumn(db, "TUTORING_SESSION", "AttendanceStatus"))
                    { MessageBox.Show("Click Set up tracking first."); return; }
                    string id = BookingData.GetColumn(db, null, "TUTORING_SESSION", "SessionID");
                    using (var cmd = new OleDbCommand(
                        "UPDATE [TUTORING_SESSION] SET [AttendanceStatus]=?, [SessionStatus]=? WHERE [" +
                        id + "]=?", db))
                    {
                        cmd.Parameters.Add("@Attendance", OleDbType.VarWChar).Value = cmbAttendance.Text;
                        cmd.Parameters.Add("@SessionStatus", OleDbType.VarWChar).Value = "Completed";
                        cmd.Parameters.Add("@SessionID", OleDbType.Integer).Value = sessionID;
                        if (cmd.ExecuteNonQuery() != 1)
                            throw new InvalidOperationException("Session not updated.");
                    }
                }
                LoadSessions();
                lblStatus.Text = "Session attendance saved.";
            }
            catch (Exception ex) { MessageBox.Show("Attendance save failed.\n\n" + ex.Message); }
        }
    }
}
