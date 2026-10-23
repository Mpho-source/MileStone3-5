using System;
using System.Drawing;
using System.Windows.Forms;

namespace NextGen__7
{
    public static class EduConnectLayout
    {
        private static readonly Font Heading = new Font("Segoe UI", 22F, FontStyle.Bold);
        private static readonly Font Subheading = new Font("Segoe UI", 13F, FontStyle.Bold);
        private static readonly Font Body = new Font("Segoe UI", 10F);
        private static readonly Font Small = new Font("Segoe UI", 9.5F);

        private static Control Find(Form form, string name)
        {
            Control[] matches = form.Controls.Find(name, true);
            if (matches.Length == 0)
                throw new InvalidOperationException("Missing form control: " + name);
            return matches[0];
        }

        private static T As<T>(Form form, string name) where T : Control
        {
            return (T)Find(form, name);
        }

        private static void Set(Control c, Control parent, int x, int y, int width, int height)
        {
            if (c.Parent != parent) parent.Controls.Add(c);
            c.AutoSize = false;
            c.Bounds = new Rectangle(x, y, width, height);
            c.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            c.Visible = true;
        }

        private static Label MakeLabel(string text, int size, bool bold, Color color)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = color,
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private static Panel Card(Form form)
        {
            Panel p = new Panel
            {
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(16)
            };
            form.Controls.Add(p);
            return p;
        }

        private static void AddNav(Form form, string active,
            Action dashboard, Action booking, Action manage, Action logout)
        {
            Panel bar = new Panel
            {
                Name = "webNavigation",
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = EduConnectTheme.Navy
            };

            Label brand = MakeLabel("EduConnect", 20, true, Color.White);
            brand.Bounds = new Rectangle(30, 13, 255, 39);
            bar.Controls.Add(brand);

            Label caption = MakeLabel("UNIVERSITY TUTORING PORTAL", 9, false,
                Color.FromArgb(207, 224, 245));
            caption.Bounds = new Rectangle(32, 51, 270, 22);
            bar.Controls.Add(caption);

            FlowLayoutPanel links = new FlowLayoutPanel
            {
                Name = "navLinks",
                Size = new Size(592, 54),
                Location = new Point(Math.Max(355, form.ClientSize.Width - 615), 18),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            NavButton(links, "Dashboard", active == "Dashboard", dashboard, 125);
            NavButton(links, "Book a session", active == "Book", booking, 157);
            NavButton(links, "My bookings", active == "Manage", manage, 139);
            NavButton(links, logout != null ? "Sign out" : "Back", false,
                logout ?? dashboard, 117);

            bar.Controls.Add(links);
            form.Controls.Add(bar);
            bar.BringToFront();
        }

        private static void NavButton(
            FlowLayoutPanel host, string label, bool active, Action action, int width)
        {
            Button button = new Button
            {
                Text = label,
                Name = "nav" + label.Replace(" ", ""),
                Size = new Size(width, 46),
                Margin = new Padding(4, 2, 4, 2),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F,
                    active ? FontStyle.Bold : FontStyle.Regular),
                BackColor = active ? Color.FromArgb(74, 122, 191) : EduConnectTheme.Navy,
                ForeColor = Color.White,
                Cursor = action == null ? Cursors.Default : Cursors.Hand,
                Enabled = true,
                AccessibleName = label,
                TabStop = action != null && !active,
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderSize = active ? 1 : 0;
            button.FlatAppearance.BorderColor = Color.FromArgb(159, 197, 243);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 106, 173);
            if (action != null && !active) button.Click += (s, e) => action();
            host.Controls.Add(button);
        }

        private static void NavOnlyLogin(Form form)
        {
            Panel bar = new Panel
            {
                Name = "webNavigation",
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = EduConnectTheme.Navy
            };
            Label brand = MakeLabel("EduConnect", 20, true, Color.White);
            brand.Bounds = new Rectangle(35, 12, 300, 43);
            bar.Controls.Add(brand);
            Label caption = MakeLabel("UNIVERSITY TUTORING PORTAL", 9, false,
                Color.FromArgb(207, 224, 245));
            caption.Bounds = new Rectangle(38, 53, 320, 22);
            bar.Controls.Add(caption);
            Label signIn = MakeLabel("Student sign in", 11, true, Color.White);
            signIn.TextAlign = ContentAlignment.MiddleRight;
            signIn.Bounds = new Rectangle(710, 25, 245, 38);
            signIn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            bar.Controls.Add(signIn);
            form.Controls.Add(bar);
            bar.BringToFront();
        }

