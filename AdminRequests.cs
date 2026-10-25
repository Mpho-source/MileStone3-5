using System;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Windows.Forms;

namespace NextGen__7
{
    public sealed class AdminRequests : Form
    {
        private readonly string connectionString;
        private readonly DataGridView requests;
        private string requestIdColumn;
        public AdminRequests()
        {
            var settings = ConfigurationManager.ConnectionStrings[
                "NextGen__7.Properties.Settings.Edu_ConnectConnectionString"];
            if (settings == null) throw new InvalidOperationException("Database connection missing.");
            connectionString = settings.ConnectionString;
            Text = "EduConnect | Tutoring requests";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(900, 650);
            Size = new Size(1100, 750);
            BackColor = EduConnectTheme.Background;
            Font = new Font("Segoe UI", 10);

            var banner = new Panel { Dock = DockStyle.Top, Height = 92,
                BackColor = EduConnectTheme.Navy };
            var title = new Label { Text = "Tutoring requests", Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.White, Location = new Point(26, 13), Size = new Size(650, 42) };
            var subtitle = new Label { Text = "Review and verify requests from students",
                ForeColor = Color.Gainsboro, Location = new Point(29, 57), Size = new Size(700, 25) };
            banner.Controls.Add(title); banner.Controls.Add(subtitle);
            Controls.Add(banner);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 66,
                Padding = new Padding(20, 10, 10, 4), BackColor = Color.White };
            AddButton(buttons, "Refresh", (s, e) => Reload());
            AddButton(buttons, "Verify selected request", (s, e) => Verify());
            AddButton(buttons, "Back", (s, e) => Close());
            Controls.Add(buttons);

            requests = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, BackgroundColor = Color.White };
            Controls.Add(requests);
            requests.BringToFront();
            EduConnectTheme.StyleGrid(requests);
            Load += (s, e) => Reload();
        }

        private static void AddButton(FlowLayoutPanel panel, string label, EventHandler handler)
        {
            var button = new Button { Text = label, AutoSize = true,
                Height = 38, Margin = new Padding(4, 2, 9, 0) };
            button.Click += handler;
            EduConnectTheme.StyleButton(button);
            panel.Controls.Add(button);
        }

        private void Reload()
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
                Reload();
                MessageBox.Show("Tutoring request verified.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to verify request.\n\n" + ex.Message);
            }
        }
    }
}
