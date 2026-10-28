using System;
using System.Windows.Forms;

namespace NextGen__7
{
    public partial class AdminDashboard : Form
    {
        private string signedInStaffNumber;
        public AdminDashboard()
        {
            InitializeComponent();
            EduConnectInputGuidance.Attach(this);
        }

        public AdminDashboard(string staffNumber, string staffName) : this()
        {
            signedInStaffNumber = staffNumber;
            lblStaffDetails.Text = staffName + "   |   " + staffNumber;
            btnManageAccounts.Click += (sender, e) =>
            {
                using (var accounts = new AccountPasswordsForm(false))
                    accounts.ShowDialog(this);
            };
            btnChangeStaffPassword.Click += (sender, e) =>
            {
                var cfg = System.Configuration.ConfigurationManager.ConnectionStrings[
                    "NextGen__7.Properties.Settings.Edu_ConnectConnectionString"];
                using (var dialog = new TutorChangePassword(
                    "Staff", signedInStaffNumber, cfg.ConnectionString))
                    dialog.ShowDialog(this);
            };
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
