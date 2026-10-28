using System;
using System.Configuration;
using System.Data.OleDb;
using System.Windows.Forms;

namespace NextGen__7
{
    public partial class AccountPasswordsForm : Form
    {
        private readonly bool firstSetup;
        private readonly string connectionString;

        public AccountPasswordsForm()
        {
            InitializeComponent();
            EduConnectInputGuidance.Attach(this);
        }

        public AccountPasswordsForm(bool firstSetup) : this()
        {
            this.firstSetup = firstSetup;
            var config = ConfigurationManager.ConnectionStrings[
                "NextGen__7.Properties.Settings.Edu_ConnectConnectionString"];
            if (config == null)
                throw new InvalidOperationException("Database connection is not configured.");
            connectionString = config.ConnectionString;

            cmbRole.Items.AddRange(new object[] { "Student", "Staff" });
            cmbRole.SelectedIndex = firstSetup ? 1 : 0;
            cmbRole.Enabled = !firstSetup;
            lblHeading.Text = firstSetup ? "Initial administrator setup" :
                "Manage student and staff passwords";
            lblDescription.Text = firstSetup
                ? "Create the first administrator password using the local setup key."
                : "Set an initial password or replace a lost password for an existing user.";
            lblSetupKey.Visible = firstSetup;
            txtSetupKey.Visible = firstSetup;
            txtSetupKey.UseSystemPasswordChar = true;
            txtPassword.UseSystemPasswordChar = true;
            txtConfirm.UseSystemPasswordChar = true;
            btnSave.Text = firstSetup ? "Create admin password" : "Save password";
            btnSave.Click += (sender, e) => SaveAccountPassword();
            btnClose.Click += (sender, e) => Close();
            cmbRole.SelectedIndexChanged += (sender, e) =>
                lblAccountNumber.Text = cmbRole.SelectedIndex == 1 ?
                    "Staff number (AdminNo)" : "Student number";
            lblAccountNumber.Text = firstSetup ? "Staff number (AdminNo)" : "Student number";
            AcceptButton = btnSave;
            CancelButton = btnClose;
        }

        private void SaveAccountPassword()
        {
            string role = Convert.ToString(cmbRole.SelectedItem);
            string number = txtAccount.Text.Trim();
            if (number.Length == 0)
            {
                lblStatus.Text = "Enter an existing student/staff number."; return;
            }
            if (txtPassword.Text != txtConfirm.Text)
            {
                lblStatus.Text = "Passwords do not match."; return;
            }
            btnSave.Enabled = false;
            try
            {
                using (var db = new OleDbConnection(connectionString))
                {
                    db.Open();
                    if (firstSetup)
                        RolePasswords.InitializeAdministrator(db, number,
                            txtSetupKey.Text, txtPassword.Text);
                    else
                        RolePasswords.SetPassword(db, role, number, txtPassword.Text);
                }
                MessageBox.Show("Password saved for " + role + " " + number +
                    ". They can now sign in using their number and personal password.",
                    "EduConnect", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtPassword.Clear();
                txtConfirm.Clear();
                txtSetupKey.Clear();
                if (firstSetup) { DialogResult = DialogResult.OK; Close(); }
                else { txtAccount.Clear(); txtAccount.Focus(); lblStatus.Text = "Password saved."; }
            }
            catch (UnauthorizedAccessException)
            {
                lblStatus.Text = "The administrator setup key is incorrect.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = ex.Message;
            }
            finally { btnSave.Enabled = true; }
        }
    }
}
