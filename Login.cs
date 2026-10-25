
using System;
using System.Configuration;
using System.Data;
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

            txtPassword.Multiline = false;
            txtPassword.UseSystemPasswordChar = false;
            txtPassword.PasswordChar = '*';

            btnLogin.Click += btnLogin_Click;
            AcceptButton = btnLogin;
            EduConnectLayout.Login(this);
            label5.Text = "Student / Admin Number";
            label2.Text = "Student and Administrator Portal";
            label4.Text = "Students use their W number; administrators use ADM number.";
        }

        public static void ClearSession()
        {
            CurrentStudentID = 0;
            CurrentStudentNo = null;
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string studentNo = txtStudentNo.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(studentNo))
            {
                MessageBox.Show(
                    "Please enter your student number.",
                    "Missing Student Number",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txtStudentNo.Focus();
                return;
            }

            if (studentNo.StartsWith("ADM", StringComparison.OrdinalIgnoreCase))
            {
                btnLogin.Enabled = false;
                try
                {
                    var settings = ConfigurationManager.ConnectionStrings[
                        "NextGen__7.Properties.Settings.Edu_ConnectConnectionString"];
                    if (settings == null) throw new InvalidOperationException("Database connection not configured.");
                    using (var db = new OleDbConnection(settings.ConnectionString))
                    using (var cmd = new OleDbCommand(
                        "SELECT [FirstName], [LastName], [IsActive] FROM [ADMINISTRATOR] WHERE [AdminNo] = ?", db))
                    {
                        cmd.Parameters.Add("@AdminNo", OleDbType.VarWChar).Value = studentNo;
                        db.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                MessageBox.Show("Administrator number not found.");
                                return;
                            }
                            if (reader.IsDBNull(2) || !Convert.ToBoolean(reader[2]))
                            {
                                MessageBox.Show("This administrator account is inactive.");
                                return;
                            }
                            string displayName = (Convert.ToString(reader[0]) + " " + Convert.ToString(reader[1])).Trim();
                            Hide();
                            try
                            {
                                using (var dashboard = new AdminDashboard(studentNo, displayName))
                                    dashboard.ShowDialog(this);
                            }
                            finally { Show(); txtStudentNo.Clear(); txtPassword.Clear(); }
                        }
                    }
                }
                catch (Exception ex) { MessageBox.Show("Admin login failed: " + ex.Message); }
                finally { btnLogin.Enabled = true; }
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show(
                    "Please enter your password.",
                    "Missing Password",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txtPassword.Focus();
                return;
            }

            btnLogin.Enabled = false;

            try
            {
                ConnectionStringSettings settings =
                    ConfigurationManager.ConnectionStrings[
                        "NextGen__7.Properties.Settings.Edu_ConnectConnectionString"];

                if (settings == null)
                {
                    MessageBox.Show(
                        "The database connection is not configured.",
                        "Configuration Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return;
                }

                using (OleDbConnection connection =
                    new OleDbConnection(settings.ConnectionString))
                {
                    connection.Open();

                    string query =
                        "SELECT [StudentID] FROM [STUDENT] " +
                        "WHERE [StudentNo] = ?";

                    using (OleDbCommand command =
                        new OleDbCommand(query, connection))
                    {
                        command.Parameters.Add(
                            "@StudentNo",
                            OleDbType.VarWChar).Value = studentNo;

                        object result = command.ExecuteScalar();

                        if (result == null || result == DBNull.Value)
                        {
                            MessageBox.Show(
                                "The student number you entered was not found. " +
                                "Please check the number and correct it.",
                                "Student Number Not Found",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);

                            txtStudentNo.Focus();
                            txtStudentNo.SelectAll();

                            return;
                        }

                        CurrentStudentID = Convert.ToInt32(result);
                        CurrentStudentNo = studentNo;
                    }
                }

                MessageBox.Show(
                    "Student verified successfully!",
                    "EduConnect",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                Hide();

                try
                {
                    using (StudentDashboard dashboard =
                        new StudentDashboard())
                    {
                        dashboard.ShowDialog();
                    }
                }
                finally
                {
                    ClearSession();

                    txtStudentNo.Clear();
                    txtPassword.Clear();

                    Show();
                    txtStudentNo.Focus();
                }
            }
            catch (OleDbException ex)
            {
                MessageBox.Show(
                    "Unable to connect to the student database.\n\n" +
                    ex.Message,
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                txtStudentNo.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An unexpected error occurred.\n\n" +
                    ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnLogin.Enabled = true;
            }
        }

        private void Login_Load(object sender, EventArgs e)
        {
        }

        private void label5_Click(object sender, EventArgs e)
        {
        }

        private void label2_Click(object sender, EventArgs e)
        {
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
        }
    }
}
