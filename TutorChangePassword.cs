using System;
using System.Data.OleDb;
using System.Windows.Forms;

namespace NextGen__7
{
    public partial class TutorChangePassword : Form
    {
        private readonly int tutorId;
        private readonly string connectionString;

        public TutorChangePassword()
        {
            InitializeComponent();
        }

        public TutorChangePassword(int tutorId, string connectionString) : this()
        {
            if (tutorId <= 0) throw new ArgumentOutOfRangeException("tutorId");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Database connection is missing.");
            this.tutorId = tutorId;
            this.connectionString = connectionString;
            txtCurrent.UseSystemPasswordChar = true;
            txtNew.UseSystemPasswordChar = true;
            txtConfirm.UseSystemPasswordChar = true;
            btnSave.Click += (sender, e) => SavePassword();
            btnCancel.Click += (sender, e) => Close();
            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private void SavePassword()
        {
            if (string.IsNullOrEmpty(txtCurrent.Text))
            {
                lblStatus.Text = "Enter your current password.";
                txtCurrent.Focus();
                return;
            }
            if (txtNew.Text != txtConfirm.Text)
            {
                lblStatus.Text = "New password and confirmation do not match.";
                txtConfirm.Focus();
                return;
            }

            btnSave.Enabled = false;
            try
            {
                using (var db = new OleDbConnection(connectionString))
                {
                    db.Open();
                    TutorAuthentication.ChangePassword(db, tutorId,
                        txtCurrent.Text, txtNew.Text);
                }

                MessageBox.Show(
                    "Your password has been changed. Use the new password next time you sign in.",
                    "Password changed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (ArgumentException ex)
            {
                lblStatus.Text = ex.Message;
            }
            catch (UnauthorizedAccessException)
            {
                lblStatus.Text = "The current password is incorrect.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Password change failed: " + ex.Message;
            }
            finally
            {
                txtCurrent.Clear();
                txtNew.Clear();
                txtConfirm.Clear();
                btnSave.Enabled = true;
            }
        }
    }
}