        public static void Login(Login form)
        {
            form.SuspendLayout();
            form.Text = "EduConnect | Student sign in";
            form.ClientSize = new Size(1040, 700);
            form.MinimumSize = new Size(1040, 700);
            form.BackColor = EduConnectTheme.Background;
            NavOnlyLogin(form);

            Panel hero = new Panel
            {
                BackColor = EduConnectTheme.Navy,
                Name = "loginHero"
            };
            form.Controls.Add(hero);

            Label heroTitle = MakeLabel("Your learning.\nYour schedule.", 25, true, Color.White);
            heroTitle.Bounds = new Rectangle(35, 104, 420, 116);
            hero.Controls.Add(heroTitle);

            Label heroSubtitle = MakeLabel(
                "Find academic support, schedule tutoring and manage sessions in one place.",
                11, false, Color.FromArgb(220, 233, 252));
            heroSubtitle.Bounds = new Rectangle(35, 234, 368, 90);
            hero.Controls.Add(heroSubtitle);

            Label heroDetails = MakeLabel(
                "BOOK SESSIONS    •    VIEW REQUESTS    •    STAY ON TRACK",
                9, true, Color.White);
            heroDetails.Bounds = new Rectangle(35, 367, 410, 36);
            hero.Controls.Add(heroDetails);

            Panel card = Card(form);
            Label pageTitle = As<Label>(form, "label1");
            Label welcome = As<Label>(form, "label3");
            Label caption = As<Label>(form, "label2");
            Label description = As<Label>(form, "label4");
            Label studentLabel = As<Label>(form, "label5");
            Label passwordLabel = As<Label>(form, "label6");
            TextBox student = As<TextBox>(form, "txtStudentNo");
            TextBox password = As<TextBox>(form, "txtPassword");
            Button submit = As<Button>(form, "btnLogin");
            Find(form, "pictureBox1").Visible = false;
            Find(form, "panelSeparator").Visible = false;

            Set(caption, card, 35, 22, 340, 22);
            caption.Text = "STUDENT PORTAL";
            caption.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            caption.ForeColor = EduConnectTheme.Blue;

            Set(pageTitle, card, 35, 51, 360, 49);
            pageTitle.Text = "Welcome back";
            pageTitle.Font = Heading;
            pageTitle.ForeColor = EduConnectTheme.Navy;

            Set(welcome, card, 35, 101, 370, 27);
            welcome.Text = "Sign in to your account";
            welcome.Font = Subheading;
            welcome.ForeColor = EduConnectTheme.Ink;

            Set(description, card, 35, 133, 368, 39);
            description.Text = "Enter your student number and password to continue.";
            description.Font = Small;
            description.ForeColor = EduConnectTheme.Muted;

            Set(studentLabel, card, 35, 188, 365, 27);
            studentLabel.Text = "Student number";
            studentLabel.ForeColor = EduConnectTheme.Navy;
            studentLabel.TextAlign = ContentAlignment.MiddleLeft;

            Set(student, card, 35, 220, 366, 38);
            student.Multiline = false;
            student.TabIndex = 0;

            Set(passwordLabel, card, 35, 277, 365, 27);
            passwordLabel.Text = "Password";
            passwordLabel.ForeColor = EduConnectTheme.Navy;
            passwordLabel.TextAlign = ContentAlignment.MiddleLeft;

            Set(password, card, 35, 310, 366, 38);
            password.TabIndex = 1;
            Set(submit, card, 35, 385, 366, 48);
            submit.Text = "Sign in";
            submit.TabIndex = 2;

            Action layout = () =>
            {
                int span = Math.Min(1150, form.ClientSize.Width - 104);
                int left = (form.ClientSize.Width - span) / 2;
                int heroWidth = Math.Max(390, (span - 26) / 2);
                hero.Bounds = new Rectangle(left, 145, heroWidth, 495);
                card.Bounds = new Rectangle(left + heroWidth + 26, 145,
                    span - heroWidth - 26, 495);
                int inputWidth = Math.Max(310, card.Width - 70);
                student.Width = inputWidth;
                password.Width = inputWidth;
                submit.Width = inputWidth;
            };
            form.Resize += (s, e) => layout();
            layout();
            form.ResumeLayout(false);
        }

