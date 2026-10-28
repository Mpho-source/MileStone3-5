
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;

namespace NextGen__7
{
    public static class BookingData
    {
        public static string GetColumn(
            OleDbConnection connection,
            OleDbTransaction transaction,
            string table,
            string logicalName)
        {
            using (OleDbCommand command = new OleDbCommand(
                "SELECT * FROM [" + table + "] WHERE 1 = 0",
                connection, transaction))
            using (OleDbDataReader reader = command.ExecuteReader())
            {
                foreach (DataRow row in reader.GetSchemaTable().Rows)
                {
                    string name = Convert.ToString(row["ColumnName"]);

                    if (name.Trim('\uFEFF', ' ')
                        .Equals(logicalName,
                            StringComparison.OrdinalIgnoreCase))
                        return name;
                }
            }

            throw new InvalidOperationException(
                logicalName + " was not found in " + table + ".");
        }

        public static TimeSpan ReadTime(object value)
        {
            if (value == null || value == DBNull.Value)
                throw new InvalidOperationException("Missing time value.");

            if (value is DateTime)
                return ((DateTime)value).TimeOfDay;

            if (value is TimeSpan)
                return (TimeSpan)value;

            return DateTime.Parse(
                Convert.ToString(value),
                CultureInfo.InvariantCulture).TimeOfDay;
        }

        public static DataRow GetRequest(
            OleDbConnection connection,
            OleDbTransaction transaction,
            int studentID,
            int requestID)
        {
            string id = GetColumn(
                connection, transaction, "TUTORING_REQUEST", "RequestID");

            DataTable table = new DataTable();

            using (OleDbCommand command = new OleDbCommand(
                "SELECT * FROM [TUTORING_REQUEST] " +
                "WHERE [" + id + "] = ? AND [StudentID] = ?",
                connection, transaction))
            {
                command.Parameters.Add(
                    "@RequestID", OleDbType.Integer).Value = requestID;
                command.Parameters.Add(
                    "@StudentID", OleDbType.Integer).Value = studentID;

                using (OleDbDataAdapter adapter =
                    new OleDbDataAdapter(command))
                    adapter.Fill(table);
            }

            if (table.Rows.Count != 1)
                throw new InvalidOperationException(
                    "This booking was not found for your account.");

            return table.Rows[0];
        }

        public static bool IsChangeableStatus(string status)
        {
            return string.Equals(status, "Submitted", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "Verified", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "Scheduled", StringComparison.OrdinalIgnoreCase);
        }

        public static void EnsureEditable(DataRow request)
        {
            string status =
                Convert.ToString(request["Status"]).Trim();

            if (!IsChangeableStatus(status))
            {
                throw new InvalidOperationException(
                    "Only Submitted, Verified or Scheduled bookings can be changed.");
            }

            DateTime sessionDate =
                Convert.ToDateTime(request["PreferredDate"]).Date;

            TimeSpan sessionTime =
                ReadTime(request["PreferredStartTime"]);

            DateTime sessionStart =
                sessionDate.Add(sessionTime);

            DateTime deadline =
                sessionStart.AddHours(-48);

            if (DateTime.Now > deadline)
            {
                throw new InvalidOperationException(
                    "Bookings can only be edited or cancelled at least " +
                    "48 hours before the scheduled session starts." +
                    "\n\nSession starts: " +
                    sessionStart.ToString("dd MMM yyyy HH:mm") +
                    "\nDeadline: " +
                    deadline.ToString("dd MMM yyyy HH:mm"));
            }
        }

