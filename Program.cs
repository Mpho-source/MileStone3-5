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
            // In Visual Studio, point every form at the project database rather
            // than a separate copy in bin\\Debug. In deployment, use the database
            // distributed next to the executable.
            string exeFolder = AppDomain.CurrentDomain.BaseDirectory;
            string projectFolder = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(exeFolder, "..", ".."));
            string projectDatabase = System.IO.Path.Combine(
                projectFolder, "Edu_Connect.accdb");
            string databaseFolder = System.IO.File.Exists(projectDatabase)
                ? projectFolder : exeFolder;
            AppDomain.CurrentDomain.SetData("DataDirectory", databaseFolder);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Login());
        }
    }
}
