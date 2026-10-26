using System;
using System.Data;
using System.Data.OleDb;
using System.Security.Cryptography;

namespace NextGen__7
{
    internal static class TutorAuthentication
    {
        private const int Iterations = 120000;
        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

        private static bool HasLoginTable(OleDbConnection db)
        {
            DataTable tables = db.GetSchema("Tables");
            foreach (DataRow row in tables.Rows)
            {
                if (string.Equals(Convert.ToString(row["TABLE_NAME"]), "TUTOR_LOGIN",
                    StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static byte[] DerivePassword(string password, byte[] salt, int iterations)
        {
            using (var derivation = new Rfc2898DeriveBytes(password, salt, iterations))
                return derivation.GetBytes(32);
        }

        private static bool FixedTimeEqual(byte[] actual, byte[] expected)
        {
            if (actual.Length != expected.Length) return false;
            int diff = 0;
            for (int i = 0; i < actual.Length; ++i)
                diff |= actual[i] ^ expected[i];
            return diff == 0;
        }

        private static string NewTemporaryPassword()
        {
            var bytes = new byte[18];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            char[] password = new char[bytes.Length];
            for (int i = 0; i < bytes.Length; i++)
                password[i] = Alphabet[bytes[i] % Alphabet.Length];
            return new string(password);
        }

        // Only administrator-facing tutor-management code calls this method.
        // A new temporary password is returned once and is never stored as plaintext.
        internal static string Provision(OleDbConnection db, int tutorId)
        {
            if (tutorId <= 0) throw new ArgumentOutOfRangeException("tutorId");
            if (!HasLoginTable(db))
            {
                using (var create = new OleDbCommand(
                    "CREATE TABLE [TUTOR_LOGIN] " +
                    "([TutorID] LONG PRIMARY KEY, [Salt] TEXT(64), " +
                    "[PasswordHash] TEXT(128), [Iterations] LONG)", db))
                    create.ExecuteNonQuery();
            }

            using (var exists = new OleDbCommand(
                "SELECT COUNT(*) FROM [TUTOR_LOGIN] WHERE [TutorID]=?", db))
            {
                exists.Parameters.Add("@TutorID", OleDbType.Integer).Value = tutorId;
                if (Convert.ToInt32(exists.ExecuteScalar()) > 0)
                    throw new InvalidOperationException(
                        "This tutor already has a login. Existing credentials were not reset.");
            }

            string password = NewTemporaryPassword();
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            string saltValue = Convert.ToBase64String(salt);
            string hash = Convert.ToBase64String(DerivePassword(password, salt, Iterations));

            using (var command = new OleDbCommand(
                "INSERT INTO [TUTOR_LOGIN] " +
                "([TutorID],[Salt],[PasswordHash],[Iterations]) VALUES (?,?,?,?)", db))
            {
                command.Parameters.Add("@TutorID", OleDbType.Integer).Value = tutorId;
                command.Parameters.Add("@Salt", OleDbType.VarWChar).Value = saltValue;
                command.Parameters.Add("@Hash", OleDbType.VarWChar).Value = hash;
                command.Parameters.Add("@Iterations", OleDbType.Integer).Value = Iterations;
                if (command.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException("Unable to create tutor credentials.");
            }
            return password;
        }

        internal static bool HasAccount(OleDbConnection db, int tutorId)
        {
            if (tutorId <= 0 || !HasLoginTable(db)) return false;
            using (var cmd = new OleDbCommand(
                "SELECT COUNT(*) FROM [TUTOR_LOGIN] WHERE [TutorID]=?", db))
            {
                cmd.Parameters.Add("@TutorID", OleDbType.Integer).Value = tutorId;
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private static void ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 12 ||
                password.Length > 128)
                throw new ArgumentException(
                    "Use a new password of 12 to 128 characters.");

            bool upper = false, lower = false, digit = false;
            foreach (char c in password)
            {
                if (char.IsUpper(c)) upper = true;
                if (char.IsLower(c)) lower = true;
                if (char.IsDigit(c)) digit = true;
            }
            if (!upper || !lower || !digit)
                throw new ArgumentException(
                    "Include at least one uppercase letter, lowercase letter and number.");
        }

        // The signed-in tutor must prove knowledge of the existing password.
        internal static void ChangePassword(OleDbConnection db, int tutorId,
            string currentPassword, string newPassword)
        {
            if (tutorId <= 0) throw new ArgumentOutOfRangeException("tutorId");
            ValidatePassword(newPassword);
            if (currentPassword == newPassword)
                throw new ArgumentException("Your new password must differ from the current one.");

            string storedSalt;
            string storedHash;
            int iterations;
            using (var cmd = new OleDbCommand(
                "SELECT [Salt],[PasswordHash],[Iterations] " +
                "FROM [TUTOR_LOGIN] WHERE [TutorID]=?", db))
            {
                cmd.Parameters.Add("@TutorID", OleDbType.Integer).Value = tutorId;
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        throw new InvalidOperationException("Tutor account was not found.");
                    storedSalt = Convert.ToString(reader[0]);
                    storedHash = Convert.ToString(reader[1]);
                    iterations = Convert.ToInt32(reader[2]);
                }
            }

            if (iterations < 10000 || iterations > 2000000 ||
                !FixedTimeEqual(
                    DerivePassword(currentPassword,
                        Convert.FromBase64String(storedSalt), iterations),
                    Convert.FromBase64String(storedHash)))
                throw new UnauthorizedAccessException("The current password is incorrect.");

            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            string newSalt = Convert.ToBase64String(salt);
            string newHash = Convert.ToBase64String(DerivePassword(newPassword, salt, Iterations));
            using (var cmd = new OleDbCommand(
                "UPDATE [TUTOR_LOGIN] SET [Salt]=?, [PasswordHash]=?, " +
                "[Iterations]=? WHERE [TutorID]=? AND [Salt]=? AND [PasswordHash]=?", db))
            {
                cmd.Parameters.Add("@Salt", OleDbType.VarWChar).Value = newSalt;
                cmd.Parameters.Add("@Hash", OleDbType.VarWChar).Value = newHash;
                cmd.Parameters.Add("@Iterations", OleDbType.Integer).Value = Iterations;
                cmd.Parameters.Add("@TutorID", OleDbType.Integer).Value = tutorId;
                cmd.Parameters.Add("@OldSalt", OleDbType.VarWChar).Value = storedSalt;
                cmd.Parameters.Add("@OldHash", OleDbType.VarWChar).Value = storedHash;
                if (cmd.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException(
                        "The account changed while updating. Please sign in again.");
            }
        }

        // Administrator recovery replaces the existing password with a new
        // one-time temporary password. It cannot reveal the old password.
        internal static string ResetPassword(OleDbConnection db, int tutorId)
        {
            if (!HasAccount(db, tutorId))
                throw new InvalidOperationException("The tutor does not have a login yet.");

            string temporary = NewTemporaryPassword();
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            using (var cmd = new OleDbCommand(
                "UPDATE [TUTOR_LOGIN] SET [Salt]=?, [PasswordHash]=?, " +
                "[Iterations]=? WHERE [TutorID]=?", db))
            {
                cmd.Parameters.Add("@Salt", OleDbType.VarWChar).Value = Convert.ToBase64String(salt);
                cmd.Parameters.Add("@Hash", OleDbType.VarWChar).Value =
                    Convert.ToBase64String(DerivePassword(temporary, salt, Iterations));
                cmd.Parameters.Add("@Iterations", OleDbType.Integer).Value = Iterations;
                cmd.Parameters.Add("@TutorID", OleDbType.Integer).Value = tutorId;
                if (cmd.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException("Unable to reset the tutor account.");
            }
            return temporary;
        }

        internal static bool TryAuthenticate(OleDbConnection db, int tutorId,
            string password, out string tutorName)
        {
            tutorName = null;
            if (tutorId <= 0 || string.IsNullOrEmpty(password) || !HasLoginTable(db))
                return false;

            byte[] salt;
            byte[] expected;
            int iterations;
            using (var command = new OleDbCommand(
                "SELECT [Salt],[PasswordHash],[Iterations] FROM [TUTOR_LOGIN] " +
                "WHERE [TutorID]=?", db))
            {
                command.Parameters.Add("@TutorID", OleDbType.Integer).Value = tutorId;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return false;
                    salt = Convert.FromBase64String(Convert.ToString(reader[0]));
                    expected = Convert.FromBase64String(Convert.ToString(reader[1]));
                    iterations = Convert.ToInt32(reader[2]);
                }
            }
            if (iterations < 10000 || iterations > 2000000) return false;
            if (!FixedTimeEqual(DerivePassword(password, salt, iterations), expected))
                return false;

            using (var command = new OleDbCommand(
                "SELECT [FirstName],[LastName] FROM [TUTOR] WHERE [TutorID]=?", db))
            {
                command.Parameters.Add("@TutorID", OleDbType.Integer).Value = tutorId;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return false;
                    tutorName = (Convert.ToString(reader[0]) + " " +
                        Convert.ToString(reader[1])).Trim();
                }
            }
            return true;
        }
    }
}
