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
                
        public FormMain(DataHandler dh)
        {
            dataHandler = dh;
            dataHandler.FileLoaded += FileLoaded;
            dataHandler.FileUnloaded += FileUnloaded;
            dataHandler.HotfileDeleted += HotfileDeleted;
            dataHandler.HotfileChanged += HotfileChanged;

            hotfileManager = new HotfileManager(dataHandler, this);
            hotfileManager.FormClosing += (o, e) => { e.Cancel = true; viewHotfileManager.Checked = false; };
            //viewHotfileManager.CheckedChanged += delegate { toggleForm(hotfileManager, viewHotfileManager.Checked); };


            defaultToolStripMenuItems = new ToolStripItem[]
            {
                new ToolStripMenuItem("Export Selected...",null, ExportSelectedToolStripMenuItem_Click),
                new ToolStripMenuItem("Replace Selected...", null, ReplaceSelectedToolStripMenuItem_Click),
                new ToolStripSeparator(),
                new ToolStripMenuItem("Create Hotfile...", null, CreateHotfileToolStripMenuItem_Click),
                new ToolStripMenuItem()
                {
                    Text = "Add to Hotfile",
                    Name = "addToHotfileToolStripMenuItem"
                },
                new ToolStripSeparator()
            };
            

            InitializeComponent();
        }

        private static void toggleForm(Form form, bool state)
        {
            if (state)
                form.Show();
            else
                form.Hide();
        }



        private void Form1_Load(object sender, EventArgs e)
        {   
            //HACK really shouldn't have to do this/have this code be here of all places
            liveContextMenuStrip.Items.AddRange(defaultToolStripMenuItems);
            viewHotfileManager.CheckedChanged += delegate { toggleForm(hotfileManager, viewHotfileManager.Checked); };
        }




        public DataGridViewCell[] selectedCells;

        /// <summary>
        /// Contains the Default Tool Strip Menu Items
        /// </summary>
        private readonly ToolStripItem[] defaultToolStripMenuItems;
        /*=
        {
            new ToolStripMenuItem("Export Selected...",null, ExportSelectedToolStripMenuItem_Click),
            new ToolStripMenuItem("Replace Selected...", null, ReplaceSelectedToolStripMenuItem_Click),
            new ToolStripSeparator(),
            new ToolStripMenuItem("Create Hotfile...", null, CreateHotfileToolStripMenuItem_Click),
            new ToolStripMenuItem()
            {
                Text = "Add to Hotfile",
                Name = "addToHotfileToolStripMenuItem"
            },
            new ToolStripSeparator()
        };
        */
        /// <summary>
        /// The ContextMenuStrip that's actually in use/will be modified at runtime.
        /// </summary>
        private ContextMenuStrip liveContextMenuStrip = new ContextMenuStrip();
              
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
                if (newSelectedPlugin == dataHandler.plugins.Count)
                {
                    MessageBox.Show("This is the part where I would bring up a thing asking you what you want to import the file as, but I haven't made that yet, sooooo-");
                    return;
                }

                dataHandler.Load(ofd.FileName, newSelectedPlugin);
            }
        }

        private void FileLoaded(object o, EventArgs e)
        {
            //Disable all buttons dependant on a file being loaded
            this.saveToolStripMenuItem.Enabled = true;
            this.saveAsToolStripMenuItem.Enabled = true;
            this.editHeaderToolStripMenuItem.Enabled = true;

            //Remove any custom plugin options
            //this.pluginOptionsToolStripMenuItem.DropDown.Items.Clear();
            this.pluginOptionsToolStripMenuItem.DropDown.Items.AddRange(dataHandler.plugins.ElementAt(dataHandler.selectedPlugin).customMenuStripOptions);

            //Reset the LiveContextMenuStrip
            //liveContextMenuStrip.Items.Clear();
            //liveContextMenuStrip.Items.AddRange(defaultToolStripMenuItems);
            liveContextMenuStrip.Items.AddRange(dataHandler.plugins.ElementAt(dataHandler.selectedPlugin).customContextMenuStripButtons);
            dataGridView1.ContextMenuStrip = liveContextMenuStrip;

            //Reset the datagridview
            //dataGridView1.Columns.Clear();
            dataGridView1.Columns.AddRange(dataHandler.plugins.ElementAt(dataHandler.selectedPlugin).columns);
            //dataGridView1.Rows.Clear();
            dataGridView1.Rows.AddRange(dataHandler.plugins.ElementAt(dataHandler.selectedPlugin).GetRows());
            
            //Set the title to be the opened file
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

        //HACK unsure if either of these should be here, or if they should be in DataHandler
        #region Export/Replace Stuff

        /// <summary>
        /// Fires whenever the user clicks the "Export Selected" Context Menu Strip item.
        /// Prompts the user to pick where they'd like to export the selected cells to.
        /// </summary>
        private void ExportSelectedToolStripMenuItem_Click(object o, EventArgs e)
        {
            if (selectedCells.Length > 0)
            {
                SaveFileDialog sfd = new SaveFileDialog()
                {
                    Filter = "Text Files (*.txt;)|*.txt|Raw Files (*.raw;)|*.raw|All Files (*.*)|*.*",
                    AddExtension = true
                };
                //If the user selected something, write the bytes from the ExportSelectedAsArray command from the loaded plugin
                if (sfd.ShowDialog() == DialogResult.OK)
                    File.WriteAllBytes(sfd.FileName, dataHandler.plugins.ElementAt(dataHandler.selectedPlugin).ExportSelectedAsArray());
            }
        }

        /// <summary>
        /// Fires whenever the user clicks the "Replace Selected" Context Menu Strip item.
        /// Prompts the user to pick a file to replace the selected cells with.
        /// </summary>
        private void ReplaceSelectedToolStripMenuItem_Click(object o, EventArgs e)
        {
            if (selectedCells.Length > 0)
            {
                OpenFileDialog ofd = new OpenFileDialog()
                {
                    Filter = "Text Files (*.txt;)|*.txt|Raw Files (*.raw;)|*.raw|All Files (*.*)|*.*",
                };
                //If the user selected something, use the replace command from the loaded plugin
                if (ofd.ShowDialog() == DialogResult.OK)
                    dataHandler.plugins.ElementAt(dataHandler.selectedPlugin).ReplaceSelectedWith(File.ReadAllBytes(ofd.FileName));
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
                //If we have yet to save the file anywhere, we're going to need to ask for it
                if (dataHandler.savedFileLocation == null)
                {
                    string newLocation = getSavedFileLocation();
                    //If the new location is still null, that means the user canceled, so we stop
                    if (newLocation == null)
                        return;
                    else
                        dataHandler.savedFileLocation = newLocation;
                }
                //If we haven't returned by this point, we're good to do the saving
                File.WriteAllBytes(dataHandler.savedFileLocation, dataHandler.plugins.ElementAt(dataHandler.selectedPlugin).ExportAll());
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
                    File.WriteAllBytes(dataHandler.savedFileLocation, dataHandler.plugins.ElementAt(dataHandler.selectedPlugin).ExportAll());
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
            if (selectedCells.Length > 0)
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
                    dataHandler.NewHotfile(selectedCells, sfd.FileName);                   
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

        //HACK...maybe?
        private delegate void HotfileDeletedDelegate(object sender, DataHandler.HotfileDeletedEventArgs e);
        private void HotfileDeleted(object sender, DataHandler.HotfileDeletedEventArgs e)
        {
            if (liveContextMenuStrip.InvokeRequired)
                Invoke(new HotfileDeletedDelegate(HotfileDeleted), new object[] { sender, e });
            else
                (liveContextMenuStrip.Items["addToHotfileToolStripMenuItem"] as ToolStripMenuItem).DropDownItems.RemoveByKey(e.name);
        }

        //HACK...maybe?
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
                    t.Click += delegate { dataHandler.AddtoHotfile(selectedCells, e.name); };
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
            dataHandler.plugins.ElementAt(dataHandler.selectedPlugin).CellEndEdit(dataGridView1[e.ColumnIndex, e.RowIndex]);
            //dataHandler.plugins.ElementAt(dataHandler.selectedPlugin).UpdateRows(dataGridView1.Rows[e.RowIndex]);
        }

        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if(dataHandler.selectedPlugin < dataHandler.plugins.Count)
                dataHandler.plugins.ElementAt(dataHandler.selectedPlugin).CellFormating(e);
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

                dataHandler.plugins.ElementAt(dataHandler.selectedPlugin).MoveChunk(rowIndexFromMouseDown, rowIndexOfItemUnderMouseToDrop);

            }
        }
        //--- ---\\
        #endregion

        //TODO stop sending each plugin a copy of the selected cells
        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if(dataHandler.selectedPlugin < dataHandler.plugins.Count)
                selectedCells = dataHandler.plugins.ElementAt(dataHandler.selectedPlugin).selectedCells = dataGridView1.SelectedCells.Cast<DataGridViewCell>().OrderBy(c => c.ColumnIndex).OrderBy(c => c.RowIndex).ToArray();
            //plugins.ElementAt(selectedPlugin).selectedCells = selectedCells;
            

            //plugins.ElementAt(selectedPlugin).selectedCells = dataGridView1.SelectedCells.Cast<DataGridViewCell>().OrderBy(c => c.ColumnIndex).OrderBy(c => c.RowIndex).ToArray();
            //cellsSelected = (dataGridView1.SelectedCells.Count > 0) ? true : false;
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            //TODO might be unnecesary?
            dataHandler.Unload();
        }

        private void dataGridView1_UserAddedRow(object sender, DataGridViewRowEventArgs e)
        {
            if (dataHandler.selectedPlugin < dataHandler.plugins.Count)
                dataHandler.plugins.ElementAt(dataHandler.selectedPlugin).UserAddedRow(e);
        }

        private void dataGridView1_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            if (dataHandler.selectedPlugin < dataHandler.plugins.Count)
                dataHandler.plugins.ElementAt(dataHandler.selectedPlugin).UserDeletingRow(e);
        }
    }
}
