namespace NextGen__7
{
    partial class AdminRequests
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.FlowLayoutPanel pnlButtons;
        private NextGen__7.BusyButton btnRefresh;
        private NextGen__7.BusyButton btnVerify;
        private NextGen__7.BusyButton btnBack;
        private System.Windows.Forms.DataGridView requests;

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
            this.pnlButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.btnRefresh = new NextGen__7.BusyButton();
            this.btnVerify = new NextGen__7.BusyButton();
            this.btnBack = new NextGen__7.BusyButton();
            this.requests = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.requests)).BeginInit();
            this.SuspendLayout();

            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Height = 92;
            this.pnlHeader.BackColor = System.Drawing.Color.FromArgb(31, 56, 100);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Text = "Tutoring requests";
            this.lblTitle.Location = new System.Drawing.Point(26, 13);
            this.lblTitle.Size = new System.Drawing.Size(650, 42);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Text = "Review and verify requests from students";
            this.lblSubtitle.Location = new System.Drawing.Point(29, 57);
            this.lblSubtitle.Size = new System.Drawing.Size(700, 25);
            this.lblSubtitle.ForeColor = System.Drawing.Color.Gainsboro;
            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Controls.Add(this.lblSubtitle);

            this.pnlButtons.Name = "pnlButtons";
            this.pnlButtons.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlButtons.Height = 66;
            this.pnlButtons.Padding = new System.Windows.Forms.Padding(20, 10, 10, 4);
            this.pnlButtons.BackColor = System.Drawing.Color.White;
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.Size = new System.Drawing.Size(126, 39);
            this.btnRefresh.Margin = new System.Windows.Forms.Padding(4, 2, 9, 0);
            this.btnRefresh.BackColor = System.Drawing.Color.FromArgb(35, 93, 177);
            this.btnRefresh.ForeColor = System.Drawing.Color.White;
            this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            this.pnlButtons.Controls.Add(this.btnRefresh);
            this.btnVerify.Name = "btnVerify";
            this.btnVerify.Text = "Verify selected request";
            this.btnVerify.Size = new System.Drawing.Size(210, 39);
            this.btnVerify.Margin = new System.Windows.Forms.Padding(4, 2, 9, 0);
            this.btnVerify.BackColor = System.Drawing.Color.FromArgb(35, 93, 177);
            this.btnVerify.ForeColor = System.Drawing.Color.White;
            this.btnVerify.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnVerify.Click += new System.EventHandler(this.btnVerify_Click);
            this.pnlButtons.Controls.Add(this.btnVerify);
            this.btnBack.Name = "btnBack";
            this.btnBack.Text = "Back";
            this.btnBack.Size = new System.Drawing.Size(120, 39);
            this.btnBack.Margin = new System.Windows.Forms.Padding(4, 2, 9, 0);
            this.btnBack.BackColor = System.Drawing.Color.FromArgb(35, 93, 177);
            this.btnBack.ForeColor = System.Drawing.Color.White;
            this.btnBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            this.pnlButtons.Controls.Add(this.btnBack);
            this.requests.Name = "requests";
            this.requests.Dock = System.Windows.Forms.DockStyle.Fill;
            this.requests.ReadOnly = true;
            this.requests.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.requests.MultiSelect = false;
            this.requests.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.requests.AllowUserToAddRows = false;
            this.requests.AllowUserToDeleteRows = false;
            this.requests.BackgroundColor = System.Drawing.Color.White;

            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1080, 715);
            this.MinimumSize = new System.Drawing.Size(900, 650);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Text = "EduConnect | Tutoring requests";
            this.BackColor = System.Drawing.Color.FromArgb(230, 237, 249);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Controls.Add(this.requests);
            this.Controls.Add(this.pnlButtons);
            this.Controls.Add(this.pnlHeader);
            this.Load += new System.EventHandler(this.AdminRequests_Load);
            ((System.ComponentModel.ISupportInitialize)(this.requests)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
