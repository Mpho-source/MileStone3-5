
using System;
using System.Drawing;
using System.Windows.Forms;

namespace NextGen__7
{
    public partial class BookingTutoringSession : Form
    {
        private readonly int studentID;
        private readonly string studentNo;

        public BookingTutoringSession()
            : this(Login.CurrentStudentID, Login.CurrentStudentNo)
        {
        }

        public BookingTutoringSession(int studentID, string studentNo)
        {
            InitializeComponent();

            if (studentID <= 0 || string.IsNullOrWhiteSpace(studentNo))
            {
                throw new InvalidOperationException(
                    "No valid student session was provided.");
            }

            this.studentID = studentID;
            this.studentNo = studentNo;

            cmbCourse.SelectedIndexChanged += cmbCourse_SelectedIndexChanged;

            toolTip1.SetToolTip(
                cmbCourse,
                "Select the course you need help with.");

            toolTip1.SetToolTip(
                dtpSessionDate,
                "Choose a date for your tutoring session.");

            toolTip1.SetToolTip(
                cmbSessionTime,
                "Select your preferred time slot.");
        }

        private void BookingTutoringSession_Load(object sender, EventArgs e)
        {
        }

        private void cmbCourse_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbCourse.SelectedIndex >= 0)
            {
                lblSelectedCourse.Text =
                    "Selected course: " + cmbCourse.SelectedItem.ToString();

                lblSelectedCourse.ForeColor = Color.Green;
                lblSelectedCourse.BackColor = Color.LightGreen;
            }
            else
            {
                lblSelectedCourse.Text = "No course selected";
                lblSelectedCourse.ForeColor = Color.Black;
                lblSelectedCourse.BackColor = SystemColors.Control;
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
        }

        private void label2_Click(object sender, EventArgs e)
        {
        }

        private void label3_Click(object sender, EventArgs e)
        {
        }

        private void label4_Click(object sender, EventArgs e)
        {
        }

        private void label6_Click(object sender, EventArgs e)
        {
        }

        private void dtpSessionDate_ValueChanged(object sender, EventArgs e)
        {
        }

        private void button1_Click(object sender, EventArgs e)
        {
            cmbCourse.SelectedIndex = -1;
            cmbSessionTime.SelectedIndex = -1;

            dtpSessionDate.Value = DateTime.Today;
            dtpSessionDate.Checked = false;

            lblSelectedCourse.Text = "No course selected";
            lblSelectedCourse.ForeColor = Color.Black;
            lblSelectedCourse.BackColor = SystemColors.Control;
        }

        private void btnBookSession_Click(object sender, EventArgs e)
        {
            if (cmbCourse.SelectedIndex == -1)
            {
                MessageBox.Show(
                    "Please select a course.",
                    "Missing Course",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                cmbCourse.Focus();
                return;
            }

            if (!dtpSessionDate.Checked)
            {
                MessageBox.Show(
                    "Please select a session date.",
                    "Missing Date",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                dtpSessionDate.Focus();
                return;
            }

            if (dtpSessionDate.Value.Date < DateTime.Today)
            {
                MessageBox.Show(
                    "The session date cannot be in the past.",
                    "Invalid Date",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                dtpSessionDate.Focus();
                return;
            }

            if (cmbSessionTime.SelectedIndex == -1)
            {
                MessageBox.Show(
                    "Please select a preferred time.",
                    "Missing Time",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                cmbSessionTime.Focus();
                return;
            }

            if (studentID != Login.CurrentStudentID ||
                studentNo != Login.CurrentStudentNo)
            {
                MessageBox.Show(
                    "Your student session is no longer valid. Please log in again.",
                    "Session Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                Close();
                return;
            }

            MessageBox.Show(
                "Student Number: " + studentNo +
                "\nCourse: " + cmbCourse.SelectedItem +
                "\nDate: " + dtpSessionDate.Value.ToString("dd MMMM yyyy") +
                "\nTime: " + cmbSessionTime.SelectedItem +
                "\n\nYour booking details are valid." +
                "\nDatabase saving still needs to be implemented.",
                "Booking Details",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
