using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GlitchHelper
{
    static class Program
    {
        public static DataHandler DataHandler { get; set; }
        public static FormMain MainForm { get; set; }
        public static HotfileManager HotfileManager { get; set; }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            DataHandler = new DataHandler();
            MainForm = new FormMain(DataHandler);

            //DataHandler.FileLoaded += MainForm.FileLoaded;
            //DataHandler.FileLoaded += HotfileManager.FileLoaded;
  
            Application.Run(MainForm);
        }
    }
}
