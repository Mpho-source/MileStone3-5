using System;
using System.Data;
using System.Data.OleDb;
using System.Security.Cryptography;

namespace NextGen__7
{
    internal static class RolePasswords
    {
        private const int Iterations = 120000;

        private static void Names(string role, out string loginTable,
            out string userTable, out string key, out bool staff)
        {
            staff = string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase);
            if (!staff && !string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Unsupported login role.");
            loginTable = staff ? "ADMIN_LOGIN" : "STUDENT_LOGIN";
            userTable = staff ? "ADMIN" : "STUDENT";
            key = staff ? "AdminNo" : "StudentNo";
        }

        private static bool TableExists(OleDbConnection db, string table)
        {
            foreach (DataRow row in db.GetSchema("Tables").Rows)
                if (string.Equals(Convert.ToString(row["TABLE_NAME"]), table,
                    StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static void EnsureTable(OleDbConnection db, string table, string key)
        {
            if (TableExists(db, table)) return;
            using (var cmd = new OleDbCommand(
                "CREATE TABLE [" + table + "] ([" + key + "] TEXT(50) PRIMARY KEY, " +
                "[Salt] TEXT(64), [PasswordHash] TEXT(128), [Iterations] LONG)", db))
                cmd.ExecuteNonQuery();
        }

        internal static bool HasAnyAdminLogin(OleDbConnection db)
        {
            if (!TableExists(db, "ADMIN_LOGIN")) return false;
            using (var cmd = new OleDbCommand("SELECT COUNT(*) FROM [ADMIN_LOGIN]", db))
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private static byte[] Derive(string pwd, byte[] salt, int iters)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(pwd, salt, iters))
                return pbkdf2.GetBytes(32);
        }

        private static bool ConstantTimeEqual(byte[] left, byte[] right)
        {
            if (left.Length != right.Length) return false;
            int difference = 0;
            for (int i = 0; i < left.Length; i++)
                difference |= left[i] ^ right[i];
            return difference == 0;
        }

        internal static void ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 12 || password.Length > 128)
                throw new ArgumentException("Password must contain 12 to 128 characters.");
            bool upper = false, lower = false, digit = false;
            foreach (char c in password)
            {
                if (char.IsUpper(c)) upper = true;
                if (char.IsLower(c)) lower = true;
                if (char.IsDigit(c)) digit = true;
            }
            if (!upper || !lower || !digit)
                throw new ArgumentException(
                    "Password needs uppercase and lowercase letters and a number.");
        }