        private static void ClickThrough(RoundedPanel panel)
        {
            foreach (Control child in panel.Controls)
            {
                child.Cursor = Cursors.Hand;
                child.Click += (s, e) => panel.Activate();
            }
        }

        public static void Dashboard(
            StudentDashboard form, Action toBook, Action toManage, Action logout)
        {
            form.SuspendLayout();
            form.Text = "EduConnect | Dashboard";
            form.ClientSize = new Size(1090, 710);
            form.MinimumSize = new Size(1010, 670);
            Find(form, "panel1").Visible = false;
            AddNav(form, "Dashboard", null, toBook, toManage, logout);

            Label title = As<Label>(form, "label3");
            Label subtitle = As<Label>(form, "label4");
            Set(title, form, 58, 134, 810, 50);
            title.Font = Heading;
            title.ForeColor = EduConnectTheme.Navy;
            title.Text = "Welcome to your student dashboard";

            Set(subtitle, form, 60, 189, 720, 27);
            subtitle.Font = Body;
            subtitle.ForeColor = EduConnectTheme.Muted;
            subtitle.Text = "What would you like to do today?";

            RoundedPanel book = As<RoundedPanel>(form, "pnlBook");
            RoundedPanel manage = As<RoundedPanel>(form, "pnlManageBooking");
            RoundedPanel oldLogout = As<RoundedPanel>(form, "pnlLogout");
            oldLogout.Visible = false;

            ConfigureTile(book, "label5", "label6", "label7", "pictureBox2",
                "Book a tutoring session",
                "Find a qualified tutor and reserve your preferred time.");
            ConfigureTile(manage, "label8", "label9", "label10", "pictureBox3",
                "Manage my bookings",
                "Review your sessions, make changes or cancel a request.");

            ClickThrough(book);
            ClickThrough(manage);

            Label policy = MakeLabel(
                "BOOKING POLICY  •  Changes and cancellations require at least 48 hours' notice.",
                10, true, EduConnectTheme.Navy);
            form.Controls.Add(policy);

            Label footer = MakeLabel(
                "EduConnect  /  Student services  /  Dashboard",
                9, false, EduConnectTheme.Muted);
            form.Controls.Add(footer);

            Action layout = () =>
            {
                int span = Math.Min(1120, form.ClientSize.Width - 112);
                int left = (form.ClientSize.Width - span) / 2;
                int tileWidth = (span - 24) / 2;
                title.Left = left;
                subtitle.Left = left + 2;
                book.Bounds = new Rectangle(left, 257, tileWidth, 236);
                manage.Bounds = new Rectangle(left + tileWidth + 24, 257,
                    tileWidth, 236);
                PositionTile(book);
                PositionTile(manage);
                policy.Bounds = new Rectangle(left, 537, span, 35);
                footer.Bounds = new Rectangle(left, 616, span, 30);
            };
            form.Resize += (s, e) => layout();
            layout();
            form.ResumeLayout(false);
        }

        private static void ConfigureTile(
            RoundedPanel panel, string titleName, string detailName,
            string arrowName, string imageName, string title, string detail)
        {
            panel.NormalColor = EduConnectTheme.SoftBlue;
            panel.HoverColor = EduConnectTheme.HoverBlue;
            panel.BorderColor = EduConnectTheme.Edge;
            panel.CornerRadius = 18;
            Label heading = (Label)panel.Controls[titleName];
            Label description = (Label)panel.Controls[detailName];
            Label arrow = (Label)panel.Controls[arrowName];
            PictureBox image = (PictureBox)panel.Controls[imageName];
            heading.Text = title;
            heading.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            heading.ForeColor = EduConnectTheme.Navy;
            description.Text = detail;
            description.Font = Body;
            description.ForeColor = EduConnectTheme.Muted;
            arrow.Text = "›";
            arrow.Font = new Font("Segoe UI", 26F, FontStyle.Bold);
            arrow.ForeColor = EduConnectTheme.Blue;
            image.SizeMode = PictureBoxSizeMode.Zoom;
        }

