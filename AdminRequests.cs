using System;
using System.Configuration;
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

        private void AdminRequests_Load(object sender, EventArgs e) { Reload(); }
        private void btnRefresh_Click(object sender, EventArgs e) { Reload(); }
        private void btnVerify_Click(object sender, EventArgs e) { Verify(); }
        private void btnBack_Click(object sender, EventArgs e) { Close(); }

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
