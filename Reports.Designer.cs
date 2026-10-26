namespace NextGen__7
{
    partial class Reports
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.FlowLayoutPanel pnlFilters;
        private System.Windows.Forms.Label lblFrom;
        private System.Windows.Forms.Label lblTo;
        private System.Windows.Forms.DateTimePicker fromDate;
        private System.Windows.Forms.DateTimePicker toDate;
        private System.Windows.Forms.Button btnGenerate;
        private System.Windows.Forms.Button btnPrint;
        private System.Windows.Forms.Button btnBack;
        private System.Windows.Forms.DataGridView grid;
        private System.Drawing.Printing.PrintDocument printer;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.pnlFilters = new System.Windows.Forms.FlowLayoutPanel();
            this.lblFrom = new System.Windows.Forms.Label();
            this.lblTo = new System.Windows.Forms.Label();
            this.fromDate = new System.Windows.Forms.DateTimePicker();
            this.toDate = new System.Windows.Forms.DateTimePicker();
            this.btnGenerate = new System.Windows.Forms.Button();
            this.btnPrint = new System.Windows.Forms.Button();
            this.btnBack = new System.Windows.Forms.Button();
            this.grid = new System.Windows.Forms.DataGridView();
            this.printer = new System.Drawing.Printing.PrintDocument();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.SuspendLayout();

            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Height = 106;
            this.pnlHeader.BackColor = System.Drawing.Color.FromArgb(31, 56, 100);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Text = "Tutor session report";
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 21F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(28, 14);
            this.lblTitle.Size = new System.Drawing.Size(660, 45);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Text = "Scheduled and completed sessions per tutor for a selected period";
            this.lblSubtitle.ForeColor = System.Drawing.Color.Gainsboro;
            this.lblSubtitle.Location = new System.Drawing.Point(30, 68);
            this.lblSubtitle.Size = new System.Drawing.Size(750, 26);
            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Controls.Add(this.lblSubtitle);

            this.pnlFilters.Name = "pnlFilters";
            this.pnlFilters.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlFilters.Height = 80;
            this.pnlFilters.Padding = new System.Windows.Forms.Padding(22, 16, 10, 6);
            this.pnlFilters.BackColor = System.Drawing.Color.White;
            this.lblFrom.Name = "lblFrom";
            this.lblFrom.Text = "From";
            this.lblFrom.AutoSize = true;
            this.lblFrom.Padding = new System.Windows.Forms.Padding(0, 9, 0, 0);
            this.lblTo.Name = "lblTo";
            this.lblTo.Text = "To";
            this.lblTo.AutoSize = true;
            this.lblTo.Padding = new System.Windows.Forms.Padding(14, 9, 0, 0);
            this.fromDate.Name = "fromDate";
            this.fromDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.fromDate.Width = 145;
            this.toDate.Name = "toDate";
            this.toDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.toDate.Width = 145;
            this.btnGenerate.Name = "btnGenerate";
            this.btnGenerate.Text = "Generate report";
            this.btnGenerate.Size = new System.Drawing.Size(170, 40);
            this.btnGenerate.Margin = new System.Windows.Forms.Padding(12, 0, 2, 0);
            this.btnGenerate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGenerate.ForeColor = System.Drawing.Color.White;
            this.btnGenerate.BackColor = System.Drawing.Color.FromArgb(35, 93, 177);
            this.btnGenerate.Click += new System.EventHandler(this.btnGenerate_Click);
            this.btnPrint.Name = "btnPrint";
            this.btnPrint.Text = "Print preview";
            this.btnPrint.Size = new System.Drawing.Size(155, 40);
            this.btnPrint.Margin = new System.Windows.Forms.Padding(12, 0, 2, 0);
            this.btnPrint.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPrint.ForeColor = System.Drawing.Color.White;
            this.btnPrint.BackColor = System.Drawing.Color.FromArgb(35, 93, 177);
            this.btnPrint.Click += new System.EventHandler(this.btnPrint_Click);
            this.btnBack.Name = "btnBack";
            this.btnBack.Text = "Back";
            this.btnBack.Size = new System.Drawing.Size(110, 40);
            this.btnBack.Margin = new System.Windows.Forms.Padding(12, 0, 2, 0);
            this.btnBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBack.ForeColor = System.Drawing.Color.White;
            this.btnBack.BackColor = System.Drawing.Color.FromArgb(35, 93, 177);
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            this.pnlFilters.Controls.Add(this.lblFrom);
            this.pnlFilters.Controls.Add(this.fromDate);
            this.pnlFilters.Controls.Add(this.lblTo);
            this.pnlFilters.Controls.Add(this.toDate);
            this.pnlFilters.Controls.Add(this.btnGenerate);
            this.pnlFilters.Controls.Add(this.btnPrint);
            this.pnlFilters.Controls.Add(this.btnBack);

            this.grid.Name = "grid";
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.ReadOnly = true;
            this.grid.BackgroundColor = System.Drawing.Color.White;
            this.grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.printer.DocumentName = "EduConnect Tutor Session Report";
            this.printer.PrintPage += new System.Drawing.Printing.PrintPageEventHandler(this.PrintPage);

            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1100, 714);
            this.MinimumSize = new System.Drawing.Size(900, 650);
            this.BackColor = System.Drawing.Color.FromArgb(230, 237, 249);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "EduConnect | Tutor session report";
            this.Controls.Add(this.grid);
            this.Controls.Add(this.pnlFilters);
            this.Controls.Add(this.pnlHeader);
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
