namespace NextGen__7
{
    partial class TutorChangePassword
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.lblCurrent = new System.Windows.Forms.Label();
            this.txtCurrent = new System.Windows.Forms.TextBox();
            this.lblNew = new System.Windows.Forms.Label();
            this.txtNew = new System.Windows.Forms.TextBox();
            this.lblConfirm = new System.Windows.Forms.Label();
            this.txtConfirm = new System.Windows.Forms.TextBox();
            this.lblRule = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Location = new System.Drawing.Point(0,0);
            this.pnlHeader.Size = new System.Drawing.Size(548,102);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.BackColor = System.Drawing.Color.FromArgb(31,56,100);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Text = "Change password";
            this.lblTitle.Location = new System.Drawing.Point(24,14);
            this.lblTitle.Size = new System.Drawing.Size(470,46);
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI",21F,System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Text = "Update the password for your tutor account.";
            this.lblSubtitle.Location = new System.Drawing.Point(26,66);
            this.lblSubtitle.Size = new System.Drawing.Size(483,27);
            this.lblSubtitle.ForeColor = System.Drawing.Color.Gainsboro;
            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Controls.Add(this.lblSubtitle);
            this.lblCurrent.Name = "lblCurrent";
            this.lblCurrent.Text = "Current password";
            this.lblCurrent.Location = new System.Drawing.Point(28,120);
            this.lblCurrent.Size = new System.Drawing.Size(470,24);
            this.lblCurrent.ForeColor = System.Drawing.Color.FromArgb(31,56,100);
            this.lblCurrent.Font = new System.Drawing.Font("Segoe UI",10F,System.Drawing.FontStyle.Bold);
            this.txtCurrent.Name = "txtCurrent";
            this.txtCurrent.Location = new System.Drawing.Point(28,147);
            this.txtCurrent.Size = new System.Drawing.Size(490,32);
            this.lblNew.Name = "lblNew";
            this.lblNew.Text = "New password";
            this.lblNew.Location = new System.Drawing.Point(28,188);
            this.lblNew.Size = new System.Drawing.Size(470,24);
            this.lblNew.ForeColor = System.Drawing.Color.FromArgb(31,56,100);
            this.lblNew.Font = new System.Drawing.Font("Segoe UI",10F,System.Drawing.FontStyle.Bold);
            this.txtNew.Name = "txtNew";
            this.txtNew.Location = new System.Drawing.Point(28,215);
            this.txtNew.Size = new System.Drawing.Size(490,32);
            this.lblConfirm.Name = "lblConfirm";
            this.lblConfirm.Text = "Confirm new password";
            this.lblConfirm.Location = new System.Drawing.Point(28,254);
            this.lblConfirm.Size = new System.Drawing.Size(470,24);
            this.lblConfirm.ForeColor = System.Drawing.Color.FromArgb(31,56,100);
            this.lblConfirm.Font = new System.Drawing.Font("Segoe UI",10F,System.Drawing.FontStyle.Bold);
            this.txtConfirm.Name = "txtConfirm";
            this.txtConfirm.Location = new System.Drawing.Point(28,282);
            this.txtConfirm.Size = new System.Drawing.Size(490,32);
            this.lblRule.Name = "lblRule";
            this.lblRule.Text = "Use 12+ characters with uppercase, lowercase and a number.";
            this.lblRule.Location = new System.Drawing.Point(28,324);
            this.lblRule.Size = new System.Drawing.Size(502,28);
            this.lblRule.ForeColor = System.Drawing.Color.FromArgb(83,104,132);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Text = "";
            this.lblStatus.Location = new System.Drawing.Point(28,354);
            this.lblStatus.Size = new System.Drawing.Size(490,43);
            this.lblStatus.ForeColor = System.Drawing.Color.FromArgb(156,50,50);
            this.btnSave.Name = "btnSave";
            this.btnSave.Text = "Change password";
            this.btnSave.Location = new System.Drawing.Point(222,411);
            this.btnSave.Size = new System.Drawing.Size(178,44);
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(35,93,177);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Text = "Cancel";
            this.btnCancel.Location = new System.Drawing.Point(410,411);
            this.btnCancel.Size = new System.Drawing.Size(108,44);
            this.btnCancel.BackColor = System.Drawing.Color.FromArgb(83,104,132);
            this.btnCancel.ForeColor = System.Drawing.Color.White;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(548,480);
            this.Text = "EduConnect | Change password";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = System.Drawing.Color.White;
            this.Font = new System.Drawing.Font("Segoe UI",10F);
            this.Controls.Add(this.pnlHeader);
            this.Controls.Add(this.lblCurrent);
            this.Controls.Add(this.txtCurrent);
            this.Controls.Add(this.lblNew);
            this.Controls.Add(this.txtNew);
            this.Controls.Add(this.lblConfirm);
            this.Controls.Add(this.txtConfirm);
            this.Controls.Add(this.lblRule);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnCancel);
            this.ResumeLayout(false);
        }
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.Label lblCurrent;
        private System.Windows.Forms.TextBox txtCurrent;
        private System.Windows.Forms.Label lblNew;
        private System.Windows.Forms.TextBox txtNew;
        private System.Windows.Forms.Label lblConfirm;
        private System.Windows.Forms.TextBox txtConfirm;
        private System.Windows.Forms.Label lblRule;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
    }
}
