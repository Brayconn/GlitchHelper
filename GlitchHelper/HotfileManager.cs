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
    public partial class HotfileManager : Form //Would really like to replace this with FormMain, but Visual Studio crashes if I try to reopen anything to do with HotfileManager after I make that change.
    {
        public HotfileManager()
        {
            InitializeComponent();
        }

        private void autoExportToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            /*
            FormMain.autoExport = autoExportToolStripMenuItem.Checked;
            
            if (FormMain.fileLoaded && FormMain.autoExport)
                setOutputFileToolStripMenuItem.Enabled = true;
            else
                setOutputFileToolStripMenuItem.Enabled = false;
            */
            autoExportModeToolStripMenuItem.Enabled =
                overwriteToolStripMenuItem.Enabled =
                iterateToolStripMenuItem.Enabled =
            setOutputFileToolStripMenuItem.Enabled = (FormMain.fileLoaded && (FormMain.autoExport = autoExportToolStripMenuItem.Checked));
        }

        private void setOutputFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            /*
            FormMain.hotfileExportFile = FormMain.SetHotfileOutput() ?? FormMain.hotfileExportFile;
            Text = FormMain.hotfileExportFile ?? "Hotfile Manager";
            */
            FormMain.SetHotfileOutput();
            //Text = (FormMain.hotfileExportFile = FormMain.SetHotfileOutput() ?? FormMain.hotfileExportFile) ?? "Hotfile Manager";
            //Text = (FormMain.hotfileIterationExportFile = FormMain.hotfileExportFile = FormMain.SetHotfileOutput() ?? FormMain.hotfileExportFile) ?? "Hotfile Manager";
            Text = FormMain.hotfileExportFile ?? "Hotfile Manager";
        }

        private void treeView1_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Delete)
            {
                FormMain.RemoveNode(treeView1.SelectedNode);
                /*
                if (treeView1.SelectedNode.Parent != null)
                {
                    List<DataGridViewCell> hotfileData = FormMain.hotfiles[treeView1.SelectedNode.Parent.Text].data.ToList();
                    hotfileData.RemoveAt(treeView1.SelectedNode.Index);
                    FormMain.hotfiles[treeView1.SelectedNode.Parent.Text].data = hotfileData.ToArray();
                    treeView1.SelectedNode.Remove();
                }
                else
                {
                    FormMain.hotfiles.Remove(treeView1.SelectedNode.Parent.Text);
                    treeView1.SelectedNode.Remove();
                }
                FormMain.DisplayHotfilesInManager();
                */
            }
        }

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
        /*
public class hotfile
{
FileSystemWatcher fsw;
List<byte[]> data;

public hotfile(string fileName, List<byte[]> data)
{
this.fsw.Path = Path.GetDirectoryName(fileName);
this.fsw.Filter = Path.GetFileName(fileName);
this.fsw.Changed +=
this.data = data;
}
}

public static List<hotfile> hotFiles = new List<hotfile>();

public void CreateHotfile(List<byte[]> input)
{
SaveFileDialog sfd = new SaveFileDialog();
sfd.Filter = "Text Files (*.txt;)|*.txt|All Files (*.*)|*.*";
sfd.AddExtension = true;
if (sfd.ShowDialog() == DialogResult.OK)
{
hotFiles.Add(new hotfile(sfd.FileName,input));
treeView1.Nodes.Add(sfd.FileName);
for(int i = 0; i < input.Count; i++)
treeView1.Nodes[sfd.FileName].Nodes.Add()
}
}
*/
    }
}
