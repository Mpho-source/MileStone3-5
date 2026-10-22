using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NextGen__7
{
    public partial class BookingTutoringSession : Form
    {



        public BookingTutoringSession()
        {
            InitializeComponent();

            cmbCourse.SelectedIndexChanged += cmbCourse_SelectedIndexChanged;
            toolTip1.SetToolTip(cmbCourse, "Select the course you need help with.");
            toolTip1.SetToolTip(dtpSessionDate, "Choose a date for your tutoring session.");
            toolTip1.SetToolTip(cmbSessionTime, "Select your preferred time slot.");
            
            //toolTip1.SetToolTip(btnBookSession, "Submit your tutoring session booking.");


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
                //lblSelectedCourse.ForeColor = Color.Black;
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

        /*private void btnBookSession_Click(object sender, EventArgs e)
        {
            if (cmbCourse.SelectedIndex == -1)
            {
                MessageBox.Show("Please select a course.");
                cmbCourse.Focus();
                return;
            }

            if (!dtpSessionDate.Checked)
            {
                MessageBox.Show("Please select a session date.");
                dtpSessionDate.Focus();
                return;
            }

            if (dtpSessionDate.Value.Date < DateTime.Today)
            {
                MessageBox.Show("The session date cannot be in the past.");
                dtpSessionDate.Focus();
                return;
            }

            if (cmbSessionTime.SelectedIndex == -1)
            {
                MessageBox.Show("Please select a preferred time.");
                cmbSessionTime.Focus();
                return;
            }

            MessageBox.Show(
                "Your booking details are valid. Database saving still needs to be implemented.",
                "Booking",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }*/

       /* private void btnClear_Click(object sender, EventArgs e)
        {
            cmbCourse.SelectedIndex = -1;
            cmbSessionTime.SelectedIndex = -1;

            dtpSessionDate.Value = DateTime.Today;
            dtpSessionDate.Checked = false;

            lblSelectedCourse.Text = "No course selected";
            lblSelectedCourse.ForeColor = Color.Black;
            lblSelectedCourse.BackColor = SystemColors.Control;

            /*lblBookingStatus.Text = "No booking submitted.";
            lblBookingStatus.ForeColor = Color.Black;
        }*/

        private void button1_Click(object sender, EventArgs e)
        {
            cmbCourse.SelectedIndex = -1;
            cmbSessionTime.SelectedIndex = -1;

            dtpSessionDate.Value = DateTime.Today;
            dtpSessionDate.Checked = false;

            lblSelectedCourse.Text = "No course selected";
            lblSelectedCourse.ForeColor = Color.Black;
            lblSelectedCourse.BackColor = SystemColors.Control;

            /*lblBookingStatus.Text = "No booking submitted.";
            lblBookingStatus.ForeColor = Color.Black;*/
        }

        private void btnBookSession_Click(object sender, EventArgs e)
        {
            if (cmbCourse.SelectedIndex == -1)
            {
                MessageBox.Show("Please select a course.");
                cmbCourse.Focus();
                return;
            }

            if (!dtpSessionDate.Checked)
            {
                MessageBox.Show("Please select a session date.");
                dtpSessionDate.Focus();
                return;
            }

            if (dtpSessionDate.Value.Date < DateTime.Today)
            {
                MessageBox.Show("The session date cannot be in the past.");
                dtpSessionDate.Focus();
                return;
            }

            if (cmbSessionTime.SelectedIndex == -1)
            {
                MessageBox.Show("Please select a preferred time.");
                cmbSessionTime.Focus();
                return;
            }

            MessageBox.Show(
                "Your booking details are valid. Database saving still needs to be implemented.",
                "Booking",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
