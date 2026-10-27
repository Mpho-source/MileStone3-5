using System;
using System.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Windows.Forms;

namespace NextGen__7
{
    public partial class AdminRequests : Form
    {
        private readonly string connectionString;
        private string requestIdColumn;
        public AdminRequests()
        {
            InitializeComponent();
            if (System.ComponentModel.LicenseManager.UsageMode ==
                System.ComponentModel.LicenseUsageMode.Designtime) return;

            var settings = ConfigurationManager.ConnectionStrings[
                "NextGen__7.Properties.Settings.Edu_ConnectConnectionString"];
            if (settings == null) throw new InvalidOperationException("Database connection missing.");
            connectionString = settings.ConnectionString;
        }

        private sealed class TutorOption
        {
            internal int Id;
            internal string Name;
            public override string ToString() { return Name + " (Tutor #" + Id + ")"; }
        }

        private void AdminRequests_Load(object sender, EventArgs e)
        {
            requests.SelectionChanged += (a, b) => RefreshMatchingChoices();
            cmbMatchTutor.SelectedIndexChanged += (a, b) =>
                btnAssign.Enabled = cmbMatchTutor.SelectedItem is TutorOption;
            Reload();
        }
        private void btnRefresh_Click(object sender, EventArgs e) { Reload(); }
        private void btnVerify_Click(object sender, EventArgs e) { Verify(); }
        private void btnBack_Click(object sender, EventArgs e) { Close(); }
        private void btnAssign_Click(object sender, EventArgs e) { AssignTutor(); }