        private static void PositionTile(RoundedPanel tile)
        {
            foreach (Control c in tile.Controls)
            {
                if (c is PictureBox)
                    Set(c, tile, 26, 34, 65, 65);
                else if (c is Label && c.Font.Size >= 23)
                    Set(c, tile, tile.Width - 63, 87, 35, 45);
                else if (c is Label && c.Font.Bold)
                    Set(c, tile, 27, 117, tile.Width - 80, 44);
                else if (c is Label)
                    Set(c, tile, 28, 162, tile.Width - 75, 49);
            }
        }

        private static void FormHeading(Form form, string title, string subtitle,
            out Label heading, out Label description)
        {
            heading = MakeLabel(title, 22, true, EduConnectTheme.Navy);
            description = MakeLabel(subtitle, 10, false, EduConnectTheme.Muted);
            form.Controls.Add(heading);
            form.Controls.Add(description);
        }

        public static void Booking(BookingTutoringSession form,
            Action dashboard, Action manage)
        {
            form.SuspendLayout();
            form.Text = "EduConnect | Book a session";
            form.ClientSize = new Size(1080, 740);
            form.MinimumSize = new Size(1030, 800);
            Find(form, "pnlHeader").Visible = false;
            AddNav(form, "Book", dashboard, null, manage, null);

            Label heading;
            Label description;
            FormHeading(form, "Book a tutoring session",
                "Complete the details below to request academic support.",
                out heading, out description);

            Panel details = Card(form);
            Panel schedule = Card(form);

            Label section1 = As<Label>(form, "label1");
            Label studentLabel = As<Label>(form, "label2");
            Label courseLabel = As<Label>(form, "label3");
            Label supportLabel = As<Label>(form, "label4");
            RichTextBox student = As<RichTextBox>(form, "txtStudentID");
            ComboBox course = As<ComboBox>(form, "cmbCourse");
            ComboBox support = As<ComboBox>(form, "cmbSupportType");
            Label chosen = As<Label>(form, "lblSelectedCourse");
            Label section2 = As<Label>(form, "label5");
            Label dateLabel = As<Label>(form, "label6");
            Label timeLabel = As<Label>(form, "label7");
            DateTimePicker date = As<DateTimePicker>(form, "dtpSessionDate");
            ComboBox time = As<ComboBox>(form, "cmbSessionTime");
            Button submit = As<Button>(form, "btnBookSession");
            Button clear = As<Button>(form, "btnClear");
            Button back = As<Button>(form, "btnBack");

            Set(section1, details, 26, 20, 600, 35);
            section1.Text = "1   Course and support details";
            section1.Font = Subheading;
            section1.ForeColor = EduConnectTheme.Navy;

            Set(studentLabel, details, 28, 73, 230, 24);
            studentLabel.Text = "Student number";
            Set(student, details, 28, 107, 203, 39);
            student.ReadOnly = true;

            Set(courseLabel, details, 260, 73, 350, 24);
            courseLabel.Text = "Course";
            Set(course, details, 260, 107, 375, 37);

            Set(supportLabel, details, 663, 73, 232, 24);
            supportLabel.Text = "Type of support";
            Set(support, details, 663, 107, 230, 37);

            Set(chosen, details, 260, 154, 390, 29);
            chosen.ForeColor = EduConnectTheme.Navy;
            chosen.BackColor = Color.FromArgb(225, 237, 252);
            chosen.TextAlign = ContentAlignment.MiddleLeft;

            Set(section2, schedule, 26, 18, 600, 34);
            section2.Text = "2   Choose a date and time";
            section2.Font = Subheading;
            section2.ForeColor = EduConnectTheme.Navy;

            Set(dateLabel, schedule, 28, 67, 295, 24);
            dateLabel.Text = "Preferred date";
            Set(date, schedule, 28, 101, 308, 38);

            Set(timeLabel, schedule, 366, 67, 290, 24);
            timeLabel.Text = "Preferred time";
            Set(time, schedule, 366, 101, 290, 37);

            Label rule = MakeLabel(
                "Need to edit later? Changes and cancellations close 48 hours before the session.",
                9, false, EduConnectTheme.Muted);
            schedule.Controls.Add(rule);
            rule.Bounds = new Rectangle(28, 151, 860, 32);

            Label notice = MakeLabel(
                "Tutor matching happens automatically when you submit your request.",
                9, false, EduConnectTheme.Muted);
            form.Controls.Add(notice);

            Action layout = () =>
            {
                int span = Math.Min(1100, form.ClientSize.Width - 112);
                int left = (form.ClientSize.Width - span) / 2;
                heading.Bounds = new Rectangle(left, 126, span, 45);
                description.Bounds = new Rectangle(left + 2, 170, span, 25);
                details.Bounds = new Rectangle(left, 216, span, 211);
                schedule.Bounds = new Rectangle(left, 448, span, 204);
                int supportLeft = span - 258;
                Set(supportLabel, details, supportLeft, 73, 228, 24);
                Set(support, details, supportLeft, 107, 229, 37);
                Set(course, details, 260, 107, supportLeft - 288, 37);
                chosen.Width = Math.Min(450, supportLeft - 288);
                submit.Bounds = new Rectangle(left, 668, 244, 47);
                clear.Bounds = new Rectangle(left + 260, 668, 158, 47);
                back.Bounds = new Rectangle(left + span - 156, 668, 156, 47);
                notice.Bounds = new Rectangle(left + 433, 672,
                    Math.Max(195, span - 620), 42);
                rule.Width = span - 56;
            };
            form.Resize += (s, e) => layout();
            layout();
            form.ResumeLayout(false);
        }

