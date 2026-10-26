using System;
using System.Configuration;
using System.Data.OleDb;
using System.Windows.Forms;

namespace NextGen__7
{
    public partial class Login : Form
    {
        public static int CurrentStudentID { get; private set; }
        public static string CurrentStudentNo { get; private set; }
        internal ComboBox RoleSelector { get; set; }

        public Login()
        {
            InitializeComponent();
            if (System.ComponentModel.LicenseManager.UsageMode ==
                System.ComponentModel.LicenseUsageMode.Designtime) return;
            RoleSelector = cmbSignInRole;
            txtPassword.UseSystemPasswordChar = true;
            btnLogin.Click += btnLogin_Click;
            AcceptButton = btnLogin;
            cmbSignInRole.SelectedIndexChanged += cmbSignInRole_SelectedIndexChanged;
            cmbSignInRole.SelectedIndex = 0;
        }

        public static void ClearSession()
        {
            CurrentStudentID = 0;
            CurrentStudentNo = null;
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string number = txtStudentNo.Text.Trim();
            string password = txtPassword.Text;
            if (RoleSelector == null || RoleSelector.SelectedIndex < 0)
            {
                MessageBox.Show("Select Student or Staff."); return;
            }
            if (string.IsNullOrWhiteSpace(number))
            {
                MessageBox.Show("Enter your " + (RoleSelector.SelectedIndex == 1 ? "staff" : "student") + " number.");
                txtStudentNo.Focus(); return;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Enter your password.");
                txtPassword.Focus(); return;
            }
            // This classroom prototype has no password verifier in the database.
            // Requiring a password input does not authenticate the password.
            btnLogin.Enabled = false;
            try
            {
                var settings = ConfigurationManager.ConnectionStrings[
                    "NextGen__7.Properties.Settings.Edu_ConnectConnectionString"];
                if (settings == null)
                    throw new InvalidOperationException("Database connection not configured.");

                bool staff = RoleSelector.SelectedIndex == 1;
                string name = "";
                int studentId = 0;
                using (var db = new OleDbConnection(settings.ConnectionString))
                {
                    db.Open();
                    if (staff)
                    {
                        using (var cmd = new OleDbCommand(
                            "SELECT [FirstName], [LastName], [IsActive] FROM [ADMIN] WHERE [AdminNo] = ?", db))
                        {
                            cmd.Parameters.Add("@AdminNo", OleDbType.VarWChar).Value = number;
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (!reader.Read())
                                {
                                    MessageBox.Show("Staff record not found.");
                                    return;
                                }
                                if (reader.IsDBNull(2) || !Convert.ToBoolean(reader[2]))
                                {
                                    MessageBox.Show("This staff account is inactive.");
                                    return;
                                }
                                name = (Convert.ToString(reader[0]) + " " + Convert.ToString(reader[1])).Trim();
                            }
                        }
                    }
                    else
                    {
                        using (var cmd = new OleDbCommand(
                            "SELECT [StudentID] FROM [STUDENT] WHERE [StudentNo] = ?", db))
                        {
                            cmd.Parameters.Add("@StudentNo", OleDbType.VarWChar).Value = number;
                            object value = cmd.ExecuteScalar();
                            if (value == null || value == DBNull.Value)
                            {
                                MessageBox.Show("Student record not found.");
                                return;
                            }
                            studentId = Convert.ToInt32(value);
                        }
                    }
                }

                Hide();
                try
                {
                    if (staff)
                    {
                        using (var dashboard = new AdminDashboard(number, name))
                            dashboard.ShowDialog(this);
                    }
                    else
                    {
                        CurrentStudentID = studentId;
                        CurrentStudentNo = number;
                        using (var dashboard = new StudentDashboard())
                            dashboard.ShowDialog(this);
                    }
                }
                finally { ResetLogin(); }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to sign in.\n\n" + ex.Message, "EduConnect",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { btnLogin.Enabled = true; }
        }

        private void ResetLogin()
        {
            ClearSession();
            txtStudentNo.Clear();
            txtPassword.Clear();
            Show();
            txtStudentNo.Focus();
        }

        private void cmbSignInRole_SelectedIndexChanged(object sender, EventArgs e)
        {
            label5.Text = cmbSignInRole.SelectedIndex == 1 ? "Staff number" : "Student number";
            txtStudentNo.Clear();
            txtPassword.Clear();
            txtStudentNo.Focus();
        }

        private void Login_Load(object sender, EventArgs e) { }
        private void label5_Click(object sender, EventArgs e) { }
        private void label2_Click(object sender, EventArgs e) { }
        private void textBox1_TextChanged(object sender, EventArgs e) { }
    }
}
