using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;

namespace NextGen__7
{
    internal static class EduConnectGpaStore
    {
        internal static DataTable Load(OleDbConnection db, int academicYear)
        {
            EnsureTable(db);
            MigrateExistingRecords(db);
            var table = new DataTable();
            using (var command = new OleDbCommand(
                "SELECT [StudentNo],[AcademicYear],[T1],[T2],[T3],[T4],[DataOrigin] " +
                "FROM [STUDENT_TERM_GPA] WHERE [AcademicYear]=? ORDER BY [StudentNo]", db))
            {
                command.Parameters.Add("@Year", OleDbType.Integer).Value = academicYear;
                using (var adapter = new OleDbDataAdapter(command))
                    adapter.Fill(table);
            }
            return table;
        }

        private static bool HasTable(OleDbConnection db, string name)
        {
            DataTable table = db.GetSchema("Tables");
            foreach (DataRow row in table.Rows)
                if (string.Equals(Convert.ToString(row["TABLE_NAME"]), name,
                    StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static void EnsureTable(OleDbConnection db)
        {
            if (HasTable(db, "STUDENT_TERM_GPA")) return;
            using (var cmd = new OleDbCommand(
                "CREATE TABLE [STUDENT_TERM_GPA] (" +
                "[StudentNo] TEXT(40) NOT NULL, [AcademicYear] LONG NOT NULL, " +
                "[T1] DOUBLE, [T2] DOUBLE, [T3] DOUBLE, [T4] DOUBLE, " +
                "[DataOrigin] TEXT(40), " +
                "CONSTRAINT [PK_STUDENT_TERM_GPA] PRIMARY KEY ([StudentNo],[AcademicYear]))", db))
                cmd.ExecuteNonQuery();
        }

        private sealed class TermRecord
        {
            internal string StudentNo;
            internal int Year;
            internal readonly object[] Values = new object[]
            {
                DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value
            };
            internal bool Illustrative;
        }

        private static void MigrateExistingRecords(OleDbConnection db)
        {
            if (!HasTable(db, "STUDENTACADEMICRECORD")) return;
            var legacy = new DataTable();
            using (var adapter = new OleDbDataAdapter(
                "SELECT * FROM [STUDENTACADEMICRECORD]", db))
                adapter.Fill(legacy);
            if (!legacy.Columns.Contains("AcademicPeriod") ||
                !legacy.Columns.Contains("GPA")) return;

            var studentsById = new Dictionary<int, string>();
            var students = new DataTable();
            using (var adapter = new OleDbDataAdapter(
                "SELECT [StudentID],[StudentNo] FROM [STUDENT]", db))
                adapter.Fill(students);
            foreach (DataRow row in students.Rows)
                if (row["StudentID"] != DBNull.Value)
                    studentsById[Convert.ToInt32(row["StudentID"])] =
                        Convert.ToString(row["StudentNo"]).Trim();

            var byStudentYear = new Dictionary<string, TermRecord>(
                StringComparer.OrdinalIgnoreCase);
            foreach (DataRow row in legacy.Rows)
            {
                if (row["GPA"] == DBNull.Value) continue;
                string studentNo = legacy.Columns.Contains("StudentNo")
                    ? Convert.ToString(row["StudentNo"]).Trim() : "";
                if (studentNo.Length == 0 && legacy.Columns.Contains("StudentID") &&
                    row["StudentID"] != DBNull.Value)
                    studentsById.TryGetValue(Convert.ToInt32(row["StudentID"]), out studentNo);
                if (string.IsNullOrWhiteSpace(studentNo)) continue;
                string period = Convert.ToString(row["AcademicPeriod"]).Trim();
                int dash = period.LastIndexOf("-T", StringComparison.OrdinalIgnoreCase);
                if (dash < 0) continue;
                int year, term;
                if (!int.TryParse(period.Substring(0, dash), NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out year) ||
                    !int.TryParse(period.Substring(dash + 2), NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out term) ||
                    year < 1900 || year > 2200 || term < 1 || term > 4) continue;
                double gpa;
                try { gpa = Convert.ToDouble(row["GPA"]); }
                catch { continue; }
                if (double.IsNaN(gpa) || double.IsInfinity(gpa) || gpa < 0 || gpa > 100)
                    continue;
                string key = studentNo + "/" + year;
                TermRecord item;
                if (!byStudentYear.TryGetValue(key, out item))
                {
                    item = new TermRecord { StudentNo = studentNo, Year = year };
                    byStudentYear.Add(key, item);
                }
                item.Values[term - 1] = gpa;
                if (legacy.Columns.Contains("RecordSource") &&
                    Convert.ToString(row["RecordSource"]).IndexOf("SYNTHETIC",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                    item.Illustrative = true;
            }

            foreach (TermRecord item in byStudentYear.Values)
            {
                var existing = new DataTable();
                using (var cmd = new OleDbCommand(
                    "SELECT [T1],[T2],[T3],[T4],[DataOrigin] FROM [STUDENT_TERM_GPA] " +
                    "WHERE [StudentNo]=? AND [AcademicYear]=?", db))
                {
                    cmd.Parameters.Add("@StudentNo", OleDbType.VarWChar).Value = item.StudentNo;
                    cmd.Parameters.Add("@Year", OleDbType.Integer).Value = item.Year;
                    using (var adapter = new OleDbDataAdapter(cmd))
                        adapter.Fill(existing);
                }
                if (existing.Rows.Count == 0)
                {
                    using (var cmd = new OleDbCommand(
                        "INSERT INTO [STUDENT_TERM_GPA] " +
                        "([StudentNo],[AcademicYear],[T1],[T2],[T3],[T4],[DataOrigin]) " +
                        "VALUES (?,?,?,?,?,?,?)", db))
                    {
                        cmd.Parameters.Add("@StudentNo", OleDbType.VarWChar).Value = item.StudentNo;
                        cmd.Parameters.Add("@Year", OleDbType.Integer).Value = item.Year;
                        for (int i = 0; i < 4; i++)
                            cmd.Parameters.Add("@T" + (i + 1), OleDbType.Double).Value =
                                item.Values[i];
                        cmd.Parameters.Add("@Origin", OleDbType.VarWChar).Value =
                            item.Illustrative ? "Illustrative records" : "Recorded data";
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    var values = new object[4];
                    bool changed = false;
                    for (int i = 0; i < 4; i++)
                    {
                        values[i] = existing.Rows[0][i];
                        if (values[i] == DBNull.Value && item.Values[i] != DBNull.Value)
                        {
                            values[i] = item.Values[i];
                            changed = true;
                        }
                    }
                    string source = Convert.ToString(existing.Rows[0]["DataOrigin"]);
                    if (item.Illustrative && source != "Illustrative records")
                    {
                        source = "Illustrative records";
                        changed = true;
                    }
                    if (!changed) continue;
                    using (var cmd = new OleDbCommand(
                        "UPDATE [STUDENT_TERM_GPA] SET [T1]=?,[T2]=?,[T3]=?,[T4]=?," +
                        "[DataOrigin]=? WHERE [StudentNo]=? AND [AcademicYear]=?", db))
                    {
                        for (int i = 0; i < 4; i++)
                            cmd.Parameters.Add("@T" + (i + 1), OleDbType.Double).Value = values[i];
                        cmd.Parameters.Add("@Origin", OleDbType.VarWChar).Value = source;
                        cmd.Parameters.Add("@StudentNo", OleDbType.VarWChar).Value = item.StudentNo;
                        cmd.Parameters.Add("@Year", OleDbType.Integer).Value = item.Year;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
