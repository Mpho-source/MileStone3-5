using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace NextGen__7
{
    public partial class Reports : Form
    {
        private readonly string connectionString;
        private readonly DateTimePicker fromDate;
        private readonly DateTimePicker toDate;
        private readonly DataGridView grid;
        private readonly PrintDocument printer;
        private DataTable result;
        private int printRow;

        public Reports()
        {
            InitializeComponent();
            var config = ConfigurationManager.ConnectionStrings[
                "NextGen__7.Properties.Settings.Edu_ConnectConnectionString"];
            if (config == null) throw new InvalidOperationException("Database connection missing.");
            connectionString = config.ConnectionString;

            Text = "EduConnect | Tutor session report";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(900, 650);
            Size = new Size(1120, 740);
            BackColor = EduConnectTheme.Background;
            Font = new Font("Segoe UI", 10);

            var header = new Panel { Dock = DockStyle.Top, Height = 106, BackColor = EduConnectTheme.Navy };
            var title = new Label { Text = "Tutor session report", Font = new Font("Segoe UI", 21, FontStyle.Bold),
                ForeColor = Color.White, Location = new Point(28, 14), Size = new Size(660, 45) };
            var subtitle = new Label { Text = "Scheduled and completed sessions per tutor for a selected period",
                ForeColor = Color.Gainsboro, Location = new Point(30, 68), Size = new Size(750, 26) };
            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            Controls.Add(header);

            var controls = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 80, BackColor = Color.White,
                Padding = new Padding(22, 16, 10, 6), FlowDirection = FlowDirection.LeftToRight };
            controls.Controls.Add(new Label { Text = "From", AutoSize = true, Padding = new Padding(0, 9, 0, 0) });
            fromDate = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 145,
                Value = DateTime.Today.AddMonths(-1) };
            controls.Controls.Add(fromDate);
            controls.Controls.Add(new Label { Text = "To", AutoSize = true,
                Padding = new Padding(14, 9, 0, 0) });
            toDate = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 145,
                Value = DateTime.Today };
            controls.Controls.Add(toDate);
            AddButton(controls, "Generate report", (s, e) => Generate());
            AddButton(controls, "Print preview", (s, e) => Preview());
            AddButton(controls, "Back", (s, e) => Close());
            Controls.Add(controls);

            grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true,
                BackgroundColor = Color.White, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            Controls.Add(grid);
            grid.BringToFront();
            EduConnectTheme.StyleGrid(grid);

            printer = new PrintDocument();
            printer.DocumentName = "EduConnect Tutor Session Report";
            printer.PrintPage += PrintPage;
        }

        private static void AddButton(FlowLayoutPanel panel, string name, EventHandler click)
        {
            var button = new Button { Text = name, Height = 40, AutoSize = true,
                Margin = new Padding(12, 0, 2, 0) };
            button.Click += click;
            EduConnectTheme.StyleButton(button);
            panel.Controls.Add(button);
        }

        private static string Column(DataTable table, string name)
        {
            foreach (DataColumn col in table.Columns)
                if (col.ColumnName.Trim('\uFEFF', ' ').Equals(name,
                    StringComparison.OrdinalIgnoreCase)) return col.ColumnName;
            throw new InvalidOperationException(name + " column not found.");
        }

        private void Generate()
        {
            if (fromDate.Value.Date > toDate.Value.Date)
            {
                MessageBox.Show("The start date must not be after the end date."); return;
            }
            try
            {
                var tutors = new DataTable();
                var sessions = new DataTable();
                using (var db = new OleDbConnection(connectionString))
                {
                    db.Open();
                    using (var adapter = new OleDbDataAdapter("SELECT * FROM [TUTOR]", db))
                        adapter.Fill(tutors);
                    using (var cmd = new OleDbCommand(
                        "SELECT [TutorID], [SessionStatus] FROM [TUTORING_SESSION] " +
                        "WHERE [SessionDate] >= ? AND [SessionDate] < ?", db))
                    {
                        cmd.Parameters.Add("@Start", OleDbType.Date).Value = fromDate.Value.Date;
                        cmd.Parameters.Add("@End", OleDbType.Date).Value = toDate.Value.Date.AddDays(1);
                        using (var adapter = new OleDbDataAdapter(cmd)) adapter.Fill(sessions);
                    }
                }

                var counts = new Dictionary<int, int[]>();
                var names = new Dictionary<int, string>();
                string idColumn = Column(tutors, "TutorID");
                foreach (DataRow row in tutors.Rows)
                {
                    int tutorId = Convert.ToInt32(row[idColumn]);
                    counts[tutorId] = new int[2];
                    names[tutorId] = (Convert.ToString(row["FirstName"]) + " " +
                        Convert.ToString(row["LastName"])).Trim();
                }
                foreach (DataRow session in sessions.Rows)
                {
                    if (session["TutorID"] == DBNull.Value) continue;
                    int id = Convert.ToInt32(session["TutorID"]);
                    if (!counts.ContainsKey(id))
                    {
                        counts[id] = new int[2]; names[id] = "Unknown tutor";
                    }
                    string status = Convert.ToString(session["SessionStatus"]).Trim();
                    if (status.Equals("Scheduled", StringComparison.OrdinalIgnoreCase))
                        counts[id][0]++;
                    else if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                        counts[id][1]++;
                }

                result = new DataTable();
                result.Columns.Add("Tutor ID", typeof(int));
                result.Columns.Add("Tutor", typeof(string));
                result.Columns.Add("Scheduled", typeof(int));
                result.Columns.Add("Completed", typeof(int));
                var ids = new List<int>(counts.Keys);
                ids.Sort();
                foreach (int id in ids)
                    result.Rows.Add(id, names[id], counts[id][0], counts[id][1]);
                grid.DataSource = result;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to generate report.\n\n" + ex.Message,
                    "Reports", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Preview()
        {
            if (result == null)
            {
                MessageBox.Show("Generate the report first."); return;
            }
            printRow = 0;
            using (var preview = new PrintPreviewDialog { Document = printer,
                WindowState = FormWindowState.Maximized })
                preview.ShowDialog(this);
        }

        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            int y = e.MarginBounds.Top;
            using (var headingFont = new Font("Segoe UI", 17, FontStyle.Bold))
            using (var bodyFont = new Font("Segoe UI", 10))
            {
                e.Graphics.DrawString("EduConnect - Tutor Session Report", headingFont, Brushes.Black,
                    e.MarginBounds.Left, y);
                y += 46;
                string period = fromDate.Value.ToShortDateString() + " - " + toDate.Value.ToShortDateString();
                e.Graphics.DrawString("Period: " + period, bodyFont, Brushes.Black, e.MarginBounds.Left, y);
                y += 40;
                e.Graphics.DrawString("Tutor ID       Tutor                           Scheduled      Completed",
                    bodyFont, Brushes.Black, e.MarginBounds.Left, y);
                y += 32;
                while (result != null && printRow < result.Rows.Count && y + 32 < e.MarginBounds.Bottom)
                {
                    DataRow row = result.Rows[printRow++];
                    string line = string.Format("{0,-12} {1,-30} {2,-13} {3}",
                        row[0], Convert.ToString(row[1]).Length > 28 ?
                        Convert.ToString(row[1]).Substring(0, 28) : Convert.ToString(row[1]),
                        row[2], row[3]);
                    e.Graphics.DrawString(line, bodyFont, Brushes.Black, e.MarginBounds.Left, y);
                    y += 28;
                }
                e.HasMorePages = result != null && printRow < result.Rows.Count;
                if (!e.HasMorePages) printRow = 0;
            }
        }
    }
}
