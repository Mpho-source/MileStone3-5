using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace NextGen__7
{
    public partial class Reports : Form
    {
        private readonly string connectionString;
        private DataTable result;
        private string reportTitle = "";
        private int printRow;
        private readonly Font printBody = new Font("Segoe UI", 9F);
        private readonly Font printHead = new Font("Segoe UI", 9F, FontStyle.Bold);

        public Reports()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            var settings = ConfigurationManager.ConnectionStrings[
                "NextGen__7.Properties.Settings.Edu_ConnectConnectionString"];
            if (settings == null) throw new InvalidOperationException("Missing database connection.");
            connectionString = settings.ConnectionString;
            numYear.Value = Math.Max(2020, Math.Min(2100, DateTime.Today.Year));
            fromDate.Value = DateTime.Today.AddMonths(-1);
            toDate.Value = DateTime.Today;
            cmbReport.SelectedIndexChanged += (s, e) => ChangeReport();
            btnGenerate.Click += (s, e) => Generate();
            btnAcademic.Click += (s, e) => OpenAcademicTracking();
            btnToggleView.Click += (s, e) => ToggleView();
            btnPrint.Click += (s, e) => PrintReport();
            btnBack.Click += (s, e) => Close();
            cmbReport.SelectedIndex = 0;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(31, 56, 100);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(221, 235, 251);
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(31, 56, 100);
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
            btnAcademic.Enabled = trend;
            btnToggleView.Visible = trend;
            btnToggleView.Enabled = false;
            grid.DataSource = null;
            result = null;
            chartTrends.Visible = false;
            grid.Visible = true;
            lblStatus.Text = trend
                ? "GPA and attendance must be recorded before a trend can be calculated."
                : "Select Generate report to retrieve the latest data.";
        }

        private void Generate()
        {
            if (cmbReport.SelectedIndex == 1 && fromDate.Value.Date > toDate.Value.Date)
            {
                MessageBox.Show("The start date must not exceed the end date."); return;
            }
            // A missing academic-record table is a setup requirement, not a crashed report.
            if (cmbReport.SelectedIndex == 2)
            {
                try
                {
                    bool trackingReady;
                    using (var database = new OleDbConnection(connectionString))
                    {
                        database.Open();
                        trackingReady = AcademicTracking.HasTable(database, "STUDENTACADEMICRECORD") &&
                            AcademicTracking.HasColumn(database, "TUTORING_SESSION", "AttendanceStatus");
                    }
                    if (!trackingReady)
                    {
                        result = null;
                        grid.DataSource = null;
                        chartTrends.Visible = false;
                        grid.Visible = true;
                        btnToggleView.Enabled = false;
                        lblStatus.Text = "Report 3 needs academic data. Open Academic records > Set up tracking.";
                        MessageBox.Show("The database does not yet contain the information for Report 3.\n\n" +
                            "1. Click Academic records.\n" +
                            "2. Select Set up tracking and confirm.\n" +
                            "3. Record students' real GPA values for Terms 1–4.\n" +
                            "4. Record attended or absent tutoring sessions.\n" +
                            "5. Return here and generate the report.\n\n" +
                            "No values will be invented.", "Academic data required",
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
                btnToggleView.Enabled = cmbReport.SelectedIndex == 2 && result.Rows.Count > 0;
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
            int year = (int)numYear.Value;
            using (var db = new OleDbConnection(connectionString))
            {
                db.Open();
                if (!AcademicTracking.HasTable(db, "STUDENTACADEMICRECORD") ||
                    !AcademicTracking.HasColumn(db, "TUTORING_SESSION", "AttendanceStatus"))
                    throw new InvalidOperationException("Academic tracking has not been set up. " +
                        "Choose Academic records to enable it and enter actual GPA/attendance.");
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
                foreach (DataRow row in sessions.Rows)
                {
                    if (row[sessionRequestId] == DBNull.Value ||
                        row["SessionDate"] == DBNull.Value) continue;
                    DataRow request;
                    if (!requests.TryGetValue(Convert.ToInt32(row[sessionRequestId]), out request) ||
                        request["StudentID"] == DBNull.Value) continue;
                    DateTime when = Convert.ToDateTime(row["SessionDate"]);
                    if (when.Year != year) continue;
                    string status = Convert.ToString(row["AttendanceStatus"]).Trim();
                    if (!status.Equals("Attended", StringComparison.OrdinalIgnoreCase) &&
                        !status.Equals("Absent", StringComparison.OrdinalIgnoreCase)) continue;
                    string key = Convert.ToInt32(request["StudentID"]) + "-T" +
                        ((when.Month - 1) / 3 + 1);
                    if (!attendance.ContainsKey(key)) attendance[key] = new int[2];
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
                    output.Rows.Add(Convert.ToString(student["StudentNo"]),
                        (Convert.ToString(student["FirstName"]) + " " +
                        Convert.ToString(student["LastName"])).Trim(),
                        "Term " + term, gpa, stats[0], stats[1], percent);
                }
            }
            var view = output.DefaultView; view.Sort = "[Student] ASC, [Term] ASC";
            output = view.ToTable();
            ShowTrendChart(output);
            return output;
        }

        private void ShowTrendChart(DataTable source)
        {
            chartTrends.Series.Clear();
            chartTrends.ChartAreas.Clear();
            chartTrends.Legends.Clear();
            var area = new ChartArea("Four terms");
            area.AxisX.Title = "Academic term";
            area.AxisY.Title = "Percentage (%)";
            area.AxisY.Minimum = 0;
            area.AxisY.Maximum = 100;
            area.AxisY.Interval = 20;
            area.AxisX.Interval = 1;
            area.AxisX.Minimum = 0.5;
            area.AxisX.Maximum = 4.5;
            area.AxisX.MajorGrid.Enabled = false;
            chartTrends.ChartAreas.Add(area);
            chartTrends.Legends.Add(new Legend("Indicators"));
            var gpaSeries = new Series("Average GPA") {
                ChartType = SeriesChartType.Line, BorderWidth = 3, MarkerStyle = MarkerStyle.Circle,
                MarkerSize = 9, ChartArea = "Four terms" };
            var attendanceSeries = new Series("Average attendance") {
                ChartType = SeriesChartType.Line, BorderWidth = 3, MarkerStyle = MarkerStyle.Circle,
                MarkerSize = 9, ChartArea = "Four terms" };
            for (int term = 1; term <= 4; term++)
            {
                var rows = source.AsEnumerable().Where(r =>
                    Convert.ToString(r["Term"]) == "Term " + term).ToList();
                if (rows.Count == 0) continue;
                gpaSeries.Points.AddXY(term, rows.Average(r => Convert.ToDouble(r["GPA (%)"])));
                var withAttendance = rows.Where(r => r["Attendance (%)"] != DBNull.Value).ToList();
                if (withAttendance.Count > 0)
                    attendanceSeries.Points.AddXY(term,
                        withAttendance.Average(r => Convert.ToDouble(r["Attendance (%)"])));
            }
            chartTrends.Series.Add(gpaSeries);
            chartTrends.Series.Add(attendanceSeries);
            chartTrends.Visible = source.Rows.Count != 0;
            grid.Visible = !chartTrends.Visible;
            if (chartTrends.Visible)
                lblStatus.Text = "Use Show table to display each student and term.";
        }

        private void ToggleView()
        {
            if (result == null || result.Rows.Count == 0 ||
                cmbReport.SelectedIndex != 2) return;
            bool showChart = !chartTrends.Visible;
            chartTrends.Visible = showChart;
            grid.Visible = !showChart;
            if (showChart) chartTrends.BringToFront();
            else grid.BringToFront();
            btnToggleView.Text = showChart ? "Show table" : "Show chart";
            lblStatus.Text = showChart
                ? "Four-term average GPA and recorded tutoring attendance."
                : "Student GPA and attendance by academic term.";
        }

        private void OpenAcademicTracking()
        {
            using (var form = new AcademicTracking(connectionString))
                form.ShowDialog(this);
            if (cmbReport.SelectedIndex == 2) Generate();
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
            while (printRow < result.Rows.Count && y + 44 < e.MarginBounds.Bottom)
            {
                DataRow row = result.Rows[printRow++];
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
            e.HasMorePages = printRow < result.Rows.Count;
            if (!e.HasMorePages)
            {
                y += 15;
                e.Graphics.DrawString("Total rows: " + result.Rows.Count,
                    printHead, Brushes.Black, left, Math.Min(y, e.MarginBounds.Bottom - 25));
                printRow = 0;
            }
        }
    }
}
