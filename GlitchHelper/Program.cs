using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GlitchHelper
{
    static class Program
    {
        //public static DataHandler DataHandler { get; set; }
        public static FormMain MainForm { get; set; }
        //public static HotfileManager HotfileManager { get; set; }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //TODO add support for a custom Plugins directory
            string[] args = Environment.GetCommandLineArgs();

            //DataHandler = new DataHandler();
            MainForm = new FormMain(args.Length >= 2 ? args[1] : null);
  
            Application.Run(MainForm);
        }
    }
}
