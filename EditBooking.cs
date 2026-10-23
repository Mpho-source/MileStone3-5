
using System;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace NextGen__7
{
    public class EditBooking : Form
    {
        private readonly int studentID;
        private readonly int requestID;
        private readonly string connectionString;

        private ComboBox cmbCourse;
        private ComboBox cmbSupportType;
        private ComboBox cmbSessionTime;
        private DateTimePicker dtpSessionDate;
        private Button btnSaveChanges;
        private Button btnBack;

        public EditBooking(
            int studentID,
            int requestID,
            string connectionString)
        {
            this.studentID = studentID;
            this.requestID = requestID;
            this.connectionString = connectionString;

            BuildForm();
            Load += EditBooking_Load;
        }

        private Label MakeLabel(string text, int top)
        {
            return new Label
            {
                Text = text,
                Left = 35,
                Top = top,
                Width = 420,
                Height = 22,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
        }

        private ComboBox MakeCombo(int top)
        {
            return new ComboBox
            {
                Left = 35,
                Top = top,
                Width = 420,
                Height = 30,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10)
            };
        }

        private void BuildForm()
        {
            Text = "Edit Tutoring Booking";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(490, 540);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(230, 237, 249);

            Label heading = new Label
            {
                Text = "Edit Tutoring Booking",
                Left = 35,
                Top = 20,
                Width = 420,
                Height = 40,
                Font = new Font(
                    "Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 56, 100)
            };

            Controls.Add(heading);

            Controls.Add(MakeLabel("Course", 80));
            cmbCourse = MakeCombo(105);
            Controls.Add(cmbCourse);

            Controls.Add(MakeLabel("Support Type", 155));
            cmbSupportType = MakeCombo(180);
            Controls.Add(cmbSupportType);

            Controls.Add(MakeLabel("Session Date", 230));

            dtpSessionDate = new DateTimePicker
            {
                Left = 35,
                Top = 255,
                Width = 420,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd MMM yyyy",
                MinDate = DateTime.Today,
                Font = new Font("Segoe UI", 10)
            };

            Controls.Add(dtpSessionDate);

            Controls.Add(MakeLabel("Session Time", 305));
            cmbSessionTime = MakeCombo(330);

            cmbSessionTime.Items.AddRange(new object[]
            {
                "09:00 - 10:00",
                "10:15 - 11:15",
                "11:30 - 12:30",
                "13:00 - 14:00",
                "14:15 - 15:15",
                "15:30 - 16:30",
                "16:45 - 17:45"
            });

            Controls.Add(cmbSessionTime);

            btnSaveChanges = new Button
            {
                Text = "Save Changes",
                Left = 35,
                Top = 415,
                Width = 200,
                Height = 45,
                Font = new Font(
                    "Segoe UI", 10, FontStyle.Bold)
            };

            btnBack = new Button
            {
                Text = "Cancel",
                Left = 255,
                Top = 415,
                Width = 200,
                Height = 45,
                Font = new Font(
                    "Segoe UI", 10, FontStyle.Bold)
            };

            btnSaveChanges.Click += btnSaveChanges_Click;
            btnBack.Click += (sender, e) => Close();

            Controls.Add(btnSaveChanges);
            Controls.Add(btnBack);

            AcceptButton = btnSaveChanges;
            CancelButton = btnBack;
        }

        private void EditBooking_Load(object sender, EventArgs e)
        {
            try
            {
                LoadCourses();
                LoadBookingDetails();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to open this booking.\n\n" + ex.Message,
                    "Edit Booking Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                BeginInvoke(new Action(Close));
            }
        }

        private void LoadCourses()
        {
            DataTable courses = new DataTable();

            using (OleDbConnection connection =
                new OleDbConnection(connectionString))
            using (OleDbDataAdapter adapter = new OleDbDataAdapter(
                "SELECT [CourseID], [CourseCode], [CourseName] " +
                "FROM [COURSE] ORDER BY [CourseCode]",
                connection))
            {
                adapter.Fill(courses);
            }

            courses.Columns.Add("DisplayName", typeof(string));

            foreach (DataRow row in courses.Rows)
            {
                row["DisplayName"] =
                    Convert.ToString(row["CourseCode"]) +
                    " - " +
                    Convert.ToString(row["CourseName"]);
            }

            cmbCourse.DisplayMember = "DisplayName";
            cmbCourse.ValueMember = "CourseID";
            cmbCourse.DataSource = courses;
            cmbCourse.SelectedIndex = -1;
        }

        private void LoadBookingDetails()
        {
            using (OleDbConnection connection =
                new OleDbConnection(connectionString))
            {
                connection.Open();

                DataRow request = BookingData.GetRequest(
                    connection, null, studentID, requestID);

                BookingData.EnsureEditable(request);

                int courseID = Convert.ToInt32(request["CourseID"]);
                cmbCourse.SelectedValue = courseID;

                if (cmbCourse.SelectedIndex < 0)
                    throw new InvalidOperationException(
                        "The original course no longer exists.");

                string supportType =
                    Convert.ToString(request["SupportType"]).Trim();

                cmbSupportType.Items.AddRange(new object[]
                {
                    "Individual tutoring",
                    "Group tutoring",
                    "Exam preparation",
                    "Assignment assistance"
                });

                if (!string.IsNullOrWhiteSpace(supportType) &&
                    !cmbSupportType.Items.Contains(supportType))
                {
                    cmbSupportType.Items.Add(supportType);
                }

                cmbSupportType.SelectedItem = supportType;

                dtpSessionDate.Value =
                    Convert.ToDateTime(request["PreferredDate"]).Date;

                TimeSpan start =
                    BookingData.ReadTime(request["PreferredStartTime"]);

                TimeSpan end =
                    BookingData.ReadTime(request["PreferredEndTime"]);

                string slot =
                    start.ToString(@"hh\:mm") +
                    " - " +
                    end.ToString(@"hh\:mm");

                if (!cmbSessionTime.Items.Contains(slot))
                    cmbSessionTime.Items.Add(slot);

                cmbSessionTime.SelectedItem = slot;
            }
        }

        private void btnSaveChanges_Click(object sender, EventArgs e)
        {
            if (cmbCourse.SelectedIndex < 0 ||
                cmbSupportType.SelectedIndex < 0 ||
                cmbSessionTime.SelectedIndex < 0)
            {
                MessageBox.Show(
                    "Please complete all booking fields.",
                    "Missing Information",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            int courseID;

            try
            {
                courseID = Convert.ToInt32(cmbCourse.SelectedValue);
            }
            catch
            {
                MessageBox.Show(
                    "Please select a valid course.",
                    "Invalid Course",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string supportType =
                Convert.ToString(cmbSupportType.SelectedItem);

            string[] parts =
                Convert.ToString(cmbSessionTime.SelectedItem)
                .Split(new string[] { " - " },
                    StringSplitOptions.None);

            TimeSpan start;
            TimeSpan end;

            if (parts.Length != 2 ||
                !TimeSpan.TryParseExact(
                    parts[0], @"hh\:mm",
                    CultureInfo.InvariantCulture, out start) ||
                !TimeSpan.TryParseExact(
                    parts[1], @"hh\:mm",
                    CultureInfo.InvariantCulture, out end))
            {
                MessageBox.Show(
                    "Please select a valid time slot.",
                    "Invalid Time",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            DateTime date = dtpSessionDate.Value.Date;

            if (date.Add(start) <= DateTime.Now)
            {
                MessageBox.Show(
                    "Please select a future date and time.",
                    "Invalid Date",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            DialogResult answer = MessageBox.Show(
                "Save these booking changes?\n\n" +
                "Course: " + cmbCourse.Text +
                "\nSupport Type: " + supportType +
                "\nDate: " + date.ToString("dd MMM yyyy") +
                "\nTime: " + cmbSessionTime.Text +
                "\n\nYour tutor assignment may change.",
                "Confirm Changes",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (answer != DialogResult.Yes)
                return;

            btnSaveChanges.Enabled = false;

            try
            {
                string status = BookingData.SaveChanges(
                    connectionString,
                    studentID,
                    requestID,
                    courseID,
                    supportType,
                    date,
                    start,
                    end);

                string extra = status == "Scheduled"
                    ? "A qualified tutor has been assigned."
                    : "No qualified tutor is available for this slot. " +
                      "Your request is awaiting assignment.";

                MessageBox.Show(
                    "Your booking has been updated successfully!" +
                    "\n\nCourse: " + cmbCourse.Text +
                    "\nSupport Type: " + supportType +
                    "\nDate: " + date.ToString("dd MMM yyyy") +
                    "\nTime: " + cmbSessionTime.Text +
                    "\nStatus: " + status +
                    "\n\n" + extra,
                    "Changes Saved",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Your changes could not be saved.\n\n" +
                    ex.Message,
                    "Edit Booking Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnSaveChanges.Enabled = true;
            }
        }
    }
}
