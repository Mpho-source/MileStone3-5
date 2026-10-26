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
            bool isDemo = string.Equals(
                Environment.GetEnvironmentVariable("EDUCONNECT_DEMO_MODE"), "1",
                StringComparison.Ordinal);
            if (isDemo)
                databaseFolder = System.IO.Path.Combine(databaseFolder, "DemoData");
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
            if (isDemo)
                MessageBox.Show(
                    "DEMONSTRATION MODE\\n\\nThis application is running from the " +
                    "DemoData database copy. Attendance and tutoring-session records " +
                    "may be synthetic test records. The main Edu_Connect.accdb is not used.",
                    "EduConnect DEMO DATABASE", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            if (isDemo)
                Application.Idle += (sender, e) =>
                {
                    foreach (Form form in Application.OpenForms)
                    {
                        if (form != null && !form.IsDisposed &&
                            !form.Text.EndsWith(" [DEMO DATA]", StringComparison.Ordinal))
                            form.Text += " [DEMO DATA]";
                    }
                };
            using (var login = new Login())
            {
                if (isDemo) login.Text += " [DEMO DATA]";
                Application.Run(login);
            }
        }
    }
}