        private void Reload(int? selectedRequestId = null)
        {
            try
            {
                var table = new DataTable();
                using (var db = new OleDbConnection(connectionString))
                using (var adapter = new OleDbDataAdapter(
                    "SELECT * FROM [TUTORING_REQUEST] ORDER BY [PreferredDate] DESC", db))
                    adapter.Fill(table);
                requestIdColumn = null;
                foreach (DataColumn column in table.Columns)
                    if (column.ColumnName.Trim('\uFEFF', ' ').Equals("RequestID",
                        StringComparison.OrdinalIgnoreCase))
                        requestIdColumn = column.ColumnName;
                requests.DataSource = table;
                requests.ClearSelection();
                cmbMatchTutor.Items.Clear();
                btnAssign.Enabled = false;
                lblMatchHelp.Text = "Select a Submitted request and verify it, or select a Verified request to choose an available tutor.";
                if (selectedRequestId.HasValue && requestIdColumn != null)
                {
                    foreach (DataGridViewRow gridRow in requests.Rows)
                    {
                        if (gridRow.IsNewRow || gridRow.Cells[requestIdColumn].Value == null)
                            continue;
                        int rowId;
                        if (int.TryParse(Convert.ToString(gridRow.Cells[requestIdColumn].Value), out rowId) &&
                            rowId == selectedRequestId.Value)
                        {
                            gridRow.Selected = true;
                            if (gridRow.Cells.Count > 0)
                                requests.CurrentCell = gridRow.Cells[0];
                            RefreshMatchingChoices();
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to load tutoring requests.\n\n" + ex.Message,
                    "Requests", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Verify()
        {
            if (requests.SelectedRows.Count != 1 || requestIdColumn == null)
            {
                MessageBox.Show("Select a request first."); return;
            }
            var row = requests.SelectedRows[0];
            int requestId = Convert.ToInt32(row.Cells[requestIdColumn].Value);
            string status = Convert.ToString(row.Cells["Status"].Value);
            if (!string.Equals(status, "Submitted", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Only submitted requests can be verified."); return;
            }
            if (MessageBox.Show("Verify this tutoring request?", "Confirm verification",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                using (var db = new OleDbConnection(connectionString))
                using (var cmd = new OleDbCommand(
                    "UPDATE [TUTORING_REQUEST] SET [Status]=? WHERE [" +
                    requestIdColumn + "]=? AND [Status]=?", db))
                {
                    cmd.Parameters.Add("@NewStatus", OleDbType.VarWChar).Value = "Verified";
                    cmd.Parameters.Add("@RequestID", OleDbType.Integer).Value = requestId;
                    cmd.Parameters.Add("@PreviousStatus", OleDbType.VarWChar).Value = "Submitted";
                    db.Open();
                    if (cmd.ExecuteNonQuery() != 1)
                        throw new InvalidOperationException("The request is no longer submitted.");
                }
                Reload(requestId);
                MessageBox.Show("Tutoring request verified. Select an available tutor from the list.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to verify request.\n\n" + ex.Message);
            }
        }

        private static bool MatchableStatus(string status)
        {
            return string.Equals(status, "Verified", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TutorCanTake(
            OleDbConnection db, OleDbTransaction tx, int tutorId, string courseId,
            DateTime day, TimeSpan start, TimeSpan end)
        {
            string courseTutorColumn = BookingData.GetColumn(db, tx, "TUTOR_COURSE", "TutorID");
            string courseKeyColumn = BookingData.GetColumn(db, tx, "TUTOR_COURSE", "CourseID");
            using (var command = new OleDbCommand(
                "SELECT COUNT(*) FROM [TUTOR_COURSE] WHERE [" + courseTutorColumn +
                "]=? AND [" + courseKeyColumn + "]=?", db, tx))
            {
                command.Parameters.Add("@Tutor", OleDbType.Integer).Value = tutorId;
                command.Parameters.Add("@Course", OleDbType.VarWChar).Value = courseId;
                if (Convert.ToInt32(command.ExecuteScalar()) < 1) return false;
            }

            bool offered = false;
            string availableTutorColumn = BookingData.GetColumn(db, tx, "TUTOR_AVAILABILITY", "TutorID");
            using (var command = new OleDbCommand(
                "SELECT [StartTime],[EndTime],[IsAvailable] FROM [TUTOR_AVAILABILITY] " +
                "WHERE [" + availableTutorColumn + "]=? AND [AvailableDate]>=? AND [AvailableDate]<?", db, tx))
            {
                command.Parameters.Add("@Tutor", OleDbType.Integer).Value = tutorId;
                command.Parameters.Add("@Date1", OleDbType.Date).Value = day.Date;
                command.Parameters.Add("@Date2", OleDbType.Date).Value = day.Date.AddDays(1);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(2))
                            continue;
                        string value = Convert.ToString(reader[2]).Trim();
                        bool active = value.Equals("True", StringComparison.OrdinalIgnoreCase) ||
                                      value.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
                                      value == "1";
                        if (active && BookingData.ReadTime(reader[0]) <= start &&
                            BookingData.ReadTime(reader[1]) >= end)
                        {
                            offered = true;
                            break;
                        }
                    }
                }
            }
            if (!offered) return false;

            using (var command = new OleDbCommand(
                "SELECT [StartTime],[EndTime],[SessionStatus] FROM [TUTORING_SESSION] " +
                "WHERE [TutorID]=? AND [SessionDate]>=? AND [SessionDate]<?", db, tx))
            {
                command.Parameters.Add("@Tutor", OleDbType.Integer).Value = tutorId;
                command.Parameters.Add("@Date1", OleDbType.Date).Value = day.Date;
                command.Parameters.Add("@Date2", OleDbType.Date).Value = day.Date.AddDays(1);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.IsDBNull(0) || reader.IsDBNull(1)) continue;
                        string status = reader.IsDBNull(2) ? "" :
                            Convert.ToString(reader[2]).Trim();
                        if (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) ||
                            status.Equals("Canceled", StringComparison.OrdinalIgnoreCase))
                            continue;
                        if (start < BookingData.ReadTime(reader[1]) &&
                            end > BookingData.ReadTime(reader[0]))
                            return false;
                    }
                }
            }
            return true;
        }

        private static DataRow GetRequest(OleDbConnection db, OleDbTransaction tx, int id)
        {
            var rows = new DataTable();
            using (var command = new OleDbCommand(
                "SELECT * FROM [TUTORING_REQUEST] WHERE [" +
                BookingData.GetColumn(db, tx, "TUTORING_REQUEST", "RequestID") + "]=?", db, tx))
            {
                command.Parameters.Add("@Request", OleDbType.Integer).Value = id;
                using (var adapter = new OleDbDataAdapter(command))
                    adapter.Fill(rows);
            }
            if (rows.Rows.Count != 1)
                throw new InvalidOperationException("The request no longer exists.");
            return rows.Rows[0];
        }

        private static bool HasActiveSession(
            OleDbConnection db, OleDbTransaction tx, int requestId)
        {
            string sessionRequestColumn = BookingData.GetColumn(db, tx, "TUTORING_SESSION", "RequestID");
            using (var command = new OleDbCommand(
                "SELECT [SessionStatus] FROM [TUTORING_SESSION] WHERE [" +
                sessionRequestColumn + "]=?", db, tx))
            {
                command.Parameters.Add("@Request", OleDbType.Integer).Value = requestId;
                using (var reader = command.ExecuteReader())
                    while (reader.Read())
                    {
                        string status = reader.IsDBNull(0) ? "" :
                            Convert.ToString(reader[0]).Trim();
                        if (!status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) &&
                            !status.Equals("Canceled", StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
            }
            return false;
        }

        private static void ReadDetails(DataRow request,
            out string courseId, out int studentId, out DateTime day,
            out TimeSpan start, out TimeSpan end)
        {
            courseId = Convert.ToString(request["CourseID"]).Trim();
            studentId = Convert.ToInt32(request["StudentID"]);
            day = Convert.ToDateTime(request["PreferredDate"]).Date;
            start = BookingData.ReadTime(request["PreferredStartTime"]);
            end = BookingData.ReadTime(request["PreferredEndTime"]);
            if (string.IsNullOrWhiteSpace(courseId) || studentId <= 0 || end <= start ||
                day.Add(start) <= DateTime.Now)
                throw new InvalidOperationException(
                    "This request needs a valid future preferred date and time.");
        }

        private void RefreshMatchingChoices()
        {
            cmbMatchTutor.Items.Clear();
            btnAssign.Enabled = false;
            if (requests.SelectedRows.Count != 1 ||
                requests.SelectedRows[0].IsNewRow ||
                requestIdColumn == null) return;

            var selected = requests.SelectedRows[0];
            int requestId;
            if (!int.TryParse(Convert.ToString(selected.Cells[requestIdColumn].Value),
                out requestId)) return;

            try
            {
                using (var db = new OleDbConnection(connectionString))
                {
                    db.Open();
                    DataRow request = GetRequest(db, null, requestId);
                    if (!MatchableStatus(Convert.ToString(request["Status"])))
                    {
                        lblMatchHelp.Text = "First click Verify selected request. " +
                            "Only Verified requests can be matched.";
                        return;
                    }
                    string course; int student; DateTime day; TimeSpan start, end;
                    ReadDetails(request, out course, out student, out day, out start, out end);
                    if (HasActiveSession(db, null, requestId))
                    {
                        lblMatchHelp.Text = "This request already has a tutoring session.";
                        return;
                    }
                    if (BookingData.HasStudentConflict(
                        db, null, student, requestId, day, start, end))
                    {
                        lblMatchHelp.Text = "The student has another overlapping request.";
                        return;
                    }

                    var tutors = new List<TutorOption>();
                    string tutorIdColumn = BookingData.GetColumn(db, null, "TUTOR", "TutorID");
                    using (var command = new OleDbCommand(
                        "SELECT [" + tutorIdColumn + "],[FirstName],[LastName] FROM [TUTOR] " +
                        "ORDER BY [FirstName],[LastName]", db))
                    using (var reader = command.ExecuteReader())
                        while (reader.Read())
                            tutors.Add(new TutorOption
                            {
                                Id = Convert.ToInt32(reader[0]),
                                Name = (Convert.ToString(reader[1]) + " " +
                                    Convert.ToString(reader[2])).Trim()
                            });

                    foreach (var tutor in tutors)
                        if (TutorCanTake(db, null, tutor.Id, course, day, start, end))
                            cmbMatchTutor.Items.Add(tutor);
                    lblMatchHelp.Text = cmbMatchTutor.Items.Count == 0
                        ? "No eligible tutor has BOTH the course qualification and an available slot on " +
                          day.ToString("dd MMM yyyy") + " at " + start.ToString(@"hh\:mm") +
                          ". Check the tutor's course assignments and availability."
                        : cmbMatchTutor.Items.Count + " eligible tutor(s) for request #" +
                          requestId + " on " + day.ToString("dd MMM yyyy") + " " +
                          start.ToString(@"hh\:mm") + "-" + end.ToString(@"hh\:mm") + ".";
                    if (cmbMatchTutor.Items.Count > 0)
                        cmbMatchTutor.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                lblMatchHelp.Text = "Cannot match this request: " + ex.Message;
                MessageBox.Show(lblMatchHelp.Text, "Tutor matching",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void AssignTutor()
        {
            var option = cmbMatchTutor.SelectedItem as TutorOption;
            if (option == null || requests.SelectedRows.Count != 1 ||
                requestIdColumn == null)
            {
                MessageBox.Show("Select a verified request and an available tutor.");
                return;
            }
            int requestId = Convert.ToInt32(
                requests.SelectedRows[0].Cells[requestIdColumn].Value);
            if (MessageBox.Show("Assign " + option.Name + " to request #" + requestId +
                "? EduConnect will create a Scheduled tutoring session for the student.",
                "Confirm tutor assignment", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                using (var db = new OleDbConnection(connectionString))
                {
                    db.Open();
                    using (var tx = db.BeginTransaction())
                    {
                        try
                        {
                            var request = GetRequest(db, tx, requestId);
                            if (!MatchableStatus(Convert.ToString(request["Status"])))
                                throw new InvalidOperationException(
                                    "The request is no longer Verified.");

                            string course; int student; DateTime day; TimeSpan start, end;
                            ReadDetails(request, out course, out student, out day, out start, out end);
                            if (HasActiveSession(db, tx, requestId))
                                throw new InvalidOperationException(
                                    "A session already exists for this request.");
                            if (BookingData.HasStudentConflict(
                                db, tx, student, requestId, day, start, end))
                                throw new InvalidOperationException(
                                    "The student has an overlapping booking.");
                            if (!TutorCanTake(db, tx, option.Id, course, day, start, end))
                                throw new InvalidOperationException(
                                    "Tutor availability or course qualification has changed.");

                            int nextSessionId;
                            using (var cmd = new OleDbCommand(
                                "SELECT MAX([SessionID]) FROM [TUTORING_SESSION]", db, tx))
                            {
                                var max = cmd.ExecuteScalar();
                                nextSessionId = max == null || max == DBNull.Value
                                    ? 1 : checked(Convert.ToInt32(max) + 1);
                            }
                            using (var command = new OleDbCommand(
                                "INSERT INTO [TUTORING_SESSION] " +
                                "([SessionID],[RequestID],[TutorID],[SessionDate]," +
                                "[StartTime],[EndTime],[SessionStatus]) " +
                                "VALUES (?,?,?,?,?,?,?)", db, tx))
                            {
                                command.Parameters.Add("@SessionID", OleDbType.Integer).Value = nextSessionId;
                                command.Parameters.Add("@RequestID", OleDbType.Integer).Value = requestId;
                                command.Parameters.Add("@TutorID", OleDbType.Integer).Value = option.Id;
                                command.Parameters.Add("@Date", OleDbType.Date).Value = day;
                                command.Parameters.Add("@Start", OleDbType.Date).Value = day.Add(start);
                                command.Parameters.Add("@End", OleDbType.Date).Value = day.Add(end);
                                command.Parameters.Add("@Status", OleDbType.VarWChar).Value = "Scheduled";
                                if (command.ExecuteNonQuery() != 1)
                                    throw new InvalidOperationException("Could not create session.");
                            }
                            using (var command = new OleDbCommand(
                                "UPDATE [TUTORING_REQUEST] SET [Status]=? " +
                                "WHERE [RequestID]=? AND [Status]=?", db, tx))
                            {
                                command.Parameters.Add("@NewStatus", OleDbType.VarWChar).Value = "Scheduled";
                                command.Parameters.Add("@RequestID", OleDbType.Integer).Value = requestId;
                                command.Parameters.Add("@PreviousStatus", OleDbType.VarWChar).Value = "Verified";
                                if (command.ExecuteNonQuery() != 1)
                                    throw new InvalidOperationException(
                                        "Request changed before the assignment was saved.");
                            }
                            tx.Commit();
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }
                }
                Reload(requestId);
                MessageBox.Show("Tutor #" + option.Id + " assigned to request #" +
                    requestId + ". A Scheduled session is now saved. " +
                    "It will appear in the tutor's portal and Report 2.",
                    "Tutor assigned", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to assign tutor.\n\n" + ex.Message,
                    "Assignment problem", MessageBoxButtons.OK, MessageBoxIcon.Error);
                RefreshMatchingChoices();
            }
        }
    }
}
