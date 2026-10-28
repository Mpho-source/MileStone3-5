using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NextGen__7
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // During development the project database is the single source of truth.
            // A portable build uses the database packaged beside the executable.
            string appFolder = AppDomain.CurrentDomain.BaseDirectory;
            string projectFolder = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(appFolder, "..", ".."));
            bool runningFromProject = System.IO.File.Exists(
                System.IO.Path.Combine(projectFolder, "NextGen_#7.csproj"));
            string databaseFolder = runningFromProject ? projectFolder : appFolder;
            string databaseFile = System.IO.Path.Combine(databaseFolder, "Edu_Connect.accdb");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (!System.IO.File.Exists(databaseFile))
            {
                MessageBox.Show(
                    "EduConnect could not find its database. Ensure Edu_Connect.accdb " +
                    "is included alongside the application, or open the complete " +
                    "Visual Studio project.",
                    "EduConnect database missing", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            AppDomain.CurrentDomain.SetData("DataDirectory", databaseFolder);
            try
            {
                var settings = System.Configuration.ConfigurationManager.ConnectionStrings[
                    "NextGen__7.Properties.Settings.Edu_ConnectConnectionString"];
                if (settings == null)
                    throw new InvalidOperationException("Database configuration is missing.");

                int updated = TutorNumberMigration.Ensure(databaseFile, settings.ConnectionString);
                if (updated > 0)
                    MessageBox.Show(
                        updated + " tutor number(s) saved in the TUTOR database table. " +
                        "Administrators can view them in Manage Tutors, for example Tut0001.",
                        "EduConnect tutor numbers", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "EduConnect could not initialise tutor numbers. " +
                    "No student, tutor or administrator records were deleted. " +
                    "Close Microsoft Access and check that the database is writable.\n\n" +
                    "Database: " + databaseFile + "\n\n" + ex.Message,
                    "EduConnect database update failed", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            Application.Idle += (sender, args) =>
                EduConnectResponsive.AttachToOpenForms();
            using (var login = new Login())
            {
                Application.Run(login);
            }
        }
    }
}
