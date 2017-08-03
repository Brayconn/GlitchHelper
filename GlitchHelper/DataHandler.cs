using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GlitchHelper
{
    class DataHandler
    {
        static FormMain MainForm = new FormMain();
        static HotfileManager HotfileManager = new HotfileManager();

        public DataHandler()
        {
            Application.Run(MainForm);
        }
    }
}