        public static ComboBox Manage(ManageBookings form,
            Action dashboard, Action booking)
        {
            form.SuspendLayout();
            form.Text = "EduConnect | My bookings";
            form.ClientSize = new Size(1100, 740);
            form.MinimumSize = new Size(1030, 740);
            Find(form, "panel1").Visible = false;
            AddNav(form, "Manage", dashboard, booking, null, null);

            Label heading;
            Label description;
            FormHeading(form, "My tutoring bookings",
                "Check your upcoming sessions and manage your requests.",
                out heading, out description);

            Label studentNo = As<Label>(form, "lblStudentNo");
            studentNo.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            studentNo.ForeColor = EduConnectTheme.Navy;
            Panel listCard = Card(form);

            Label filterLabel = MakeLabel("Show bookings", 10, true, EduConnectTheme.Navy);
            form.Controls.Add(filterLabel);
            ComboBox filter = new ComboBox
            {
                Name = "cmbBookingStatusFilter",
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Standard,
                Font = Body,
                BackColor = Color.White,
                ForeColor = EduConnectTheme.Ink
            };
            filter.Items.AddRange(new object[]
                { "All bookings", "Scheduled", "Submitted", "Completed", "Cancelled" });
            form.Controls.Add(filter);

            DataGridView grid = As<DataGridView>(form, "dgvBookings");
            EduConnectTheme.StyleGrid(grid);
            grid.Parent = listCard;
            grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                AnchorStyles.Left | AnchorStyles.Right;

            Button edit = As<Button>(form, "btnEditBooking");
            Button cancel = As<Button>(form, "btnCancelBooking");
            Button back = As<Button>(form, "btnBack");
            back.Text = "Back to dashboard";

            Label guidance = MakeLabel(
                "Select a booking first. Editing and cancellation require at least 48 hours' notice.",
                9, false, EduConnectTheme.Muted);
            form.Controls.Add(guidance);

            Action layout = () =>
            {
                int span = Math.Min(1140, form.ClientSize.Width - 112);
                int left = (form.ClientSize.Width - span) / 2;
                heading.Bounds = new Rectangle(left, 126, span, 45);
                description.Bounds = new Rectangle(left + 1, 173, span, 26);
                studentNo.Bounds = new Rectangle(left, 204, 380, 30);
                filterLabel.Bounds = new Rectangle(left + span - 300, 205, 135, 29);
                filter.Bounds = new Rectangle(left + span - 161, 204, 161, 33);
                int height = Math.Max(310, form.ClientSize.Height - 410);
                listCard.Bounds = new Rectangle(left, 251, span, height);
                grid.Bounds = new Rectangle(15, 15,
                    listCard.Width - 30, listCard.Height - 30);
                guidance.Bounds = new Rectangle(left, listCard.Bottom + 16,
                    span, 28);
                edit.Bounds = new Rectangle(left, listCard.Bottom + 59, 175, 46);
                cancel.Bounds = new Rectangle(left + 192, listCard.Bottom + 59, 193, 46);
                back.Bounds = new Rectangle(left + span - 198,
                    listCard.Bottom + 59, 198, 46);
            };
            form.Resize += (s, e) => layout();
            layout();
            form.ResumeLayout(false);
            filter.SelectedIndex = 0;
            return filter;
        }

