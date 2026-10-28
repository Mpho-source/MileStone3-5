using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;

namespace NextGen__7
{
    internal static class TutorNumberMigration
    {
        private static bool HasTutorNumberColumn(OleDbConnection db)
        {
            using (var cmd = new OleDbCommand("SELECT * FROM [TUTOR] WHERE 1=0", db))
            using (var reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
            {
                foreach (DataRow row in reader.GetSchemaTable().Rows)
                    if (string.Equals(Convert.ToString(row["ColumnName"]).Trim('\uFEFF', ' '),
                        "TutorNumber", StringComparison.OrdinalIgnoreCase))
                        return true;
            }
            return false;
        }

        internal static int Ensure(string databasePath, string connectionString)
        {
            if (File.Exists(Path.ChangeExtension(databasePath, ".laccdb")))
                throw new InvalidOperationException(
                    "Close Microsoft Access and any other EduConnect window before updating tutor numbers.");

            var records = new List<Tuple<int, string>>();
            bool hasColumn;
            using (var db = new OleDbConnection(connectionString))
            {
                db.Open();
                hasColumn = HasTutorNumberColumn(db);
                using (var cmd = new OleDbCommand(
                    hasColumn
                        ? "SELECT [TutorID],[TutorNumber] FROM [TUTOR]"
                        : "SELECT [TutorID] FROM [TUTOR]", db))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.IsDBNull(0))
                            throw new InvalidOperationException(
                                "A tutor has no TutorID. The database was not modified.");

                        int tutorId = Convert.ToInt32(reader[0]);
                        if (tutorId < 1)
                            throw new InvalidOperationException(
                                "A tutor has an invalid TutorID. The database was not modified.");

                        string expected = "Tut" + tutorId.ToString("D4");
                        string current = hasColumn && !reader.IsDBNull(1)
                            ? Convert.ToString(reader[1]) : "";
                        if (!string.Equals(current, expected, StringComparison.Ordinal))
                            records.Add(Tuple.Create(tutorId, expected));
                    }
                }
            }

            if (hasColumn && records.Count == 0)
                return 0;

            string backupPath = databasePath + ".tutor-numbers-" +
                DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".bak";
            File.Copy(databasePath, backupPath, false);

            using (var db = new OleDbConnection(connectionString))
            {
                db.Open();
                if (!HasTutorNumberColumn(db))
                {
                    using (var cmd = new OleDbCommand(
                        "ALTER TABLE [TUTOR] ADD COLUMN [TutorNumber] TEXT(16)", db))
                        cmd.ExecuteNonQuery();
                }

                using (var tx = db.BeginTransaction())
                {
                    try
                    {
                        foreach (var record in records)
                        {
                            using (var cmd = new OleDbCommand(
                                "UPDATE [TUTOR] SET [TutorNumber]=? WHERE [TutorID]=?", db, tx))
                            {
                                cmd.Parameters.Add("@TutorNumber", OleDbType.VarWChar).Value =
                                    record.Item2;
                                cmd.Parameters.Add("@TutorID", OleDbType.Integer).Value =
                                    record.Item1;
                                if (cmd.ExecuteNonQuery() != 1)
                                    throw new InvalidOperationException(
                                        "Tutor number could not be saved for TutorID " + record.Item1 + ".");
                            }
                        }
                        tx.Commit();
                    }
                    catch
                    {
                        try { tx.Rollback(); }
                        catch { }
                        throw;
                    }
                }
            }

            return records.Count;
        }
    }
}
