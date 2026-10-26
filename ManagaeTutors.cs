using System;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace NextGen__7
{
    public partial class ManagaeTutors : Form
    {
        private string connectionString;
        private string idColumn = "TutorID";
        private string specialtyColumn;
        private string activeColumn;
        private int selectedId = -1;

        public ManagaeTutors()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            var config = ConfigurationManager.ConnectionStrings[
                "NextGen__7.Properties.Settings.Edu_ConnectConnectionString"];
            if (config == null) throw new InvalidOperationException("Database connection is missing.");
            connectionString = config.ConnectionString;
            ConfigureScreen();
            Load += (sender, e) => LoadTutors();
        }

        private void ConfigureScreen()
        {
            gridTutors.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridTutors.MultiSelect = false;
            gridTutors.ReadOnly = true;
            gridTutors.SelectionChanged += (sender, e) => ShowSelected();
            btnNew.Click += (sender, e) => NewTutor();
            btnSave.Click += (sender, e) => SaveTutor();
            btnCancel.Click += (sender, e) => ClearEditor();
            btnDelete.Click += (sender, e) => DeactivateTutor();
            btnAddTutor.Click += (sender, e) => NewTutor();
            btnUpdateTutor.Click += (sender, e) => EditSelected();
            btnBack.Click += (sender, e) => Close();
            txtEmail.MaxLength = 255;
            txtFirstName.MaxLength = 255;
            txtLastName.MaxLength = 255;
            txtSubjectSpecialty.MaxLength = 255;
        }

        private static string Column(DataTable table, string logicalName)
        {
            foreach (DataColumn col in table.Columns)
                if (col.ColumnName.Trim('\uFEFF', ' ').Equals(logicalName,
                    StringComparison.OrdinalIgnoreCase)) return col.ColumnName;
            return null;
        }

        private void LoadTutors()
        {
            try
            {
                var table = new DataTable();
                using (var db = new OleDbConnection(connectionString))
                using (var adapter = new OleDbDataAdapter("SELECT * FROM [TUTOR]", db))
                    adapter.Fill(table);

                idColumn = Column(table, "TutorID");
                if (idColumn == null) throw new InvalidOperationException("TutorID column missing.");
                specialtyColumn = Column(table, "SubjectSpecialty");
                activeColumn = Column(table, "IsActive");
                txtSubjectSpecialty.Visible = specialtyColumn != null;
                label4.Visible = specialtyColumn != null;
                chkIsActive.Visible = activeColumn != null;
                btnDelete.Enabled = activeColumn != null;
                btnDelete.Text = "Deactivate";
                gridTutors.DataSource = table;
                if (gridTutors.Columns.Contains(idColumn))
                    gridTutors.Columns[idColumn].HeaderText = "Tutor ID";
                EduConnectTheme.StyleGrid(gridTutors);
                gridTutors.ClearSelection();
                ClearEditor();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to load tutors.\n\n" + ex.Message,
                    "Tutor management", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowSelected()
        {
            if (gridTutors.SelectedRows.Count != 1 || idColumn == null) return;
            DataGridViewRow row = gridTutors.SelectedRows[0];
            try
            {
                selectedId = Convert.ToInt32(row.Cells[idColumn].Value);
                txtFirstName.Text = Convert.ToString(row.Cells["FirstName"].Value);
                txtLastName.Text = Convert.ToString(row.Cells["LastName"].Value);
                txtEmail.Text = Convert.ToString(row.Cells["Email"].Value);
                if (specialtyColumn != null)
                    txtSubjectSpecialty.Text = Convert.ToString(row.Cells[specialtyColumn].Value);
                if (activeColumn != null)
                {
                    var value = row.Cells[activeColumn].Value;
                    chkIsActive.Checked = value != null && value != DBNull.Value &&
                        Convert.ToBoolean(value);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to select tutor.\n\n" + ex.Message);
            }
        }

        private void NewTutor()
        {
            gridTutors.ClearSelection();
            ClearEditor();
            txtFirstName.Focus();
        }

        private void EditSelected()
        {
            if (gridTutors.SelectedRows.Count == 0)
            {
                MessageBox.Show("Select a tutor from the table first.");
                return;
            }
            ShowSelected();
            txtFirstName.Focus();
        }

        private void ClearEditor()
        {
            selectedId = -1;
            txtFirstName.Clear();
            txtLastName.Clear();
            txtEmail.Clear();
            txtSubjectSpecialty.Clear();
            chkIsActive.Checked = true;
        }

        private void SaveTutor()
        {
            string first = txtFirstName.Text.Trim();
            string last = txtLastName.Text.Trim();
            string email = txtEmail.Text.Trim();
            if (first.Length == 0 || last.Length == 0 || email.Length == 0 || !email.Contains("@"))
            {
                MessageBox.Show("Enter the tutor's first name, last name and a valid email address.");
                return;
            }
            try
            {
                using (var db = new OleDbConnection(connectionString))
                {
                    db.Open();
                    string sql;
                    bool adding = selectedId < 0;
                    if (adding)
                    {
                        sql = "INSERT INTO [TUTOR] ([" + idColumn + "], [FirstName], [LastName], [Email]";
                        if (specialtyColumn != null) sql += ", [" + specialtyColumn + "]";
                        if (activeColumn != null) sql += ", [" + activeColumn + "]";
                        sql += ") VALUES (?, ?, ?, ?";
                        if (specialtyColumn != null) sql += ", ?";
                        if (activeColumn != null) sql += ", ?";
                        sql += ")";
                    }
                    else
                    {
                        sql = "UPDATE [TUTOR] SET [FirstName]=?, [LastName]=?, [Email]=?";
                        if (specialtyColumn != null) sql += ", [" + specialtyColumn + "]=?";
                        if (activeColumn != null) sql += ", [" + activeColumn + "]=?";
                        sql += " WHERE [" + idColumn + "]=?";
                    }
                    using (var cmd = new OleDbCommand(sql, db))
                    {
                        if (adding)
                        {
                            using (var idcmd = new OleDbCommand("SELECT MAX([" + idColumn + "]) FROM [TUTOR]", db))
                            {
                                object max = idcmd.ExecuteScalar();
                                cmd.Parameters.Add("@TutorID", OleDbType.Integer).Value =
                                    max == null || max == DBNull.Value ? 1 : checked(Convert.ToInt32(max) + 1);
                            }
                        }
                        cmd.Parameters.Add("@FirstName", OleDbType.VarWChar).Value = first;
                        cmd.Parameters.Add("@LastName", OleDbType.VarWChar).Value = last;
                        cmd.Parameters.Add("@Email", OleDbType.VarWChar).Value = email;
                        if (specialtyColumn != null)
                            cmd.Parameters.Add("@Specialty", OleDbType.VarWChar).Value =
                                txtSubjectSpecialty.Text.Trim();
                        if (activeColumn != null)
                            cmd.Parameters.Add("@Active", OleDbType.Boolean).Value = chkIsActive.Checked;
                        if (!adding)
                            cmd.Parameters.Add("@TutorID", OleDbType.Integer).Value = selectedId;
                        if (cmd.ExecuteNonQuery() != 1)
                            throw new InvalidOperationException("No tutor record was saved.");
                    }
                }
                LoadTutors();
                MessageBox.Show("Tutor saved successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not save tutor.\n\n" + ex.Message,
                    "Tutor management", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeactivateTutor()
        {
            if (selectedId < 0) { MessageBox.Show("Select a tutor first."); return; }
            if (activeColumn == null)
            {
                MessageBox.Show("This database has no IsActive column. Deactivation is unavailable.");
                return;
            }
            if (MessageBox.Show("Deactivate this tutor?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try
            {
                using (var db = new OleDbConnection(connectionString))
                using (var cmd = new OleDbCommand("UPDATE [TUTOR] SET [" + activeColumn + "]=? WHERE [" +
                    idColumn + "]=?", db))
                {
                    cmd.Parameters.Add("@Active", OleDbType.Boolean).Value = false;
                    cmd.Parameters.Add("@TutorID", OleDbType.Integer).Value = selectedId;
                    db.Open();
                    cmd.ExecuteNonQuery();
                }
                LoadTutors();
            }
            catch (Exception ex) { MessageBox.Show("Deactivation failed.\n\n" + ex.Message); }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
    }
}
