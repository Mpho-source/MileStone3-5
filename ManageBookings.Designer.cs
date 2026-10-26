namespace NextGen__7
{
    partial class ManageBookings
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ManageBookings));
            this.pnlListCard = new System.Windows.Forms.Panel();
            this.lblHeading = new System.Windows.Forms.Label();
            this.lblDescription = new System.Windows.Forms.Label();
            this.lblBookingActionHelp = new System.Windows.Forms.Label();
            this.lblFilter = new System.Windows.Forms.Label();
            this.cmbBookingStatusFilter = new System.Windows.Forms.ComboBox();
            this.btnNavDashboard = new System.Windows.Forms.Button();
            this.btnNavBook = new System.Windows.Forms.Button();
            this.btnNavBack = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.btnEditBooking = new System.Windows.Forms.Button();
            this.btnCancelBooking = new System.Windows.Forms.Button();
            this.btnBack = new System.Windows.Forms.Button();
            this.edu_ConnectDataSet1 = new NextGen__7.Edu_ConnectDataSet1();
            this.tUTORINGREQUESTBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.tUTORING_REQUESTTableAdapter = new NextGen__7.Edu_ConnectDataSet1TableAdapters.TUTORING_REQUESTTableAdapter();
            this.tUTORINGREQUESTBindingSource1 = new System.Windows.Forms.BindingSource(this.components);
            this.lblStudentNo = new System.Windows.Forms.Label();
            this.dgvBookings = new System.Windows.Forms.DataGridView();
            this.tUTORINGREQUESTBindingSource2 = new System.Windows.Forms.BindingSource(this.components);
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.edu_ConnectDataSet1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tUTORINGREQUESTBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tUTORINGREQUESTBindingSource1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvBookings)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tUTORINGREQUESTBindingSource2)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(56)))), ((int)(((byte)(100)))));
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.pictureBox1);
            this.panel1.Location = new System.Drawing.Point(1, 2);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(803, 101);
            this.panel1.TabIndex = 0;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.Gainsboro;
            this.label2.Location = new System.Drawing.Point(142, 69);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(321, 16);
            this.label2.TabIndex = 2;
            this.label2.Text = "VIEW AND MANAGE YOUR TUTORING SESSIONS";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.label1.Location = new System.Drawing.Point(138, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(184, 37);
            this.label1.TabIndex = 1;
            this.label1.Text = "My Bookings";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(26, 25);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(60, 60);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // btnEditBooking
            // 
            this.btnEditBooking.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnEditBooking.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.btnEditBooking.Location = new System.Drawing.Point(185, 436);
            this.btnEditBooking.Name = "btnEditBooking";
            this.btnEditBooking.Size = new System.Drawing.Size(138, 41);
            this.btnEditBooking.TabIndex = 1;
            this.btnEditBooking.Text = "Edit Booking";
            this.btnEditBooking.UseVisualStyleBackColor = true;
            // 
            // btnCancelBooking
            // 
            this.btnCancelBooking.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCancelBooking.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.btnCancelBooking.Location = new System.Drawing.Point(439, 436);
            this.btnCancelBooking.Name = "btnCancelBooking";
            this.btnCancelBooking.Size = new System.Drawing.Size(169, 41);
            this.btnCancelBooking.TabIndex = 2;
            this.btnCancelBooking.Text = "Cancel Booking";
            this.btnCancelBooking.UseVisualStyleBackColor = true;
            // 
            // btnBack
            // 
            this.btnBack.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBack.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBack.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.btnBack.Location = new System.Drawing.Point(259, 510);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(247, 35);
            this.btnBack.TabIndex = 20;
            this.btnBack.Text = "Back to DashBoard";
            this.btnBack.UseVisualStyleBackColor = true;
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            // 
            // edu_ConnectDataSet1
            // 
            this.edu_ConnectDataSet1.DataSetName = "Edu_ConnectDataSet1";
            this.edu_ConnectDataSet1.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema;
            // 
            // tUTORINGREQUESTBindingSource
            // 
            this.tUTORINGREQUESTBindingSource.DataMember = "TUTORING_REQUEST";
            this.tUTORINGREQUESTBindingSource.DataSource = this.edu_ConnectDataSet1;
            // 
            // tUTORING_REQUESTTableAdapter
            // 
            this.tUTORING_REQUESTTableAdapter.ClearBeforeFill = true;
            // 
            // tUTORINGREQUESTBindingSource1
            // 
            this.tUTORINGREQUESTBindingSource1.DataMember = "TUTORING_REQUEST";
            this.tUTORINGREQUESTBindingSource1.DataSource = this.edu_ConnectDataSet1;
            // 
            // lblStudentNo
            // 
            this.lblStudentNo.AutoSize = true;
            this.lblStudentNo.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStudentNo.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.lblStudentNo.Location = new System.Drawing.Point(12, 115);
            this.lblStudentNo.Name = "lblStudentNo";
            this.lblStudentNo.Size = new System.Drawing.Size(94, 17);
            this.lblStudentNo.TabIndex = 22;
            this.lblStudentNo.Text = "Student No:";
            // 
            // dgvBookings
            // 
            this.dgvBookings.AllowUserToAddRows = false;
            this.dgvBookings.AllowUserToDeleteRows = false;
            this.dgvBookings.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvBookings.BackgroundColor = System.Drawing.SystemColors.ButtonFace;
            this.dgvBookings.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvBookings.Location = new System.Drawing.Point(30, 155);
            this.dgvBookings.MultiSelect = false;
            this.dgvBookings.Name = "dgvBookings";
            this.dgvBookings.ReadOnly = true;
            this.dgvBookings.RowHeadersVisible = false;
            this.dgvBookings.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvBookings.Size = new System.Drawing.Size(745, 250);
            this.dgvBookings.TabIndex = 23;
            // 
            // tUTORINGREQUESTBindingSource2
            // 
            this.tUTORINGREQUESTBindingSource2.DataMember = "TUTORING_REQUEST";
            this.tUTORINGREQUESTBindingSource2.DataSource = this.edu_ConnectDataSet1;
            // Designer-managed booking filters and navigation
            this.btnNavDashboard.Name = "btnNavDashboard";
            this.btnNavDashboard.Text = "Dashboard";
            this.btnNavDashboard.Location = new System.Drawing.Point(548, 19);
            this.btnNavDashboard.Size = new System.Drawing.Size(135, 45);
            this.btnNavBook.Name = "btnNavBook";
            this.btnNavBook.Text = "Book a session";
            this.btnNavBook.Location = new System.Drawing.Point(693, 19);
            this.btnNavBook.Size = new System.Drawing.Size(156, 45);
            this.btnNavBack.Name = "btnNavBack";
            this.btnNavBack.Text = "Back";
            this.btnNavBack.Location = new System.Drawing.Point(861, 19);
            this.btnNavBack.Size = new System.Drawing.Size(110, 45);
            this.btnNavDashboard.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNavDashboard.BackColor = System.Drawing.Color.FromArgb(74, 122, 191);
            this.btnNavDashboard.ForeColor = System.Drawing.Color.White;
            this.panel1.Controls.Add(this.btnNavDashboard);
            this.btnNavBook.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNavBook.BackColor = System.Drawing.Color.FromArgb(74, 122, 191);
            this.btnNavBook.ForeColor = System.Drawing.Color.White;
            this.panel1.Controls.Add(this.btnNavBook);
            this.btnNavBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNavBack.BackColor = System.Drawing.Color.FromArgb(74, 122, 191);
            this.btnNavBack.ForeColor = System.Drawing.Color.White;
            this.panel1.Controls.Add(this.btnNavBack);
            this.lblHeading.Name = "lblHeading";
            this.lblHeading.Text = "My tutoring bookings";
            this.lblHeading.Location = new System.Drawing.Point(56, 126);
            this.lblHeading.Size = new System.Drawing.Size(970, 45);
            this.lblHeading.Font = new System.Drawing.Font("Segoe UI", 22F, System.Drawing.FontStyle.Bold);
            this.lblHeading.ForeColor = System.Drawing.Color.FromArgb(31, 56, 100);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Text = "Check your upcoming sessions and manage your requests.";
            this.lblDescription.Location = new System.Drawing.Point(58, 173);
            this.lblDescription.Size = new System.Drawing.Size(970, 27);
            this.lblDescription.ForeColor = System.Drawing.Color.FromArgb(83, 104, 132);
            this.lblFilter.Name = "lblFilter";
            this.lblFilter.Text = "Show bookings";
            this.lblFilter.Location = new System.Drawing.Point(747, 205);
            this.lblFilter.Size = new System.Drawing.Size(139, 29);
            this.cmbBookingStatusFilter.Name = "cmbBookingStatusFilter";
            this.cmbBookingStatusFilter.Location = new System.Drawing.Point(894, 204);
            this.cmbBookingStatusFilter.Size = new System.Drawing.Size(149, 33);
            this.cmbBookingStatusFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbBookingStatusFilter.Items.AddRange(new object[] {
                "All bookings", "Scheduled", "Submitted", "Completed", "Cancelled"
            });
            this.pnlListCard.Name = "pnlListCard";
            this.pnlListCard.Location = new System.Drawing.Point(56, 251);
            this.pnlListCard.Size = new System.Drawing.Size(987, 356);
            this.pnlListCard.BackColor = System.Drawing.Color.White;
            this.pnlListCard.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblBookingActionHelp.Name = "lblBookingActionHelp";
            this.lblBookingActionHelp.Text = "Select a booking first. Editing and cancellation require at least 48 hours' notice.";
            this.lblBookingActionHelp.Location = new System.Drawing.Point(56, 614);
            this.lblBookingActionHelp.Size = new System.Drawing.Size(940, 28);
            this.lblBookingActionHelp.ForeColor = System.Drawing.Color.FromArgb(83, 104, 132);
            // 
            // ManageBookings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(237)))), ((int)(((byte)(249)))));
            this.ClientSize = new System.Drawing.Size(1100, 740);
            this.MinimumSize = new System.Drawing.Size(1030, 740);
            this.pnlListCard.Controls.Add(this.dgvBookings);
            this.Controls.Add(this.lblHeading);
            this.Controls.Add(this.lblDescription);
            this.Controls.Add(this.lblFilter);
            this.Controls.Add(this.cmbBookingStatusFilter);
            this.Controls.Add(this.pnlListCard);
            this.Controls.Add(this.lblBookingActionHelp);
            this.Controls.Add(this.lblStudentNo);
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.btnCancelBooking);
            this.Controls.Add(this.btnEditBooking);
            this.Controls.Add(this.panel1);
            this.Name = "ManageBookings";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "EduConnect | My bookings";
            this.Load += new System.EventHandler(this.ManageBookings_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.edu_ConnectDataSet1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tUTORINGREQUESTBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tUTORINGREQUESTBindingSource1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvBookings)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tUTORINGREQUESTBindingSource2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel pnlListCard;
        private System.Windows.Forms.Label lblHeading;
        private System.Windows.Forms.Label lblDescription;
        private System.Windows.Forms.Label lblBookingActionHelp;
        private System.Windows.Forms.Label lblFilter;
        private System.Windows.Forms.ComboBox cmbBookingStatusFilter;
        private System.Windows.Forms.Button btnNavDashboard;
        private System.Windows.Forms.Button btnNavBook;
        private System.Windows.Forms.Button btnNavBack;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnEditBooking;
        private System.Windows.Forms.Button btnCancelBooking;
        private System.Windows.Forms.Button btnBack;
        private Edu_ConnectDataSet1 edu_ConnectDataSet1;
        private System.Windows.Forms.BindingSource tUTORINGREQUESTBindingSource;
        private Edu_ConnectDataSet1TableAdapters.TUTORING_REQUESTTableAdapter tUTORING_REQUESTTableAdapter;
        private System.Windows.Forms.BindingSource tUTORINGREQUESTBindingSource1;
        private System.Windows.Forms.Label lblStudentNo;
        private System.Windows.Forms.DataGridView dgvBookings;
        private System.Windows.Forms.BindingSource tUTORINGREQUESTBindingSource2;
    }
}