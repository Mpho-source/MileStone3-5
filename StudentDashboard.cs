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
    public partial class StudentDashboard : Form
    {
        public StudentDashboard()
        {
            InitializeComponent();
            pnlBook.Clicked += (s, e) =>
            {
                this.Hide();

                using (BookingTutoringSession form = new BookingTutoringSession())
                {
                    form.ShowDialog();
                }

                this.Show();
            };

            // Manage existing bookings
            pnlManageBooking.Clicked += (s, e) =>
            {
                this.Hide();

                using (ManageBookings form = new ManageBookings())
                {
                    form.ShowDialog();
                }

                this.Show();
            };

            // Log out
            pnlLogout.Clicked += (s, e) =>
            {
                this.Hide();

                using (Login form = new Login())
                {
                    form.ShowDialog();
                }

                this.Close();
            };



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
