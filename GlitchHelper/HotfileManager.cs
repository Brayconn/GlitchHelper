using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GlitchHelper
{
    //Would really like to replace "Form" with "FormMain", but Visual Studio crashes if I try to reopen anything to do with HotfileManager after I make that change.
    public partial class HotfileManager : Form 
    {
        public HotfileManager()
        {
            InitializeComponent();
        }

        private void autoExportToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            //Only enable all buttons relating to AutoExporting if we have a file loaded, and the user has auto export mode on.
            autoExportModeToolStripMenuItem.Enabled =
                overwriteToolStripMenuItem.Enabled =
                iterateToolStripMenuItem.Enabled =
            setOutputFileToolStripMenuItem.Enabled = (FormMain.fileLoaded && (FormMain.autoExport = autoExportToolStripMenuItem.Checked));
        }

        private void setOutputFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormMain.SetHotfileOutput();
            //If the hotfileExportFile is still null by this point, set the text to it's default value
            Text = FormMain.hotfileExportFile ?? "Hotfile Manager";
        }

        private void treeView1_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Delete)
            {
                FormMain.RemoveNode(treeView1.SelectedNode);
            }
        }

        //Despite the fact these are both checkboxes, I have them set up to act like radio buttons
        private void overwriteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormMain.iterateMode = //false;
            iterateToolStripMenuItem.Checked = false;
            overwriteToolStripMenuItem.Checked = true;
        }
        private void iterateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormMain.iterateMode = //true;
            iterateToolStripMenuItem.Checked = true;
            overwriteToolStripMenuItem.Checked = false;           
        }
    }
}
