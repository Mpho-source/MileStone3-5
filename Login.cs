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

        public Login()
        {
            InitializeComponent();
            EduConnectTheme.Apply(this);
            btnLogin.Click += btnLogin_Click;
            AcceptButton = btnLogin;
            EduConnectLayout.Login(this);
        }

        public static void ClearSession()
        {
            CurrentStudentID = 0;
            CurrentStudentNo = null;
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string accountNo = txtStudentNo.Text.Trim();
            if (string.IsNullOrWhiteSpace(accountNo))
            {
                MessageBox.Show("Please enter your account number.", "Account number required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtStudentNo.Focus();
                return;
            }

            btnLogin.Enabled = false;
            try
            {
                var settings = ConfigurationManager.ConnectionStrings[
                    "NextGen__7.Properties.Settings.Edu_ConnectConnectionString"];
                if (settings == null)
                    throw new InvalidOperationException("Database connection is not configured.");

                using (var connection = new OleDbConnection(settings.ConnectionString))
                {
                    connection.Open();
                    using (var admin = new OleDbCommand(
                        "SELECT [FirstName], [LastName], [IsActive] FROM [ADMIN] WHERE [AdminNo] = ?", connection))
                    {
                        admin.Parameters.Add("@AdminNo", OleDbType.VarWChar).Value = accountNo;
                        using (var reader = admin.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                if (reader.IsDBNull(2) || !Convert.ToBoolean(reader[2]))
                                {
                                    MessageBox.Show("This account is inactive.", "Sign in unavailable");
                                    return;
                                }
                                string name = (Convert.ToString(reader[0]) + " " + Convert.ToString(reader[1])).Trim();
                                Hide();
                                try
                                {
                                    using (var dashboard = new AdminDashboard(accountNo, name))
                                        dashboard.ShowDialog(this);
                                }
                                finally { ResetLogin(); }
                                return;
                            }
                        }
                    }

                    using (var student = new OleDbCommand(
                        "SELECT [StudentID] FROM [STUDENT] WHERE [StudentNo] = ?", connection))
                    {
                        student.Parameters.Add("@StudentNo", OleDbType.VarWChar).Value = accountNo;
                        object value = student.ExecuteScalar();
                        if (value == null || value == DBNull.Value)
                        {
                            MessageBox.Show("Account not found. Check the account number and try again.",
                                "Sign in unsuccessful", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        CurrentStudentID = Convert.ToInt32(value);
                        CurrentStudentNo = accountNo;
                    }
                }

                Hide();
                try
                {
                    using (var dashboard = new StudentDashboard())
                        dashboard.ShowDialog(this);
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

        private void Login_Load(object sender, EventArgs e) { }
        private void label5_Click(object sender, EventArgs e) { }
        private void label2_Click(object sender, EventArgs e) { }
        private void textBox1_TextChanged(object sender, EventArgs e) { }
    }
}