        public static void Edit(EditBooking form)
        {
            form.SuspendLayout();
            form.Text = "EduConnect | Edit booking";
            form.ClientSize = new Size(825, 677);
            form.MinimumSize = new Size(825, 677);
            form.BackColor = EduConnectTheme.Background;
            form.StartPosition = FormStartPosition.CenterParent;
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.MaximizeBox = false;

            Panel banner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 89,
                BackColor = EduConnectTheme.Navy
            };
            Label brand = MakeLabel("EduConnect   /   My bookings   /   Edit", 14,
                true, Color.White);
            brand.Bounds = new Rectangle(30, 19, 735, 45);
            banner.Controls.Add(brand);
            form.Controls.Add(banner);
            banner.BringToFront();

            Label heading = (Label)form.Controls.Find("lblEditHeading", true)[0];
            Panel card = Card(form);
            Label courseLabel = As<Label>(form, "lblEditCourse");
            Label supportLabel = As<Label>(form, "lblEditSupport");
            Label dateLabel = As<Label>(form, "lblEditDate");
            Label timeLabel = As<Label>(form, "lblEditTime");
            ComboBox course = As<ComboBox>(form, "cmbCourse");
            ComboBox support = As<ComboBox>(form, "cmbSupportType");
            DateTimePicker date = As<DateTimePicker>(form, "dtpSessionDate");
            ComboBox time = As<ComboBox>(form, "cmbSessionTime");
            Button save = As<Button>(form, "btnSaveChanges");
            Button back = As<Button>(form, "btnBack");

            Set(heading, form, 48, 119, 720, 46);
            heading.Text = "Edit your tutoring booking";
            heading.Font = Heading;
            heading.ForeColor = EduConnectTheme.Navy;

            Set(courseLabel, card, 30, 22, 630, 29);
            courseLabel.Text = "Course";
            Set(course, card, 30, 56, 661, 39);
            Set(supportLabel, card, 30, 111, 630, 29);
            supportLabel.Text = "Type of support";
            Set(support, card, 30, 145, 661, 39);
            Set(dateLabel, card, 30, 200, 320, 29);
            dateLabel.Text = "Preferred date";
            Set(date, card, 30, 234, 320, 39);
            Set(timeLabel, card, 373, 200, 310, 29);
            timeLabel.Text = "Preferred time";
            Set(time, card, 373, 234, 318, 39);
            Label policy = MakeLabel(
                "Changes and cancellations must be made at least 48 hours before the original session.",
                9, false, EduConnectTheme.Muted);
            policy.Bounds = new Rectangle(30, 294, 660, 39);
            card.Controls.Add(policy);

            card.Bounds = new Rectangle(47, 183, 728, 355);
            Set(save, form, 47, 563, 234, 47);
            Set(back, form, 295, 563, 176, 47);
            form.ResumeLayout(false);
        }
    }
}
