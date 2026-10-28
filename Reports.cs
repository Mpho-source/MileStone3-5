using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace NextGen__7
{
    public partial class Reports : Form
    {
        private readonly string connectionString;
        private DataTable result;
        private bool loadingStudentChoices;

        private sealed class StudentTrendChoice
        {
            internal string Number;
            internal string Label;
            public override string ToString() { return Label; }
        }
        private string reportTitle = "";
        private int printRow;
        private bool printTrendCoverPending;
        private List<DataRow> printRows = new List<DataRow>();
        private readonly Font printBody = new Font("Segoe UI", 9F);
        private readonly Font printHead = new Font("Segoe UI", 9F, FontStyle.Bold);

        public Reports()
        {
            InitializeComponent();
            EduConnectInputGuidance.Attach(this);
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            var settings = ConfigurationManager.ConnectionStrings[
                "NextGen__7.Properties.Settings.Edu_ConnectConnectionString"];
            if (settings == null) throw new InvalidOperationException("Missing database connection.");
            connectionString = settings.ConnectionString;
            numYear.Value = Math.Max(2020, Math.Min(2100, DateTime.Today.Year));
            fromDate.Value = DateTime.Today.AddMonths(-1);
            toDate.Value = DateTime.Today;
            cmbReport.SelectedIndexChanged += (s, e) => ChangeReport();
            fromDate.ValueChanged += (s, e) => UpdateCriteria();
            toDate.ValueChanged += (s, e) => UpdateCriteria();
            numYear.ValueChanged += (s, e) => UpdateCriteria();
            btnGenerate.Click += (s, e) => Generate();
            cmbStudentChoice.SelectedIndexChanged += (s, e) =>
            {
                if (loadingStudentChoices || result == null ||
                    cmbReport.SelectedIndex != 2) return;
                ShowTrendChart(result);
            };
            btnToggleView.Click += (s, e) => ToggleView();
            btnPreview.Click += (s, e) => PreviewReport();
            btnPrint.Click += (s, e) => PrintReport();
            printer.BeginPrint += (sender, e) => PreparePrintPages();
            btnBack.Click += (s, e) => Close();
            cmbReport.SelectedIndex = 0;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(31, 56, 100);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(221, 235, 251);
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(31, 56, 100);
        }

        private static bool HasTable(OleDbConnection connection, string tableName)
        {
            DataTable schema = connection.GetSchema("Tables");
            foreach (DataRow row in schema.Rows)
            {
                if (string.Equals(Convert.ToString(row["TABLE_NAME"]), tableName,
                    StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool HasColumn(OleDbConnection connection, string table, string column)
        {
            using (var command = new OleDbCommand("SELECT * FROM [" + table + "] WHERE 1=0", connection))
            using (var reader = command.ExecuteReader(CommandBehavior.SchemaOnly))
            {
                foreach (DataRow row in reader.GetSchemaTable().Rows)
                {
                    if (string.Equals(
                        Convert.ToString(row["ColumnName"]).Trim('\uFEFF', ' '),
                        column, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        private DataTable ReadTable(OleDbConnection db, string name)
        {
            var table = new DataTable();
            using (var adapter = new OleDbDataAdapter("SELECT * FROM [" + name + "]", db))
                adapter.Fill(table);
            return table;
        }

        private static string Column(DataTable table, string logical)
        {
            foreach (DataColumn col in table.Columns)
                if (col.ColumnName.Trim('\uFEFF', ' ').Equals(logical,
                    StringComparison.OrdinalIgnoreCase)) return col.ColumnName;
            throw new InvalidOperationException("Missing " + logical + " column in " + table.TableName);
        }

        private static Dictionary<int, DataRow> Index(DataTable table, string logical)
        {
            var output = new Dictionary<int, DataRow>();
            string key = Column(table, logical);
            foreach (DataRow row in table.Rows)
                if (row[key] != DBNull.Value)
                    output[Convert.ToInt32(row[key])] = row;
            return output;
        }

        private void ChangeReport()
        {
            bool workload = cmbReport.SelectedIndex == 1;
            bool trend = cmbReport.SelectedIndex == 2;
            fromDate.Enabled = workload;
            toDate.Enabled = workload;
            lblFrom.Enabled = workload;
            lblTo.Enabled = workload;
            numYear.Enabled = trend;
            lblYear.Enabled = trend;
            lblStudentChoice.Visible = trend;
            cmbStudentChoice.Visible = trend;
            lblFrom.Visible = workload;
            fromDate.Visible = workload;
            lblTo.Visible = workload;
            toDate.Visible = workload;
            lblTrendSummary.Visible = false;
            UpdateCriteria();
            btnToggleView.Visible = trend;
            btnToggleView.Enabled = false;
            grid.DataSource = null;
            result = null;
            chartTrends.Visible = false;
            grid.Visible = true;
            lblStatus.Text = trend
                ? "Displays available academic results and tutor-recorded session attendance."
                : "Select Generate report to retrieve the latest data.";
        }

        private void UpdateCriteria()
        {
            if (cmbReport.SelectedIndex == 0)
                lblReportCriteria.Text = "Eligible records: Status = Submitted, RequestDate on or before " +
                    DateTime.Today.AddDays(-7).ToString("dd MMM yyyy") + ".";
            else if (cmbReport.SelectedIndex == 1)
                lblReportCriteria.Text = "Counts sessions with status Scheduled or Completed from " +
                    fromDate.Value.ToString("dd MMM yyyy") + " to " +
                    toDate.Value.ToString("dd MMM yyyy") + ".";
            else if (cmbReport.SelectedIndex == 2)
                lblReportCriteria.Text = "GPA / attendance for " + numYear.Value +
                    ". Provisional grouping: Jan-Mar, Apr-Jun, Jul-Sep, Oct-Dec (confirm actual term dates).";
        }

        private void Generate()
        {
            if (cmbReport.SelectedIndex == 1 && fromDate.Value.Date > toDate.Value.Date)
            {
                MessageBox.Show("The start date must not exceed the end date."); return;
            }
            // Report 3 is read-only and uses academic records supplied separately.
            if (cmbReport.SelectedIndex == 2)
            {
                try
                {
                    bool trackingReady;
                    using (var database = new OleDbConnection(connectionString))
                    {
                        database.Open();
                        trackingReady = HasTable(database, "STUDENTACADEMICRECORD") &&
                            HasColumn(database, "TUTORING_SESSION", "AttendanceStatus");
                    }
                    if (!trackingReady)
                    {
                        result = null;
                        grid.DataSource = null;
                        chartTrends.Visible = false;
                        grid.Visible = true;
                        btnToggleView.Enabled = false;
                        lblStatus.Text = "Academic results or attendance data source is not available.";
                        MessageBox.Show(
                            "This report needs existing academic results and attendance records.\n\n" +
                            "GPA should come from an approved academic records source or import.\n" +
                            "Attendance must be recorded as part of the tutoring session workflow.\n\n" +
                            "This reporting screen does not create or edit those records.",
                            "Report data unavailable",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }
                catch (OleDbException ex)
                {
                    lblStatus.Text = "Could not verify academic data.";
                    MessageBox.Show("Could not check academic data.\n\n" + ex.Message,
                        "Database problem", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            try
            {
                chartTrends.Visible = false;
                grid.Visible = true;
                switch (cmbReport.SelectedIndex)
                {
                    case 0:
                        reportTitle = "Outstanding Tutoring Requests (7+ Days)";
                        result = OutstandingRequests();
                        break;
                    case 1:
                        reportTitle = "Tutor Workload by Reporting Period";
                        result = TutorWorkload();
                        break;
                    case 2:
                        reportTitle = "Student GPA and Tutoring Attendance Trend";
                        result = AcademicTrend();
                        break;
                    default: return;
                }
                grid.DataSource = result;
                grid.ClearSelection();
                bool hasTrend = cmbReport.SelectedIndex == 2 && result.Rows.Count > 0;
                btnToggleView.Enabled = hasTrend;
                if (hasTrend)
                {
                    BindTrendChoices(result);
                    ShowTrendChart(result);
                }
                else
                    lblTrendSummary.Visible = false;
                btnToggleView.Text = chartTrends.Visible ? "Show table" : "Show chart";
                grid.AutoResizeColumnHeadersHeight();
                if (result.Rows.Count == 0)
                {
                    lblStatus.Text = cmbReport.SelectedIndex == 0
                        ? "No requests are both Submitted and at least 7 days old."
                        : cmbReport.SelectedIndex == 1
                            ? "No tutors or sessions found. Review dates and assigned tutors."
                            : "No GPA records found for the selected academic year.";
                }
                else if (cmbReport.SelectedIndex == 1)
                {
                    int scheduled = result.AsEnumerable().Sum(row => row.Field<int>("Scheduled"));
                    int completed = result.AsEnumerable().Sum(row => row.Field<int>("Completed"));
                    lblStatus.Text = scheduled + " scheduled, " + completed + " completed | " +
                        fromDate.Value.ToShortDateString() + " to " + toDate.Value.ToShortDateString();
                    if (scheduled == 0 && completed == 0)
                        MessageBox.Show("The TUTOR table has " + result.Rows.Count +
                            " tutors, but no Scheduled or Completed tutoring sessions fall in " +
                            "the selected period.\n\n" +
                            "Check TUTORING_SESSION: TutorID, SessionDate, and SessionStatus. " +
                            "Book a session with an available tutor or select a period containing sessions.",
                            "No matching tutor sessions", MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                }
                else
                    lblStatus.Text = result.Rows.Count + " matching records | " +
                        (cmbReport.SelectedIndex == 2 ? numYear.Value + " academic year" :
                         "Submitted requests aged 7+ days");
            }
            catch (Exception ex)
            {
                result = null;
                grid.DataSource = null;
                chartTrends.Visible = false;
                grid.Visible = true;
                btnToggleView.Enabled = false;
                lblStatus.Text = "Unable to generate report.";
                MessageBox.Show("The report could not be generated.\n\n" + ex.Message,
                    "Report error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DataTable OutstandingRequests()
        {
            var output = new DataTable();
            output.Columns.Add("Request ID", typeof(int));
            output.Columns.Add("Student number", typeof(string));
            output.Columns.Add("Student", typeof(string));
            output.Columns.Add("Course", typeof(string));
            output.Columns.Add("Submitted", typeof(DateTime));
            output.Columns.Add("Age (days)", typeof(int));
            output.Columns.Add("Status", typeof(string));
            using (var db = new OleDbConnection(connectionString))
            {
                db.Open();
                var students = Index(ReadTable(db, "STUDENT"), "StudentID");
                var courses = Index(ReadTable(db, "COURSE"), "CourseID");
                var requests = ReadTable(db, "TUTORING_REQUEST");
                string id = Column(requests, "RequestID");
                foreach (DataRow row in requests.Rows)
                {
                    string status = Convert.ToString(row["Status"]).Trim();
                    if (!status.Equals("Submitted", StringComparison.OrdinalIgnoreCase) ||
                        row["RequestDate"] == DBNull.Value) continue;
                    DateTime submitted = Convert.ToDateTime(row["RequestDate"]);
                    int age = (DateTime.Today - submitted.Date).Days;
                    if (age < 7) continue;
                    DataRow student = null, course = null;
                    if (row["StudentID"] != DBNull.Value)
                        students.TryGetValue(Convert.ToInt32(row["StudentID"]), out student);
                    if (row["CourseID"] != DBNull.Value)
                    {
                        int courseId;
                        if (int.TryParse(Convert.ToString(row["CourseID"]), out courseId))
                            courses.TryGetValue(courseId, out course);
                    }
                    string studentName = student == null ? "Unknown" :
                        (Convert.ToString(student["FirstName"]) + " " +
                        Convert.ToString(student["LastName"])).Trim();
                    output.Rows.Add(Convert.ToInt32(row[id]),
                        student == null ? "-" : Convert.ToString(student["StudentNo"]),
                        studentName, course == null ? "-" :
                        Convert.ToString(course["CourseCode"]), submitted, age, status);
                }
            }
            var view = output.DefaultView;
            view.Sort = "[Submitted] ASC";
            return view.ToTable();
        }

        private DataTable TutorWorkload()
        {
            var output = new DataTable();
            output.Columns.Add("Tutor ID", typeof(int));
            output.Columns.Add("Tutor", typeof(string));
            output.Columns.Add("Scheduled", typeof(int));
            output.Columns.Add("Completed", typeof(int));
            output.Columns.Add("Total sessions", typeof(int));
            output.Columns.Add("Completion rate", typeof(string));
            using (var db = new OleDbConnection(connectionString))
            {
                db.Open();
                var tutors = ReadTable(db, "TUTOR");
                var sessions = new DataTable();
                using (var cmd = new OleDbCommand(
                    "SELECT [TutorID], [SessionStatus] FROM [TUTORING_SESSION] " +
                    "WHERE [SessionDate]>=? AND [SessionDate]<?", db))
                {
                    cmd.Parameters.Add("@Start", OleDbType.Date).Value = fromDate.Value.Date;
                    cmd.Parameters.Add("@End", OleDbType.Date).Value = toDate.Value.Date.AddDays(1);
                    using (var da = new OleDbDataAdapter(cmd)) da.Fill(sessions);
                }
                var counts = new Dictionary<int, int[]>();
                var names = new Dictionary<int, string>();
                string id = Column(tutors, "TutorID");
                foreach (DataRow tutor in tutors.Rows)
                {
                    int tid = Convert.ToInt32(tutor[id]);
                    counts[tid] = new int[2];
                    names[tid] = (Convert.ToString(tutor["FirstName"]) + " " +
                                  Convert.ToString(tutor["LastName"])).Trim();
                }
                foreach (DataRow session in sessions.Rows)
                {
                    if (session["TutorID"] == DBNull.Value) continue;
                    int tid = Convert.ToInt32(session["TutorID"]);
                    if (!counts.ContainsKey(tid))
                    {
                        counts[tid] = new int[2]; names[tid] = "Unknown tutor";
                    }
                    string status = Convert.ToString(session["SessionStatus"]).Trim();
                    if (status.Equals("Scheduled", StringComparison.OrdinalIgnoreCase))
                        counts[tid][0]++;
                    else if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                        counts[tid][1]++;
                }
                foreach (int tid in counts.Keys.OrderBy(a => names[a]))
                {
                    int scheduled = counts[tid][0], completed = counts[tid][1];
                    int total = scheduled + completed;
                    output.Rows.Add(tid, names[tid], scheduled, completed, total,
                        total == 0 ? "—" : ((100.0 * completed / total).ToString("0.0") + "%"));
                }
            }
            return output;
        }

        private DataTable AcademicTrend()
        {
            var output = new DataTable();
            output.Columns.Add("Student number", typeof(string));
            output.Columns.Add("Student", typeof(string));
            output.Columns.Add("Term", typeof(string));
            output.Columns.Add("GPA (%)", typeof(double));
            output.Columns.Add("Attended", typeof(int));
            output.Columns.Add("Recorded sessions", typeof(int));
            output.Columns.Add("Attendance (%)", typeof(double));
            output.Columns.Add("Data origin", typeof(string));
            int year = (int)numYear.Value;
            using (var db = new OleDbConnection(connectionString))
            {
                db.Open();
                if (!HasTable(db, "STUDENTACADEMICRECORD") ||
                    !HasColumn(db, "TUTORING_SESSION", "AttendanceStatus"))
                    throw new InvalidOperationException(
                        "The database does not contain both academic results and " +
                        "tutor-recorded session attendance. No values will be invented.");
                var studentTable = ReadTable(db, "STUDENT");
                var students = Index(studentTable, "StudentID");
                var studentsByNumber = new Dictionary<string, DataRow>(
                    StringComparer.OrdinalIgnoreCase);
                foreach (DataRow studentRow in studentTable.Rows)
                {
                    string number = Convert.ToString(studentRow["StudentNo"]).Trim();
                    if (number.Length > 0) studentsByNumber[number] = studentRow;
                }
                var records = ReadTable(db, "STUDENTACADEMICRECORD");
                var sessions = ReadTable(db, "TUTORING_SESSION");
                var requests = Index(ReadTable(db, "TUTORING_REQUEST"), "RequestID");
                string sessionRequestId = Column(sessions, "RequestID");
                var attendance = new Dictionary<string, int[]>();
                var syntheticAttendance = new HashSet<string>();
                foreach (DataRow row in sessions.Rows)
                {
                    if (row[sessionRequestId] == DBNull.Value ||
                        row["SessionDate"] == DBNull.Value) continue;
                    DataRow request;
                    if (!requests.TryGetValue(Convert.ToInt32(row[sessionRequestId]), out request) ||
                        request["StudentID"] == DBNull.Value) continue;
                    DateTime when = Convert.ToDateTime(row["SessionDate"]);
                    if (when.Year != year) continue;
                    // An attendance value on an unfinished booking is not a held session.
                    if (!Convert.ToString(row["SessionStatus"]).Trim().Equals(
                        "Completed", StringComparison.OrdinalIgnoreCase)) continue;
                    string status = Convert.ToString(row["AttendanceStatus"]).Trim();
                    if (!status.Equals("Attended", StringComparison.OrdinalIgnoreCase) &&
                        !status.Equals("Absent", StringComparison.OrdinalIgnoreCase)) continue;
                    string key = Convert.ToInt32(request["StudentID"]) + "-T" +
                        ((when.Month - 1) / 3 + 1);
                    if (!attendance.ContainsKey(key)) attendance[key] = new int[2];
                    if (sessions.Columns.Contains("Feedback") &&
                        Convert.ToString(row["Feedback"]).Contains(
                            "EDUCONNECT SYNTHETIC TEST ATTENDANCE"))
                        syntheticAttendance.Add(key);
                    attendance[key][1]++;
                    if (status.Equals("Attended", StringComparison.OrdinalIgnoreCase))
                        attendance[key][0]++;
                }
                bool byStudentNumber = records.Columns.Contains("StudentNo");
                if (!byStudentNumber && !records.Columns.Contains("StudentID"))
                    throw new InvalidOperationException(
                        "Academic records require StudentNo or the legacy StudentID.");
                foreach (DataRow rec in records.Rows)
                {
                    if (rec["GPA"] == DBNull.Value) continue;
                    int studentID;
                    DataRow student;
                    if (byStudentNumber)
                    {
                        string number = Convert.ToString(rec["StudentNo"]).Trim();
                        if (!studentsByNumber.TryGetValue(number, out student)) continue;
                        studentID = Convert.ToInt32(student["StudentID"]);
                    }
                    else
                    {
                        if (rec["StudentID"] == DBNull.Value) continue;
                        studentID = Convert.ToInt32(rec["StudentID"]);
                        if (!students.TryGetValue(studentID, out student)) continue;
                    }
                    string period = Convert.ToString(rec["AcademicPeriod"]).Trim();
                    if (!period.StartsWith(year + "-T", StringComparison.OrdinalIgnoreCase)) continue;
                    int term;
                    if (!int.TryParse(period.Substring((year + "-T").Length), out term) ||
                        term < 1 || term > 4) continue;
                    double gpa = Convert.ToDouble(rec["GPA"]);
                    string key = studentID + "-T" + term;
                    int[] stats;
                    if (!attendance.TryGetValue(key, out stats)) stats = new int[2];
                    object percent = stats[1] == 0 ? (object)DBNull.Value :
                        Math.Round(100.0 * stats[0] / stats[1], 1);
                    bool demoGpa = records.Columns.Contains("RecordSource") &&
                        Convert.ToString(rec["RecordSource"]).Contains(
                            "EDUCONNECT SYNTHETIC TEST GPA");
                    string dataOrigin = demoGpa || syntheticAttendance.Contains(key) ?
                        "SYNTHETIC TEST DATA" : "Recorded data";
                    output.Rows.Add(Convert.ToString(student["StudentNo"]),
                        (Convert.ToString(student["FirstName"]) + " " +
                        Convert.ToString(student["LastName"])).Trim(),
                        "Term " + term, gpa, stats[0], stats[1], percent, dataOrigin);
                }
            }
            var view = output.DefaultView; view.Sort = "[Student] ASC, [Term] ASC";
            output = view.ToTable();
            return output;
        }

        private void BindTrendChoices(DataTable data)
        {
            string previous = (cmbStudentChoice.SelectedItem as StudentTrendChoice)?.Number;
            loadingStudentChoices = true;
            try
            {
                cmbStudentChoice.Items.Clear();
                cmbStudentChoice.Items.Add(new StudentTrendChoice
                {
                    Number = "",
                    Label = "All students (average)"
                });
                var students = data.AsEnumerable()
                    .GroupBy(r => Convert.ToString(r["Student number"]))
                    .Select(g => new StudentTrendChoice
                    {
                        Number = g.Key,
                        Label = Convert.ToString(g.First()["Student"]) + " (" + g.Key + ")"
                    })
                    .OrderBy(item => item.Label, StringComparer.OrdinalIgnoreCase);
                foreach (StudentTrendChoice item in students)
                    cmbStudentChoice.Items.Add(item);

                int index = 0;
                for (int i = 1; i < cmbStudentChoice.Items.Count; i++)
                {
                    if (((StudentTrendChoice)cmbStudentChoice.Items[i]).Number == previous)
                    {
                        index = i;
                        break;
                    }
                }
                cmbStudentChoice.SelectedIndex = index;
            }
            finally
            {
                loadingStudentChoices = false;
            }
        }

        private static string ClassifyTrend(List<DataRow> studentRows)
        {
            var gpas = studentRows.OrderBy(r => Convert.ToString(r["Term"])).ToList();
            int recordedTerms = studentRows.Count(r =>
                Convert.ToInt32(r["Recorded sessions"]) > 0);
            if (gpas.Count < 2 || recordedTerms < 2) return "Insufficient data";
            int total = studentRows.Sum(r => Convert.ToInt32(r["Recorded sessions"]));
            int attended = studentRows.Sum(r => Convert.ToInt32(r["Attended"]));
            double attendancePct = total == 0 ? 0 : 100.0 * attended / total;
            double first = Convert.ToDouble(gpas.First()["GPA (%)"]);
            double latest = Convert.ToDouble(gpas.Last()["GPA (%)"]);
            double change = latest - first;
            // Indicative thresholds, not official university academic policy.
            if (latest < 50 && attendancePct < 70) return "Intervention";
            if (change <= -3 && attendancePct >= 70) return "Tutor review";
            if (change >= 3 && attendancePct >= 70) return "Progressing";
            return "Monitor";
        }

        private void ShowTrendChart(DataTable data)
        {
            var selected = cmbStudentChoice.SelectedItem as StudentTrendChoice;
            bool individual = selected != null && !string.IsNullOrEmpty(selected.Number);
            var rows = data.AsEnumerable()
                .Where(r => !individual ||
                    Convert.ToString(r["Student number"]) == selected.Number).ToList();

            chartTrends.Series.Clear();
            chartTrends.ChartAreas.Clear();
            chartTrends.Legends.Clear();
            chartTrends.Titles.Clear();
            chartTrends.AntiAliasing = AntiAliasingStyles.All;
            chartTrends.BackColor = Color.White;

            var area = new ChartArea("Academic terms");
            area.BackColor = Color.White;
            area.Position = new ElementPosition(8, 16, 88, 67);
            area.InnerPlotPosition = new ElementPosition(9, 3, 88, 83);
            area.AxisX.Minimum = 0.5;
            area.AxisX.Maximum = 4.5;
            area.AxisX.Interval = 1;
            area.AxisX.Title = "ACADEMIC TERM";
            area.AxisX.TitleFont = new Font("Segoe UI", 9F, FontStyle.Bold);
            area.AxisX.LabelStyle.Font = new Font("Segoe UI", 10F);
            area.AxisX.LineColor = Color.FromArgb(157, 173, 197);
            area.AxisX.MajorTickMark.Enabled = false;
            area.AxisX.MajorGrid.Enabled = false;
            for (int term = 1; term <= 4; term++)
                area.AxisX.CustomLabels.Add(term - 0.45, term + 0.45, "Term " + term);

            area.AxisY.Minimum = 0;
            area.AxisY.Maximum = 100;
            area.AxisY.Interval = 20;
            area.AxisY.Title = "PERCENTAGE (%)";
            area.AxisY.TitleFont = new Font("Segoe UI", 9F, FontStyle.Bold);
            area.AxisY.LabelStyle.Font = new Font("Segoe UI", 9F);
            area.AxisY.LabelStyle.Format = "0'%'"; 
            area.AxisY.LineColor = Color.FromArgb(157, 173, 197);
            area.AxisY.MajorTickMark.Enabled = false;
            area.AxisY.MajorGrid.LineColor = Color.FromArgb(230, 235, 243);
            area.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Solid;
            chartTrends.ChartAreas.Add(area);

            bool hasSynthetic = rows.Any(r => Convert.ToString(r["Data origin"]) ==
                "SYNTHETIC TEST DATA");
            string title = individual ? selected.Label :
                "All students - four-term overview";
            if (hasSynthetic) title = "DEMO DATA | " + title;
            chartTrends.Titles.Add(new Title(
                title, Docking.Top,
                new Font("Segoe UI", 13F, FontStyle.Bold),
                Color.FromArgb(31, 56, 100)));
            var legend = new Legend("Indicators")
            {
                Docking = Docking.Bottom,
                Alignment = StringAlignment.Center,
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.White
            };
            chartTrends.Legends.Add(legend);

            var gpaSeries = new Series(individual ? "GPA" : "Average GPA")
            {
                ChartType = SeriesChartType.Line,
                ChartArea = "Academic terms",
                Legend = "Indicators",
                Color = Color.FromArgb(35, 93, 177),
                BorderWidth = 4,
                MarkerStyle = MarkerStyle.Circle,
                MarkerSize = 9,
                MarkerColor = Color.White,
                MarkerBorderColor = Color.FromArgb(35, 93, 177),
                MarkerBorderWidth = 3,
                IsVisibleInLegend = true
            };
            var attendanceSeries = new Series(individual ? "Tutoring attendance" : "Recorded attendance")
            {
                ChartType = SeriesChartType.Line,
                ChartArea = "Academic terms",
                Legend = "Indicators",
                Color = Color.FromArgb(17, 142, 133),
                BorderWidth = 4,
                MarkerStyle = MarkerStyle.Diamond,
                MarkerSize = 10,
                MarkerColor = Color.White,
                MarkerBorderColor = Color.FromArgb(17, 142, 133),
                MarkerBorderWidth = 3,
                BorderDashStyle = ChartDashStyle.Dash,
                IsVisibleInLegend = true
            };

            int attendanceTerms = 0, totalAttended = 0, totalRecorded = 0;
            var gpas = new List<KeyValuePair<int, double>>();
            for (int term = 1; term <= 4; term++)
            {
                int capturedTerm = term;
                var termRows = rows.Where(r =>
                    Convert.ToString(r["Term"]) == "Term " + capturedTerm).ToList();

                var gpa = gpaSeries.Points.AddXY(term, termRows.Count == 0 ?
                    0 : termRows.Average(r => Convert.ToDouble(r["GPA (%)"])));
                if (termRows.Count == 0)
                    gpaSeries.Points[gpa].IsEmpty = true;
                else
                {
                    double value = termRows.Average(r => Convert.ToDouble(r["GPA (%)"]));
                    gpaSeries.Points[gpa].ToolTip =
                        "Term " + term + " - GPA: " + value.ToString("0.0") + "%";
                    gpaSeries.Points[gpa].Label = value.ToString("0.0") + "%";
                    gpaSeries.Points[gpa].LabelForeColor = Color.FromArgb(35, 93, 177);
                    gpas.Add(new KeyValuePair<int, double>(term, value));
                }

                int recorded = termRows.Sum(r => Convert.ToInt32(r["Recorded sessions"]));
                int attended = termRows.Sum(r => Convert.ToInt32(r["Attended"]));
                totalRecorded += recorded;
                totalAttended += attended;
                int point = attendanceSeries.Points.AddXY(term,
                    recorded == 0 ? 0 : Math.Round(100.0 * attended / recorded, 1));
                if (recorded == 0)
                    attendanceSeries.Points[point].IsEmpty = true;
                else
                {
                    attendanceTerms++;
                    attendanceSeries.Points[point].ToolTip =
                        "Term " + term + " - Attended " + attended +
                        " of " + recorded + " completed session(s)";
                    attendanceSeries.Points[point].Label =
                        (100.0 * attended / recorded).ToString("0.0") + "%";
                    attendanceSeries.Points[point].LabelForeColor = Color.FromArgb(17, 142, 133);
                }
            }

            gpaSeries.SmartLabelStyle.Enabled = true;
            attendanceSeries.SmartLabelStyle.Enabled = true;
            chartTrends.Series.Add(gpaSeries);
            chartTrends.Series.Add(attendanceSeries);

            string recording = totalRecorded == 0 ?
                "No attendance has been recorded. The attendance line is hidden, not treated as 0%." :
                totalAttended + " of " + totalRecorded + " completed sessions attended (" +
                (100.0 * totalAttended / totalRecorded).ToString("0.0") +
                "%). Attendance available for " + attendanceTerms + " of 4 terms.";

            if (individual)
            {
                string progress = gpas.Count < 2 ? "GPA trend needs at least two term results." :
                    "GPA change: " + (gpas.Last().Value - gpas.First().Value).ToString("+0.0;-0.0;0.0") +
                    " percentage points (first to latest available term).";
                string category = ClassifyTrend(rows);
                lblTrendSummary.Text = progress + "  " + recording +
                    Environment.NewLine + "Indicative: " + category +
                    " (review flags use ±3 GPA points and a 70% attendance guide). " +
                    "This does not prove tutoring caused a GPA change.";
            }
            else
            {
                var flags = rows.GroupBy(r => Convert.ToString(r["Student number"]))
                    .Select(group => ClassifyTrend(group.ToList())).ToList();
                lblTrendSummary.Text = "Cohort: average available GPA per term.  " +
                    recording + Environment.NewLine +
                    "Indicative student flags - Progressing: " + flags.Count(f => f == "Progressing") +
                    ", Intervention: " + flags.Count(f => f == "Intervention") +
                    ", Tutor review: " + flags.Count(f => f == "Tutor review") +
                    ", insufficient data: " + flags.Count(f => f == "Insufficient data") +
                    ". Select a student for details.";
            }

            if (hasSynthetic)
                lblTrendSummary.Text = "SYNTHETIC TEST GPA AND ATTENDANCE - NOT REAL STUDENT RESULTS." +
                    Environment.NewLine + lblTrendSummary.Text;
            lblTrendSummary.Visible = true;
            chartTrends.Visible = true;
            chartTrends.BringToFront();
            grid.Visible = false;
            btnToggleView.Text = "Show table";
            lblStatus.Text = hasSynthetic ?
                "DEMO ONLY - Synthetic GPA and attendance. Not real student results." :
                (individual ? "Student trend from recorded academic and session data." :
                "Cohort trends - no missing attendance values have been estimated.");
        }

        private void ToggleView()
        {
            if (result == null || result.Rows.Count == 0 ||
                cmbReport.SelectedIndex != 2) return;
            bool showChart = !chartTrends.Visible;
            chartTrends.Visible = showChart;
            grid.Visible = !showChart;
            lblTrendSummary.Visible = showChart;
            if (showChart) chartTrends.BringToFront();
            else grid.BringToFront();
            btnToggleView.Text = showChart ? "Show table" : "Show chart";
            lblStatus.Text = showChart
                ? "Four-term GPA and recorded tutoring attendance trends."
                : "Student GPA and attendance by academic term.";
        }

        private void PreviewReport()
        {
            if (result == null || result.Rows.Count == 0)
            {
                MessageBox.Show("Generate a report with results before previewing.",
                    "No report data", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                printRow = 0;
                printer.DefaultPageSettings.Landscape = result.Columns.Count > 5;
                using (var preview = new PrintPreviewDialog())
                {
                    preview.Document = printer;
                    preview.Text = "EduConnect | " + reportTitle;
                    preview.WindowState = FormWindowState.Maximized;
                    preview.UseAntiAlias = true;
                    preview.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Print preview could not open.\n\n" + ex.Message,
                    "Preview error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrintReport()
        {
            if (result == null || result.Rows.Count == 0)
            {
                MessageBox.Show("Generate a report with results before printing."); return;
            }
            try
            {
                printRow = 0;
                printer.DefaultPageSettings.Landscape = result.Columns.Count > 5;
                using (var dialog = new PrintDialog { Document = printer, UseEXDialog = true })
                {
                    if (dialog.ShowDialog(this) == DialogResult.OK) printer.Print();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Printing failed.\n\n" + ex.Message,
                    "Print error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PreparePrintPages()
        {
            printRow = 0;
            printTrendCoverPending = cmbReport.SelectedIndex == 2;
            printRows = new List<DataRow>();
            if (result == null) return;
            string studentNumber = "";
            var selected = cmbStudentChoice.SelectedItem as StudentTrendChoice;
            if (printTrendCoverPending && selected != null)
                studentNumber = selected.Number ?? "";

            foreach (DataRow row in result.Rows)
                if (studentNumber.Length == 0 ||
                    Convert.ToString(row["Student number"]) == studentNumber)
                    printRows.Add(row);
        }

        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            int left = e.MarginBounds.Left, width = e.MarginBounds.Width;
            int y = e.MarginBounds.Top;
            using (var titleFont = new Font("Segoe UI", 16F, FontStyle.Bold))
            {
                e.Graphics.DrawString(reportTitle, titleFont, Brushes.Black,
                    new RectangleF(left, y, width, 55));
                y += 53;
            }
            string context = cmbReport.SelectedIndex == 1 ?
                fromDate.Value.ToShortDateString() + " - " + toDate.Value.ToShortDateString() :
                cmbReport.SelectedIndex == 2 ? numYear.Value + " academic year" :
                "Report date: " + DateTime.Today.ToShortDateString();
            e.Graphics.DrawString(context, printBody, Brushes.DimGray, left, y);
            y += 34;

            if (printTrendCoverPending)
            {
                // For Report 3 print the four-term chart first regardless of whether
                // the administrator was viewing the table or graph when printing.
                using (var stream = new MemoryStream())
                {
                    chartTrends.SaveImage(stream, ChartImageFormat.Png);
                    stream.Position = 0;
                    using (var picture = Image.FromStream(stream))
                    {
                        float factor = Math.Min((float)width / picture.Width,
                            (float)(e.MarginBounds.Bottom - y - 125) / picture.Height);
                        float scaledWidth = picture.Width * Math.Max(0.1f, factor);
                        float scaledHeight = picture.Height * Math.Max(0.1f, factor);
                        e.Graphics.DrawImage(picture,
                            new RectangleF(left + (width - scaledWidth) / 2, y,
                                scaledWidth, scaledHeight));
                        y += (int)scaledHeight + 18;
                    }
                }

                using (var summaryFont = new Font("Segoe UI", 10F))
                {
                    e.Graphics.DrawString(lblTrendSummary.Text, summaryFont,
                        Brushes.Black,
                        new RectangleF(left, y, width,
                            Math.Max(45, e.MarginBounds.Bottom - y - 24)));
                }
                printTrendCoverPending = false;
                e.HasMorePages = printRows.Count > 0;
                return;
            }

            int columns = result.Columns.Count;
            int columnWidth = Math.Max(48, width / columns);
            for (int c = 0; c < columns; c++)
            {
                var rect = new RectangleF(left + c * columnWidth, y, columnWidth - 3, 34);
                e.Graphics.DrawString(result.Columns[c].ColumnName, printHead,
                    Brushes.Black, rect);
            }
            y += 39;
            e.Graphics.DrawLine(Pens.Gray, left, y, left + width, y);
            y += 9;

            while (printRow < printRows.Count && y + 44 < e.MarginBounds.Bottom)
            {
                DataRow row = printRows[printRow++];
                for (int c = 0; c < columns; c++)
                {
                    string value = row[c] == DBNull.Value ? "—" :
                        row[c] is DateTime ? ((DateTime)row[c]).ToString("dd MMM yyyy") :
                        Convert.ToString(row[c]);
                    if (value.Length > 25) value = value.Substring(0, 22) + "...";
                    var rect = new RectangleF(left + c * columnWidth, y, columnWidth - 3, 37);
                    e.Graphics.DrawString(value, printBody, Brushes.Black, rect);
                }
                y += 37;
            }
            e.HasMorePages = printRow < printRows.Count;
            if (!e.HasMorePages)
            {
                y += 15;
                e.Graphics.DrawString("Total rows: " + printRows.Count,
                    printHead, Brushes.Black, left,
                    Math.Min(y, e.MarginBounds.Bottom - 25));
                printRow = 0;
            }
        }
    }
}
