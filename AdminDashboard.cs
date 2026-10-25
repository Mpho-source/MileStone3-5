using System;
using System.Drawing;
using System.Windows.Forms;

namespace NextGen__7
{
    public sealed class AdminDashboard : Form
    {
        private readonly string staffNumber;
        private readonly string staffName;
        private readonly FlowLayoutPanel actions;

        public AdminDashboard(string staffNumber, string staffName)
        {
            this.staffNumber = staffNumber;
            this.staffName = staffName;
            Text = "EduConnect | Administration";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(950, 680);
            Size = new Size(1140, 760);
            BackColor = EduConnectTheme.Background;
            Font = new Font("Segoe UI", 10);

            var header = new Panel { Dock = DockStyle.Top, Height = 108, BackColor = EduConnectTheme.Navy };
            var brand = new Label { Text = "EduConnect", ForeColor = Color.White,
                Font = new Font("Segoe UI", 23, FontStyle.Bold),
                Location = new Point(34, 15), Size = new Size(340, 45) };
            var strap = new Label { Text = "UNIVERSITY TUTORING PORTAL",
                ForeColor = Color.FromArgb(208, 223, 246), Font = new Font("Segoe UI", 10),
                Location = new Point(38, 65), Size = new Size(410, 27) };
            header.Controls.Add(brand);
            header.Controls.Add(strap);
            Controls.Add(header);

            var profile = new Panel { Dock = DockStyle.Top, Height = 124, BackColor = Color.White,
                Padding = new Padding(30, 14, 24, 12) };
            var title = new Label { Text = "Administration dashboard",
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = EduConnectTheme.Navy, Dock = DockStyle.Top, Height = 52 };
            var identity = new Label { Text = staffName + "   |   " + staffNumber,
                ForeColor = EduConnectTheme.Muted, Font = new Font("Segoe UI", 12),
                Dock = DockStyle.Fill };
            profile.Controls.Add(identity);
            profile.Controls.Add(title);
            Controls.Add(profile);

            actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true,
                WrapContents = true, FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(33, 28, 20, 30), BackColor = EduConnectTheme.Background };
            AddTile("Manage tutors", "Add new tutors, edit tutor profiles and review the tutor table.",
                () => OpenForm(new ManagaeTutors()));
            AddTile("Tutoring requests", "Review submitted requests and verify them.",
                () => OpenForm(new AdminRequests()));
            AddTile("Tutoring reports", "View scheduled and completed tutoring sessions by tutor.",
                () => OpenForm(new Reports()));
            AddTile("Sign out", "Leave the administration dashboard and return to the login screen.",
                () => ConfirmLogout());
            Controls.Add(actions);
            actions.BringToFront();
        }

        private void AddTile(string heading, string detail, Action action)
        {
            var tile = new Panel { Size = new Size(475, 164), BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(11) };
            var name = new Label { Text = heading, ForeColor = EduConnectTheme.Navy,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(18, 14), Size = new Size(410, 35) };
            var summary = new Label { Text = detail, ForeColor = EduConnectTheme.Muted,
                Font = new Font("Segoe UI", 10), Location = new Point(19, 58),
                Size = new Size(425, 48) };
            var button = new Button { Text = "Open", Location = new Point(19, 115),
                Size = new Size(142, 38) };
            button.Click += (s, e) => action();
            EduConnectTheme.StyleButton(button);
            tile.Controls.Add(name);
            tile.Controls.Add(summary);
            tile.Controls.Add(button);
            actions.Controls.Add(tile);
        }

        private void OpenForm(Form form)
        {
            using (form) form.ShowDialog(this);
        }

        private void ConfirmLogout()
        {
            if (MessageBox.Show("Sign out of EduConnect?", "Confirm sign out",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                Close();
        }
    }
}
