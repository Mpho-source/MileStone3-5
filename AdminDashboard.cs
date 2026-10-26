using System;
using System.Windows.Forms;

namespace NextGen__7
{
    public partial class AdminDashboard : Form
    {
        public AdminDashboard()
        {
            InitializeComponent();
        }

        public AdminDashboard(string staffNumber, string staffName) : this()
        {
            lblStaffDetails.Text = staffName + "   |   " + staffNumber;
        }

        private void btnTutors_Click(object sender, EventArgs e)
        {
            using (var form = new ManagaeTutors())
                form.ShowDialog(this);
        }

        private void btnRequests_Click(object sender, EventArgs e)
        {
            using (var form = new AdminRequests())
                form.ShowDialog(this);
        }

        private void btnReports_Click(object sender, EventArgs e)
        {
            using (var form = new Reports())
                form.ShowDialog(this);
        }

        private void btnSignOut_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Sign out of EduConnect?", "Confirm sign out",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                Close();
        }
    }
}
