namespace NextGen__7
{
    partial class BookingTutoringSession
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BookingTutoringSession));
            this.pnlDetails = new System.Windows.Forms.Panel();
            this.pnlSchedule = new System.Windows.Forms.Panel();
            this.lblPageHeading = new System.Windows.Forms.Label();
            this.lblPageDescription = new System.Windows.Forms.Label();
            this.lblBookingPolicy = new System.Windows.Forms.Label();
            this.lblBookingNotice = new System.Windows.Forms.Label();
            this.btnNavDashboard = new NextGen__7.BusyButton();
            this.btnNavBookings = new NextGen__7.BusyButton();
            this.btnNavBack = new NextGen__7.BusyButton();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtStudentID = new System.Windows.Forms.RichTextBox();
            this.cmbCourse = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.cmbSupportType = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.dtpSessionDate = new System.Windows.Forms.DateTimePicker();
            this.cmbSessionTime = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.lblSelectedCourse = new System.Windows.Forms.Label();
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.picLogo = new System.Windows.Forms.PictureBox();
            this.btnBookSession = new NextGen__7.BusyButton();
            this.btnClear = new NextGen__7.BusyButton();
            this.btnBack = new NextGen__7.BusyButton();
            this.pnlHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = false;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Navy;
            this.label1.Location = new System.Drawing.Point(26, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(600, 35);
            this.label1.TabIndex = 1;
            this.label1.Text = "1. Student and course details";
            // 
            // label2
            // 
            this.label2.AutoSize = false;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.SystemColors.WindowFrame;
            this.label2.Location = new System.Drawing.Point(28, 73);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(230, 24);
            this.label2.TabIndex = 2;
            this.label2.Text = "StudentID";
            this.label2.Click += new System.EventHandler(this.label2_Click);
            // 
            // txtStudentID
            // 
            this.txtStudentID.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtStudentID.Location = new System.Drawing.Point(28, 107);
            this.txtStudentID.Name = "txtStudentID";
            this.txtStudentID.Size = new System.Drawing.Size(204, 37);
            this.txtStudentID.TabIndex = 3;
            this.txtStudentID.Text = "";
            // 
            // cmbCourse
            // 
            this.cmbCourse.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbCourse.FormattingEnabled = true;
            this.cmbCourse.Items.AddRange(new object[] {
            "COMS1015A - Introduction to Computer Science",
            "COMS1016A - Computer Science Fundamentals",
            "COMS2015A - Analysis of Algorithms",
            "COMS2014A - Computer Networks",
            "MATH1034A - Mathematics I",
            "MATH1041A - Calculus",
            "STAT1000A - Introduction to Statistics",
            "PHYS1001A - Physics I",
            "ECON1000A - Introduction to Economics",
            "ACCT1000A - Introduction to Accounting",
            "CHEM1000A - General Chemistry",
            "INFO2001A - Information Systems"});
            this.cmbCourse.Location = new System.Drawing.Point(260, 107);
            this.cmbCourse.Name = "cmbCourse";
            this.cmbCourse.Size = new System.Drawing.Size(428, 37);
            this.cmbCourse.TabIndex = 4;
            this.cmbCourse.SelectedIndexChanged += new System.EventHandler(this.cmbCourse_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = false;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.SystemColors.GrayText;
            this.label3.Location = new System.Drawing.Point(260, 73);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(385, 24);
            this.label3.TabIndex = 5;
            this.label3.Text = "Course";
            this.label3.Click += new System.EventHandler(this.label3_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = false;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.SystemColors.GrayText;
            this.label4.Location = new System.Drawing.Point(712, 73);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(228, 24);
            this.label4.TabIndex = 6;
            this.label4.Text = "Type of tutoring support";
            this.label4.Click += new System.EventHandler(this.label4_Click);
            // 
            // cmbSupportType
            // 
            this.cmbSupportType.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbSupportType.FormattingEnabled = true;
            this.cmbSupportType.Items.AddRange(new object[] {
            "Individual Tutoring",
            "Group Tutoring",
            "Exam Preparation",
            "Assignment Support."});
            this.cmbSupportType.Location = new System.Drawing.Point(712, 107);
            this.cmbSupportType.Name = "cmbSupportType";
            this.cmbSupportType.Size = new System.Drawing.Size(229, 37);
            this.cmbSupportType.TabIndex = 7;
            // 
            // label5
            // 
            this.label5.AutoSize = false;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.Color.Navy;
            this.label5.Location = new System.Drawing.Point(26, 18);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(560, 35);
            this.label5.TabIndex = 8;
            this.label5.Text = "2. Session details";
            // 
            // label6
            // 
            this.label6.AutoSize = false;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.ForeColor = System.Drawing.SystemColors.GrayText;
            this.label6.Location = new System.Drawing.Point(28, 67);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(295, 24);
            this.label6.TabIndex = 9;
            this.label6.Text = "Preferred Date";
            this.label6.Click += new System.EventHandler(this.label6_Click);
            // 
            // dtpSessionDate
            // 
            this.dtpSessionDate.Checked = false;
            this.dtpSessionDate.CustomFormat = "";
            this.dtpSessionDate.Location = new System.Drawing.Point(28, 101);
            this.dtpSessionDate.Name = "dtpSessionDate";
            this.dtpSessionDate.Size = new System.Drawing.Size(308, 38);
            this.dtpSessionDate.TabIndex = 10;
            this.dtpSessionDate.ValueChanged += new System.EventHandler(this.dtpSessionDate_ValueChanged);
            // 
            // cmbSessionTime
            // 
            this.cmbSessionTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbSessionTime.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.cmbSessionTime.FormattingEnabled = true;
            this.cmbSessionTime.Items.AddRange(new object[] {
            "09:00 - 10:00",
            "10:15 - 11:15",
            "11:30 - 12:30",
            "13:00 - 14:00",
            "14:15 - 15:15",
            "15:30 - 16:30",
            "16:45 - 17:45"});
            this.cmbSessionTime.Location = new System.Drawing.Point(366, 101);
            this.cmbSessionTime.Name = "cmbSessionTime";
            this.cmbSessionTime.Size = new System.Drawing.Size(290, 37);
            this.cmbSessionTime.TabIndex = 11;
            // 
            // label7
            // 
            this.label7.AutoSize = false;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ForeColor = System.Drawing.SystemColors.GrayText;
            this.label7.Location = new System.Drawing.Point(366, 67);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(290, 24);
            this.label7.TabIndex = 12;
            this.label7.Text = "Preferred Time";
            // 
            // lblSelectedCourse
            // 
            this.lblSelectedCourse.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSelectedCourse.ForeColor = System.Drawing.SystemColors.ButtonShadow;
            this.lblSelectedCourse.Location = new System.Drawing.Point(260, 155);
            this.lblSelectedCourse.Name = "lblSelectedCourse";
            this.lblSelectedCourse.Size = new System.Drawing.Size(420, 28);
            this.lblSelectedCourse.TabIndex = 14;
            this.lblSelectedCourse.Text = "No course selected";
            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(56)))), ((int)(((byte)(100)))));
            this.pnlHeader.Controls.Add(this.picLogo);
            this.pnlHeader.Controls.Add(this.label9);
            this.pnlHeader.Controls.Add(this.label8);
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Size = new System.Drawing.Size(900, 100);
            this.pnlHeader.TabIndex = 15;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Segoe UI", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.ForeColor = System.Drawing.Color.White;
            this.label8.Location = new System.Drawing.Point(121, 9);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(169, 37);
            this.label8.TabIndex = 0;
            this.label8.Text = "EduConnect";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.ForeColor = System.Drawing.Color.Gainsboro;
            this.label9.Location = new System.Drawing.Point(125, 55);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(176, 17);
            this.label9.TabIndex = 1;
            this.label9.Text = "BOOK A TUTORING SESSION";
            // 
            // picLogo
            // 
            this.picLogo.BackColor = System.Drawing.Color.Transparent;
            this.picLogo.Image = ((System.Drawing.Image)(resources.GetObject("picLogo.Image")));
            this.picLogo.InitialImage = ((System.Drawing.Image)(resources.GetObject("picLogo.InitialImage")));
            this.picLogo.Location = new System.Drawing.Point(20, 20);
            this.picLogo.Name = "picLogo";
            this.picLogo.Size = new System.Drawing.Size(60, 60);
            this.picLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picLogo.TabIndex = 2;
            this.picLogo.TabStop = false;
            // 
            // btnBookSession
            // 
            this.btnBookSession.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBookSession.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBookSession.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.btnBookSession.Location = new System.Drawing.Point(56, 668);
            this.btnBookSession.Name = "btnBookSession";
            this.btnBookSession.Size = new System.Drawing.Size(244, 47);
            this.btnBookSession.TabIndex = 17;
            this.btnBookSession.Text = "Book Session";
            this.btnBookSession.UseVisualStyleBackColor = true;
            this.btnBookSession.Click += new System.EventHandler(this.btnBookSession_Click);
            // 
            // btnClear
            // 
            this.btnClear.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClear.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnClear.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.btnClear.Location = new System.Drawing.Point(316, 668);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(158, 47);
            this.btnClear.TabIndex = 18;
            this.btnClear.Text = "Clear";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.button1_Click);
            // 
            // btnBack
            // 
            this.btnBack.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBack.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBack.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.btnBack.Location = new System.Drawing.Point(880, 668);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(156, 47);
            this.btnBack.TabIndex = 19;
            this.btnBack.Text = "Back";
            this.btnBack.UseVisualStyleBackColor = true;
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            // Designer-managed booking navigation and form sections
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Height = 90;
            this.pnlHeader.BackColor = System.Drawing.Color.FromArgb(31, 56, 100);
            this.btnNavDashboard.Name = "btnNavDashboard";
            this.btnNavDashboard.Text = "Dashboard";
            this.btnNavDashboard.Location = new System.Drawing.Point(560, 20);
            this.btnNavDashboard.Size = new System.Drawing.Size(140, 45);
            this.btnNavBookings.Name = "btnNavBookings";
            this.btnNavBookings.Text = "My bookings";
            this.btnNavBookings.Location = new System.Drawing.Point(712, 20);
            this.btnNavBookings.Size = new System.Drawing.Size(150, 45);
            this.btnNavBack.Name = "btnNavBack";
            this.btnNavBack.Text = "Back";
            this.btnNavBack.Location = new System.Drawing.Point(875, 20);
            this.btnNavBack.Size = new System.Drawing.Size(115, 45);
            this.btnNavDashboard.BackColor = System.Drawing.Color.FromArgb(74, 122, 191);
            this.btnNavDashboard.ForeColor = System.Drawing.Color.White;
            this.btnNavDashboard.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.pnlHeader.Controls.Add(this.btnNavDashboard);
            this.btnNavBookings.BackColor = System.Drawing.Color.FromArgb(74, 122, 191);
            this.btnNavBookings.ForeColor = System.Drawing.Color.White;
            this.btnNavBookings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.pnlHeader.Controls.Add(this.btnNavBookings);
            this.btnNavBack.BackColor = System.Drawing.Color.FromArgb(74, 122, 191);
            this.btnNavBack.ForeColor = System.Drawing.Color.White;
            this.btnNavBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.pnlHeader.Controls.Add(this.btnNavBack);
            this.lblPageHeading.Name = "lblPageHeading";
            this.lblPageHeading.Text = "Book a tutoring session";
            this.lblPageHeading.Location = new System.Drawing.Point(56, 126);
            this.lblPageHeading.Size = new System.Drawing.Size(950, 45);
            this.lblPageHeading.Font = new System.Drawing.Font("Segoe UI", 22F, System.Drawing.FontStyle.Bold);
            this.lblPageHeading.ForeColor = System.Drawing.Color.FromArgb(31, 56, 100);
            this.lblPageDescription.Name = "lblPageDescription";
            this.lblPageDescription.Text = "Complete the details below to request academic support.";
            this.lblPageDescription.Location = new System.Drawing.Point(58, 170);
            this.lblPageDescription.Size = new System.Drawing.Size(950, 27);
            this.lblPageDescription.ForeColor = System.Drawing.Color.FromArgb(83, 104, 132);
            this.pnlDetails.Name = "pnlDetails";
            this.pnlDetails.Location = new System.Drawing.Point(56, 216);
            this.pnlDetails.Size = new System.Drawing.Size(980, 211);
            this.pnlDetails.BackColor = System.Drawing.Color.White;
            this.pnlDetails.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlSchedule.Name = "pnlSchedule";
            this.pnlSchedule.Location = new System.Drawing.Point(56, 448);
            this.pnlSchedule.Size = new System.Drawing.Size(980, 204);
            this.pnlSchedule.BackColor = System.Drawing.Color.White;
            this.pnlSchedule.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblBookingPolicy.Name = "lblBookingPolicy";
            this.lblBookingPolicy.Text = "Need to edit later? Changes and cancellations close 48 hours before the session.";
            this.lblBookingPolicy.Location = new System.Drawing.Point(28, 151);
            this.lblBookingPolicy.Size = new System.Drawing.Size(860, 32);
            this.lblBookingPolicy.ForeColor = System.Drawing.Color.FromArgb(83, 104, 132);
            this.pnlSchedule.Controls.Add(this.lblBookingPolicy);
            this.lblBookingNotice.Name = "lblBookingNotice";
            this.lblBookingNotice.Text = "Tutor matching happens automatically when you submit your request.";
            this.lblBookingNotice.Location = new System.Drawing.Point(490, 672);
            this.lblBookingNotice.Size = new System.Drawing.Size(350, 42);
            this.lblBookingNotice.ForeColor = System.Drawing.Color.FromArgb(83, 104, 132);
            // 
            // BookingTutoringSession
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(237)))), ((int)(((byte)(249)))));
            this.ClientSize = new System.Drawing.Size(1080, 740);
            this.MinimumSize = new System.Drawing.Size(1030, 740);
            this.Controls.Add(this.pnlDetails);
            this.Controls.Add(this.pnlSchedule);
            this.Controls.Add(this.lblPageHeading);
            this.Controls.Add(this.lblPageDescription);
            this.Controls.Add(this.lblBookingNotice);
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.btnBookSession);
            this.Controls.Add(this.pnlHeader);
            this.pnlDetails.Controls.Add(this.lblSelectedCourse);
            this.pnlSchedule.Controls.Add(this.label7);
            this.pnlSchedule.Controls.Add(this.cmbSessionTime);
            this.pnlSchedule.Controls.Add(this.dtpSessionDate);
            this.pnlSchedule.Controls.Add(this.label6);
            this.pnlSchedule.Controls.Add(this.label5);
            this.pnlDetails.Controls.Add(this.cmbSupportType);
            this.pnlDetails.Controls.Add(this.label4);
            this.pnlDetails.Controls.Add(this.label3);
            this.pnlDetails.Controls.Add(this.cmbCourse);
            this.pnlDetails.Controls.Add(this.txtStudentID);
            this.pnlDetails.Controls.Add(this.label2);
            this.pnlDetails.Controls.Add(this.label1);
            this.Name = "BookingTutoringSession";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "EduConnect | Book a session";
            this.Load += new System.EventHandler(this.BookingTutoringSession_Load);
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Panel pnlDetails;
        private System.Windows.Forms.Panel pnlSchedule;
        private System.Windows.Forms.Label lblPageHeading;
        private System.Windows.Forms.Label lblPageDescription;
        private System.Windows.Forms.Label lblBookingPolicy;
        private System.Windows.Forms.Label lblBookingNotice;
        private NextGen__7.BusyButton btnNavDashboard;
        private NextGen__7.BusyButton btnNavBookings;
        private NextGen__7.BusyButton btnNavBack;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.RichTextBox txtStudentID;
        private System.Windows.Forms.ComboBox cmbCourse;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cmbSupportType;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.DateTimePicker dtpSessionDate;
        private System.Windows.Forms.ComboBox cmbSessionTime;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label lblSelectedCourse;
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.PictureBox picLogo;
        private NextGen__7.BusyButton btnBookSession;
        private NextGen__7.BusyButton btnClear;
        private NextGen__7.BusyButton btnBack;
    }
}

