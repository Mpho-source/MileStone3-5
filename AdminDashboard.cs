using System;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace NextGen__7
{
    public sealed class AdminDashboard : Form
    {
        private readonly string adminNo;
        private readonly string connectionString;
        private readonly Label heading;
        private readonly DataGridView grid;
        private readonly ComboBox section;
        private readonly ComboBox status;
        private readonly DateTimePicker fromDate;
        private readonly DateTimePicker toDate;
        private DataTable current;
        private string selectedTable;

        public AdminDashboard(string adminNo, string displayName)
        {
            this.adminNo = adminNo;
            var cfg = ConfigurationManager.ConnectionStrings["NextGen__7.Properties.Settings.Edu_ConnectConnectionString"];
            if (cfg == null) throw new InvalidOperationException("Database configuration missing.");
            connectionString = cfg.ConnectionString;
            Text = "EduConnect | Administration";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1000, 660);
            Size = new Size(1180, 740);
            BackColor = Color.FromArgb(242, 246, 252);
            Font = new Font("Segoe UI", 10);
            var header = new Panel { Dock = DockStyle.Top, Height = 112, BackColor = Color.FromArgb(24, 41, 73) };
            heading = new Label { Text = "Administration  |  " + displayName + "  (" + adminNo + ")", ForeColor = Color.White,
                Font = new Font("Segoe UI", 18, FontStyle.Bold), Dock = DockStyle.Top, Height = 57, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(24, 8, 0, 0) };
            var subheading = new Label { Text = "Manage tutoring operations, verify requests and export reports", ForeColor = Color.Gainsboro,
                Dock = DockStyle.Fill, Padding = new Padding(26, 0, 0, 0) };
            header.Controls.Add(subheading);
            header.Controls.Add(heading);
            Controls.Add(header);

            var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 110, Padding = new Padding(16, 12, 10, 4),
                FlowDirection = FlowDirection.LeftToRight, WrapContents = true, BackColor = Color.White };
            section = new ComboBox { Width = 190, DropDownStyle = ComboBoxStyle.DropDownList };
            section.Items.AddRange(new object[] { "Tutors", "Tutor courses", "Tutor availability", "Requests", "Sessions", "Students", "Courses" });
            section.SelectedIndexChanged += (s, e) => LoadSection();
            bar.Controls.Add(section);
            var refresh = MakeButton("Refresh", (s, e) => LoadSection());
            bar.Controls.Add(refresh);
            status = new ComboBox { Width = 125, DropDownStyle = ComboBoxStyle.DropDownList };
            status.Items.AddRange(new object[] { "Submitted", "Verified", "Scheduled", "Completed", "Cancelled" });
            status.SelectedIndex = 1;
            bar.Controls.Add(status);
            bar.Controls.Add(MakeButton("Set request status", (s, e) => ChangeRequestStatus()));
            fromDate = new DateTimePicker { Width = 132, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-1) };
            toDate = new DateTimePicker { Width = 132, Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            bar.Controls.Add(fromDate);
            bar.Controls.Add(toDate);
            bar.Controls.Add(MakeButton("Tutor session report", (s, e) => SessionReport()));
            bar.Controls.Add(MakeButton("Export CSV", (s, e) => ExportCsv()));
            bar.Controls.Add(MakeButton("Logout", (s, e) => Close()));
            Controls.Add(bar);

            grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                AllowUserToDeleteRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None, RowHeadersVisible = false };
            Controls.Add(grid);
            grid.BringToFront();
            section.SelectedIndex = 0;
        }

        private Button MakeButton(string text, EventHandler handler)
        {
            var b = new Button { Text = text, AutoSize = true, Height = 35,
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(39, 88, 156), ForeColor = Color.White, Margin = new Padding(5) };
            b.FlatAppearance.BorderSize = 0;
            b.Click += handler;
            return b;
        }

        private void LoadSection()
        {
            if (section.SelectedItem == null) return;
            var tables = new [] { "TUTOR", "TUTOR_COURSE", "TUTOR_AVAILABILITY", "TUTORING_REQUEST",
                "TUTORING_SESSION", "STUDENT", "COURSE" };
            selectedTable = tables[section.SelectedIndex];
            try
            {
                using (var db = new OleDbConnection(connectionString))
                using (var adapter = new OleDbDataAdapter("SELECT * FROM [" + selectedTable + "]", db))
                {
                    current = new DataTable();
                    adapter.Fill(current);
                    grid.DataSource = current;
                }
            }
            catch (Exception ex) { MessageBox.Show("Unable to load " + selectedTable + ": " + ex.Message); }
        }

        private void ChangeRequestStatus()
        {
            if (selectedTable != "TUTORING_REQUEST")
            {
                MessageBox.Show("Select Requests first."); return;
            }
            if (grid.SelectedRows.Count != 1 || current == null || !current.Columns.Contains("RequestID"))
            {
                MessageBox.Show("Select one request with a RequestID."); return;
            }
            object requestId = grid.SelectedRows[0].Cells["RequestID"].Value;
            if (MessageBox.Show("Change request " + requestId + " to " + status.Text + "?",
                "Confirm request update", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                using (var db = new OleDbConnection(connectionString))
                using (var cmd = new OleDbCommand("UPDATE [TUTORING_REQUEST] SET [Status] = ? WHERE [RequestID] = ?", db))
                {
                    cmd.Parameters.Add("@Status", OleDbType.VarWChar).Value = status.Text;
                    cmd.Parameters.Add("@RequestID", OleDbType.Integer).Value = Convert.ToInt32(requestId);
                    db.Open();
                    if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Request not updated.");
                }
                LoadSection();
            }
            catch (Exception ex) { MessageBox.Show("Update failed: " + ex.Message); }
        }

        private void SessionReport()
        {
            if (fromDate.Value.Date > toDate.Value.Date) { MessageBox.Show("Start date must not exceed end date."); return; }
            try
            {
                var report = new DataTable();
                using (var db = new OleDbConnection(connectionString))
                using (var command = new OleDbCommand(
                    "SELECT [TutorID], [SessionStatus], Count(*) AS [SessionCount] FROM [TUTORING_SESSION] " +
                    "WHERE [SessionDate] >= ? AND [SessionDate] < ? " +
                    "GROUP BY [TutorID], [SessionStatus] ORDER BY [TutorID], [SessionStatus]", db))
                {
                    command.Parameters.Add("@Start", OleDbType.Date).Value = fromDate.Value.Date;
                    command.Parameters.Add("@End", OleDbType.Date).Value = toDate.Value.Date.AddDays(1);
                    using (var adapter = new OleDbDataAdapter(command)) adapter.Fill(report);
                }
                current = report;
                selectedTable = "REPORT";
                grid.DataSource = report;
                MessageBox.Show("Session totals grouped by tutor and status. Use Export CSV to save.");
            }
            catch (Exception ex) { MessageBox.Show("Report failed: " + ex.Message); }
        }

        private void ExportCsv()
        {
            if (current == null || current.Columns.Count == 0) { MessageBox.Show("Load a table or report first."); return; }
            using (var dialog = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = "EduConnect_" + selectedTable + ".csv" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    using (var writer = new StreamWriter(dialog.FileName, false, Encoding.UTF8))
                    {
                        for (int c = 0; c < current.Columns.Count; c++)
                        { if (c > 0) writer.Write(","); writer.Write(Csv(current.Columns[c].ColumnName)); }
                        writer.WriteLine();
                        foreach (DataRow row in current.Rows)
                        {
                            for (int c = 0; c < current.Columns.Count; c++)
                            { if (c > 0) writer.Write(","); writer.Write(Csv(Convert.ToString(row[c]))); }
                            writer.WriteLine();
                        }
                    }
                    MessageBox.Show("Report exported successfully.");
                }
                catch (Exception ex) { MessageBox.Show("Export failed: " + ex.Message); }
            }
        }

        private static string Csv(string value)
        {
            value = value ?? "";
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
