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
        public DataHandler dataHandler {get; set;}
        private HotfileManager hotfileManager { get; set; }
                
        public FormMain(string pluginpath)
        {
            dataHandler = new DataHandler(pluginpath);
            dataHandler.FileLoaded += FileLoaded;
            dataHandler.FileUnloaded += FileUnloaded;
            dataHandler.HotfileDeleted += HotfileDeleted;
            dataHandler.HotfileChanged += HotfileChanged;
            dataHandler.RowUpdated += RowUpdated;
            dataHandler.AskToContinue += AskToContinue;
            dataHandler.DisplayInformation += DisplayInformation;
            dataHandler.LoadPlugins(pluginpath);

            hotfileManager = new HotfileManager(dataHandler, this);
            hotfileManager.FormClosing += (o, e) => { e.Cancel = true; viewHotfileManager.Checked = false; };
            //viewHotfileManager.CheckedChanged += delegate { toggleForm(hotfileManager, viewHotfileManager.Checked); };


            defaultToolStripMenuItems = new ToolStripItem[]
            {
                new ToolStripMenuItem("Export Selected...",null, ExportSelectedToolStripMenuItem_Click),
                new ToolStripMenuItem("Replace Selected...", null, ReplaceSelectedToolStripMenuItem_Click),
                new ToolStripMenuItem("Update Selected", null, UpdateSelectedToolStripMenuItem_Click),
                new ToolStripSeparator(),
                new ToolStripMenuItem("Create Hotfile...", null, CreateHotfileToolStripMenuItem_Click),
                new ToolStripMenuItem()
                {
                    Text = "Add to Hotfile",
                    Name = "addToHotfileToolStripMenuItem"
                },
                //new ToolStripSeparator() //TODO make this only appear when there are custom ones
            };
            

            InitializeComponent();
        }

        public void DisplayInformation(object sender, DisplayInformationEventArgs e)
        {
            MessageBox.Show(e.text, e.header);
        }

        public void AskToContinue(object sender, AskToContinueEventArgs e)
        {
            e.result = (MessageBox.Show(e.text, e.header, MessageBoxButtons.YesNo) == DialogResult.Yes);
        }

        private delegate void RowUpdatedDelegate(object sender, DataHandler.RowUpdatedEventArgs e);
        private void RowUpdated(object sender, DataHandler.RowUpdatedEventArgs e)
        {
            if (dataGridView1.InvokeRequired)
            {
                Invoke(new RowUpdatedDelegate(RowUpdated), new object[] { sender, e });
            }
            else
            {
                for (int i = 0; i < e.references.Length; i++)
                {
                    dataGridView1[i,e.row].Value = e.references[i].text;

                    Color color = Color.White;
                    switch (e.references[i].validity)
                    {
                        case (Reference.ReferenceValidity.valid):
                            color = Color.White;
                            break;
                        case (Reference.ReferenceValidity.unknown):
                            color = Color.Yellow;
                            break;
                        case (Reference.ReferenceValidity.invalid):
                            color = Color.Red;
                            break;
                    }

                    dataGridView1[i,e.row].Style.BackColor = color;
                }
            }
        }

        //TODO remove, then fix using shown property
        private static void toggleForm(Form form, bool state)
        {
            if (state)
                form.Show();
            else
                form.Hide();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {   
            //HACK really shouldn't have to do this/have this code be here of all places
            liveContextMenuStrip.Items.AddRange(defaultToolStripMenuItems);
            viewHotfileManager.CheckedChanged += delegate { toggleForm(hotfileManager, viewHotfileManager.Checked); };
        }

        /// <summary>
        /// Contains the Default Tool Strip Menu Items
        /// </summary>
        private readonly ToolStripItem[] defaultToolStripMenuItems;
       
        /// <summary>
        /// The ContextMenuStrip that's actually in use/will be modified at runtime.
        /// </summary>
        private ContextMenuStrip liveContextMenuStrip = new ContextMenuStrip();

        #region Opening/File Loading

        /*TODO AMAZING IDEA
         * Make selecting a filter arbitrary
         * no matter what filter you pick, it will always bring up the same "import options" menu (assuming the plugin has options) (will always show on "all files")
         * from *there* you get to pick any extra options you would want for the plugin 
         * store info in a dictionary(?) to know what plugin to select for each filter, so you can filter by indivdual file type, instead of by plugin, since not everyone knows what an ISO Base Media Format file is
         */
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter = dataHandler.openableFileTypes
            };
            //If the user selected something and said ok, then we can start working
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                //Store the selected plugin
                int newSelectedPlugin = ofd.FilterIndex - 1;

                //If the user picked the last thing on the list, that means they selected "All Files", so we'll need to ask them what to import this file as
                if (newSelectedPlugin + 1 > dataHandler.pluginCount)
                {
                    SelectPlugin sp = new SelectPlugin(dataHandler.openableFileTypes.Split('|').Reverse().Skip(2).Reverse().Where((x, i) => i % 2 == 0).ToArray()/*HACK this is bad*/);
                    if(sp.ShowDialog() != DialogResult.OK)
                        return;
                    else
                        newSelectedPlugin = sp.SelectedIndex;
                    //MessageBox.Show("This is the part where I would bring up a thing asking you what you want to import the file as, but I haven't made that yet, sooooo-");
                    //return;
                }

                dataHandler.Load(ofd.FileName, newSelectedPlugin);
            }
        }

        private void FileLoaded(object o, DataHandler.FileLoadedEventArgs e)
        {
            this.saveToolStripMenuItem.Enabled = true;
            this.saveAsToolStripMenuItem.Enabled = true;
            this.editHeaderToolStripMenuItem.Enabled = true;

            //this.pluginOptionsToolStripMenuItem.DropDown.Items.AddRange(e.customMenuStripOptions);

            for (int i = 0; i < e.customOptions.Length; i++) //HACK bad stuff going on to remember what value belongs to what tool strip
            {
                unsafe
                {
                    if (e.customOptions[i].type == typeof(bool))
                    {
                        bool* value = (bool*)e.customOptions[i].value;
                        ToolStripMenuItem tsmi = new ToolStripMenuItem()
                        {
                            Text = e.customOptions[i].name,
                            Name = i.ToString(),
                            CheckOnClick = true,
                            Checked = *value
                        };
                        tsmi.CheckedChanged += (_o, _e) =>
                        {
                            bool* v = (bool*)e.customOptions[int.Parse((_o as ToolStripMenuItem).Name)].value;
                            *v = tsmi.Checked;
                            
                        };

                        this.pluginOptionsToolStripMenuItem.DropDown.Items.Add(tsmi);
                    }
                }
            }

            //liveContextMenuStrip.Items.AddRange(e.customContextMenuStripButtons);
            dataGridView1.ContextMenuStrip = liveContextMenuStrip;

            foreach(string column in e.columntext)
                dataGridView1.Columns.Add(column.ToLower(),column);

            DataGridViewRow[] rows = new DataGridViewRow[e.cellinfo.Length];
            for(int i = 0; i < e.cellinfo.Length; i++)
            {
                DataGridViewRow row = new DataGridViewRow();
                for(int _i = 0; _i < e.cellinfo[i].Length; _i++)
                {
                    DataGridViewCellStyle style = new DataGridViewCellStyle();
                    switch (e.cellinfo[i][_i].validity)
                    {
                        case (Reference.ReferenceValidity.valid):
                            style.BackColor = Color.White;
                            break;
                        case (Reference.ReferenceValidity.unknown):
                            style.BackColor = Color.Yellow;
                            break;
                        case (Reference.ReferenceValidity.invalid):
                            style.BackColor = Color.Red;
                            break;
                    }

                    DataGridViewCell cell = new DataGridViewTextBoxCell()
                    {
                        Value = e.cellinfo[i][_i].text,
                        //ReadOnly = (e.cellinfo[i][_i].editable) ? true : false,
                        Style = style
                    };
                    row.Cells.Add(cell);
                    row.Cells[_i].ReadOnly = !e.cellinfo[i][_i].editable;// (e.cellinfo[i][_i].editable) ? true : false;
                }
                rows[i] = row;
            }
            dataGridView1.Rows.AddRange(rows);

            this.Text = dataHandler.openFileLocation;            
        }

        private void FileUnloaded(object sender, EventArgs e)
        {
            this.saveToolStripMenuItem.Enabled = false;
            this.saveAsToolStripMenuItem.Enabled = false;
            this.editHeaderToolStripMenuItem.Enabled = false;

            //Remove any custom plugin options
            this.pluginOptionsToolStripMenuItem.DropDown.Items.Clear();

            //Reset the LiveContextMenuStrip
            liveContextMenuStrip.Items.Clear();
            liveContextMenuStrip.Items.AddRange(defaultToolStripMenuItems);

            dataGridView1.Columns.Clear();
            dataGridView1.Rows.Clear();

            this.Text = "Glitch Helper";
        }

        #endregion



        //HACK unsure if either of these should be here, or if they should be in DataHandler
        #region Export/Replace Stuff

        private Tuple<int,int>[] GetCellCoords(DataGridViewSelectedCellCollection cells)
        {
            Tuple<int, int>[] output = new Tuple<int, int>[cells.Count];
            for (int i = 0; i < cells.Count; i++)
                output[i] = new Tuple<int, int>(cells[i].RowIndex, cells[i].ColumnIndex);
            return output.OrderBy(r => r.Item1).OrderBy(c => c.Item2).ToArray();
        }
        private Tuple<int,int>[] GetCellCoords(DataGridCell[] cells)
        {
            Tuple<int, int>[] output = new Tuple<int, int>[cells.Length];
            for (int i = 0; i < cells.Length; i++)
                output[i] = new Tuple<int, int>(cells[i].RowNumber, cells[i].ColumnNumber);
            return output.OrderBy(r => r.Item1).OrderBy(c => c.Item2).ToArray();
        }

        /// <summary>
        /// Fires whenever the user clicks the "Export Selected" Context Menu Strip item.
        /// Prompts the user to pick where they'd like to export the selected cells to.
        /// </summary>
        private void ExportSelectedToolStripMenuItem_Click(object o, EventArgs e)
        {
            if (dataGridView1.SelectedCells.Count > 0)
            {
                SaveFileDialog sfd = new SaveFileDialog()
                {
                    Filter = "Text Files (*.txt;)|*.txt|Raw Files (*.raw;)|*.raw|All Files (*.*)|*.*",
                    AddExtension = true
                };
                if (sfd.ShowDialog() == DialogResult.OK)
                    dataHandler.Export(GetCellCoords(dataGridView1.SelectedCells), sfd.FileName);
                    //File.WriteAllBytes(sfd.FileName, dataHandler.plugins.ElementAt(dataHandler.selectedPlugin).ExportSelectedAsArray(selectedCells));
            }
        }

        /// <summary>
        /// Fires whenever the user clicks the "Replace Selected" Context Menu Strip item.
        /// Prompts the user to pick a file to replace the selected cells with.
        /// </summary>
        private void ReplaceSelectedToolStripMenuItem_Click(object o, EventArgs e)
        {
            if (dataGridView1.SelectedCells.Count > 0)
            {
                OpenFileDialog ofd = new OpenFileDialog()
                {
                    Filter = "Text Files (*.txt;)|*.txt|Raw Files (*.raw;)|*.raw|All Files (*.*)|*.*",
                };
                //If the user selected something, use the replace command from the loaded plugin
                if (ofd.ShowDialog() == DialogResult.OK)
                    dataHandler.Replace(GetCellCoords(dataGridView1.SelectedCells), ofd.FileName);
                    //dataHandler.plugins.ElementAt(dataHandler.selectedPlugin).ReplaceSelectedWith(File.ReadAllBytes(ofd.FileName));
            }
        }

        private void UpdateSelectedToolStripMenuItem_Click(object o, EventArgs e)
        {
            if(dataGridView1.SelectedCells.Count > 0)
            {
                int[] rows = GetCellCoords(dataGridView1.SelectedCells).Select(r => r.Item1).Distinct().ToArray();
                dataHandler.UpdateReferenceValidity(rows);
            }
        }

        #endregion

        #region Saving Stuff

        /// <summary>
        /// Fires whenever the user clicks the "Save" tool strip menu item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(dataHandler.saveableFileType != null)
            {
                if (dataHandler.savedFileLocation == null)
                {
                    string newLocation = getSavedFileLocation();
                    //If the new location is still null, that means the user canceled, so we stop
                    if (newLocation == null)
                        return;
                    else
                        dataHandler.savedFileLocation = newLocation;
                }
                dataHandler.Save();
                //File.WriteAllBytes(dataHandler.savedFileLocation, dataHandler.plugins.ElementAt(dataHandler.selectedPlugin).ExportAll());
            }
        }

        /// <summary>
        /// Fires whenever the user clicks the "Save As..." tool strip menu item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataHandler.saveableFileType != null)
            {
                string newLocation = getSavedFileLocation();
                //If that returned null, that means the user canceled, so we stop
                if (newLocation != null)
                {
                    //Set the new save location and save
                    dataHandler.savedFileLocation = newLocation;
                    dataHandler.Save();
                    //File.WriteAllBytes(dataHandler.savedFileLocation, dataHandler.plugins.ElementAt(dataHandler.selectedPlugin).ExportAll());
                }
            }
        }

        /// <summary>
        /// Asks the user to give a location to save the file to.
        /// </summary>
        /// <returns>The path of the file to save (will return null if canceled).</returns>
        private string getSavedFileLocation()
        {
            SaveFileDialog sfd = new SaveFileDialog()
            {
                Filter = dataHandler.saveableFileType + "|All Files (*.*)|*.*",
                AddExtension = true
            };            
            //Return either the file path the user chose, or, if the user canceled, null
            return (sfd.ShowDialog() == DialogResult.OK) ? sfd.FileName : null;
        }

        #endregion

        #region Hotfile stuff

        //HACK unsure if either of these should be here, or if they should be in DataHandler
        /// <summary>
        /// Fires whenever the user clicks the "Create Hotfile" tool strip menu item.
        /// </summary>
        private void CreateHotfileToolStripMenuItem_Click(object o, EventArgs e)
        {
            //If the user has selected some cells, then we can do the stuff
            if (dataGridView1.SelectedCells.Count > 0)
            {
                //If autoexport is on, but the user hasn't provided an auto export location, ask for one
                if (dataHandler.hotfileExportFile == null && dataHandler.autoExport)
                {
                    SetHotfileOutput();
                    //If the hotfileExportFile is still null, return
                    if (dataHandler.hotfileExportFile == null)
                        return;
                }

                //Next, ask the user where they want the new hotfile to be

                SaveFileDialog sfd = new SaveFileDialog()
                {
                    Filter = "Text Files (*.txt;)|*.txt|Raw Files (*.raw;)|*.raw|All Files (*.*)|*.*",
                    AddExtension = true,
                    Title = "Pick a destination hotfile."
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    dataHandler.NewHotfile(GetCellCoords(dataGridView1.SelectedCells), sfd.FileName);                   
                }
            }
        }
        
        /// <summary>
        /// Ask the user where they'd like to set the hotfile's autoexport file to.
        /// </summary>
        /// <returns>The path of the file the user has set.</returns>
        public void SetHotfileOutput()
        {
            SaveFileDialog sfd = new SaveFileDialog()
            {
                Filter = dataHandler.saveableFileType + "|All Files (*.*)|*.*",
                AddExtension = true,
                Title = "Pick a destination output file."
            };
            //Only save the file if the new FileName is different than the one that was already there
            if (sfd.ShowDialog() == DialogResult.OK && sfd.FileName != dataHandler.hotfileExportFile)
            {
                dataHandler.iterateCount = 1;
                dataHandler.hotfileIterationExportFile = dataHandler.hotfileExportFile = hotfileManager.Text = sfd.FileName;
            }
        }

        /*
        /// <summary>
        /// Creates a new hotfile out of the cells provided.
        /// </summary>
        /// <param name="cells">The cells to make a hotfile out of.</param>
        public static void NewHotfile(DataGridViewCell[] cells)
        {
            //If autoexport is on, but the user hasn't provided an auto export location, ask for one
            if (hotfileExportFile == null && autoExport)
            {
                SetHotfileOutput();
                //If the hotfileExportFile is still null, return
                if (hotfileExportFile == null)
                    return;
            }          

            //Next, ask the user where they want the new hotfile to be

            SaveFileDialog sfd = new SaveFileDialog()
            {
                Filter = "Text Files (*.txt;)|*.txt|All Files (*.*)|*.*",
                AddExtension = true,
                Title = "Pick a destination hotfile."
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                //Out of the selected cells, get the ones that are actually safe to make a hotfile out of
                var sortedCells = GetSafeCells(cells);
                //If this returns null, the user has cancelled, so we stop
                if (sortedCells == null)
                    return;
                
                //Write the actual hotfile and add to the hotfile list
                File.WriteAllBytes(sfd.FileName, plugins.ElementAt(selectedPlugin).ExportSelectedAsArray(sortedCells.ToArray()));
                hotfiles.Add(sfd.FileName, new Hotfile(sfd.FileName, sortedCells.ToArray()));

                //Add all the cells contained in the new hotfile to the cellsInHotfiles list
                for (int i = 0; i < sortedCells.Length; i++)
                    cellsInHotfiles.Add(sortedCells[i], sfd.FileName);
            }
            //Update the relevant displays
            UpdateHotfileContextMenuStrip();
            DisplayHotfilesInManager();
        }
        */

        /*
        /// <summary>
        /// Adds the given cells to the given hotfile.
        /// </summary>
        /// <param name="cells">The cells to add to the given hotfile.</param>
        /// <param name="hotfile">The hotfile to add the given cells to.</param>
        public static void AddtoHotfile(DataGridViewCell[] cells, string hotfile)
        { 
            //Out of the selected cells, get all the ones that aren't already in a hotfile
            var sortedCells = GetSafeCells(cells);
            //If this returned null, that means that the user either cancelled, or every cell they selected is part of a hotfile. Either way, we return
            if (sortedCells == null)
                return;

            //Store the current hotfile data, add the cells, then give back the new data
            var currentData = hotfiles[hotfile].data.ToList();
            currentData.AddRange(sortedCells);
            hotfiles[hotfile].data = currentData.ToArray();
            
            //Write the hotfile again
            File.WriteAllBytes(hotfile, plugins.ElementAt(selectedPlugin).ExportSelectedAsArray(hotfiles[hotfile].data));

            DisplayHotfilesInManager();
        }
        */

        /*
        /// <summary>
        /// Removes the hotfile (or part of hotfile) represented by the given TreeNode.
        /// </summary>
        /// <param name="removedNode">The TreeNode about to be removed.</param>
        public static void RemoveNode(TreeNode removedNode)
        {
            //Very confident I can refactor this at some point...

            //store the hotfile that's going to be either edited or removed
            string hotfileName = removedNode.Parent.Text ?? removedNode.Text;
            
            //If the user it trying to delete an entire hotfile, or is trying to delete the last part of a hotfile, remove the entire hotfile                        
            if(removedNode.Parent == null || removedNode.Parent.Nodes.Count == 1)
            {
                //Dispose of the fileWatcher to prevent any extra event triggering
                hotfiles[hotfileName].fileWatcher.Dispose();
                hotfiles.Remove(hotfileName);
                
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
                var hotfileData = hotfiles[hotfileName].data.ToList();
                //Remove the data that corresponded to the removed node
                hotfileData.RemoveAt(removedNode.Index);
                //Set the data of the hotfile to be the new data
                hotfiles[hotfileName].data = hotfileData.ToArray();

                //Remove the node from the hotfile manager
                removedNode.Remove();

                //Update the actual hotfile
                File.WriteAllBytes(hotfileName, plugins.ElementAt(selectedPlugin).ExportSelectedAsArray(hotfiles[hotfileName].data));
            }
            
            //Update all displays
            UpdateHotfileContextMenuStrip();
            DisplayHotfilesInManager();
        }
        */

        //----------------------------------------------------------------------\\

        //UNSURE
        private delegate void HotfileDeletedDelegate(object sender, DataHandler.HotfileDeletedEventArgs e);
        private void HotfileDeleted(object sender, DataHandler.HotfileDeletedEventArgs e)
        {
            if (liveContextMenuStrip.InvokeRequired)
                Invoke(new HotfileDeletedDelegate(HotfileDeleted), new object[] { sender, e });
            else
                (liveContextMenuStrip.Items["addToHotfileToolStripMenuItem"] as ToolStripMenuItem).DropDownItems.RemoveByKey(e.name);
        }

        //UNSURE
        private delegate void HotfileChangedDelegate(object sender, DataHandler.HotfileModifiedEventArgs e);
        private void HotfileChanged(object sender, DataHandler.HotfileModifiedEventArgs e)
        {
            if(liveContextMenuStrip.InvokeRequired)
            {
                Invoke(new HotfileChangedDelegate(HotfileChanged),new object[] {sender,e});
            }
            else
            {
                if (!(liveContextMenuStrip.Items["addToHotfileToolStripMenuItem"] as ToolStripMenuItem).DropDownItems.ContainsKey(e.name))
                {
                    ToolStripMenuItem t = new ToolStripMenuItem
                    {
                        Text = e.name,
                        Name = e.name
                    };
                    t.Click += delegate { dataHandler.AddtoHotfile(GetCellCoords(dataGridView1.SelectedCells), e.name); };
                    (liveContextMenuStrip.Items["addToHotfileToolStripMenuItem"] as ToolStripMenuItem).DropDownItems.Add(t);
                    /*
                    (liveContextMenuStrip.Items["addToHotfileToolStripMenuItem"] as ToolStripMenuItem).DropDownItems.Add(
                            e.name,
                            null,
                            delegate { dataHandler.AddtoHotfile(selectedCells, e.name); }
                            );
                */
                }
            }
        }

        /*
        /// <summary>
        /// ...Pretty self explanitory tbh... Displays all loaded hotfiles in the Hotfile Manager.
        /// </summary>
        public static void DisplayHotfilesInManager()
        {
            hotfileManager.treeView1.Nodes.Clear();

            //Create new list that will contain every hotfile + their related data in a dictionary
            Dictionary<string, DataGridViewCell[]> hotfilesForFunction = new Dictionary<string, DataGridViewCell[]>(hotfiles.Count);
            for (int i = 0; i < hotfiles.Count; i++)
                hotfilesForFunction.Add(hotfiles.ElementAt(i).Key, hotfiles.ElementAt(i).Value.data);
            
            /* Maybe should be using this instead?
            foreach(var entry in hotfiles)
                hotfilesForFunction.Add(entry.Key, entry.Value.data);
            * /

            //Let the loaded plugin do the actual displaying part
            plugins.ElementAt(selectedPlugin).DisplayHotfilesInManager(hotfileManager.treeView1, hotfilesForFunction);
        }

        /// <summary>
        /// ...Again, pretty self explanitory... Updates the "Add to Hotfile" button's drop down options.
        /// </summary>
        public static void UpdateHotfileContextMenuStrip()
        {
            //Clear all existing drop down items
            (liveContextMenuStrip.Items["addToHotfileToolStripMenuItem"] as ToolStripMenuItem).DropDownItems.Clear();

            for (int i = 0; i < hotfiles.Count; i++)
            {
                //Store the hotfile's name
                string hotfilename = hotfiles.ElementAt(i).Key;
                //Add the new item to the drop down list
                (liveContextMenuStrip.Items["addToHotfileToolStripMenuItem"] as ToolStripMenuItem).DropDownItems.Add(
                    hotfilename,
                    null,
                    delegate { AddtoHotfile(selectedCells, hotfilename); }
                    );
            }
        }

        /*
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
            
            List<DataGridViewCell> safeCells = new List<DataGridViewCell>();
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
        */

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
            dataHandler.Replace(new Tuple<int, int>(e.RowIndex, e.ColumnIndex), dataGridView1[e.ColumnIndex, e.RowIndex].Value);
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
                dataHandler.MoveReferences(rowIndexFromMouseDown, rowIndexOfItemUnderMouseToDrop);
                //dataHandler.plugins.ElementAt(dataHandler.selectedPlugin).MoveChunk(rowIndexFromMouseDown, rowIndexOfItemUnderMouseToDrop);
            }
        }
        //--- ---\\
        #endregion

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            //UNSURE
            dataHandler.Unload();
        }

        private void dataGridView1_UserAddedRow(object sender, DataGridViewRowEventArgs e)
        {
            dataHandler.AddReference();
        }

        private void dataGridView1_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            dataHandler.RemoveReferences(e.Row.Index);
        }
    }
}
