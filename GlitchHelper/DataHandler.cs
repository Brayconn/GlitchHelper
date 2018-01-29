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
        public event EventHandler<AskToContinueEventArgs> AskToContinue = new EventHandler<AskToContinueEventArgs>((o, e) => { });
        void askToContinueHook(object sender, AskToContinueEventArgs e)
        {
            AskToContinue(sender, e);
        }

        public event EventHandler<DisplayInformationEventArgs> DisplayInformation = new EventHandler<DisplayInformationEventArgs>((o, e) => { });
        void displayInformationHook(object sender, DisplayInformationEventArgs e)
        {
            DisplayInformation(sender, e);
        }

        #region Plugin Variables
        private IGHPlugin[] plugins { get; set; } = null;
        public int pluginCount { get; private set; }
        private int selectedPlugin { get; set; }
        #endregion

        #region Data Variables
        public Reference header { get; set; }
        private List<Reference[]> openedData { get; set; } = new List<Reference[]>();
        #endregion

        public string openableFileTypes { get; private set; }
        public string saveableFileType { get; set; }

        public string openFileLocation { get; set; }
        public string savedFileLocation { get; set; }

        #region Hotfile Variables

        public Dictionary<string, Hotfile> hotfiles = new Dictionary<string, Hotfile>();
        public Dictionary<Tuple<int,int>, string> cellsInHotfiles = new Dictionary<Tuple<int,int>, string>();
        public string hotfileExportFile {get; set;}
        public string hotfileIterationExportFile { get; set; }
        //public string hotfileIterationExportFile = null;

        public bool autoExport { get; set; } = true;
        public bool iterateMode { get; set; } = false;
        public long iterateCount { get; set; } = 1;

        public bool deleteOrphanedHotfiles { get; set; } = true;
        public bool ignoreHotfileRenaming { get; set; } = false;
        public bool ignoreHotfileDeletions { get; set; } = false;
        
        public class Hotfile
        {
            private DataHandler dataHandler { get; }
            public FileSystemWatcher fileWatcher { get; set; }
            public List<Tuple<int,int>> references { get; set; }

            //HACK not happy with the fact that I have to give each hotfile a reference to the DataHandler...
            public Hotfile(DataHandler dh, string path, List<Tuple<int,int>> referenceCoords)
            {
                dataHandler = dh;
                fileWatcher = new FileSystemWatcher(Path.GetDirectoryName(path), Path.GetFileName(path));
                references = referenceCoords;
                fileWatcher.EnableRaisingEvents = true;
                fileWatcher.Changed += delegate { dataHandler.Hotfile_Changed(fileWatcher, references.ToArray()); };
                fileWatcher.Renamed += dataHandler.Hotfile_Renamed;
                fileWatcher.Deleted += dataHandler.Hotfile_Deleted;
            }
        }

        #endregion


        #region Constructing/Loading/Unloading
        
        //USURE this structure seems weird... require the use of a seperate function before the class will be able to do anything?
        public DataHandler(string pluginPath = null)
        {
            //if (pluginPath != null)
                LoadPlugins(pluginPath);
        }

        //TODO add support for reloading plugins on the fly
        public void LoadPlugins(string pluginPath = null)
        {
            if(pluginPath == null)
                pluginPath = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), @"Plugins");

            //Plugin loading stuff
            plugins = PluginLoader<IGHPlugin>.LoadPlugins(pluginPath).ToArray();

            if (plugins == null || plugins.Length <= 0)
            {
                DisplayInformation(this, new DisplayInformationEventArgs("No Plugins Found. ;(\nRemember, Plugins (by default) belong in a folder called \"Plugins\", placed in the same directory as the program itself.", "Plugin Loading"));
                return;
            }
            else
            {
                pluginCount = plugins.Length;
                openableFileTypes = string.Join("|", plugins.Select(x => x.ofdFilter)) + "|All Files (*.*)|*.*";
                selectedPlugin = plugins.Length - 1;

                HotfileChanged += UpdateHotfile;
            }
            //See Line 184
            //HotfileDeleted += DeleteHotfile;
        }

        //TODO change parameters to "filepath" and "rownames"... why?
        public class FileLoadedEventArgs : EventArgs
        {
            public string[] columntext { get; }
            public Reference[][] cellinfo { get; }
            public Option[] customOptions { get; set; }

            public FileLoadedEventArgs(string[] columntext, Reference[][] cellinfo, Option[] customOptions)
            {
                this.columntext = columntext;
                this.cellinfo = cellinfo;
                this.customOptions = customOptions;
            }
        }
        public event EventHandler<FileLoadedEventArgs> FileLoaded = new EventHandler<FileLoadedEventArgs>((o,e) => { });
        public void Load(string filePath, int pluginToUse)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                //HACK this works, but still feels messy...
                plugins[pluginToUse].AskToContinue += askToContinueHook;
                plugins[pluginToUse].DisplayInformation += displayInformationHook;
                bool safeToContinue = plugins.ElementAt(pluginToUse).IsFileValid(fs);

                if (!safeToContinue)
                {
                    plugins[pluginToUse].AskToContinue -= askToContinueHook;
                    plugins[pluginToUse].DisplayInformation -= displayInformationHook;
                    return;
                }

                Unload();

                //HACK might be a messy way of transfering the header?
                var newData = plugins.ElementAt(pluginToUse).Load(fs);
                header = newData.Item1;
                openedData = newData.Item2;
            }

            saveableFileType = plugins.ElementAt(pluginToUse).sfdFilter;
            openFileLocation = filePath;

            selectedPlugin = pluginToUse;
            /*
            Tuple<string[],reference.ReferenceValidity[]>[] rows = new Tuple<string[], reference.ReferenceValidity[]>[_openedData.Count];
            for(int i = 0; i < _openedData.Count; i++)
            {
                for(int _i = 0; _i < plugins.ElementAt(selectedPlugin).columns.Length; _i++)
                {
                    rows[i][_i] = plugins.ElementAt(selectedPlugin).GetReferenceInfo(_openedData[i][_i]);
                }
            }
            */
            FileLoaded(this, new FileLoadedEventArgs(
                plugins.ElementAt(selectedPlugin).columnNames,
                openedData.ToArray(),
                plugins.ElementAt(selectedPlugin).customOptions //UNSURE Don't know if I should really be giving the UI full control over the variable...
                ));


        }

        public event EventHandler FileUnloaded = new EventHandler((o, e) => { });
        public void Unload()
        {
            plugins[selectedPlugin].AskToContinue -= askToContinueHook;
            plugins[selectedPlugin].DisplayInformation -= displayInformationHook;
            selectedPlugin = plugins.Length;
            
            #region Reset Hotfiles

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

            #endregion

            #region Reset References

            //HACK???
            if ((header != null) ? header.file != openFileLocation : false)
                File.Delete(header.file);

            //Delete all temp files
            for(int i = 0; i < openedData.Count; i++)
            {
                for(int _i = 0; _i < openedData[i].Length; _i++)
                {
                    if(openedData[i][_i].file != openFileLocation)
                    {
                        File.Delete(openedData[i][_i].file);
                    }
                }
            }

            header = null;
            openedData = new List<Reference[]>();

            openFileLocation = null;
            saveableFileType = null;
            savedFileLocation = null;

            #endregion

            FileUnloaded(this, new EventArgs());
        }

        #endregion
        
        public class RowUpdatedEventArgs : EventArgs
        {
            public int row { get; }
            public Reference[] references { get; }

            public RowUpdatedEventArgs(int row, Reference[] references)
            {
                this.row = row;
                this.references = references;
            }
        }
        public event EventHandler<RowUpdatedEventArgs> RowUpdated = new EventHandler<RowUpdatedEventArgs>((o, e) => { });
        public void UpdateReferenceValidity(int[] rows)
        {
            for (int i = 0; i < rows.Length; i++)
                UpdateReferenceValidity(rows[i]);
        }
        public void UpdateReferenceValidity(int row)
        {
            plugins.ElementAt(selectedPlugin).UpdateReferenceValidity(openedData[row], openFileLocation);
            RowUpdated(this, new RowUpdatedEventArgs(row, openedData[row]));
        }

        public void Save(string path = null)
        {
            if (path == null)
                path = savedFileLocation;

            using (FileStream fsw = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (BinaryWriter bw = new BinaryWriter(fsw))
            {
                //HACK really want to merge this with the code below...
                if(header != null)
                {
                    using (FileStream fsr = new FileStream(header.file, FileMode.Open, FileAccess.Read))
                    using (BinaryReader br = new BinaryReader(fsr))
                    {
                        fsr.Seek(header.offset, SeekOrigin.Begin);
                        bw.Write(br.ReadBytes(header.length));
                    }
                }

                for (int i = 0; i < openedData.Count; i++)
                {
                    for (int _i = 0; _i < openedData[i].Length; _i++)
                    {
                        using (FileStream fsr = new FileStream(openedData[i][_i].file, FileMode.Open, FileAccess.Read))
                        using (BinaryReader br = new BinaryReader(fsr))
                        {
                            //HACK might still have issues with REALLY large files (longer than long length)
                            fsr.Seek(openedData[i][_i].offset, SeekOrigin.Begin);
                            bw.Write(br.ReadBytes(openedData[i][_i].length));
                        }
                    }
                }
            }
        }

        public void Export(Tuple<int, int>[] references, string path)
        {
            using (FileStream fsw = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (BinaryWriter bw = new BinaryWriter(fsw))
            {
                for (int i = 0; i < references.Length; i++)
                {
                    using (FileStream fsr = new FileStream(openedData[references[i].Item1][references[i].Item2].file, FileMode.Open, FileAccess.Read))
                    using (BinaryReader br = new BinaryReader(fsr))
                    {
                        //HACK might still have issues with REALLY large files (longer than long length)
                        fsr.Seek(openedData[references[i].Item1][references[i].Item2].offset, SeekOrigin.Begin);
                        bw.Write(br.ReadBytes(openedData[references[i].Item1][references[i].Item2].length));
                    }
                }
            }
        }

        //TODO I'm not even using the return value rn... remove?
        //TODO figure out how to implement dat error checking n stuff... don't think I need to anymore?
        /// <summary>
        /// Replaces the given references with the given file's data
        /// </summary>
        /// <param name="referenceCoords">The references to the data to replace</param>
        /// <param name="file">The new data to replace</param>
        /// <returns>Whether or not the replacement was unique</returns>
        public bool? Replace(Tuple<int, int>[] referenceCoords, string file)
        {
            bool globalUnique = false;
            bool localUnique = false;
            referenceCoords = referenceCoords.OrderBy(x => x.Item1).OrderBy(x => x.Item2).ToArray();

            #region Valid cell finder
            int badCells = referenceCoords.Where(x => x.Item1 >= openedData.Count).Count();
            if (badCells > 0)
            {
                //TODO replace all MessageBox.show stuff here (maybe use exceptions?)
                if (badCells == referenceCoords.Length)
                {
                    DisplayInformation(this, new DisplayInformationEventArgs("The references you have selected to replace are not valid for replacing.", "Reference Replacement"));
                    return null;
                }
                else
                {
                    //TODO try and simplify?
                    AskToContinueEventArgs a = new AskToContinueEventArgs($"{badCells} of the cells you've selected for replacing are not valid to repalce. Would you like to continue using all remaining valid cells?", "Reference Replacement");
                    AskToContinue(this, a);
                    if (!a.result)
                        return null;
                    else
                        referenceCoords = referenceCoords.Where(x => x.Item1 < openedData.Count).ToArray();
                }
            }
            #endregion

            #region Length of replacement finder

            long lengthOfReplacementBytes = new FileInfo(file).Length;

            long lengthOfVariables = 0;
            long lengthOfInvariables = 0;

            for (int i = 0; i < referenceCoords.Length; i++)
            {
                switch(openedData[referenceCoords[i].Item1][referenceCoords[i].Item2].lengthType)
                {
                    case (Reference.LengthType.variable):
                        lengthOfVariables += openedData[referenceCoords[i].Item1][referenceCoords[i].Item2].length;
                        break;
                    case (Reference.LengthType.invariable):
                        lengthOfInvariables += openedData[referenceCoords[i].Item1][referenceCoords[i].Item2].length;
                        break;
                }
            }
            #endregion

            if(lengthOfInvariables > 0)
            {
                DisplayInformation(this, new DisplayInformationEventArgs("THIS MIGHT NOT WORK", "WARNING"));
            }

            #region reference replacer

            long replacementBytesReadOffset = 0;
            int previousRow = referenceCoords[0].Item1;

            //Open the new bytes
            using (FileStream fsr = new FileStream(file, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fsr))
            {
                for (int i = 0; i < referenceCoords.Length; i++)
                {
                    //If the reference doesn't already haev a temp file of its own, give it one
                    Reference r = openedData[referenceCoords[i].Item1][referenceCoords[i].Item2];
                    if (r.file == openFileLocation)
                    {
                        r.file = Path.GetTempFileName();
                        r.offset = 0;
                    }

                    //int readLength = r.length;
                    if (r.lengthType == Reference.LengthType.variable && lengthOfReplacementBytes != lengthOfVariables)
                    {
                        //This equation is acutally a lot simpler than it looks.
                        //(LengthOfReference / LengthOfVariables) * (LengthOfReplacementBytes - LengthOfInvariables) rounded to nearest whole number, then cast to an int
                        //TODO add cases for when r.length exceeds int.MaxValue
                        r.length = (int)Math.Round(decimal.Divide(r.length, lengthOfVariables) * (lengthOfReplacementBytes - lengthOfInvariables));
                        globalUnique = localUnique = true;
                    }

                    if (new FileInfo(r.file).Length > 0)
                    {
                        //Store the original offset to return to after the replacing
                        long originalOffset = replacementBytesReadOffset;

                        //HACK this is probably really slow...
                        using (FileStream oldfsr = new FileStream(r.file, FileMode.Open, FileAccess.Read))
                        using (BinaryReader oldbr = new BinaryReader(oldfsr))
                        {
                            for (int _i = 0; _i < r.length; _i++)
                            {
                                if (br.ReadByte() != oldbr.ReadByte())
                                {
                                    globalUnique = localUnique = true;
                                    fsr.Seek(originalOffset, SeekOrigin.Begin);
                                    r.validity = Reference.ReferenceValidity.unknown;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        r.validity = Reference.ReferenceValidity.unknown;
                        globalUnique = localUnique = true;
                    }

                    //TODO maybe use this instead of WriteAllBytes?
                    /*
                    using (FileStream fsw = new FileStream(r.file, FileMode.Create, FileAccess.Write))
                    using (BinaryWriter bw = new BinaryWriter(fsw))
                    {
                        fsr.Seek(replacementBytesReadOffset, SeekOrigin.Begin);
                        bw.Write(br.ReadBytes(r.length));
                    }
                    */
                    //TODO or maybe use Stream.CopyTo?
                    
                    File.WriteAllBytes(r.file, br.ReadBytes(r.length));

                    if (localUnique && ((i < referenceCoords.Length - 1) ? referenceCoords[i].Item1 == referenceCoords[i + 1].Item1 : true))
                    {
                        UpdateReferenceValidity(referenceCoords[i].Item1);
                        localUnique = false;
                    }

                    /*Thank goodness this is no longer needed
                    if (localUnique)
                    {
                        if (i != referenceCoords.Length - 1)
                        {
                            if (referenceCoords[i].Item1 == referenceCoords[i + 1].Item1)
                            {
                                UpdateReferenceValidity(referenceCoords[i].Item1);
                                //RowUpdated(this, new RowUpdatedEventArgs(referenceCoords[i].Item1, openedData[referenceCoords[i].Item1]));
                                localUnique = false;
                            }
                        }
                        else
                        {
                            UpdateReferenceValidity(referenceCoords[i].Item1);
                            //RowUpdated(this, new RowUpdatedEventArgs(referenceCoords[i].Item1, openedData[referenceCoords[i].Item1]));
                            localUnique = false;
                        }
                    }
                    */

                    replacementBytesReadOffset += r.length;
                }
                return globalUnique;
            }
            #endregion   
        }
        public bool? Replace(Tuple<int,int> referenceCoord, object data)
        {
            bool unique = false;

            #region Valid cell finder

            if (referenceCoord.Item1 >= openedData.Count)
            {
                DisplayInformation(this, new DisplayInformationEventArgs("The references you have selected to replace are not valid for replacing.", "Error"));
                return null;
            }

            #endregion

            #region Input formatter

            byte[] newData = plugins[selectedPlugin].FormatReplacementInput(referenceCoord.Item2, data);
            if (newData == null)
            {
                UpdateReferenceValidity(referenceCoord.Item1);
                return null;
            }

            #endregion

            #region invariable reference length checker
            if (openedData[referenceCoord.Item1][referenceCoord.Item2].lengthType == Reference.LengthType.invariable)
            {
                int referenceLength = openedData[referenceCoord.Item1][referenceCoord.Item2].length;
                if ( referenceLength > newData.Length)
                {
                    AskToContinueEventArgs aTC = new AskToContinueEventArgs($"The data you've provided is longer than the required length of the reference.\nWould you like to use the first {referenceLength} bytes?", "WARNING");
                    AskToContinue(this, aTC);
                    //if (MessageBox.Show($"The data you've provided is longer than the required length of the reference.\nWould you like to use the first {referenceLength} bytes?", "WARNING", MessageBoxButtons.YesNo) != DialogResult.Yes)
                    if(!aTC.result)
                        return null;
                }
                else if (referenceLength < newData.Length)
                {
                    //TODO maybe put something here that will add zeros until it reaches the threshold?
                    DisplayInformation(this, new DisplayInformationEventArgs("The data you've provided is too small...", "ERROR"));
                    //MessageBox.Show("The data you've provided is too small...", "ERROR");
                    return null;
                }
            }
            #endregion

            #region reference replacer

            Reference r = openedData[referenceCoord.Item1][referenceCoord.Item2];
            if (r.file == openFileLocation)
            {
                r.file = Path.GetTempFileName();
                r.offset = 0;
            }

            if (r.lengthType == Reference.LengthType.variable)
            {
                //TODO add cases for when r.length exceeds int.MaxValue
                r.length = newData.Length;
                unique = true;
            }

            if (new FileInfo(r.file).Length > 0)
            {
                //HACK this is probably really slow...
                using (FileStream oldfsr = new FileStream(r.file, FileMode.Open, FileAccess.Read))
                using (BinaryReader oldbr = new BinaryReader(oldfsr))
                {
                    for (int i = 0; i < r.length; i++)
                    {
                        if (newData[i] != oldbr.ReadByte())
                        {
                            unique = true;
                            r.validity = Reference.ReferenceValidity.unknown;
                            break;
                        }
                    }
                }
            }
            else
            {
                r.validity = Reference.ReferenceValidity.unknown;
                unique = true;
            }
            
            File.WriteAllBytes(r.file, newData.Take(r.length).ToArray());
            //r.validity = reference.ReferenceValidity.unknown;

            if (unique)
            {
                UpdateReferenceValidity(referenceCoord.Item1);
                //RowUpdated(this, new RowUpdatedEventArgs(referenceCoord.Item1, openedData[referenceCoord.Item1]));
            }

            return unique;

            #endregion
        }
        

        #region Hotfile stuff

        public class HotfileModifiedEventArgs : EventArgs
        {
            public string name { get; }
            public Tuple<int,int>[] contents { get; }
            public string[] contentNames { get; }

            public HotfileModifiedEventArgs(string name, Tuple<int,int>[] contents, string[] contentNames)
            {
                this.name = name;
                this.contents = contents;
                this.contentNames = contentNames;
            }
        }
        public event EventHandler<HotfileModifiedEventArgs> HotfileChanged = new EventHandler<HotfileModifiedEventArgs>((o, e) => { });
        private void UpdateHotfile(object o, HotfileModifiedEventArgs e)
        {
            Export(e.contents, e.name);
            //File.WriteAllBytes(e.name, plugins.ElementAt(selectedPlugin).ExportSelectedAsArray(e.contents));
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
                Tuple<int,int>[] cellsToRename = cellsInHotfiles.Where(x => x.Value == e.OldFullPath).Select(x => x.Key).ToArray();
                for (int i = 0; i < cellsToRename.Length; i++)
                {
                    cellsInHotfiles.Remove(cellsToRename[i]);
                    cellsInHotfiles.Add(cellsToRename[i], e.FullPath);
                }

                HotfileDeleted(this, new HotfileDeletedEventArgs(e.OldFullPath));
                HotfileChanged(this, new HotfileModifiedEventArgs(e.FullPath, cellsToRename,plugins[selectedPlugin].GetHotfileInfo(cellsToRename)));
            }
        }

        /// <summary>
        /// Fires whenever a hotfile has its contents changed.
        /// Replaces the given cells with the contents of the watched file.
        /// </summary>
        /// <param name="fsw">The FileSystemWatcher that is watching the hotfile.</param>
        /// <param name="referenceCoords">the references to be replaced.</param>
        int hcTryCount = 0;
        public void Hotfile_Changed(FileSystemWatcher fsw, Tuple<int,int>[] referenceCoords)
        {
            bool? unique;

            #region replacement code
            try
            {
                //plugins.ElementAt(selectedPlugin).ReplaceSelectedWith(File.ReadAllBytes(Path.Combine(fsw.Path, fsw.Filter)), referenceCoords);
                unique = Replace(referenceCoords, Path.Combine(fsw.Path, fsw.Filter));
                hcTryCount = 0;
            }
            //If we encounter an IOException, that just means that something is still holding up the replacement of the file, so try again. (Maximum of 10 trys; not sure if this is the best idea?)
            catch (IOException)
            {
                if (hcTryCount < 10)
                {
                    hcTryCount++;
                    Hotfile_Changed(fsw, referenceCoords);
                }
                else
                {
                    MessageBox.Show("Replacement failed. :(", "ERROR");
                    hcTryCount = 0;
                }
                return;
            }
            #endregion

            #region Auto Export code
            if (autoExport && unique == true)
            {
                //var bytesToWrite = plugins.ElementAt(selectedPlugin).ExportAll();

                /*If the previous file we saved and the file we're about to save are no different than the last one we exported, then we stop
                if (File.Exists((iterateMode) ? hotfileIterationExportFile : hotfileExportFile))
                    if (Enumerable.SequenceEqual(bytesToWrite, (iterateMode) ? File.ReadAllBytes(hotfileIterationExportFile) : File.ReadAllBytes(hotfileExportFile)))
                        return;
                */

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
                Save(fileToWrite);
                //File.WriteAllBytes(fileToWrite, bytesToWrite);
            }
            #endregion
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
                Tuple<int,int>[] cellsToRemove = cellsInHotfiles.Where(x => x.Value == e.FullPath).Select(x => x.Key).ToArray();
                for (int i = 0; i < cellsToRemove.Length; i++)
                    cellsInHotfiles.Remove(cellsToRemove[i]);

                HotfileDeleted(this, new HotfileDeletedEventArgs(e.FullPath));
            }
        }
        
        /// <summary>
        /// Creates a new hotfile out of the cells provided.
        /// </summary>
        /// <param name="hotfileReferenceCoords">The cells to make a hotfile out of.</param>
        /// <param name="hotfilePath">The path to create the hotile at.</param>
        public void NewHotfile(Tuple<int,int>[] hotfileReferenceCoords, string hotfilePath)
        {
            var safeReferences = GetSafeReferences(hotfileReferenceCoords);
            //If this returns null, the user has cancelled, so we stop
            if (safeReferences == null)
                return;

            //We trigger the event first to prevent the newly created hotfile from immedietly exporting.
            HotfileChanged(this, new HotfileModifiedEventArgs(hotfilePath, safeReferences,plugins[selectedPlugin].GetHotfileInfo(safeReferences)));

            //Write the actual hotfile and add to the hotfile list
            //File.WriteAllBytes(hotfilePath, plugins.ElementAt(selectedPlugin).ExportSelectedAsArray(sortedCells.ToArray()));
            hotfiles.Add(hotfilePath, new Hotfile(this, hotfilePath, safeReferences.ToList()));

            //Add all the cells contained in the new hotfile to the cellsInHotfiles list
            for (int i = 0; i < safeReferences.Length; i++)
                cellsInHotfiles.Add(safeReferences[i], hotfilePath);

            //HotfileChanged(this, new HotfileModifiedEventArgs(hotfilePath, sortedCells.ToArray()));
        }

        /// <summary>
        /// Adds the given cells to the given hotfile.
        /// </summary>
        /// <param name="cells">The cells to add to the given hotfile.</param>
        /// <param name="hotfile">The hotfile to add the given cells to.</param>
        public void AddtoHotfile(Tuple<int,int>[] cells, string hotfile)
        {
            //Out of the selected cells, get all the ones that aren't already in a hotfile
            Tuple<int,int>[] sortedCells = GetSafeReferences(cells);
            //If this returned null, that means that the user either cancelled, or every cell they selected is part of a hotfile. Either way, we return
            if (sortedCells == null)
                return;

            //Store the current hotfile data, add the cells, then give back the new data
            List<Tuple<int,int>> currentData = hotfiles[hotfile].references.ToList();
            currentData.AddRange(sortedCells);
            hotfiles[hotfile].references = currentData.ToList();

            //Write the hotfile again
            //File.WriteAllBytes(hotfile, plugins.ElementAt(selectedPlugin).ExportSelectedAsArray(hotfiles[hotfile].data));

            HotfileChanged(this, new HotfileModifiedEventArgs(hotfile, currentData.ToArray(), plugins[selectedPlugin].GetHotfileInfo(currentData.ToArray())));
        }

        //TODO make this not require a TreeNode... somehow...
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
                Tuple<int,int>[] cellsToRemove = cellsInHotfiles.Where(x => x.Value == hotfileName).Select(x => x.Key).ToArray();
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
                var hotfileData = hotfiles[hotfileName].references.ToList();
                //Remove the data that corresponded to the removed node
                hotfileData.RemoveAt(removedNode.Index);
                //Set the data of the hotfile to be the new data
                hotfiles[hotfileName].references = hotfileData.ToList();

                //Remove the node from the hotfile manager
                //removedNode.Remove();

                //Update the actual hotfile
                //File.WriteAllBytes(hotfileName, plugins.ElementAt(selectedPlugin).ExportSelectedAsArray(hotfiles[hotfileName].data));
                HotfileChanged(this, new HotfileModifiedEventArgs(hotfileName, hotfiles[hotfileName].references.ToArray(),plugins[selectedPlugin].GetHotfileInfo(hotfiles[hotfileName].references.ToArray())));
            }
        }  

        /// <summary>
        /// Returns what cells out of the given cells are safe to be adding to hotfiles.
        /// Will return null if the user cancels at this point.
        /// </summary>
        /// <param name="references">The cells to check.</param>
        /// <returns>The cells that are safe for adding to hotfiles.</returns>
        public Tuple<int,int>[] GetSafeReferences(Tuple<int,int>[] references)
        {
            //Sort the references by Column index, then by Row index
            references = references.OrderBy(c => c.Item1).OrderBy(r => r.Item2).ToArray();

            List<Tuple<int,int>> safeReferences = new List<Tuple<int,int>>();
            bool unsafeReferencesExist = false;

            for (int i = 0; i < references.Length; i++)
            {
                //If the reference is already in a hotfile, or the reference is not exportable, the it's not safe
                if (cellsInHotfiles.ContainsKey(references[i]) || !openedData[references[i].Item1][references[i].Item2].exportable)
                    unsafeReferencesExist = true;
                else
                    safeReferences.Add(references[i]);
            }

            if (unsafeReferencesExist)
            {
                AskToContinueEventArgs aTC = new AskToContinueEventArgs($"Only {safeReferences.Count} out of the given {references.Length} references are not already in a hotfile.\nWould you like to use the remaining references anyways?", "Warning");
                AskToContinue(this, aTC);
                /*
                DialogResult dr = MessageBox.Show($"Only {safeReferences.Count} out of the given {references.Length} references are not already in a hotfile.\nWould you like to use the remaining references anyways?", "Warning", MessageBoxButtons.YesNo);
                if (dr != DialogResult.Yes)
                    */
                if(!aTC.result)
                    return null;
            }

            //Return the safe cells
            return safeReferences.ToArray();
        }

        #endregion

        #region Reference adding/moving/removing

        //TODO add support for updating hotfiles
        //HACK might not work
        public void MoveReferences(int oldRow, int newRow)
        {
            List<string> hotfilesToEdit = new List<string>();
            for (int i = 0; i < cellsInHotfiles.Count; i++)
            {
                if (cellsInHotfiles.ElementAt(i).Key.Item1 == oldRow)
                {
                    var reference = cellsInHotfiles.ElementAt(i);
                    hotfilesToEdit.Add(reference.Value);
                    cellsInHotfiles.Remove(reference.Key);
                    cellsInHotfiles.Add(new Tuple<int, int>(newRow, reference.Key.Item2), reference.Value);
                }
            }
            
            for(int i = 0; i < hotfilesToEdit.Count; i++)
                for (int _i = 0; _i < hotfiles[hotfilesToEdit[i]].references.Count; _i++)
                    if (hotfiles[hotfilesToEdit[i]].references[_i].Item1 == oldRow)
                        hotfiles[hotfilesToEdit[i]].references[_i] = new Tuple<int, int>(newRow, hotfiles[hotfilesToEdit[i]].references[_i].Item2);
            

            var rowToMove = openedData[oldRow];
            openedData.RemoveAt(oldRow);
            openedData.Insert(newRow, rowToMove);
        }

        public void AddReference()
        {
            openedData.Add(plugins[selectedPlugin].defaultReference);
        }

        public void RemoveReferences(int row)
        {
            //TODO look into that ToDictionary part...
            cellsInHotfiles = cellsInHotfiles.Where(x => x.Key.Item1 != row).ToDictionary( x=> x.Key, x => x.Value);

            for (int i = 0; i < hotfiles.Count; i++)
            {
                var newReferences = hotfiles.ElementAt(i).Value.references.Where(x => x.Item1 != row).ToList();
                if (newReferences.Count == 0)
                    hotfiles.Remove(hotfiles.ElementAt(i).Key);
                else if(newReferences.Count != hotfiles.ElementAt(i).Value.references.Count)
                    hotfiles.ElementAt(i).Value.references = newReferences;
            }

            openedData.RemoveAt(row);
        }

        #endregion

    }
}