        private static bool IdentityExists(OleDbConnection db, string role,
            string accountNo)
        {
            string table, source, key; bool staff;
            Names(role, out table, out source, out key, out staff);
            using (var cmd = new OleDbCommand(
                "SELECT COUNT(*) FROM [" + source + "] WHERE [" + key + "]=?" +
                (staff ? " AND [IsActive]=True" : ""), db))
            {
                cmd.Parameters.Add("@No", OleDbType.VarWChar).Value = accountNo;
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        internal static bool HasAccount(OleDbConnection db, string role, string accountNo)
        {
            string table, source, key; bool staff;
            Names(role, out table, out source, out key, out staff);
            if (!TableExists(db, table)) return false;
            using (var cmd = new OleDbCommand(
                "SELECT COUNT(*) FROM [" + table + "] WHERE [" + key + "]=?", db))
            {
                cmd.Parameters.Add("@No", OleDbType.VarWChar).Value = accountNo.Trim();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        internal static bool Verify(OleDbConnection db, string role,
            string accountNo, string password)
        {
            if (string.IsNullOrWhiteSpace(accountNo) || string.IsNullOrEmpty(password))
                return false;
            string table, source, key; bool staff;
            Names(role, out table, out source, out key, out staff);
            accountNo = accountNo.Trim();
            if (!TableExists(db, table) || !IdentityExists(db, role, accountNo)) return false;
            using (var cmd = new OleDbCommand(
                "SELECT [Salt],[PasswordHash],[Iterations] FROM [" + table + "] " +
                "WHERE [" + key + "]=?", db))
            {
                cmd.Parameters.Add("@No", OleDbType.VarWChar).Value = accountNo;
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return false;
                    try
                    {
                        byte[] salt = Convert.FromBase64String(Convert.ToString(reader[0]));
                        byte[] expected = Convert.FromBase64String(Convert.ToString(reader[1]));
                        int count = Convert.ToInt32(reader[2]);
                        if (count < 10000 || count > 2000000) return false;
                        return ConstantTimeEqual(Derive(password, salt, count), expected);
                    }
                    catch (FormatException) { return false; }
                }
            }
        }

        // Only a verified administrator can call this from Account Management.
        // It creates or replaces a student/administrator credential with the
        // specified password. Old passwords cannot be recovered.
        internal static void SetPassword(OleDbConnection db, string role,
            string accountNo, string password)
        {
            ValidatePassword(password);
            if (string.IsNullOrWhiteSpace(accountNo))
                throw new ArgumentException("Enter a student or administrator number.");
            accountNo = accountNo.Trim();
            string table, source, key; bool staff;
            Names(role, out table, out source, out key, out staff);
            if (!IdentityExists(db, role, accountNo))
                throw new InvalidOperationException(
                    "The selected active " + role.ToLowerInvariant() + " record does not exist.");
            EnsureTable(db, table, key);
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            string saltStr = Convert.ToBase64String(salt);
            string hashStr = Convert.ToBase64String(Derive(password, salt, Iterations));
            bool exists = HasAccount(db, role, accountNo);
            string sql = exists
                ? "UPDATE [" + table + "] SET [Salt]=?,[PasswordHash]=?,[Iterations]=?" +
                  " WHERE [" + key + "]=?"
                : "INSERT INTO [" + table + "] ([Salt],[PasswordHash],[Iterations],[" +
                  key + "]) VALUES (?,?,?,?)";
            using (var cmd = new OleDbCommand(sql, db))
            {
                cmd.Parameters.Add("@Salt", OleDbType.VarWChar).Value = saltStr;
                cmd.Parameters.Add("@Hash", OleDbType.VarWChar).Value = hashStr;
                cmd.Parameters.Add("@Iterations", OleDbType.Integer).Value = Iterations;
                cmd.Parameters.Add("@Account", OleDbType.VarWChar).Value = accountNo;
                if (cmd.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException("The password could not be saved.");
            }
        }

        // First administrator bootstrap requires a locally supplied secret.
        // No default/setup password is stored in source or the database.
        internal static void InitializeAdministrator(OleDbConnection db, string adminNo,
            string setupKey, string password)
        {
            string expected = Environment.GetEnvironmentVariable("EDUCONNECT_ADMIN_SETUP_KEY");
            if (string.IsNullOrWhiteSpace(expected) || expected.Length < 16)
                throw new InvalidOperationException(
                    "The computer administrator must set EDUCONNECT_ADMIN_SETUP_KEY " +
                    "(at least 16 characters) before first-time setup.");
            byte[] left = System.Text.Encoding.UTF8.GetBytes(setupKey ?? "");
            byte[] right = System.Text.Encoding.UTF8.GetBytes(expected);
            if (!ConstantTimeEqual(left, right))
                throw new UnauthorizedAccessException("Setup key is incorrect.");
            if (HasAnyAdminLogin(db))
                throw new InvalidOperationException(
                    "Administrator accounts already exist. Sign in and use Manage accounts.");
            SetPassword(db, "Staff", adminNo, password);
        }

        internal static void ChangePassword(OleDbConnection db, string role,
            string accountNo, string oldPassword, string newPassword)
        {
            ValidatePassword(newPassword);
            if (newPassword == oldPassword)
                throw new ArgumentException("Choose a new password.");
            if (!Verify(db, role, accountNo, oldPassword))
                throw new UnauthorizedAccessException("Current password is incorrect.");
            SetPassword(db, role, accountNo, newPassword);
        }
    }
}
