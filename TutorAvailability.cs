using System;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace NextGen__7
{
    public partial class TutorAvailability : Form
    {
        private readonly int tutorId;
        private readonly string tutorName;
        private readonly string connectionString;
        private DataTable slots;
        private string availabilityIdColumn;
        private bool idIsAutoNumber;

        public TutorAvailability() : this(0, "Tutor") { }

        public TutorAvailability(int tutorId, string tutorName)
        {
            InitializeComponent();
            EduConnectInputGuidance.Attach(this);
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            this.tutorId = tutorId;
            this.tutorName = tutorName;
            var cfg = ConfigurationManager.ConnectionStrings[
                "NextGen__7.Properties.Settings.Edu_ConnectConnectionString"];
            if (cfg == null) throw new InvalidOperationException("Database configuration is missing.");
            connectionString = cfg.ConnectionString;
            lblTutor.Text = tutorName + "   |   Tutor #" + tutorId;
            dtpDate.MinDate = DateTime.Today;
            dtpDate.Value = DateTime.Today.AddDays(1);
            dtpStart.Value = DateTime.Today.AddHours(9);
            dtpEnd.Value = DateTime.Today.AddHours(11);
            btnAdd.Click += (s, e) => AddSlot();
            btnDisable.Click += (s, e) => DisableSlot();
            btnRefresh.Click += (s, e) => LoadSlots();
            btnBack.Click += (s, e) => Close();
            gridSlots.CellFormatting += FormatAvailability;
            Load += (s, e) => LoadSlots();
        }

        private static string FindColumn(DataTable table, params string[] names)
        {
            foreach (DataColumn c in table.Columns)
                foreach (string name in names)
                    if (c.ColumnName.Trim('\uFEFF', ' ').Equals(name,
                        StringComparison.OrdinalIgnoreCase)) return c.ColumnName;
            return null;
        }

        private static bool IsSlotAvailable(object value)
        {
            if (value == null || value == DBNull.Value) return false;
            if (value is bool) return (bool)value;
            string text = Convert.ToString(value).Trim();
            return text.Equals("True", StringComparison.OrdinalIgnoreCase) ||
                   text.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
                   text.Equals("On", StringComparison.OrdinalIgnoreCase) ||
                   text == "1" || text == "-1";
        }

        private void FormatAvailability(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 ||
                gridSlots.Columns[e.ColumnIndex].Name != "IsAvailable") return;
            if (gridSlots.Columns[e.ColumnIndex] is DataGridViewCheckBoxColumn) return;
            bool active = IsSlotAvailable(e.Value);
            e.Value = active ? "Available" : "Unavailable";
            e.CellStyle.BackColor = active
                ? System.Drawing.Color.FromArgb(225, 245, 233)
                : System.Drawing.Color.FromArgb(255, 232, 232);
            e.CellStyle.ForeColor = active
                ? System.Drawing.Color.FromArgb(18, 103, 67)
                : System.Drawing.Color.FromArgb(146, 44, 51);
            e.CellStyle.SelectionBackColor = e.CellStyle.BackColor;
            e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
            e.FormattingApplied = true;
        }

        private void LoadSlots()
        {
            try
            {
                slots = new DataTable();
                using (var db = new OleDbConnection(connectionString))
                {
                    db.Open();
                    using (var cmd = new OleDbCommand(
                        "SELECT * FROM [TUTOR_AVAILABILITY] WHERE [TutorID]=? " +
                        "ORDER BY [AvailableDate] DESC", db))
                    {
                        cmd.Parameters.Add("@TutorID", OleDbType.Integer).Value = tutorId;
                        using (var da = new OleDbDataAdapter(cmd)) da.Fill(slots);
                    }
                    availabilityIdColumn = FindColumn(slots,
                        "AvailabilityID", "TutorAvailabilityID");
                    idIsAutoNumber = false;
                    if (availabilityIdColumn != null)
                    {
                        using (var schemaCmd = new OleDbCommand(
                            "SELECT * FROM [TUTOR_AVAILABILITY] WHERE 1=0", db))
                        using (var reader = schemaCmd.ExecuteReader(CommandBehavior.SchemaOnly))
                        {
                            foreach (DataRow row in reader.GetSchemaTable().Rows)
                                if (Convert.ToString(row["ColumnName"]) == availabilityIdColumn)
                                {
                                    idIsAutoNumber = row.Table.Columns.Contains("IsAutoIncrement") &&
                                        row["IsAutoIncrement"] != DBNull.Value &&
                                        Convert.ToBoolean(row["IsAutoIncrement"]);
                                    break;
                                }
                        }
                    }
                }
                gridSlots.DataSource = slots;
                if (availabilityIdColumn != null && gridSlots.Columns.Contains(availabilityIdColumn))
                    gridSlots.Columns[availabilityIdColumn].Visible = false;
                if (gridSlots.Columns.Contains("TutorID"))
                    gridSlots.Columns["TutorID"].Visible = false;
                if (gridSlots.Columns.Contains("AvailableDate"))
                {
                    gridSlots.Columns["AvailableDate"].HeaderText = "Date";
                    gridSlots.Columns["AvailableDate"].DefaultCellStyle.Format = "dd MMM yyyy";
                }
                if (gridSlots.Columns.Contains("StartTime"))
                    gridSlots.Columns["StartTime"].DefaultCellStyle.Format = "HH:mm";
                if (gridSlots.Columns.Contains("EndTime"))
                    gridSlots.Columns["EndTime"].DefaultCellStyle.Format = "HH:mm";
                if (gridSlots.Columns.Contains("IsAvailable"))
                    gridSlots.Columns["IsAvailable"].HeaderText = "Availability";
                gridSlots.ClearSelection();
                lblStatus.Text = slots.Rows.Count + " recorded availability slots.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Unable to load availability.";
                MessageBox.Show("Could not retrieve tutor availability.\n\n" + ex.Message,
                    "Database error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddSlot()
        {
            DateTime date = dtpDate.Value.Date;
            DateTime start = date.Add(dtpStart.Value.TimeOfDay);
            DateTime end = date.Add(dtpEnd.Value.TimeOfDay);
            if (date < DateTime.Today)
            {
                MessageBox.Show("Availability cannot be created for a past date."); return;
            }
            if (start <= DateTime.Now)
            {
                MessageBox.Show("Choose an availability start time in the future.");
                dtpStart.Focus();
                return;
            }
            if (end <= start)
            {
                MessageBox.Show("The end time must be later than the start time."); return;
            }
            try
            {
                using (var db = new OleDbConnection(connectionString))
                {
                    db.Open();
                    using (var tx = db.BeginTransaction())
                    {
                        try
                        {
                            using (var cmd = new OleDbCommand(
                                "SELECT [StartTime], [EndTime], [IsAvailable] " +
                                "FROM [TUTOR_AVAILABILITY] WHERE [TutorID]=? " +
                                "AND [AvailableDate]>=? AND [AvailableDate]<?", db, tx))
                            {
                                cmd.Parameters.Add("@TutorID", OleDbType.Integer).Value = tutorId;
                                cmd.Parameters.Add("@Day", OleDbType.Date).Value = date;
                                cmd.Parameters.Add("@NextDay", OleDbType.Date).Value = date.AddDays(1);
                                using (var reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        if (reader.IsDBNull(0) || reader.IsDBNull(1)) continue;
                                        if (reader.IsDBNull(2) || !IsSlotAvailable(reader[2])) continue;
                                        TimeSpan oldStart = BookingData.ReadTime(reader[0]);
                                        TimeSpan oldEnd = BookingData.ReadTime(reader[1]);
                                        if (start.TimeOfDay < oldEnd && end.TimeOfDay > oldStart)
                                        {
                                            MessageBox.Show("This tutor already has overlapping availability.");
                                            return;
                                        }
                                    }
                                }
                            }
                            string column = availabilityIdColumn;
                            string query = column != null && !idIsAutoNumber
                                ? "INSERT INTO [TUTOR_AVAILABILITY] ([" + column + "]," +
                                  "[TutorID],[AvailableDate],[StartTime],[EndTime],[IsAvailable]) " +
                                  "VALUES (?,?,?,?,?,?)"
                                : "INSERT INTO [TUTOR_AVAILABILITY] " +
                                  "([TutorID],[AvailableDate],[StartTime],[EndTime],[IsAvailable]) " +
                                  "VALUES (?,?,?,?,?)";
                            using (var cmd = new OleDbCommand(query, db, tx))
                            {
                                if (column != null && !idIsAutoNumber)
                                {
                                    using (var max = new OleDbCommand(
                                        "SELECT MAX([" + column + "]) FROM [TUTOR_AVAILABILITY]", db, tx))
                                    {
                                        object value = max.ExecuteScalar();
                                        cmd.Parameters.Add("@ID", OleDbType.Integer).Value =
                                            value == null || value == DBNull.Value
                                            ? 1 : checked(Convert.ToInt32(value) + 1);
                                    }
                                }
                                cmd.Parameters.Add("@Tutor", OleDbType.Integer).Value = tutorId;
                                cmd.Parameters.Add("@Date", OleDbType.Date).Value = date;
                                cmd.Parameters.Add("@Start", OleDbType.Date).Value = start;
                                cmd.Parameters.Add("@End", OleDbType.Date).Value = end;
                                cmd.Parameters.Add("@Available", OleDbType.Boolean).Value = true;
                                if (cmd.ExecuteNonQuery() != 1)
                                    throw new InvalidOperationException("No availability was saved.");
                            }
                            tx.Commit();
                        }
                        catch { tx.Rollback(); throw; }
                    }
                }
                LoadSlots();
                lblStatus.Text = "Availability added for " + date.ToString("dd MMM yyyy") + ".";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to add availability.\n\n" + ex.Message,
                    "Save error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DisableSlot()
        {
            if (gridSlots.SelectedRows.Count != 1)
            { MessageBox.Show("Select an availability slot first."); return; }
            var row = gridSlots.SelectedRows[0];
            if (availabilityIdColumn == null)
            {
                MessageBox.Show("The table does not expose a supported AvailabilityID, " +
                    "so this slot cannot be safely updated."); return;
            }
            if (!IsSlotAvailable(row.Cells["IsAvailable"].Value))
            { MessageBox.Show("This availability slot is already disabled."); return; }
            int id = Convert.ToInt32(row.Cells[availabilityIdColumn].Value);
            DateTime date = Convert.ToDateTime(row.Cells["AvailableDate"].Value).Date;
            TimeSpan slotStart = BookingData.ReadTime(row.Cells["StartTime"].Value);
            TimeSpan slotEnd = BookingData.ReadTime(row.Cells["EndTime"].Value);
            try
            {
                using (var db = new OleDbConnection(connectionString))
                {
                    db.Open();
                    // Do not remove availability committed to an existing session.
                    using (var check = new OleDbCommand(
                        "SELECT [StartTime],[EndTime] FROM [TUTORING_SESSION] " +
                        "WHERE [TutorID]=? AND [SessionDate]>=? AND [SessionDate]<? " +
                        "AND [SessionStatus]='Scheduled'", db))
                    {
                        check.Parameters.Add("@TutorID", OleDbType.Integer).Value = tutorId;
                        check.Parameters.Add("@Date", OleDbType.Date).Value = date;
                        check.Parameters.Add("@NextDay", OleDbType.Date).Value = date.AddDays(1);
                        using (var reader = check.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (reader.IsDBNull(0) || reader.IsDBNull(1)) continue;
                                TimeSpan bookedStart = BookingData.ReadTime(reader[0]);
                                TimeSpan bookedEnd = BookingData.ReadTime(reader[1]);
                                if (slotStart < bookedEnd && slotEnd > bookedStart)
                                {
                                    MessageBox.Show("This slot contains a scheduled tutoring session. " +
                                        "Resolve the existing booking before removing availability.");
                                    return;
                                }
                            }
                        }
                    }
                    if (MessageBox.Show("Mark this slot unavailable?", "Confirm",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                        return;
                    using (var cmd = new OleDbCommand(
                        "UPDATE [TUTOR_AVAILABILITY] SET [IsAvailable]=? WHERE [" +
                        availabilityIdColumn + "]=? AND [TutorID]=?", db))
                    {
                        cmd.Parameters.Add("@Available", OleDbType.Boolean).Value = false;
                        cmd.Parameters.Add("@ID", OleDbType.Integer).Value = id;
                        cmd.Parameters.Add("@Tutor", OleDbType.Integer).Value = tutorId;
                        if (cmd.ExecuteNonQuery() != 1)
                            throw new InvalidOperationException("The selected slot was not found.");
                    }
                }
                LoadSlots();
                lblStatus.Text = "Availability updated.";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to change availability.\n\n" + ex.Message,
                    "Database error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
