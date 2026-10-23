
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

            txtPassword.UseSystemPasswordChar = true;
            btnLogin.Click += btnLogin_Click;
            AcceptButton = btnLogin;
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

                string connectionString = settings.ConnectionString;

                using (OleDbConnection connection =
                    new OleDbConnection(connectionString))
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

                using (StudentDashboard dashboard =
                    new StudentDashboard())
                {
                    dashboard.ShowDialog();
                }

                Close();
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
