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
            this.lblTitle = new System.Windows.Forms.Label();
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
            this.btnBookSession = new System.Windows.Forms.Button();
            this.lblSelectedCourse = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.BackColor = System.Drawing.SystemColors.InactiveCaption;
            this.lblTitle.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblTitle.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.lblTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.lblTitle.Location = new System.Drawing.Point(4, 9);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(936, 128);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "EDUCONNECT\r\nTUTORING SUPPORT SYSTEM\r\n\r\nBook a Tutoring Session\r\nFind available tu" +
    "toring support and submit your booking.";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblTitle.Click += new System.EventHandler(this.label1_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 165);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(245, 20);
            this.label1.TabIndex = 1;
            this.label1.Text = "1. Student and course details";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.SystemColors.WindowFrame;
            this.label2.Location = new System.Drawing.Point(31, 200);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(72, 18);
            this.label2.TabIndex = 2;
            this.label2.Text = "StudentID";
            this.label2.Click += new System.EventHandler(this.label2_Click);
            // 
            // txtStudentID
            // 
            this.txtStudentID.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtStudentID.Location = new System.Drawing.Point(34, 220);
            this.txtStudentID.Name = "txtStudentID";
            this.txtStudentID.Size = new System.Drawing.Size(152, 32);
            this.txtStudentID.TabIndex = 3;
            this.txtStudentID.Text = "";
            // 
            // cmbCourse
            // 
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
            this.cmbCourse.Location = new System.Drawing.Point(267, 224);
            this.cmbCourse.Name = "cmbCourse";
            this.cmbCourse.Size = new System.Drawing.Size(150, 21);
            this.cmbCourse.TabIndex = 4;
            this.cmbCourse.SelectedIndexChanged += new System.EventHandler(this.cmbCourse_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.SystemColors.GrayText;
            this.label3.Location = new System.Drawing.Point(297, 200);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(57, 18);
            this.label3.TabIndex = 5;
            this.label3.Text = "Course";
            this.label3.Click += new System.EventHandler(this.label3_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.SystemColors.GrayText;
            this.label4.Location = new System.Drawing.Point(31, 305);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(164, 18);
            this.label4.TabIndex = 6;
            this.label4.Text = "Type of tutoring support";
            this.label4.Click += new System.EventHandler(this.label4_Click);
            // 
            // cmbSupportType
            // 
            this.cmbSupportType.FormattingEnabled = true;
            this.cmbSupportType.Items.AddRange(new object[] {
            "Individual tutoring",
            "",
            "",
            "",
            "",
            "Group tutoring",
            "",
            "",
            "",
            "",
            "Online tutoring",
            "",
            "",
            "",
            "",
            "In-person tutoring"});
            this.cmbSupportType.Location = new System.Drawing.Point(34, 335);
            this.cmbSupportType.Name = "cmbSupportType";
            this.cmbSupportType.Size = new System.Drawing.Size(121, 21);
            this.cmbSupportType.TabIndex = 7;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(12, 390);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(151, 20);
            this.label5.TabIndex = 8;
            this.label5.Text = "2. Session details";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.ForeColor = System.Drawing.SystemColors.GrayText;
            this.label6.Location = new System.Drawing.Point(31, 423);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(104, 18);
            this.label6.TabIndex = 9;
            this.label6.Text = "Preferred Date";
            this.label6.Click += new System.EventHandler(this.label6_Click);
            // 
            // dtpSessionDate
            // 
            this.dtpSessionDate.Checked = false;
            this.dtpSessionDate.CustomFormat = "Select a session date";
            this.dtpSessionDate.Location = new System.Drawing.Point(34, 444);
            this.dtpSessionDate.Name = "dtpSessionDate";
            this.dtpSessionDate.Size = new System.Drawing.Size(200, 20);
            this.dtpSessionDate.TabIndex = 10;
            this.dtpSessionDate.ValueChanged += new System.EventHandler(this.dtpSessionDate_ValueChanged);
            // 
            // cmbSessionTime
            // 
            this.cmbSessionTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbSessionTime.ForeColor = System.Drawing.SystemColors.ScrollBar;
            this.cmbSessionTime.FormattingEnabled = true;
            this.cmbSessionTime.Items.AddRange(new object[] {
            "09:00 - 10:00",
            "10:15 - 11:15",
            "11:30 - 12:30",
            "13:00 - 14:00",
            "14:15 - 15:15",
            "15:30 - 16:30",
            "16:45 - 17:45"});
            this.cmbSessionTime.Location = new System.Drawing.Point(295, 442);
            this.cmbSessionTime.Name = "cmbSessionTime";
            this.cmbSessionTime.Size = new System.Drawing.Size(121, 26);
            this.cmbSessionTime.TabIndex = 11;
            this.cmbSessionTime.Text = "Pick Time";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ForeColor = System.Drawing.SystemColors.GrayText;
            this.label7.Location = new System.Drawing.Point(297, 421);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(106, 18);
            this.label7.TabIndex = 12;
            this.label7.Text = "Preferred Time";
            // 
            // btnBookSession
            // 
            this.btnBookSession.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.btnBookSession.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBookSession.FlatAppearance.BorderSize = 0;
            this.btnBookSession.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBookSession.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBookSession.ForeColor = System.Drawing.Color.Black;
            this.btnBookSession.Location = new System.Drawing.Point(34, 526);
            this.btnBookSession.Name = "btnBookSession";
            this.btnBookSession.Size = new System.Drawing.Size(121, 28);
            this.btnBookSession.TabIndex = 13;
            this.btnBookSession.Text = "Book Session";
            this.btnBookSession.UseVisualStyleBackColor = false;
            // 
            // lblSelectedCourse
            // 
            this.lblSelectedCourse.AutoSize = true;
            this.lblSelectedCourse.ForeColor = System.Drawing.SystemColors.ButtonShadow;
            this.lblSelectedCourse.Location = new System.Drawing.Point(275, 248);
            this.lblSelectedCourse.Name = "lblSelectedCourse";
            this.lblSelectedCourse.Size = new System.Drawing.Size(99, 13);
            this.lblSelectedCourse.TabIndex = 14;
            this.lblSelectedCourse.Text = "No course selected";
            // 
            // BookingTutoringSession
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Azure;
            this.ClientSize = new System.Drawing.Size(937, 858);
            this.Controls.Add(this.lblSelectedCourse);
            this.Controls.Add(this.btnBookSession);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.cmbSessionTime);
            this.Controls.Add(this.dtpSessionDate);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.cmbSupportType);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.cmbCourse);
            this.Controls.Add(this.txtStudentID);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lblTitle);
            this.Name = "BookingTutoringSession";
            this.Text = "BookingTutoringSession";
            this.Load += new System.EventHandler(this.BookingTutoringSession_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
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
        private System.Windows.Forms.Button btnBookSession;
        private System.Windows.Forms.Label lblSelectedCourse;
    }
}