        public static bool HasStudentConflict(
            OleDbConnection connection,
            OleDbTransaction transaction,
            int studentID,
            int excludeRequestID,
            DateTime date,
            TimeSpan start,
            TimeSpan end)
        {
            string id = GetColumn(
                connection, transaction, "TUTORING_REQUEST", "RequestID");

            using (OleDbCommand command = new OleDbCommand(
                "SELECT [PreferredStartTime], [PreferredEndTime] " +
                "FROM [TUTORING_REQUEST] " +
                "WHERE [StudentID] = ? AND [" + id + "] <> ? " +
                "AND [PreferredDate] >= ? AND [PreferredDate] < ? " +
                "AND [Status] IN ('Submitted','Verified','Scheduled')",
                connection, transaction))
            {
                command.Parameters.Add(
                    "@StudentID", OleDbType.Integer).Value = studentID;
                command.Parameters.Add(
                    "@RequestID", OleDbType.Integer).Value = excludeRequestID;
                command.Parameters.Add(
                    "@DateStart", OleDbType.Date).Value = date.Date;
                command.Parameters.Add(
                    "@DateEnd", OleDbType.Date).Value = date.Date.AddDays(1);

                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.IsDBNull(0) || reader.IsDBNull(1))
                            continue;

                        if (start < ReadTime(reader[1]) &&
                            end > ReadTime(reader[0]))
                            return true;
                    }
                }
            }

            return false;
        }

        public static int FindTutor(
            OleDbConnection connection,
            OleDbTransaction transaction,
            int courseID,
            DateTime date,
            TimeSpan start,
            TimeSpan end,
            int excludeRequestID)
        {
            List<int> tutors = new List<int>();

            using (OleDbCommand command = new OleDbCommand(
                "SELECT [TutorID] FROM [TUTOR_COURSE] " +
                "WHERE [CourseID] = ? ORDER BY [TutorID]",
                connection, transaction))
            {
                command.Parameters.Add(
                    "@CourseID", OleDbType.Integer).Value = courseID;

                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(0))
                            tutors.Add(Convert.ToInt32(reader[0]));
                    }
                }
            }

            foreach (int tutorID in tutors)
            {
                bool available = false;

                using (OleDbCommand command = new OleDbCommand(
                    "SELECT [StartTime], [EndTime], [IsAvailable] " +
                    "FROM [TUTOR_AVAILABILITY] " +
                    "WHERE [TutorID] = ? " +
                    "AND [AvailableDate] >= ? AND [AvailableDate] < ?",
                    connection, transaction))
                {
                    command.Parameters.Add(
                        "@TutorID", OleDbType.Integer).Value = tutorID;
                    command.Parameters.Add(
                        "@DateStart", OleDbType.Date).Value = date.Date;
                    command.Parameters.Add(
                        "@DateEnd", OleDbType.Date).Value =
                        date.Date.AddDays(1);

                    using (OleDbDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader.IsDBNull(0) ||
                                reader.IsDBNull(1) ||
                                reader.IsDBNull(2))
                                continue;

                            string value =
                                Convert.ToString(reader[2]).Trim();

                            bool enabled =
                                value.Equals("True",
                                    StringComparison.OrdinalIgnoreCase) ||
                                value.Equals("Yes",
                                    StringComparison.OrdinalIgnoreCase) ||
                                value == "1" || value == "-1";

                            if (enabled &&
                                ReadTime(reader[0]) <= start &&
                                ReadTime(reader[1]) >= end)
                            {
                                available = true;
                                break;
                            }
                        }
                    }
                }

                if (!available)
                    continue;

                bool conflict = false;

                using (OleDbCommand command = new OleDbCommand(
                    "SELECT S.[RequestID], S.[StartTime], " +
                    "S.[EndTime], S.[SessionStatus] " +
                    "FROM [TUTORING_SESSION] AS S " +
                    "WHERE S.[TutorID] = ? " +
                    "AND S.[SessionDate] >= ? AND S.[SessionDate] < ?",
                    connection, transaction))
                {
                    command.Parameters.Add(
                        "@TutorID", OleDbType.Integer).Value = tutorID;
                    command.Parameters.Add(
                        "@DateStart", OleDbType.Date).Value = date.Date;
                    command.Parameters.Add(
                        "@DateEnd", OleDbType.Date).Value =
                        date.Date.AddDays(1);

                    using (OleDbDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader.IsDBNull(0) ||
                                reader.IsDBNull(1) ||
                                reader.IsDBNull(2))
                                continue;

                            if (Convert.ToInt32(reader[0]) ==
                                excludeRequestID)
                                continue;

                            string status = reader.IsDBNull(3)
                                ? ""
                                : Convert.ToString(reader[3]).Trim();

                            if (status.Equals("Cancelled",
                                    StringComparison.OrdinalIgnoreCase) ||
                                status.Equals("Canceled",
                                    StringComparison.OrdinalIgnoreCase))
                                continue;

                            if (start < ReadTime(reader[2]) &&
                                end > ReadTime(reader[1]))
                            {
                                conflict = true;
                                break;
                            }
                        }
                    }
                }

                if (!conflict)
                    return tutorID;
            }

            return 0;
        }

        private static int NextSessionID(
            OleDbConnection connection,
            OleDbTransaction transaction)
        {
            string id = GetColumn(
                connection, transaction, "TUTORING_SESSION", "SessionID");

            using (OleDbCommand command = new OleDbCommand(
                "SELECT MAX([" + id + "]) FROM [TUTORING_SESSION]",
                connection, transaction))
            {
                object value = command.ExecuteScalar();

                return value == null || value == DBNull.Value
                    ? 1
                    : checked(Convert.ToInt32(value) + 1);
            }
        }

        public static string SaveChanges(
            string connectionString,
            int studentID,
            int requestID,
            int courseID,
            string supportType,
            DateTime date,
            TimeSpan start,
            TimeSpan end)
        {
            DateTime startDateTime = date.Date.Add(start);
            DateTime endDateTime = date.Date.Add(end);

            if (courseID <= 0 || string.IsNullOrWhiteSpace(supportType))
                throw new InvalidOperationException(
                    "Please select a course and support type.");

            if (startDateTime <= DateTime.Now ||
                endDateTime <= startDateTime)
                throw new InvalidOperationException(
                    "Please select a valid future session.");

            using (OleDbConnection connection =
                new OleDbConnection(connectionString))
            {
                connection.Open();

                using (OleDbTransaction transaction =
                    connection.BeginTransaction())
                {
                    try
                    {
                        DataRow request = GetRequest(
                            connection, transaction, studentID, requestID);

                        EnsureEditable(request);

                        if (HasStudentConflict(
                            connection, transaction, studentID,
                            requestID, date, start, end))
                            throw new InvalidOperationException(
                                "You already have another active booking " +
                                "that overlaps this time.");

                        string newStatus = "Submitted";

                        string requestIdColumn = GetColumn(
                            connection, transaction,
                            "TUTORING_REQUEST", "RequestID");

                        using (OleDbCommand command = new OleDbCommand(
                            "UPDATE [TUTORING_REQUEST] SET " +
                            "[CourseID] = ?, [SupportType] = ?, " +
                            "[PreferredDate] = ?, " +
                            "[PreferredStartTime] = ?, " +
                            "[PreferredEndTime] = ?, [Status] = ? " +
                            "WHERE [" + requestIdColumn + "] = ? " +
                            "AND [StudentID] = ?",
                            connection, transaction))
                        {
                            command.Parameters.Add(
                                "@CourseID", OleDbType.VarWChar).Value =
                                courseID.ToString(CultureInfo.InvariantCulture);
                            command.Parameters.Add(
                                "@SupportType", OleDbType.VarWChar).Value =
                                supportType.Trim();
                            command.Parameters.Add(
                                "@Date", OleDbType.Date).Value = date.Date;
                            command.Parameters.Add(
                                "@Start", OleDbType.Date).Value =
                                startDateTime;
                            command.Parameters.Add(
                                "@End", OleDbType.Date).Value =
                                endDateTime;
                            command.Parameters.Add(
                                "@Status", OleDbType.VarWChar).Value =
                                newStatus;
                            command.Parameters.Add(
                                "@RequestID", OleDbType.Integer).Value =
                                requestID;
                            command.Parameters.Add(
                                "@StudentID", OleDbType.Integer).Value =
                                studentID;

                            if (command.ExecuteNonQuery() != 1)
                                throw new InvalidOperationException(
                                    "The booking could not be updated.");
                        }

                        string sessionIdColumn = GetColumn(
                            connection, transaction,
                            "TUTORING_SESSION", "SessionID");

                        DataTable sessions = new DataTable();

                        using (OleDbCommand command = new OleDbCommand(
                            "SELECT [" + sessionIdColumn + "] AS SessionKey, " +
                            "[SessionStatus] FROM [TUTORING_SESSION] " +
                            "WHERE [RequestID] = ?",
                            connection, transaction))
                        {
                            command.Parameters.Add(
                                "@RequestID", OleDbType.Integer).Value =
                                requestID;

                            using (OleDbDataAdapter adapter =
                                new OleDbDataAdapter(command))
                                adapter.Fill(sessions);
                        }

                        int activeSessionID = 0;

                        foreach (DataRow row in sessions.Rows)
                        {
                            string status =
                                Convert.ToString(row["SessionStatus"]).Trim();

                            if (!status.Equals("Cancelled",
                                    StringComparison.OrdinalIgnoreCase) &&
                                !status.Equals("Canceled",
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                if (activeSessionID != 0)
                                    throw new InvalidOperationException(
                                        "Multiple active sessions exist " +
                                        "for this booking.");

                                activeSessionID =
                                    Convert.ToInt32(row["SessionKey"]);
                            }
                        }

                        if (activeSessionID != 0)
                        {
                            using (OleDbCommand command = new OleDbCommand(
                                "UPDATE [TUTORING_SESSION] SET " +
                                "[SessionStatus] = 'Cancelled' " +
                                "WHERE [" + sessionIdColumn + "] = ?",
                                connection, transaction))
                            {
                                command.Parameters.Add(
                                    "@SessionID", OleDbType.Integer).Value =
                                    activeSessionID;
                                command.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        return newStatus;
                    }
                    catch
                    {
                        try { transaction.Rollback(); }
                        catch { }
                        throw;
                    }
                }
            }
        }

        public static void CancelBooking(
            string connectionString,
            int studentID,
            int requestID)
        {
            using (OleDbConnection connection =
                new OleDbConnection(connectionString))
            {
                connection.Open();

                using (OleDbTransaction transaction =
                    connection.BeginTransaction())
                {
                    try
                    {
                        DataRow request = GetRequest(
                            connection, transaction, studentID, requestID);

                        EnsureEditable(request);

                        string requestIdColumn = GetColumn(
                            connection, transaction,
                            "TUTORING_REQUEST", "RequestID");

                        using (OleDbCommand command = new OleDbCommand(
                            "UPDATE [TUTORING_REQUEST] " +
                            "SET [Status] = 'Cancelled' " +
                            "WHERE [" + requestIdColumn + "] = ? " +
                            "AND [StudentID] = ?",
                            connection, transaction))
                        {
                            command.Parameters.Add(
                                "@RequestID", OleDbType.Integer).Value =
                                requestID;
                            command.Parameters.Add(
                                "@StudentID", OleDbType.Integer).Value =
                                studentID;

                            if (command.ExecuteNonQuery() != 1)
                                throw new InvalidOperationException(
                                    "The booking could not be cancelled.");
                        }

                        using (OleDbCommand command = new OleDbCommand(
                            "UPDATE [TUTORING_SESSION] " +
                            "SET [SessionStatus] = 'Cancelled' " +
                            "WHERE [RequestID] = ? " +
                            "AND ([SessionStatus] IS NULL " +
                            "OR [SessionStatus] <> 'Cancelled')",
                            connection, transaction))
                        {
                            command.Parameters.Add(
                                "@RequestID", OleDbType.Integer).Value =
                                requestID;
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        try { transaction.Rollback(); }
                        catch { }
                        throw;
                    }
                }
            }
        }
    }
}
