
using System;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace NextGen__7
{
    public partial class ManageBookings : Form
    {
        private readonly int studentID;
        private readonly string studentNo;
        private readonly string connectionString;

        public ManageBookings()
            : this(Login.CurrentStudentID, Login.CurrentStudentNo)
        {
        }

        public ManageBookings(int studentID, string studentNo)
        {
            InitializeComponent();

            this.studentID = studentID;
            this.studentNo = studentNo;

            ConnectionStringSettings settings =
                ConfigurationManager.ConnectionStrings[
                    "NextGen__7.Properties.Settings.Edu_ConnectConnectionString"];

            if (settings == null)
            {
                throw new InvalidOperationException(
                    "Database connection string not found.");
            }

            connectionString = settings.ConnectionString;

            lblStudentNo.Text = "Student No: " + studentNo;

            dgvBookings.ReadOnly = true;
            dgvBookings.MultiSelect = false;
            dgvBookings.SelectionMode =
                DataGridViewSelectionMode.FullRowSelect;
            dgvBookings.AutoGenerateColumns = true;

            Load += ManageBookings_Load;
            btnEditBooking.Click += btnEditBooking_Click;
            btnCancelBooking.Click += btnCancelBooking_Click;
        }

        private void ManageBookings_Load(object sender, EventArgs e)
        {
            LoadBookings();
        }

        private void LoadBookings()
        {


            string query =
                "SELECT R.[StudentID], C.[CourseCode], C.[CourseName], " +
                "R.[RequestDate], R.[PreferredDate], " +
                "R.[PreferredStartTime], R.[PreferredEndTime], R.[Status] " +
                "FROM [TUTORING_REQUEST] AS R " +
                "LEFT JOIN [COURSE] AS C " +
                "ON R.[CourseID] = C.[CourseID] " +
                "WHERE R.[StudentID] = ?";



            try
            {
                DataTable table = new DataTable();

                using (OleDbConnection connection =
                    new OleDbConnection(connectionString))
                {
                    using (OleDbCommand command =
                        new OleDbCommand(query, connection))
                    {
                        command.Parameters.Add(
                            "@StudentID", OleDbType.Integer).Value = studentID;

                        using (OleDbDataAdapter adapter =
                            new OleDbDataAdapter(command))
                        {
                            adapter.Fill(table);
                        }
                    }
                }

                dgvBookings.DataSource = table;



                dgvBookings.Columns["StudentID"].Visible = false;

                dgvBookings.Columns["CourseCode"].HeaderText = "Course Code";
                dgvBookings.Columns["CourseName"].HeaderText = "Course Name";

                dgvBookings.Columns["PreferredDate"].HeaderText = "Session Date";
                dgvBookings.Columns["PreferredDate"].DefaultCellStyle.Format = "dd MMM yyyy";

                dgvBookings.Columns["PreferredStartTime"].HeaderText = "Start Time";
                dgvBookings.Columns["PreferredStartTime"].DefaultCellStyle.Format = "HH:mm";

                dgvBookings.Columns["PreferredEndTime"].HeaderText = "End Time";
                dgvBookings.Columns["PreferredEndTime"].DefaultCellStyle.Format = "HH:mm";

                dgvBookings.Columns["Status"].HeaderText = "Booking Status";
                dgvBookings.Columns["RequestDate"].HeaderText = "Request Date";
                dgvBookings.Columns["RequestDate"].DefaultCellStyle.Format = "dd MMM yyyy";


                dgvBookings.ClearSelection();

                bool hasBookings = table.Rows.Count > 0;

                btnEditBooking.Enabled = hasBookings;
                btnCancelBooking.Enabled = hasBookings;

                if (!hasBookings)
                {
                    MessageBox.Show(
                        "You have no tutoring bookings yet.",
                        "No Bookings Found",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to load your bookings.\n\n" + ex.Message,
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private bool TryGetSelectedBooking(
            out int studentBookingID,
            out string status)
        {
            studentBookingID = 0;
            status = null;

            if (dgvBookings.SelectedRows.Count == 0)
            {
                MessageBox.Show(
                    "Please select a booking first.",
                    "No Booking Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return false;
            }

            DataGridViewRow row = dgvBookings.SelectedRows[0];

            studentBookingID =
                Convert.ToInt32(row.Cells["StudentID"].Value);

            status =
                Convert.ToString(row.Cells["Status"].Value);

            return true;
        }

        private void btnEditBooking_Click(object sender, EventArgs e)
        {
            int bookingID;
            string status;

            if (!TryGetSelectedBooking(out bookingID, out status))
                return;

            if (status != "Submitted")
            {
                MessageBox.Show(
                    "Only submitted bookings can be edited.",
                    "Editing Not Allowed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            MessageBox.Show(
                "The editing functionality requires an Edit Booking form.",
                "Edit Booking",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void btnCancelBooking_Click(object sender, EventArgs e)
        {
            int bookingID;
            string status;

            if (!TryGetSelectedBooking(out bookingID, out status))
                return;

            MessageBox.Show(
                "Cancellation will be enabled once the request identifier " +
                "is available from the database.",
                "Cancel Booking",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
