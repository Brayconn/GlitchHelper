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
    //TODO
    //Would really like to replace "Form" with "FormMain", but Visual Studio crashes if I try to reopen anything to do with HotfileManager after I make that change.
    //...Also not sure I really need to do that anymore...?
    public partial class HotfileManager : Form
    {
        private DataHandler dataHandler { get; set; }
        private static FormMain mainForm { get; set; }

        public HotfileManager(DataHandler dh, FormMain fm)
        {
            dataHandler = dh;
            dataHandler.FileLoaded += FileLoaded;
            dataHandler.HotfileChanged += HotfileChanged;
            dataHandler.HotfileDeleted += HotfileDeleted;
            
            mainForm = fm;

            InitializeComponent();
        }

        //HACK...maybe?
        private delegate void HotfileChangedDelegate(object sender, DataHandler.HotfileModifiedEventArgs e);
        private void HotfileChanged(object sender, DataHandler.HotfileModifiedEventArgs e)
        {
            if (treeView1.InvokeRequired)
            {
                /*
                HotfileChangedDelegate hc = new HotfileChangedDelegate(HotfileChanged);
                this.Invoke(hc, new object[] { sender, e });
                */
                Invoke(new HotfileChangedDelegate(HotfileChanged), new object[] { sender, e });
            }
            else
            {
                //TODO maybe I should be using this instead:
                //if (!treeView1.Nodes[e.name] == null)
                if (!treeView1.Nodes.ContainsKey(e.name))
                    treeView1.Nodes.Add(e.name, e.name);

                treeView1.Nodes[e.name].Nodes.Clear();
                treeView1.Nodes[e.name].Nodes.AddRange(dataHandler.plugins.ElementAt(dataHandler.selectedPlugin).GetHotfileInfo(e.contents));
            }
        }

        //HACK...maybe?
        private delegate void HotfileDeletedDelegate(object sender, DataHandler.HotfileDeletedEventArgs e);
        private void HotfileDeleted(object sender, DataHandler.HotfileDeletedEventArgs e)
        {
            if(treeView1.InvokeRequired)
            {
                /*
                HotfileDeletedDelegate hd = new HotfileDeletedDelegate(HotfileDeleted);
                this.Invoke(hd, new object[] { sender, e });
                */
                Invoke(new HotfileDeletedDelegate(HotfileDeleted), new object[] { sender, e });
            }
            else
                treeView1.Nodes[e.name].Remove();
        }

        private void autoExportToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            //Only enable all buttons relating to AutoExporting if we have a file loaded, and the user has auto export mode on.
            autoExportModeToolStripMenuItem.Enabled =
                overwriteToolStripMenuItem.Enabled =
                iterateToolStripMenuItem.Enabled =
            setOutputFileToolStripMenuItem.Enabled = (dataHandler.saveableFileType != null && (dataHandler.autoExport = autoExportToolStripMenuItem.Checked));
        }

        private void setOutputFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //TODO since FormMain and HotfileManager both need to access this, I should maybe move it to a new class?
            mainForm.SetHotfileOutput();
            //If the hotfileExportFile is still null by this point, set the text to it's default value
            Text = dataHandler.hotfileExportFile ?? "Hotfile Manager";
        }

        private void treeView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                dataHandler.RemoveNode(treeView1.SelectedNode);
            }
        }
        
        //Despite the fact these are both checkboxes, I have them set up to act like radio buttons
        private void overwriteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataHandler.iterateMode = false;
            iterateToolStripMenuItem.Checked = false;
            overwriteToolStripMenuItem.Checked = true;
        }
        private void iterateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataHandler.iterateMode = true;
            iterateToolStripMenuItem.Checked = true;
            overwriteToolStripMenuItem.Checked = false;
        }
        

        public void FileLoaded(object o, EventArgs e)
        {
            this.Text = "Hotfile Manager";

            this.setOutputFileToolStripMenuItem.Enabled = true;
            this.autoExportModeToolStripMenuItem.Enabled = true;
            this.autoExportToolStripMenuItem.Enabled = true;
            this.overwriteToolStripMenuItem.Enabled = true;
            this.iterateToolStripMenuItem.Enabled = true;
        }
    }
}
