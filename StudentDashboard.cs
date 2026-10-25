
using System;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace NextGen__7
{
    public partial class StudentDashboard : Form
    {
        private readonly int studentID;
        private readonly string studentNo;

        public StudentDashboard()
        {
            InitializeComponent();
            if (System.ComponentModel.LicenseManager.UsageMode ==
                System.ComponentModel.LicenseUsageMode.Designtime) return;
            EduConnectTheme.Apply(this);

            studentID = Login.CurrentStudentID;
            studentNo = Login.CurrentStudentNo;

            if (studentID <= 0 || string.IsNullOrWhiteSpace(studentNo))
            {
                throw new InvalidOperationException(
                    "No student is currently logged in.");
            }

            pnlBook.Clicked += (s, e) =>
            {
                Hide();

                try
                {
                    using (BookingTutoringSession form =
                        new BookingTutoringSession(studentID, studentNo))
                    {
                        form.ShowDialog(this);
                    }
                }
                finally
                {
                    Show();
                }
            };

            pnlManageBooking.Clicked += (s, e) =>
            {
                Hide();

                try
                {
                    using (ManageBookings form =
                        new ManageBookings(studentID, studentNo))
                    {
                        form.ShowDialog(this);
                    }
                }
                finally
                {
                    Show();
                }
            };

            pnlLogout.Clicked += (s, e) => ConfirmLogout();

            EduConnectLayout.Dashboard(
                this,
                () => pnlBook.Activate(),
                () => pnlManageBooking.Activate(),
                () => ConfirmLogout());
        }

        private void ConfirmLogout()
        {
            DialogResult answer = MessageBox.Show(
                "Do you want to sign out of EduConnect?",
                "Confirm sign out",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (answer == DialogResult.Yes)
                Close();
        }

        private void label2_Click(object sender, EventArgs e)
        {
        }

        private void label3_Click(object sender, EventArgs e)
        {
        }

        private void StudentDashboard_Load(object sender, EventArgs e)
        {
            string displayName = "Name unavailable";

            try
            {
                ConnectionStringSettings settings =
                    ConfigurationManager.ConnectionStrings[
                        "NextGen__7.Properties.Settings.Edu_ConnectConnectionString"];

                if (settings == null)
                    throw new InvalidOperationException(
                        "The student database connection is not configured.");

                using (OleDbConnection connection =
                    new OleDbConnection(settings.ConnectionString))
                {
                    connection.Open();

                    string studentIDColumn = BookingData.GetColumn(
                        connection, null, "STUDENT", "StudentID");

                    using (OleDbCommand command = new OleDbCommand(
                        "SELECT [FirstName], [LastName] FROM [STUDENT] " +
                        "WHERE [" + studentIDColumn + "] = ?",
                        connection))
                    {
                        command.Parameters.Add(
                            "@StudentID", OleDbType.Integer).Value = studentID;

                        using (OleDbDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string first = reader.IsDBNull(0) ? "" :
                                    Convert.ToString(reader[0]).Trim();
                                string last = reader.IsDBNull(1) ? "" :
                                    Convert.ToString(reader[1]).Trim();

                                displayName = (first + " " + last).Trim();

                                if (string.IsNullOrWhiteSpace(displayName))
                                    displayName = "Name not provided";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Your student name could not be loaded. " +
                    "Your tutoring features remain available.\n\n" +
                    ex.Message,
                    "Student profile",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            EduConnectLayout.UpdateStudentProfile(
                this, displayName, studentNo);
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
        }
    }
}
