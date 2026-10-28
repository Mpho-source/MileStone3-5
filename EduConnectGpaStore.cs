using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;

namespace NextGen__7
{
    internal static class EduConnectGpaStore
    {
        private static DataTable Empty()
        {
            var result = new DataTable();
            result.Columns.Add("StudentNo", typeof(string));
            result.Columns.Add("AcademicYear", typeof(int));
            for (int term = 1; term <= 4; term++)
                result.Columns.Add("T" + term, typeof(double));
            result.Columns.Add("DataOrigin", typeof(string));
            return result;
        }

        private static bool HasTable(OleDbConnection db, string name)
        {
            foreach (DataRow row in db.GetSchema("Tables").Rows)
                if (string.Equals(Convert.ToString(row["TABLE_NAME"]), name,
                    StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static DataTable Read(OleDbConnection db, string table)
        {
            var data = new DataTable();
            using (var adapter = new OleDbDataAdapter(
                "SELECT * FROM [" + table + "]", db))
                adapter.Fill(data);
            return data;
        }

        private static string Origin(string source)
        {
            return source.IndexOf("SYNTHETIC", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   source.IndexOf("Illustrative", StringComparison.OrdinalIgnoreCase) >= 0
                ? "Illustrative records" : "Recorded data";
        }

        private static DataRow GetOrAdd(DataTable output,
            Dictionary<string, DataRow> index, string studentNo, int year)
        {
            string key = studentNo.ToUpperInvariant() + "/" +
                year.ToString(CultureInfo.InvariantCulture);
            DataRow row;
            if (!index.TryGetValue(key, out row))
            {
                row = output.NewRow();
                row["StudentNo"] = studentNo;
                row["AcademicYear"] = year;
                row["DataOrigin"] = "Recorded data";
                output.Rows.Add(row);
                index.Add(key, row);
            }
            return row;
        }

        private static void SetValue(DataRow row, int term, object value,
            string source, bool overwrite)
        {
            if (value == null || value == DBNull.Value) return;
            double grade = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            if (double.IsNaN(grade) || double.IsInfinity(grade) ||
                grade < 0 || grade > 100) return;
            string name = "T" + term;
            if (overwrite || row[name] == DBNull.Value)
                row[name] = grade;
            if (Origin(source) == "Illustrative records")
                row["DataOrigin"] = "Illustrative records";
        }

        internal static DataTable Load(OleDbConnection db, int academicYear)
        {
            DataTable output = Empty();
            var index = new Dictionary<string, DataRow>(
                StringComparer.OrdinalIgnoreCase);

            if (HasTable(db, "STUDENTACADEMICRECORD"))
            {
                DataTable academic = Read(db, "STUDENTACADEMICRECORD");
                bool compact = academic.Columns.Contains("AcademicYear") &&
                    academic.Columns.Contains("Term1") &&
                    academic.Columns.Contains("Term2") &&
                    academic.Columns.Contains("Term3") &&
                    academic.Columns.Contains("Term4");

                if (compact)
                {
                    foreach (DataRow record in academic.Rows)
                    {
                        if (record["AcademicYear"] == DBNull.Value ||
                            Convert.ToInt32(record["AcademicYear"]) != academicYear)
                            continue;
                        string number = Convert.ToString(record["StudentNo"]).Trim();
                        if (number.Length == 0) continue;
                        DataRow row = GetOrAdd(output, index, number, academicYear);
                        string source = academic.Columns.Contains("RecordSource")
                            ? Convert.ToString(record["RecordSource"]) : "";
                        for (int term = 1; term <= 4; term++)
                            SetValue(row, term, record["Term" + term], source, true);
                    }
                    return output;
                }

                if (academic.Columns.Contains("AcademicPeriod") &&
                    academic.Columns.Contains("GPA"))
                {
                    var studentsById = new Dictionary<int, string>();
                    if (HasTable(db, "STUDENT"))
                    {
                        DataTable students = Read(db, "STUDENT");
                        foreach (DataRow student in students.Rows)
                        {
                            if (student["StudentID"] != DBNull.Value)
                                studentsById[Convert.ToInt32(student["StudentID"])] =
                                    Convert.ToString(student["StudentNo"]).Trim();
                        }
                    }
                    foreach (DataRow record in academic.Rows)
                    {
                        string number = academic.Columns.Contains("StudentNo")
                            ? Convert.ToString(record["StudentNo"]).Trim() : "";
                        if (number.Length == 0 && academic.Columns.Contains("StudentID") &&
                            record["StudentID"] != DBNull.Value)
                            studentsById.TryGetValue(Convert.ToInt32(record["StudentID"]),
                                out number);
                        if (string.IsNullOrWhiteSpace(number)) continue;
                        string period = Convert.ToString(record["AcademicPeriod"]).Trim();
                        int split = period.LastIndexOf("-T", StringComparison.OrdinalIgnoreCase);
                        if (split < 0) continue;
                        int year, term;
                        if (!int.TryParse(period.Substring(0, split), out year) ||
                            !int.TryParse(period.Substring(split + 2), out term) ||
                            year != academicYear || term < 1 || term > 4) continue;
                        DataRow row = GetOrAdd(output, index, number, year);
                        string source = academic.Columns.Contains("RecordSource")
                            ? Convert.ToString(record["RecordSource"]) : "";
                        SetValue(row, term, record["GPA"], source, true);
                    }
                }
            }

            if (HasTable(db, "STUDENT_TERM_GPA"))
            {
                DataTable wide = Read(db, "STUDENT_TERM_GPA");
                foreach (DataRow record in wide.Rows)
                {
                    if (record["AcademicYear"] == DBNull.Value ||
                        Convert.ToInt32(record["AcademicYear"]) != academicYear)
                        continue;
                    string number = Convert.ToString(record["StudentNo"]).Trim();
                    if (number.Length == 0) continue;
                    DataRow row = GetOrAdd(output, index, number, academicYear);
                    string source = wide.Columns.Contains("DataOrigin")
                        ? Convert.ToString(record["DataOrigin"]) : "";
                    for (int term = 1; term <= 4; term++)
                        SetValue(row, term, record["T" + term], source, false);
                }
            }
            return output;
        }
    }
}
