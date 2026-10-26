namespace NextGen__7
{
    partial class EditBooking
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Panel pnlBanner;
        private System.Windows.Forms.Label lblBreadcrumb;
        private System.Windows.Forms.Label lblEditHeading;
        private System.Windows.Forms.Panel pnlCard;
        private System.Windows.Forms.Label lblEditCourse;
        private System.Windows.Forms.ComboBox cmbCourse;
        private System.Windows.Forms.Label lblEditSupport;
        private System.Windows.Forms.ComboBox cmbSupportType;
        private System.Windows.Forms.Label lblEditDate;
        private System.Windows.Forms.DateTimePicker dtpSessionDate;
        private System.Windows.Forms.Label lblEditTime;
        private System.Windows.Forms.ComboBox cmbSessionTime;
        private System.Windows.Forms.Label lblPolicy;
        private NextGen__7.BusyButton btnSaveChanges;
        private NextGen__7.BusyButton btnBack;
        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }
        private void InitializeComponent()
        {
            this.pnlBanner = new System.Windows.Forms.Panel();
            this.lblBreadcrumb = new System.Windows.Forms.Label();
            this.lblEditHeading = new System.Windows.Forms.Label();
            this.pnlCard = new System.Windows.Forms.Panel();
            this.lblEditCourse = new System.Windows.Forms.Label();
            this.cmbCourse = new System.Windows.Forms.ComboBox();
            this.lblEditSupport = new System.Windows.Forms.Label();
            this.cmbSupportType = new System.Windows.Forms.ComboBox();
            this.lblEditDate = new System.Windows.Forms.Label();
            this.dtpSessionDate = new System.Windows.Forms.DateTimePicker();
            this.lblEditTime = new System.Windows.Forms.Label();
            this.cmbSessionTime = new System.Windows.Forms.ComboBox();
            this.lblPolicy = new System.Windows.Forms.Label();
            this.btnSaveChanges = new NextGen__7.BusyButton();
            this.btnBack = new NextGen__7.BusyButton();
            this.SuspendLayout();
            this.pnlBanner.Name = "pnlBanner";
            this.pnlBanner.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlBanner.Height = 89;
            this.pnlBanner.BackColor = System.Drawing.Color.FromArgb(31, 56, 100);
            this.lblBreadcrumb.Name = "lblBreadcrumb";
            this.lblBreadcrumb.Text = "EduConnect  /  My bookings  /  Edit";
            this.lblBreadcrumb.Location = new System.Drawing.Point(30, 19);
            this.lblBreadcrumb.Size = new System.Drawing.Size(735, 45);
            this.lblBreadcrumb.ForeColor = System.Drawing.Color.White;
            this.lblBreadcrumb.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.pnlBanner.Controls.Add(this.lblBreadcrumb);
            this.lblEditHeading.Name = "lblEditHeading";
            this.lblEditHeading.Text = "Edit your tutoring booking";
            this.lblEditHeading.Location = new System.Drawing.Point(48, 119);
            this.lblEditHeading.Size = new System.Drawing.Size(720, 46);
            this.lblEditHeading.ForeColor = System.Drawing.Color.FromArgb(31, 56, 100);
            this.lblEditHeading.Font = new System.Drawing.Font("Segoe UI", 22F, System.Drawing.FontStyle.Bold);
            this.pnlCard.Name = "pnlCard";
            this.pnlCard.Location = new System.Drawing.Point(47, 183);
            this.pnlCard.Size = new System.Drawing.Size(728, 355);
            this.pnlCard.BackColor = System.Drawing.Color.White;
            this.pnlCard.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblEditCourse.Name = "lblEditCourse";
            this.lblEditCourse.Location = new System.Drawing.Point(30, 22);
            this.lblEditCourse.Size = new System.Drawing.Size(630, 29);
            this.lblEditCourse.Text = "Course";
            this.pnlCard.Controls.Add(this.lblEditCourse);
            this.cmbCourse.Name = "cmbCourse";
            this.cmbCourse.Location = new System.Drawing.Point(30, 56);
            this.cmbCourse.Size = new System.Drawing.Size(661, 39);
            this.cmbCourse.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.pnlCard.Controls.Add(this.cmbCourse);
            this.lblEditSupport.Name = "lblEditSupport";
            this.lblEditSupport.Location = new System.Drawing.Point(30, 111);
            this.lblEditSupport.Size = new System.Drawing.Size(630, 29);
            this.lblEditSupport.Text = "Type of support";
            this.pnlCard.Controls.Add(this.lblEditSupport);
            this.cmbSupportType.Name = "cmbSupportType";
            this.cmbSupportType.Location = new System.Drawing.Point(30, 145);
            this.cmbSupportType.Size = new System.Drawing.Size(661, 39);
            this.cmbSupportType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.pnlCard.Controls.Add(this.cmbSupportType);
            this.lblEditDate.Name = "lblEditDate";
            this.lblEditDate.Location = new System.Drawing.Point(30, 200);
            this.lblEditDate.Size = new System.Drawing.Size(320, 29);
            this.lblEditDate.Text = "Preferred date";
            this.pnlCard.Controls.Add(this.lblEditDate);
            this.dtpSessionDate.Name = "dtpSessionDate";
            this.dtpSessionDate.Location = new System.Drawing.Point(30, 234);
            this.dtpSessionDate.Size = new System.Drawing.Size(320, 39);
            this.pnlCard.Controls.Add(this.dtpSessionDate);
            this.lblEditTime.Name = "lblEditTime";
            this.lblEditTime.Location = new System.Drawing.Point(373, 200);
            this.lblEditTime.Size = new System.Drawing.Size(310, 29);
            this.lblEditTime.Text = "Preferred time";
            this.pnlCard.Controls.Add(this.lblEditTime);
            this.cmbSessionTime.Name = "cmbSessionTime";
            this.cmbSessionTime.Location = new System.Drawing.Point(373, 234);
            this.cmbSessionTime.Size = new System.Drawing.Size(318, 39);
            this.cmbSessionTime.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.pnlCard.Controls.Add(this.cmbSessionTime);
            this.lblPolicy.Name = "lblPolicy";
            this.lblPolicy.Location = new System.Drawing.Point(30, 294);
            this.lblPolicy.Size = new System.Drawing.Size(660, 39);
            this.lblPolicy.Text = "Changes and cancellations must be made at least 48 hours before the original session.";
            this.pnlCard.Controls.Add(this.lblPolicy);
            this.dtpSessionDate.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpSessionDate.CustomFormat = "dd MMM yyyy";
            this.cmbSessionTime.Items.AddRange(new object[]
            { "09:00 - 10:00", "10:15 - 11:15", "11:30 - 12:30",
              "13:00 - 14:00", "14:15 - 15:15", "15:30 - 16:30", "16:45 - 17:45" });
            this.btnSaveChanges.Name = "btnSaveChanges";
            this.btnSaveChanges.Text = "Save Changes";
            this.btnSaveChanges.Location = new System.Drawing.Point(47, 563);
            this.btnSaveChanges.Size = new System.Drawing.Size(234, 47);
            this.btnSaveChanges.BackColor = System.Drawing.Color.FromArgb(35, 93, 177);
            this.btnSaveChanges.ForeColor = System.Drawing.Color.White;
            this.btnSaveChanges.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSaveChanges.Click += new System.EventHandler(this.btnSaveChanges_Click);
            this.btnBack.Name = "btnBack";
            this.btnBack.Text = "Cancel";
            this.btnBack.Location = new System.Drawing.Point(295, 563);
            this.btnBack.Size = new System.Drawing.Size(176, 47);
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            this.AcceptButton = this.btnSaveChanges;
            this.CancelButton = this.btnBack;
            this.ClientSize = new System.Drawing.Size(825, 677);
            this.MinimumSize = new System.Drawing.Size(825, 677);
            this.BackColor = System.Drawing.Color.FromArgb(230, 237, 249);
            this.Text = "EduConnect | Edit booking";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Controls.Add(this.pnlCard);
            this.Controls.Add(this.btnSaveChanges);
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.lblEditHeading);
            this.Controls.Add(this.pnlBanner);
            this.Load += new System.EventHandler(this.EditBooking_Load);
            this.ResumeLayout(false);
        }
    }
}
