using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace NextGen__7
{
    public partial class TutorSessions : Form
    {
        private readonly int tutorId;
        private readonly string connectionString;

        public TutorSessions()
        {
            InitializeComponent();
            EduConnectInputGuidance.Attach(this);
        }

        public TutorSessions(int tutorId, string tutorName) : this()
        {
            if (tutorId <= 0)
                throw new ArgumentOutOfRangeException("tutorId");
            this.tutorId = tutorId;
            lblTutorName.Text = tutorName + "  |  Tutor #" + tutorId;
            var cfg = ConfigurationManager.ConnectionStrings[
                "NextGen__7.Properties.Settings.Edu_ConnectConnectionString"];
            if (cfg == null)
                throw new InvalidOperationException("Database connection is not configured.");
            connectionString = cfg.ConnectionString;
            btnRefresh.Click += (sender, e) => LoadSessions();
            btnChangePassword.Click += (sender, e) => ChangePassword();
            btnRecord.Click += (sender, e) => RecordSession();
            btnLogout.Click += (sender, e) => Close();
            gridSessions.SelectionChanged += (sender, e) => UpdateSelection();
            Load += (sender, e) => LoadSessions();
        }

        private void ChangePassword()
        {
            using (var dialog = new TutorChangePassword(tutorId, connectionString))
                dialog.ShowDialog(this);
        }

        private static DataTable ReadTable(OleDbConnection db, string table)
        {
            var result = new DataTable();
            using (var cmd = new OleDbCommand("SELECT * FROM [" + table + "]", db))
            using (var da = new OleDbDataAdapter(cmd))
                da.Fill(result);
            return result;
        }

        private static Dictionary<int, DataRow> Index(DataTable data, string key)
        {
            var lookup = new Dictionary<int, DataRow>();
            foreach (DataRow row in data.Rows)
                if (row[key] != DBNull.Value)
                    lookup[Convert.ToInt32(row[key])] = row;
            return lookup;
        }

        private static bool HasColumn(OleDbConnection db, string name)
        {
            using (var cmd = new OleDbCommand(
                "SELECT * FROM [TUTORING_SESSION] WHERE 1=0", db))
            using (var reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
            {
                foreach (DataRow row in reader.GetSchemaTable().Rows)
                    if (string.Equals(Convert.ToString(row["ColumnName"]).Trim('\uFEFF',' '),
                        name, StringComparison.OrdinalIgnoreCase))
                        return true;
            }
            return false;
        }

        private static string RecordingEligibility(DataRow session)
        {
            string status = Convert.ToString(session["SessionStatus"]).Trim();
            if (!status.Equals("Scheduled", StringComparison.OrdinalIgnoreCase) &&
                !status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                return "Not eligible: " + status;
            if (session["SessionDate"] == DBNull.Value)
                return "Date missing";
            DateTime day = Convert.ToDateTime(session["SessionDate"]).Date;
            if (day > DateTime.Today)
                return "Upcoming - not held yet";
            if (day == DateTime.Today)
            {
                if (session["EndTime"] == DBNull.Value)
                    return "End time missing";
                try
                {
                    TimeSpan end = BookingData.ReadTime(session["EndTime"]);
                    if (DateTime.Now < day.Add(end))
                        return "Session not finished yet";
                }
                catch (Exception) { return "Invalid end time"; }
            }
            return "Ready";
        }

        private void LoadSessions()
        {
            if (tutorId <= 0) return;
            btnRecord.Enabled = false;
            gridSessions.DataSource = null;
            try
            {
                using (var db = new OleDbConnection(connectionString))
                {
                    db.Open();
                    if (!HasColumn(db, "AttendanceStatus"))
                    {
                        lblMessage.Text = "The session attendance field is unavailable. Contact system support.";
                        return;
                    }
                    var requests = Index(ReadTable(db, "TUTORING_REQUEST"), "RequestID");
                    var students = Index(ReadTable(db, "STUDENT"), "StudentID");
                    var sessions = new DataTable();
                    using (var command = new OleDbCommand(
                        "SELECT [SessionID],[RequestID],[SessionDate],[StartTime],[EndTime]," +
                        "[SessionStatus],[AttendanceStatus],[Feedback] " +
                        "FROM [TUTORING_SESSION] WHERE [TutorID]=? " +
                        "ORDER BY [SessionDate] DESC", db))
                    {
                        command.Parameters.Add("@Tutor", OleDbType.Integer).Value = tutorId;
                        using (var adapter = new OleDbDataAdapter(command))
                            adapter.Fill(sessions);
                    }
                    var result = new DataTable();
                    result.Columns.Add("Session ID", typeof(int));
                    result.Columns.Add("Student", typeof(string));
                    result.Columns.Add("Date", typeof(DateTime));
                    result.Columns.Add("Time", typeof(string));
                    result.Columns.Add("Status", typeof(string));
                    result.Columns.Add("Attendance", typeof(string));
                    result.Columns.Add("Feedback", typeof(string));
                    result.Columns.Add("Recording", typeof(string));
                    foreach (DataRow session in sessions.Rows)
                    {
                        string name = "Unknown student";
                        DataRow request, student;
                        if (session["RequestID"] != DBNull.Value &&
                            requests.TryGetValue(Convert.ToInt32(session["RequestID"]), out request) &&
                            request["StudentID"] != DBNull.Value &&
                            students.TryGetValue(Convert.ToInt32(request["StudentID"]), out student))
                            name = (Convert.ToString(student["FirstName"]) + " " +
                                    Convert.ToString(student["LastName"])).Trim();

                        string time = session["StartTime"] == DBNull.Value ||
                                      session["EndTime"] == DBNull.Value ? "Time not recorded" :
                            BookingData.ReadTime(session["StartTime"]).ToString(@"hh\:mm") + " - " +
                            BookingData.ReadTime(session["EndTime"]).ToString(@"hh\:mm");
                        result.Rows.Add(Convert.ToInt32(session["SessionID"]), name,
                            session["SessionDate"] == DBNull.Value ? (object)DBNull.Value :
                                Convert.ToDateTime(session["SessionDate"]),
                            time, Convert.ToString(session["SessionStatus"]),
                            Convert.ToString(session["AttendanceStatus"]),
                            Convert.ToString(session["Feedback"]), RecordingEligibility(session));
                    }
                    gridSessions.DataSource = result;
                    if (gridSessions.Columns.Contains("Date"))
                        gridSessions.Columns["Date"].DefaultCellStyle.Format = "dd MMM yyyy";
                    if (gridSessions.Columns.Contains("Feedback"))
                        gridSessions.Columns["Feedback"].Visible = false;
                    gridSessions.ClearSelection();
                    lblMessage.Text = result.Rows.Count + " assigned session(s). " +
                        "Select a past session marked Ready to record attendance and feedback.";
                    txtFeedback.Clear();
                    cmbAttendance.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Could not load sessions: " + ex.Message;
                gridSessions.DataSource = null;
            }
        }

        private void UpdateSelection()
        {
            btnRecord.Enabled = false;
            if (gridSessions.SelectedRows.Count != 1) return;
            DataGridViewRow row = gridSessions.SelectedRows[0];
            if (row.IsNewRow || !gridSessions.Columns.Contains("Recording")) return;
            string state = Convert.ToString(row.Cells["Recording"].Value);
            btnRecord.Enabled = state == "Ready";
            string currentAttendance = Convert.ToString(row.Cells["Attendance"].Value);
            int ix = cmbAttendance.Items.IndexOf(currentAttendance);
            cmbAttendance.SelectedIndex = ix;
            txtFeedback.Text = Convert.ToString(row.Cells["Feedback"].Value);
            lblMessage.Text = state == "Ready"
                ? "Record attendance for Session #" +
                  Convert.ToString(row.Cells["Session ID"].Value) + "."
                : "This session cannot be completed: " + state;
        }

        private void RecordSession()
        {
            if (gridSessions.SelectedRows.Count != 1 ||
                Convert.ToString(gridSessions.SelectedRows[0].Cells["Recording"].Value) != "Ready")
            {
                MessageBox.Show("Select a session that has already finished.");
                return;
            }
            if (cmbAttendance.SelectedIndex < 0)
            {
                MessageBox.Show("Choose Attended or Absent."); return;
            }
            string feedback = txtFeedback.Text.Trim();
            if (feedback.Length == 0)
            {
                MessageBox.Show("Enter a short factual session feedback note."); return;
            }
            if (feedback.Length > 240)
            {
                MessageBox.Show("Feedback cannot exceed 240 characters."); return;
            }
            int sessionId = Convert.ToInt32(gridSessions.SelectedRows[0].Cells["Session ID"].Value);
            if (MessageBox.Show("Complete this tutoring session and save its attendance and " +
                "feedback?", "Confirm session", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                using (var db = new OleDbConnection(connectionString))
                {
                    db.Open();
                    using (var transaction = db.BeginTransaction())
                    {
                        try
                        {
                            var record = new DataTable();
                            using (var query = new OleDbCommand(
                                "SELECT * FROM [TUTORING_SESSION] " +
                                "WHERE [SessionID]=? AND [TutorID]=?", db, transaction))
                            {
                                query.Parameters.Add("@SessionID", OleDbType.Integer).Value = sessionId;
                                query.Parameters.Add("@TutorID", OleDbType.Integer).Value = tutorId;
                                using (var adapter = new OleDbDataAdapter(query))
                                    adapter.Fill(record);
                            }
                            if (record.Rows.Count != 1 ||
                                RecordingEligibility(record.Rows[0]) != "Ready")
                                throw new InvalidOperationException(
                                    "This session is not eligible to be completed.");

                            using (var update = new OleDbCommand(
                                "UPDATE [TUTORING_SESSION] SET [SessionStatus]=?, " +
                                "[AttendanceStatus]=?, [Feedback]=? " +
                                "WHERE [SessionID]=? AND [TutorID]=?", db, transaction))
                            {
                                update.Parameters.Add("@Status", OleDbType.VarWChar).Value = "Completed";
                                update.Parameters.Add("@Attendance", OleDbType.VarWChar).Value =
                                    Convert.ToString(cmbAttendance.SelectedItem);
                                update.Parameters.Add("@Feedback", OleDbType.VarWChar).Value = feedback;
                                update.Parameters.Add("@SessionID", OleDbType.Integer).Value = sessionId;
                                update.Parameters.Add("@TutorID", OleDbType.Integer).Value = tutorId;
                                if (update.ExecuteNonQuery() != 1)
                                    throw new InvalidOperationException("No session was updated.");
                            }
                            string sessionRequestColumn = BookingData.GetColumn(
                                db, transaction, "TUTORING_SESSION", "RequestID");
                            if (record.Rows[0][sessionRequestColumn] == DBNull.Value)
                                throw new InvalidOperationException(
                                    "The completed session has no linked tutoring request.");
                            int requestId = Convert.ToInt32(
                                record.Rows[0][sessionRequestColumn]);
                            string requestKey = BookingData.GetColumn(
                                db, transaction, "TUTORING_REQUEST", "RequestID");
                            using (var changeRequest = new OleDbCommand(
                                "UPDATE [TUTORING_REQUEST] SET [Status]=? " +
                                "WHERE [" + requestKey + "]=? AND [Status]=?", db, transaction))
                            {
                                changeRequest.Parameters.Add("@Status", OleDbType.VarWChar).Value = "Completed";
                                changeRequest.Parameters.Add("@RequestID", OleDbType.Integer).Value = requestId;
                                changeRequest.Parameters.Add("@Previous", OleDbType.VarWChar).Value = "Scheduled";
                                if (changeRequest.ExecuteNonQuery() != 1)
                                    throw new InvalidOperationException(
                                        "Could not mark the linked tutoring request as Completed.");
                            }
                            transaction.Commit();
                        }
                        catch { transaction.Rollback(); throw; }
                    }
                }
                LoadSessions();
                lblMessage.Text = "Session #" + sessionId +
                    " completed. The linked request and attendance were updated.";
                MessageBox.Show(lblMessage.Text,
                    "Attendance saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Session could not be saved.\n\n" + ex.Message,
                    "Record session", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
