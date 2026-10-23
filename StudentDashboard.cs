
using System;
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
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
        }
    }
}
