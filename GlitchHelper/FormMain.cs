using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Collections.Specialized;
using System.IO.Compression;
using System.Reflection;
using PluginBase;
using System.Text.RegularExpressions;

namespace GlitchHelper
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }

        public static HotfileManager hotfileManager = new HotfileManager();

        private void Form1_Load(object sender, EventArgs e)
        {
            //Hotfile manager menu
            hotfileManager.FormClosing += (_o, _e) => { _e.Cancel = true; toggleForm(hotfileManager, viewHotfileManager, false); };
            viewHotfileManager.CheckedChanged += delegate { toggleForm(hotfileManager, viewHotfileManager, viewHotfileManager.Checked); };

            //Plugin loading stuff
            plugins = PluginLoader<IPlugin>.LoadPlugins(Path.Combine(Directory.GetCurrentDirectory(), @"Plugins"));

            if (plugins.Count <= 0 || plugins == null)
            {
                MessageBox.Show("No Plugins Found. ;(");
                Close();
            }
            else
            {
                for (int i = 0; i < plugins.Count; i++)
                    openableFileTypes += plugins.ElementAt(i).filter + "|";
                openableFileTypes += "All Files (*.*)|*.*";
            }
            //dataGridView1.DataSource = openFile;
            //var items = defaultContextMenuStrip.Items;
            //while(items.Count > 0)
            //{
            //    dcms.Items.Add(items[0]);
            //}
            //lcms = dcms;

            lcms.Items.AddRange(dtsmi);
            selectedPlugin = plugins.Count;
        }

        private void toggleForm(Form form, ToolStripMenuItem button, bool state)
        {
            button.Checked = state;
            if (state)
                form.Show();
            else
                form.Hide();
        }

        #region Plugin Variables
        public static ICollection<IPlugin> plugins = null;
        public static int selectedPlugin;
        #endregion

        public static DataGridViewCell[] selectedCells;

        //public static bool cellsSelected = false;

        public static string openableFileTypes = null;
        public static string saveableFileType = null;
        public static bool fileLoaded = false;
        public static string savedFileLocation = null;

        /// <summary>
        /// Default Tool Strip Menu Items
        /// </summary>
        public static readonly ToolStripItem[] dtsmi =
        {
            new ToolStripMenuItem("Export Selected...", null, delegate { exportSelectedToolStripMenuItem_Click(); }),
            new ToolStripMenuItem("Replace Selected...", null, delegate { replaceSelectedToolStripMenuItem_Click(); }),
            new ToolStripSeparator(),
            new ToolStripMenuItem("Create Hotfile...", null, delegate { createHotfileToolStripMenuItem_Click(); }),
            new ToolStripMenuItem()
            {
                Text = "Add to Hotfile",
                Name = "addToHotfileToolStripMenuItem"
            },
            new ToolStripSeparator()
        };

        /// <summary>
        /// Live Context Menu Strip
        /// </summary>
        public static ContextMenuStrip lcms = new ContextMenuStrip();

        #region Hotfile Variables

        public static Dictionary<string, Hotfile> hotfiles = new Dictionary<string, Hotfile>();
        public static Dictionary<DataGridViewCell, string> cellsInHotfiles = new Dictionary<DataGridViewCell, string>();
        public static string hotfileExportFile = null;
        public static string hotfileIterationExportFile = null;
        public static bool autoExport = true;
        public static bool iterateMode = false;
        public static long iterateCount = 1;

        public class Hotfile : FormMain
        {
            public FileSystemWatcher fileWatcher { get; set; }
            public DataGridViewCell[] data { get; set; }

            public Hotfile(string path, DataGridViewCell[] cells)
            {
                fileWatcher = new FileSystemWatcher(Path.GetDirectoryName(path), Path.GetFileName(path));
                data = cells;
                fileWatcher.EnableRaisingEvents = true;
                fileWatcher.Changed += delegate { Hotfile_Changed(fileWatcher, data); };
                fileWatcher.Renamed += (o, e) => { Hotfile_Renamed(e); };
                fileWatcher.Deleted += delegate { Hotfile_Deleted(Path.Combine(fileWatcher.Path, fileWatcher.Filter)); };
            }
        }

        #endregion

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Create new OpenFileDialog
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter = openableFileTypes
            };
            //If the user selected something and said ok, then we can start working
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                #region Plugin Selection stuff

                //Store the selected plugin
                int newSelectedPlugin = ofd.FilterIndex - 1;

                //If the user picked the last thing on the list, that means they selected "All Files", so we'll need to ask them what to import this file as
                if (newSelectedPlugin == plugins.Count)
                {
                    MessageBox.Show("This is the part where I would bring up a thing asking you what you want to import the file as, but I haven't made that yet, sooooo-");
                    return;
                }

                //Attempt to load the selected file
                saveableFileType = plugins.ElementAt(newSelectedPlugin).Load(File.ReadAllBytes(ofd.FileName));
                //If that operation returned null, stop, that means something went wrong
                if (saveableFileType == null)
                    return;

                //If we've gotten this far, the new selected plugin is fine, so save it.
                selectedPlugin = newSelectedPlugin;

                #endregion

                #region Unloading stuff

                //Reset the title to be "Glitch Helper"
                Text = "Glitch Helper";

                //Reset the iterateCount
                iterateCount = 1;

                //Tell the program we have nothing loaded anymore (even though we technically do)
                fileLoaded = //false;

                //Disable all buttons dependant on a file being loaded
                //Main form
                (menuStrip1.Items["fileToolStripMenuItem"] as ToolStripMenuItem).DropDown.Items["saveToolStripMenuItem"].Enabled =
                (menuStrip1.Items["fileToolStripMenuItem"] as ToolStripMenuItem).DropDown.Items["saveAsToolStripMenuItem"].Enabled =
                (menuStrip1.Items["fileToolStripMenuItem"] as ToolStripMenuItem).DropDown.Items["editHeaderToolStripMenuItem"].Enabled =
                //Hotfile Manager
                (hotfileManager.menuStrip1.Items["fileToolStripMenuItem"] as ToolStripMenuItem).DropDown.Items["setOutputFileToolStripMenuItem"].Enabled =
                (hotfileManager.menuStrip1.Items["fileToolStripMenuItem"] as ToolStripMenuItem).DropDown.Items["autoExportToolStripMenuItem"].Enabled =
                (hotfileManager.menuStrip1.Items["fileToolStripMenuItem"] as ToolStripMenuItem).DropDown.Items["autoExportModeToolStripMenuItem"].Enabled =
                ((hotfileManager.menuStrip1.Items["fileToolStripMenuItem"] as ToolStripMenuItem).DropDown.Items["autoExportModeToolStripMenuItem"] as ToolStripMenuItem).DropDown.Items["overwriteToolStripMenuItem"].Enabled =
                ((hotfileManager.menuStrip1.Items["fileToolStripMenuItem"] as ToolStripMenuItem).DropDown.Items["autoExportModeToolStripMenuItem"] as ToolStripMenuItem).DropDown.Items["iterateToolStripMenuItem"].Enabled =
                false;

                //Remove any custom plugin options
                /*
                while ((menuStrip1.Items["pluginOptionsToolStripMenuItem"] as ToolStripMenuItem).DropDown.Items.Count > 0)
                    (menuStrip1.Items["pluginOptionsToolStripMenuItem"] as ToolStripMenuItem).DropDown.Items.RemoveAt(0);
                */
                (menuStrip1.Items["pluginOptionsToolStripMenuItem"] as ToolStripMenuItem).DropDown.Items.Clear();


                //Remove everything from the contextMenuStrip
                lcms.Items.Clear();

                //Re populate the context menu strip with the default buttons
                lcms.Items.AddRange(dtsmi);

                //Clear the datagridview
                dataGridView1.Columns.Clear();
                dataGridView1.Rows.Clear();

                //Reset hotfile variables
                for (int i = 0; i < hotfiles.Count; i++)
                    hotfiles.ElementAt(i).Value.fileWatcher.Dispose();
                hotfiles.Clear();

                cellsInHotfiles.Clear();
                hotfileManager.Text = "Hotfile Manager";
                hotfileExportFile = //null;
                hotfileIterationExportFile = null;
                UpdateHotfileContextMenuStrip();
                DisplayHotfilesInManager();

                //Reset where the user saved a file last
                savedFileLocation = null;


                #endregion

                #region Loading stuff

                //Set the title to be the opened file
                Text = ofd.FileName;

                //Actually load the file
                dataGridView1.Columns.AddRange(plugins.ElementAt(selectedPlugin).columns);
                dataGridView1.Rows.AddRange(plugins.ElementAt(selectedPlugin).GetRows());
                //plugins.ElementAt(selectedPlugin).PopulateRows(dataGridView1.Rows);

                //dataGridView1.DataSource = plugins.ElementAt(selectedPlugin).Temp();

                (menuStrip1.Items["pluginOptionsToolStripMenuItem"] as ToolStripMenuItem).DropDown.Items.AddRange(plugins.ElementAt(selectedPlugin).customMenuStripOptions);

                //Add any custom contextMenuStripItems to the contextMenuStrip
                //lcms.Items.AddRange(plugins.ElementAt(selectedPlugin).getCustomContextMenuStripItems());
                lcms.Items.AddRange(plugins.ElementAt(selectedPlugin).customContextMenuStripButtons);
                //Also, set the context menu strip to the one we've been editing (probably only matters the first time the program starts up)
                dataGridView1.ContextMenuStrip = lcms;

                //Tell the program we have a file loaded now
                fileLoaded = //true;

                //Re enable all those buttons from earlier
                //Main form
                (menuStrip1.Items["fileToolStripMenuItem"] as ToolStripMenuItem).DropDown.Items["saveToolStripMenuItem"].Enabled =
                (menuStrip1.Items["fileToolStripMenuItem"] as ToolStripMenuItem).DropDown.Items["saveAsToolStripMenuItem"].Enabled =
                (menuStrip1.Items["fileToolStripMenuItem"] as ToolStripMenuItem).DropDown.Items["editHeaderToolStripMenuItem"].Enabled =
                //Hotfile Manager
                (hotfileManager.menuStrip1.Items["fileToolStripMenuItem"] as ToolStripMenuItem).DropDown.Items["setOutputFileToolStripMenuItem"].Enabled =
                (hotfileManager.menuStrip1.Items["fileToolStripMenuItem"] as ToolStripMenuItem).DropDown.Items["autoExportToolStripMenuItem"].Enabled =
                (hotfileManager.menuStrip1.Items["fileToolStripMenuItem"] as ToolStripMenuItem).DropDown.Items["autoExportModeToolStripMenuItem"].Enabled =
                ((hotfileManager.menuStrip1.Items["fileToolStripMenuItem"] as ToolStripMenuItem).DropDown.Items["autoExportModeToolStripMenuItem"] as ToolStripMenuItem).DropDown.Items["overwriteToolStripMenuItem"].Enabled =
                ((hotfileManager.menuStrip1.Items["fileToolStripMenuItem"] as ToolStripMenuItem).DropDown.Items["autoExportModeToolStripMenuItem"] as ToolStripMenuItem).DropDown.Items["iterateToolStripMenuItem"].Enabled =
                true;
                
                #endregion
            }
        }

        #region Export/Replace Code

        /// <summary>
        /// Fires whenever the user clicks the "Export Selected" Context Menu Strip item.
        /// Prompts the user to pick where they'd like to export the selected cells to.
        /// </summary>
        private static void exportSelectedToolStripMenuItem_Click()
        {
            //If the user had selected some cells, then we can do stuff
            if (selectedCells.Length > 0)
            {
                //Create a SaveFileDialog
                SaveFileDialog sfd = new SaveFileDialog()
                {
                    Filter = "Text Files (*.txt;)|*.txt|All Files (*.*)|*.*",
                    AddExtension = true
                };
                //If the user selected something, write the bytes from the ExportSelectedAsArray command from the loaded plugin
                if (sfd.ShowDialog() == DialogResult.OK)
                    File.WriteAllBytes(sfd.FileName, plugins.ElementAt(selectedPlugin).ExportSelectedAsArray());
            }
        }

        /// <summary>
        /// Fires whenever the user clicks the "Replace Selected" Context Menu Strip item.
        /// Prompts the user to pick a file to replace the selected cells with.
        /// </summary>
        private static void replaceSelectedToolStripMenuItem_Click()
        {
            //If the user has selected something, then we can replace stuff
            if (selectedCells.Length > 0)
            {
                //Create an OpenFileDialog
                OpenFileDialog ofd = new OpenFileDialog()
                {
                    Filter = "Text Files (*.txt;)|*.txt|All Files (*.*)|*.*"
                };
                //If the user selected something, use the replace command from the loaded plugin
                if (ofd.ShowDialog() == DialogResult.OK)
                    plugins.ElementAt(selectedPlugin).ReplaceSelectedWith(File.ReadAllBytes(ofd.FileName));
            }
        }

        #region Saving Stuff

        /// <summary>
        /// Fires whenever the user clicks the "Save" tool strip menu item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //If a file is actually loaded, then we can try to save the file
            if(fileLoaded)
            {
                //If we have yet to save the file anywhere, we're going to need to ask for it
                if (savedFileLocation == null)
                {
                    //Ask for a new saved file location and store the result
                    string newLocation = getSavedFileLocation();
                    //If that returned null, that means the user canceled, so we stop
                    if (newLocation == null)
                        return;
                    //If not, that measn they actually picked something, so store it
                    else
                        savedFileLocation = newLocation;
                }
                //If we haven't returned by this point, we're good to do the saving
                File.WriteAllBytes(savedFileLocation, plugins.ElementAt(selectedPlugin).ExportAll());
            }
        }

        /// <summary>
        /// Fires whenever the user clicks the "Save As..." tool strip menu item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //If a file is actually loaded, then we can try to save the file
            if (fileLoaded)
            {
                //Ask for a new saved file location and store the result
                string newLocation = getSavedFileLocation();
                //If that returned null, that means the user canceled, so we stop
                if (newLocation == null)
                    return;
                //If not, then we continue
                else
                {
                    //Set the new save location
                    savedFileLocation = newLocation;
                    //Save the file
                    File.WriteAllBytes(savedFileLocation, plugins.ElementAt(selectedPlugin).ExportAll());
                }
            }
        }

        /// <summary>
        /// Asks the user to give a location to save the file to.
        /// </summary>
        /// <returns>The path of the file to save (will return null if canceled).</returns>
        private string getSavedFileLocation()
        {
            //Create a SaveFileDialog
            SaveFileDialog sfd = new SaveFileDialog()
            {
                Filter = saveableFileType + "|All Files (*.*)|*.*",
                AddExtension = true
            };
            
            /*
            if (sfd.ShowDialog() == DialogResult.OK)
                return sfd.FileName;
            else
                return null;
            */

            //Return either the file path the user chose, or, if the user canceled, null
            return (sfd.ShowDialog() == DialogResult.OK) ? sfd.FileName : null;
        }

        #endregion

        #endregion

        #region Hotfile stuff

        /// <summary>
        /// Fires whenever the user clicks the "Create Hotfile" tool strip menu item.
        /// </summary>
        private static void createHotfileToolStripMenuItem_Click()
        {
            //If the user has selected some cells, then we can do the stuff
            if (selectedCells.Length > 0)
            {
                //Create a new hotfile useing the selected cells
                NewHotfile(selectedCells);
            }
        }

        /// <summary>
        /// Fires whenever a hotfile is renamed.
        /// ...Renames the hotfile...
        /// </summary>
        /// <param name="e">The RenamedEventArgs realted to the renaming.</param>
        public void Hotfile_Renamed(RenamedEventArgs e)
        {
            //Store the old path of the file (might not need to?)
            //string oldPath = e.OldFullPath;

            //Store the new path of the file
            //string newPath = e.FullPath;

            //hotfiles[e.OldFullPath].fileWatcher.Path = Path.GetDirectoryName(e.FullPath);
            //hotfiles[e.OldFullPath].fileWatcher.Filter = Path.GetFileName(e.FullPath);

            //Store the hotfile we're about to rename
            Hotfile renamedHotfile = hotfiles[e.OldFullPath];

            //Change the hotfile we just stored to have the new file loaction
            renamedHotfile.fileWatcher.Path = Path.GetDirectoryName(e.FullPath);
            renamedHotfile.fileWatcher.Filter = Path.GetFileName(e.FullPath);

            //Delete the old hotfile
            //hotfiles[oldPath].fileWatcher.Dispose();
            hotfiles.Remove(e.OldFullPath);
            //Add the new hotfile
            hotfiles.Add(e.FullPath, renamedHotfile);

            //Update the displays
            UpdateHotfileContextMenuStrip();
            DisplayHotfilesInManager();
        }

        /// <summary>
        /// Fires whenever a hotfile has its contents changed.
        /// Replaces the given cells with the contents of the watched file.
        /// </summary>
        /// <param name="fsw">The FileSystemWatcher that is watching the hotfile.</param>
        /// <param name="cells">the cells to be replaced.</param>
        public void Hotfile_Changed(FileSystemWatcher fsw, DataGridViewCell[] cells)
        {
            int trycount = 0;
            //Try to do the repalcing
            try
            {
                plugins.ElementAt(selectedPlugin).ReplaceSelectedWith(File.ReadAllBytes(Path.Combine(fsw.Path, fsw.Filter)),cells);
            }
            //If we encounter an IOException, that just means that something is still holding up the replacement of the file, so try again. (Maximum of 10 trys; not sure if this is the best idea?)
            catch(IOException)
            {
                if (trycount < 10)
                {
                    trycount++;
                    Hotfile_Changed(fsw, cells);
                }
                else
                {
                    MessageBox.Show("Replacement failed. :(", "ERROR");
                    return;
                }
            }
            //If we're set to be auto exporting, export.
            if (autoExport)
            {
                //Store the file that we're about to write
                var bytesToWrite = plugins.ElementAt(selectedPlugin).ExportAll();
                switch(iterateMode)
                {
                    //If we're supposed to be iterating, then we have work to do
                    case (true):
                        //If the file we're about to export is different than the file that's already there, then we can export
                        if (!Enumerable.SequenceEqual(bytesToWrite, File.ReadAllBytes(hotfileIterationExportFile)))
                        {
                            //Split the hotfile's path into a ton of different variables
                            var hotfileExtention = Path.GetExtension(hotfileIterationExportFile);
                            var hotfilePathName = hotfileIterationExportFile.Remove(hotfileIterationExportFile.Length - hotfileExtention.Length, hotfileExtention.Length);

                            //As long as the file exists, keep iterating through numbers
                            while (File.Exists(hotfileIterationExportFile))
                            {
                                //Replace the existing number
                                if (Regex.IsMatch(hotfilePathName, @"\(\d+\)$"))
                                    hotfilePathName = Regex.Replace(hotfilePathName, @"\(\d+\)$", $"({iterateCount})");
                                //Add a new number
                                else
                                    hotfilePathName += $"({iterateCount})";
                                //Increase the number
                                iterateCount++;
                                //Set the new file location
                                hotfileIterationExportFile = hotfilePathName + hotfileExtention;
                            }

                            //Finally export the file
                            File.WriteAllBytes(hotfileIterationExportFile, bytesToWrite);
                        }
                        break;
                    case (false):
                        //If the file we're about to export is different than the file that's already there, then we can export
                        if (!Enumerable.SequenceEqual(bytesToWrite, File.ReadAllBytes(hotfileExportFile)))
                            File.WriteAllBytes(hotfileExportFile, bytesToWrite);
                        break;
                }
            }
        }
        
        /// <summary>
        /// Delete the given hotfile.
        /// </summary>
        /// <param name="hotfile">The path of the hotfile to delete</param>
        public void Hotfile_Deleted(string hotfile)
        {
            //Dispose of the removed hotfile's FileSystemWatcher, then remove it from the hotfile list
            hotfiles[hotfile].fileWatcher.Dispose();
            hotfiles.Remove(hotfile);

            //Get all cells that are associated with said hotfile
            var cellsToRemove = cellsInHotfiles.Where(x => x.Value == hotfile).ToList();
            //Remove each cell from the cellsInHotfiles dictionary
            for (int i = 0; i < cellsToRemove.Count; i++)
                cellsInHotfiles.Remove(cellsToRemove[i].Key);

            //Push these updates to the contextMenuStrip, and the HotfileManager
            UpdateHotfileContextMenuStrip();
            DisplayHotfilesInManager();
        }

        /// <summary>
        /// Ask the user where they'd like to set the hotfile's autoexport file to.
        /// </summary>
        /// <returns>The path of the file the user has set.</returns>
        public static void SetHotfileOutput()
        {
            SaveFileDialog sfd = new SaveFileDialog()
            {
                Filter = saveableFileType + "|All Files (*.*)|*.*",
                AddExtension = true,
                Title = "Pick a destination output file."
            };
            /*
            if (sfd.ShowDialog() == DialogResult.OK)
                return sfd.FileName;
            else
                return null;
            */
            if (sfd.ShowDialog() == DialogResult.OK && sfd.FileName != hotfileExportFile)
            {
                iterateCount = 1;
                hotfileIterationExportFile = hotfileExportFile = sfd.FileName;
            }

            //return (sfd.ShowDialog() == DialogResult.OK) ? sfd.FileName : null;
        }

        /// <summary>
        /// Creates a new hotfile out of the cells provided.
        /// </summary>
        /// <param name="cells">The cells to make a hotfile out of.</param>
        public static void NewHotfile(DataGridViewCell[] cells)
        {
            //If autoexport is on, but the user hasn't provided an auto export location, ask for one
            if (hotfileExportFile == null && autoExport)
            {
                //Set the hotfileExportFile (hopefully)
                SetHotfileOutput();
                //If the hotfileExportFile is still null, return
                if (hotfileExportFile == null)
                    return;
            }          

            //Next, ask the user where they want the new hotfile to be

            //Preparing a SaveFileDialog
            SaveFileDialog sfd = new SaveFileDialog()
            {
                Filter = "Text Files (*.txt;)|*.txt|All Files (*.*)|*.*",
                AddExtension = true,
                Title = "Pick a destination hotfile."
            };

            //If they say yes, we have more work to do
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                //Out of the selected cells, get the ones that are actually safe to make a hotfile out of
                var sortedCells = GetSafeCells(cells);
                //If this returns null, the user has cancelled, so we stop
                if (sortedCells == null)
                    return;
                
                //Write the actual hotfile
                File.WriteAllBytes(sfd.FileName, plugins.ElementAt(selectedPlugin).ExportSelectedAsArray(sortedCells.ToArray()));
                //Add a new hotfile to the list of hotfiles
                hotfiles.Add(sfd.FileName, new Hotfile(sfd.FileName, sortedCells.ToArray()));
                //Add all the cells contained in the new hotfile to the cellsInHotfiles list
                for (int i = 0; i < sortedCells.Length; i++)
                    cellsInHotfiles.Add(sortedCells[i], sfd.FileName);
            }
            //Update both the "Add To Hotfile" context menu strip item's items, and the hotfile manager's display
            UpdateHotfileContextMenuStrip();
            DisplayHotfilesInManager();
        }
        
        /// <summary>
        /// Adds the given cells to the given hotfile.
        /// </summary>
        /// <param name="cells">The cells to add to the given hotfile.</param>
        /// <param name="hotfile">The hotfile to add the given cells to.</param>
        public static void AddtoHotfile(DataGridViewCell[] cells, string hotfile)
        { 
            //Out of the selected cells, get all the ones that aren't already in a hotfile
            var sortedCells = GetSafeCells(cells);
            //If this returned null, that means that the user either cancelled, or every cell they selected is part of a hotfile, so we cancel
            if (sortedCells == null)
                return;

            //Store the current hotfile data, add the cells, then give back the new data
            var currentData = hotfiles[hotfile].data.ToList();
            currentData.AddRange(sortedCells);
            hotfiles[hotfile].data = currentData.ToArray();
            
            //Write the hotfile again
            File.WriteAllBytes(hotfile, plugins.ElementAt(selectedPlugin).ExportSelectedAsArray(hotfiles[hotfile].data));

            //Update the hotfile manager
            DisplayHotfilesInManager();
        }

        /// <summary>
        /// Removes the hotfile (or part of hotfile) represented by the given TreeNode.
        /// </summary>
        /// <param name="removedNode">The TreeNode about to be removed.</param>
        public static void RemoveNode(TreeNode removedNode)
        {
            ///Variable that contains the hotfile that's going to be either edited or removed
            string hotfile = removedNode.Parent.Text ?? removedNode.Text;
            
            //If the user it trying to delete an entire hotfile, or is trying to delete the last part of a hotfile, remove the entire hotfile                        
            if(removedNode.Parent == null || removedNode.Parent.Nodes.Count == 1)
            {
                //Dispose of the fileWatcher to prevent any extra event triggering
                hotfiles[hotfile].fileWatcher.Dispose();
                //Delete the hotfile
                hotfiles.Remove(hotfile);
                
                //Remove the hotfile from the hotfileManager
                if (removedNode.Parent != null)
                    removedNode.Parent.Remove();
                else
                    removedNode.Remove();
            }
            //If not, they must be deleting a part of an existing hotfile
            else
            {
                //Get the data of the hotfile
                var hotfileData = hotfiles[hotfile].data.ToList();
                //Remove the data that corresponded to the removed node
                hotfileData.RemoveAt(removedNode.Index);
                //Set the data of the hotfile to be the new data
                hotfiles[hotfile].data = hotfileData.ToArray();

                //Remove the node from the hotfile manager
                removedNode.Remove();

                //Update the actual hotfile
                File.WriteAllBytes(hotfile, plugins.ElementAt(selectedPlugin).ExportSelectedAsArray(hotfiles[hotfile].data));
            }
            
            //Update all displays
            UpdateHotfileContextMenuStrip();
            DisplayHotfilesInManager();
        }

        //----------------------------------------------------------------------\\

        /// <summary>
        /// ...Pretty self explanitory tbh... Displays all loaded hotfiles in the Hotfile Manager.
        /// </summary>
        public static void DisplayHotfilesInManager()
        {
            //Clear the TreeView
            hotfileManager.treeView1.Nodes.Clear();

            //Create new list that will contain every hotfile + their related data in a dictionary
            Dictionary<string, DataGridViewCell[]> hotfilesForFunction = new Dictionary<string, DataGridViewCell[]>(hotfiles.Count);
            //Add each hotfile to the list
            for (int i = 0; i < hotfiles.Count; i++)
                hotfilesForFunction.Add(hotfiles.ElementAt(i).Key, hotfiles.ElementAt(i).Value.data);
            
            /* Maybe should be using this instead?
            foreach(var entry in hotfiles)
                hotfilesForFunction.Add(entry.Key, entry.Value.data);
            */

            //Let the loaded plugin do the actual displaying part
            plugins.ElementAt(selectedPlugin).DisplayHotfilesInManager(hotfileManager.treeView1, hotfilesForFunction);
        }

        /// <summary>
        /// ...Again, pretty self explanitory... Updates the "Add to Hotfile" button's drop down options.
        /// </summary>
        public static void UpdateHotfileContextMenuStrip()
        {
            //Clear all existing drop down items
            (lcms.Items["addToHotfileToolStripMenuItem"] as ToolStripMenuItem).DropDownItems.Clear();

            for (int i = 0; i < hotfiles.Count; i++)
            {
                //Store the hotfile's name
                string hotfilename = hotfiles.ElementAt(i).Key;
                //Add the new item to the drop down list
                (lcms.Items["addToHotfileToolStripMenuItem"] as ToolStripMenuItem).DropDownItems.Add(
                    hotfilename,
                    null,
                    delegate { AddtoHotfile(selectedCells, hotfilename); }
                    );
            }
        }

        /// <summary>
        /// Returns what cells out of the given cells are safe to be adding to hotfiles.
        /// Will return null if the user cancels at this point.
        /// </summary>
        /// <param name="cells">The cells to check.</param>
        /// <returns>The cells that are safe for adding to hotfiles.</returns>
        public static DataGridViewCell[] GetSafeCells(DataGridViewCell[] cells)
        {
            //Sort the cells by Column index, then by Row index
            cells = cells.OrderBy(c => c.ColumnIndex).OrderBy(r => r.RowIndex).ToArray();
            //Create new blank list
            List<DataGridViewCell> safeCells = new List<DataGridViewCell>();
            //bool that determines whether or not any of the hotfiles are unsafe
            bool unsafeCellsExist = false;
            //Loop through each gicen cell to determine whether the cell is safe
            for (int i = 0; i < cells.Length; i++)
            {
                //If the cell isn't safe, set the unsafeCellExist bool to true
                if (cellsInHotfiles.ContainsKey(cells[i]))
                    unsafeCellsExist = true;
                //If not, then it's safe, so add it to the list of safe cells
                else
                    safeCells.Add(cells[i]);
            }

            //If unsafe cells exist, ask the user if they want to continue with the remaining cells
            if (unsafeCellsExist)
            {
                DialogResult dr = MessageBox.Show($"Only {safeCells.Count} out of the given {cells.Length} cells are not already in a hotfile.\nWould you like to use the remaining cells anyways?", "Warning", MessageBoxButtons.YesNo);
                //If the user doesn't say yes, then we cancel
                if (dr != DialogResult.Yes)
                    return null;
            }

            //Return the safe cells
            return safeCells.ToArray();
        }

        #endregion

        //----------------------------------------------------------------------------------------------\\

        private void dataGridView1_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if(e.Button == MouseButtons.Right)
            {
                //Might do something here eventually...
            }
        }

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            plugins.ElementAt(selectedPlugin).CellEndEdit(dataGridView1[e.ColumnIndex, e.RowIndex]);
            plugins.ElementAt(selectedPlugin).UpdateRows(dataGridView1.Rows[e.RowIndex]);
        }

        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            //plugins.ElementAt(selectedPlugin).UpdateRows(dataGridView1.Rows[e.RowIndex]);
            plugins.ElementAt(selectedPlugin).CellFormating(e);
            //plugins.ElementAt(selectedPlugin).CellFormating(dataGridView1.Rows[e.RowIndex]);
        }
        
        #region Drag and Drop code
        //--- Drag and drop code (taken from https://stackoverflow.com/questions/1620947/how-could-i-drag-and-drop-datagridview-rows-under-each-other ) ---\\
        private Rectangle dragBoxFromMouseDown;
        private int rowIndexFromMouseDown;
        private int rowIndexOfItemUnderMouseToDrop;

        private void dataGridView1_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                // If the mouse moves outside the rectangle, start the drag.
                if (dragBoxFromMouseDown != Rectangle.Empty &&
                    !dragBoxFromMouseDown.Contains(e.X, e.Y))
                {

                    // Proceed with the drag and drop, passing in the list item.                    
                    DragDropEffects dropEffect = dataGridView1.DoDragDrop(
                    dataGridView1.Rows[rowIndexFromMouseDown],
                    DragDropEffects.Move);
                }
            }
        }

        private void dataGridView1_MouseDown(object sender, MouseEventArgs e)
        {
            // Get the index of the item the mouse is below.
            rowIndexFromMouseDown = dataGridView1.HitTest(e.X, e.Y).RowIndex;
            if (rowIndexFromMouseDown != -1)
            {
                // Remember the point where the mouse down occurred. 
                // The DragSize indicates the size that the mouse can move 
                // before a drag event should be started.                
                Size dragSize = SystemInformation.DragSize;

                // Create a rectangle using the DragSize, with the mouse position being
                // at the center of the rectangle.
                dragBoxFromMouseDown = new Rectangle(new Point(e.X - (dragSize.Width / 2),
                                                               e.Y - (dragSize.Height / 2)),
                                    dragSize);
            }
            else
                // Reset the rectangle if the mouse is not over an item in the ListBox.
                dragBoxFromMouseDown = Rectangle.Empty;
        }

        private void dataGridView1_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void dataGridView1_DragDrop(object sender, DragEventArgs e)
        {
            // The mouse locations are relative to the screen, so they must be 
            // converted to client coordinates.
            Point clientPoint = dataGridView1.PointToClient(new Point(e.X, e.Y));

            // Get the row index of the item the mouse is below. 
            rowIndexOfItemUnderMouseToDrop =
                dataGridView1.HitTest(clientPoint.X, clientPoint.Y).RowIndex;

            // If the drag operation was a move then remove and insert the row.
            if (e.Effect == DragDropEffects.Move)
            {
                DataGridViewRow rowToMove = e.Data.GetData(
                    typeof(DataGridViewRow)) as DataGridViewRow;

                dataGridView1.Rows.RemoveAt(rowIndexFromMouseDown);

                if (rowIndexOfItemUnderMouseToDrop >= dataGridView1.Rows.Count)
                    rowIndexOfItemUnderMouseToDrop--;
                dataGridView1.Rows.Insert(rowIndexOfItemUnderMouseToDrop, rowToMove);

                plugins.ElementAt(selectedPlugin).Move(rowIndexFromMouseDown, rowIndexOfItemUnderMouseToDrop);

            }
        }
        //--- ---\\
        #endregion

        //NEEDS TO BE REFACTORED SINCE IT'S REALLY MESSY TO HAVE BOTH THE PLUGIN AND THIS FORM HAVE A REFERENCE TO THE SELECTED CELLS
        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            selectedCells = plugins.ElementAt(selectedPlugin).selectedCells = dataGridView1.SelectedCells.Cast<DataGridViewCell>().OrderBy(c => c.ColumnIndex).OrderBy(c => c.RowIndex).ToArray();
            //plugins.ElementAt(selectedPlugin).selectedCells = selectedCells;
            

            //plugins.ElementAt(selectedPlugin).selectedCells = dataGridView1.SelectedCells.Cast<DataGridViewCell>().OrderBy(c => c.ColumnIndex).OrderBy(c => c.RowIndex).ToArray();
            //cellsSelected = (dataGridView1.SelectedCells.Count > 0) ? true : false;
        }
    }
}
