using PluginBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GlitchHelper
{
    public class DataHandler
    {
        #region Plugin Variables
        public ICollection<IGHPlugin> plugins = null;
        public int selectedPlugin;
        #endregion

        public string openableFileTypes = null;
        public string saveableFileType = null;
        public string openFileLocation = null;
        //public bool fileLoaded = false;
        public string savedFileLocation = null;

        #region Hotfile Variables

        public Dictionary<string, Hotfile> hotfiles = new Dictionary<string, Hotfile>();
        public Dictionary<DataGridViewCell, string> cellsInHotfiles = new Dictionary<DataGridViewCell, string>();
        public string hotfileExportFile, hotfileIterationExportFile = null;
        //public string hotfileIterationExportFile = null;
        
        public bool autoExport = true;
        public bool iterateMode = false;
        public long iterateCount = 1;

        public bool deleteOrphanedHotfiles = true;
        public bool ignoreHotfileRenaming = false;
        public bool ignoreHotfileDeletions = false;
        
        public class Hotfile
        {
            private DataHandler dataHandler { get; }
            public FileSystemWatcher fileWatcher { get; set; }
            public DataGridViewCell[] data { get; set; }

            //HACK not happy with the fact that I have to give each hotfile a reference to the DataHandler...
            public Hotfile(DataHandler dh, string path, DataGridViewCell[] cells)
            {
                dataHandler = dh;
                fileWatcher = new FileSystemWatcher(Path.GetDirectoryName(path), Path.GetFileName(path));
                data = cells;
                fileWatcher.EnableRaisingEvents = true;
                fileWatcher.Changed += delegate { dataHandler.Hotfile_Changed(fileWatcher, data); };
                fileWatcher.Renamed += dataHandler.Hotfile_Renamed;
                fileWatcher.Deleted += dataHandler.Hotfile_Deleted;
            }
        }

        #endregion


        #region Constructing/Loading/Unloading
        
        public DataHandler(string pluginPath = null)
        {
            if (pluginPath == null)
                pluginPath = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), @"Plugins");
            else
                pluginPath = Path.GetDirectoryName(pluginPath);

            //Plugin loading stuff
            plugins = PluginLoader<IGHPlugin>.LoadPlugins(pluginPath);

            if (plugins == null || plugins.Count <= 0 )
            {
                MessageBox.Show("No Plugins Found. ;(\nRemember, Plugins (by default) belong in a folder called \"Plugins\", placed in the same directory as the program itself.");
                return;
            }
            else
            {
                openableFileTypes = string.Join("|", plugins.Select(x => x.filter)) + "|All Files (*.*)|*.*";
                /*
                for (int i = 0; i < plugins.Count; i++)
                    openableFileTypes += plugins.ElementAt(i).filter + "|";
                openableFileTypes += "|All Files (*.*)|*.*";
                */
            }
            selectedPlugin = plugins.Count;

            HotfileChanged += UpdateHotfile;
            //See Line 184
            //HotfileDeleted += DeleteHotfile;
        }

        public event EventHandler FileLoaded = new EventHandler((o,e) => { });
        public void Load(string filePath, int pluginToUse)
        {
            string newSaveableFileType = plugins.ElementAt(pluginToUse).Load(File.ReadAllBytes(filePath));
            if(newSaveableFileType != null)
            {
                Unload();

                saveableFileType = newSaveableFileType;
                openFileLocation = filePath;

                selectedPlugin = pluginToUse;

                /*
                //Reset hotfile variables
                for (int i = 0; i < hotfiles.Count; i++)
                    hotfiles.ElementAt(i).Value.fileWatcher.Dispose();
                hotfiles.Clear();
                cellsInHotfiles.Clear();
                
                //UpdateHotfileContextMenuStrip();
                //DisplayHotfilesInManager();
                hotfileExportFile = null;
                hotfileIterationExportFile = null;
                */

                FileLoaded(this,new EventArgs());
            }
        }
        
        public event EventHandler FileUnloaded = new EventHandler((o, e) => { });
        public void Unload()
        {
            selectedPlugin = plugins.Count;
            saveableFileType = null;
            savedFileLocation = null;

            iterateCount = 1;

            //TODO Probably can do this a better way...
            for(int i = 0; i < hotfiles.Count; i++)
            {
                string hn = hotfiles.ElementAt(i).Key;
                hotfiles.ElementAt(i).Value.fileWatcher.Dispose(); //HACK Unsure if I have to do this...?
                if (deleteOrphanedHotfiles) //TODO fix System.IO errors
                    File.Delete(hn);
                HotfileDeleted(this, new HotfileDeletedEventArgs(hn));
            }
            hotfiles.Clear();
            cellsInHotfiles.Clear();

            hotfileExportFile = hotfileIterationExportFile = null;

            FileUnloaded(this, new EventArgs());
        }

        #endregion



        #region Hotfile stuff

        public class HotfileModifiedEventArgs : EventArgs
        {
            public string name { get; set; }
            public DataGridViewCell[] contents { get; set; }

            public HotfileModifiedEventArgs(string name, DataGridViewCell[] contents)
            {
                this.name = name;
                this.contents = contents;
            }
        }
        public event EventHandler<HotfileModifiedEventArgs> HotfileChanged = new EventHandler<HotfileModifiedEventArgs>((o, e) => { });
        private void UpdateHotfile(object o, HotfileModifiedEventArgs e)
        {
            File.WriteAllBytes(e.name, plugins.ElementAt(selectedPlugin).ExportSelectedAsArray(e.contents));
        }

        public class HotfileDeletedEventArgs : EventArgs
        {
            public string name { get; set; }

            public HotfileDeletedEventArgs(string name)
            {
                this.name = name;
            }
        }
        public event EventHandler<HotfileDeletedEventArgs> HotfileDeleted = new EventHandler<HotfileDeletedEventArgs>((o, e) => { });

        /* Might need this code later...
        int dhTryCount = 0;
        private void DeleteHotfile(object o, HotfileDeletedEventArgs e)
        {
            if (!ignoreHotfileDeletions)
            {
                try
                {
                    File.Delete(e.name);
                    dhTryCount = 0;
                }
                catch
                {
                    if (dhTryCount < 10)
                    {
                        dhTryCount++;
                        DeleteHotfile(o, e);
                    }
                    else
                    {
                        MessageBox.Show("Deletion failed. :(", "ERROR");
                        dhTryCount = 0;
                    }
                    return;
                }
            }
        }
        */

        /// <summary>
        /// Fires whenever a hotfile is renamed.
        /// ...Renames the hotfile...
        /// </summary>
        /// <param name="e">The RenamedEventArgs realted to the renaming.</param>
        public void Hotfile_Renamed(object o, RenamedEventArgs e)
        {
            //TODO this probably needs work/might come into conflicts with this implementation
            if (!ignoreHotfileRenaming && hotfiles.ContainsKey(e.OldFullPath) && !hotfiles.ContainsKey(e.FullPath))
            {
                //Store the hotfile we're about to rename
                Hotfile renamedHotfile = hotfiles[e.OldFullPath];

                //Change the hotfile we just stored to have the new file loaction
                renamedHotfile.fileWatcher.Path = Path.GetDirectoryName(e.FullPath);
                renamedHotfile.fileWatcher.Filter = Path.GetFileName(e.FullPath);

                //Delete the old hotfile/add the new one
                //hotfiles[oldPath].fileWatcher.Dispose();
                hotfiles.Remove(e.OldFullPath);
                hotfiles.Add(e.FullPath, renamedHotfile);

                //Delete any cells that were assigned to the old hotfile and re-add them to the list with the new path
                DataGridViewCell[] cellsToRename = cellsInHotfiles.Where(x => x.Value == e.OldFullPath).Select(x => x.Key).ToArray();
                for (int i = 0; i < cellsToRename.Length; i++)
                {
                    cellsInHotfiles.Remove(cellsToRename[i]);
                    cellsInHotfiles.Add(cellsToRename[i], e.FullPath);
                }

                HotfileDeleted(this, new HotfileDeletedEventArgs(e.OldFullPath));
                HotfileChanged(this, new HotfileModifiedEventArgs(e.FullPath, cellsToRename));
            }
        }

        /// <summary>
        /// Fires whenever a hotfile has its contents changed.
        /// Replaces the given cells with the contents of the watched file.
        /// </summary>
        /// <param name="fsw">The FileSystemWatcher that is watching the hotfile.</param>
        /// <param name="cells">the cells to be replaced.</param>
        int hcTryCount = 0;
        public void Hotfile_Changed(FileSystemWatcher fsw, DataGridViewCell[] cells)
        {
            
            try
            {
                plugins.ElementAt(selectedPlugin).ReplaceSelectedWith(File.ReadAllBytes(Path.Combine(fsw.Path, fsw.Filter)), cells);
                hcTryCount = 0;
            }
            //If we encounter an IOException, that just means that something is still holding up the replacement of the file, so try again. (Maximum of 10 trys; not sure if this is the best idea?)
            catch (IOException)
            {
                if (hcTryCount < 10)
                {
                    hcTryCount++;
                    Hotfile_Changed(fsw, cells);
                }
                else
                {
                    MessageBox.Show("Replacement failed. :(", "ERROR");
                    hcTryCount = 0;
                }
                return;
            }
            if (autoExport)
            {
                var bytesToWrite = plugins.ElementAt(selectedPlugin).ExportAll();

                //If the file we've exported something before, and the file we're about to write is no different than the last one we exported, then we stop
                if (File.Exists((iterateMode) ? hotfileIterationExportFile : hotfileExportFile))
                    if (Enumerable.SequenceEqual(bytesToWrite, (iterateMode) ? File.ReadAllBytes(hotfileIterationExportFile) : File.ReadAllBytes(hotfileExportFile)))
                        return;

                var fileToWrite = hotfileExportFile;

                if (iterateMode)
                {
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

                        iterateCount++;
                        hotfileIterationExportFile = hotfilePathName + hotfileExtention;
                    }

                    fileToWrite = hotfileIterationExportFile;
                }
                File.WriteAllBytes(fileToWrite, bytesToWrite);
            }
        }

        /// <summary>
        /// Delete the given hotfile.
        /// </summary>
        /// <param name="hotfile">The path of the hotfile to delete</param>
        public void Hotfile_Deleted(object sender, FileSystemEventArgs e)
        {
            if (!ignoreHotfileDeletions)
            {
                //Dispose of the removed hotfile's FileSystemWatcher, then remove it from the hotfile list
                hotfiles[e.FullPath].fileWatcher.Dispose();
                hotfiles.Remove(e.FullPath);

                //TODO merge this and VVV
                //Get all cells that are associated with said hotfile and remove each cell from the cellsInHotfiles dictionary
                DataGridViewCell[] cellsToRemove = cellsInHotfiles.Where(x => x.Value == e.FullPath).Select(x => x.Key).ToArray();
                for (int i = 0; i < cellsToRemove.Length; i++)
                    cellsInHotfiles.Remove(cellsToRemove[i]);

                HotfileDeleted(this, new HotfileDeletedEventArgs(e.FullPath));
            }
        }

        /*
        /// <summary>
        /// Ask the user where they'd like to set the hotfile's autoexport file to.
        /// </summary>
        /// <returns>The path of the file the user has set.</returns>
        public void SetHotfileOutput()
        {
            SaveFileDialog sfd = new SaveFileDialog()
            {
                Filter = saveableFileType + "|All Files (*.*)|*.*",
                AddExtension = true,
                Title = "Pick a destination output file."
            };
            //Only save the file if the new FileName is different than the one that was already there
            if (sfd.ShowDialog() == DialogResult.OK && sfd.FileName != hotfileExportFile)
            {
                iterateCount = 1;
                hotfileIterationExportFile = hotfileExportFile = sfd.FileName;
            }
        }
        */


        
        /// <summary>
        /// Creates a new hotfile out of the cells provided.
        /// </summary>
        /// <param name="hotfileCells">The cells to make a hotfile out of.</param>
        /// <param name="hotfilePath">The path to create the hotile at.</param>
        public void NewHotfile(DataGridViewCell[] hotfileCells, string hotfilePath)
        {
            //Out of the selected cells, get the ones that are actually safe to make a hotfile out of
            var sortedCells = GetSafeCells(hotfileCells);
            //If this returns null, the user has cancelled, so we stop
            if (sortedCells == null)
                return;

            //We trigger the event first to prevent the newly created hotfile from immedietly exporting.
            HotfileChanged(this, new HotfileModifiedEventArgs(hotfilePath, sortedCells.ToArray()));

            //Write the actual hotfile and add to the hotfile list
            //File.WriteAllBytes(hotfilePath, plugins.ElementAt(selectedPlugin).ExportSelectedAsArray(sortedCells.ToArray()));
            hotfiles.Add(hotfilePath, new Hotfile(this, hotfilePath, sortedCells.ToArray()));

            //Add all the cells contained in the new hotfile to the cellsInHotfiles list
            for (int i = 0; i < sortedCells.Length; i++)
                cellsInHotfiles.Add(sortedCells[i], hotfilePath);

            //HotfileChanged(this, new HotfileModifiedEventArgs(hotfilePath, sortedCells.ToArray()));
        }

        /// <summary>
        /// Adds the given cells to the given hotfile.
        /// </summary>
        /// <param name="cells">The cells to add to the given hotfile.</param>
        /// <param name="hotfile">The hotfile to add the given cells to.</param>
        public void AddtoHotfile(DataGridViewCell[] cells, string hotfile)
        {
            //Out of the selected cells, get all the ones that aren't already in a hotfile
            DataGridViewCell[] sortedCells = GetSafeCells(cells);
            //If this returned null, that means that the user either cancelled, or every cell they selected is part of a hotfile. Either way, we return
            if (sortedCells == null)
                return;

            //Store the current hotfile data, add the cells, then give back the new data
            List<DataGridViewCell> currentData = hotfiles[hotfile].data.ToList();
            currentData.AddRange(sortedCells);
            hotfiles[hotfile].data = currentData.ToArray();

            //Write the hotfile again
            //File.WriteAllBytes(hotfile, plugins.ElementAt(selectedPlugin).ExportSelectedAsArray(hotfiles[hotfile].data));

            HotfileChanged(this, new HotfileModifiedEventArgs(hotfile, currentData.ToArray()));
        }

        /// <summary>
        /// Removes the hotfile (or part of hotfile) represented by the given TreeNode.
        /// </summary>
        /// <param name="removedNode">The TreeNode about to be removed.</param>
        public void RemoveNode(TreeNode removedNode)
        {
            //HACK can probably be refactored(?)

            //store the hotfile that's going to be either edited or removed
            string hotfileName = (removedNode.Parent != null) ? removedNode.Parent.Text : removedNode.Text;

            //If the user it trying to delete an entire hotfile, or is trying to delete the last part of a hotfile, remove the entire hotfile                        
            if (removedNode.Parent == null || removedNode.Parent.Nodes.Count == 1)
            {
                //Dispose of the fileWatcher to prevent any extra event triggering
                hotfiles[hotfileName].fileWatcher.Dispose();
                hotfiles.Remove(hotfileName);

                //TODO merge this and ^^^
                //Get all cells that are associated with said hotfile and remove each cell from the cellsInHotfiles dictionary
                DataGridViewCell[] cellsToRemove = cellsInHotfiles.Where(x => x.Value == hotfileName).Select(x => x.Key).ToArray();
                for (int i = 0; i < cellsToRemove.Length; i++)
                    cellsInHotfiles.Remove(cellsToRemove[i]);

                if (deleteOrphanedHotfiles)
                    File.Delete(hotfileName);

                /*Remove the hotfile from the hotfileManager
                if (removedNode.Parent != null)
                    removedNode.Parent.Remove();
                else
                    removedNode.Remove();
                */
                HotfileDeleted(this, new HotfileDeletedEventArgs(hotfileName));
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
                //removedNode.Remove();

                //Update the actual hotfile
                //File.WriteAllBytes(hotfileName, plugins.ElementAt(selectedPlugin).ExportSelectedAsArray(hotfiles[hotfileName].data));
                HotfileChanged(this, new HotfileModifiedEventArgs(hotfileName, hotfiles[hotfileName].data));
            }
        }

        //----------------------------------------------------------------------\\
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
        */


        /// <summary>
        /// Returns what cells out of the given cells are safe to be adding to hotfiles.
        /// Will return null if the user cancels at this point.
        /// </summary>
        /// <param name="cells">The cells to check.</param>
        /// <returns>The cells that are safe for adding to hotfiles.</returns>
        public DataGridViewCell[] GetSafeCells(DataGridViewCell[] cells)
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

            //TODO remove the message box
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


    }
}
