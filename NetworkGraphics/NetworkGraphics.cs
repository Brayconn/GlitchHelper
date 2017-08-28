using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PluginBase;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Data;
using System.Text.RegularExpressions;

namespace NetworkGraphics
{
    public class NetworkGraphics : IGHPlugin
    {
        public string filter
        {
            get
            {
                return "Network Graphics Files (*.png;*.apng;*.mng;*.jng)|*.png;*.apng;*.mng;*.jng";
            }
        }

        public DataGridViewCell[] selectedCells { get; set; }

        public DataGridViewColumn[] columns
        {
            get
            {
                return new DataGridViewColumn[]
                {
                new DataGridViewTextBoxColumn()
                {
                    Name = "length",
                    HeaderText = "Length",
                    SortMode = DataGridViewColumnSortMode.NotSortable,
                    ValueType = typeof(int)
                },
                new DataGridViewTextBoxColumn()
                {
                    Name = "type",
                    HeaderText = "Type",
                    SortMode = DataGridViewColumnSortMode.NotSortable,
                    ValueType = typeof(string)
                },
                new DataGridViewTextBoxColumn()
                {
                    Name = "data",
                    HeaderText = "Data",
                    SortMode = DataGridViewColumnSortMode.NotSortable,
                    ValueType = typeof(string),
                    ReadOnly = true
                },
                new DataGridViewTextBoxColumn()
                {
                    Name = "crc",
                    HeaderText = "CRC",
                    SortMode = DataGridViewColumnSortMode.NotSortable,
                    ValueType = typeof(string),
                    //ReadOnly = true
                }
                };
            }
        }

        public ToolStripItem[] customMenuStripOptions
        {
            get
            {
                ToolStripMenuItem recalculateCRCToolStripMenuItem = new ToolStripMenuItem
                {
                    Text = "Auto-Recalculate CRC",
                    Checked = true,
                    CheckOnClick = true,
                };
                recalculateCRCToolStripMenuItem.CheckedChanged += delegate { autoCalculateCRC = recalculateCRCToolStripMenuItem.Checked; };

                ToolStripMenuItem verboseCRCCheckingToolStripMenuItem = new ToolStripMenuItem
                {
                    Text = "Verbose CRC Checks",
                    Checked = false,
                    CheckOnClick = true
                };
                verboseCRCCheckingToolStripMenuItem.CheckedChanged += delegate { verboseCRCChecks = verboseCRCCheckingToolStripMenuItem.Checked; };

                return new ToolStripItem[]
                {
                    recalculateCRCToolStripMenuItem,
                    verboseCRCCheckingToolStripMenuItem
                };
            }
        }

        public ToolStripItem[] customContextMenuStripButtons
        {
            get
            {
                return new ToolStripItem[]
                {
                    new ToolStripMenuItem("Re-Calculate CRC", null, delegate { ReCalculateCRCContextMenuStripButton_Click(); })
                };
            }
        }

        #region Default Headers
        private static readonly byte[] pngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly byte[] mngHeader = { 0x8A, 0x4D, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly byte[] jngHeader = { 0x8B, 0x4A, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        #endregion

        public bool verboseCRCChecks = false;
        public bool autoCalculateCRC = true;
        private NetworkGraphic openedFile;
        private enum FileType
        {
            png,
            mng,
            jng,
            unknown
        }

        #region Chunk types (hopefully I actually understood the docs properly 3:)

        private static readonly byte[][] pngChunkTypes =
        {
            //Critical
            Encoding.ASCII.GetBytes("IHDR"),
            Encoding.ASCII.GetBytes("PLTE"),
            Encoding.ASCII.GetBytes("IDAT"),
            Encoding.ASCII.GetBytes("IEND"),

            //Ancillary
            Encoding.ASCII.GetBytes("tRNS"),
            Encoding.ASCII.GetBytes("gAMA"),
            Encoding.ASCII.GetBytes("cHRM"),
            Encoding.ASCII.GetBytes("sRGB"),
            Encoding.ASCII.GetBytes("iCCP"),
            Encoding.ASCII.GetBytes("iTXt"),
            Encoding.ASCII.GetBytes("tEXt"),
            Encoding.ASCII.GetBytes("zTXt"),
            Encoding.ASCII.GetBytes("bKGD"),
            Encoding.ASCII.GetBytes("pHYs"),
            Encoding.ASCII.GetBytes("sBIT"),
            Encoding.ASCII.GetBytes("sPLT"),
            Encoding.ASCII.GetBytes("hIST"),
            Encoding.ASCII.GetBytes("tIME"),
            Encoding.ASCII.GetBytes("sCAL"),
            Encoding.ASCII.GetBytes("oFFs"),
            Encoding.ASCII.GetBytes("hIST"),
            Encoding.ASCII.GetBytes("pCAL"),
            Encoding.ASCII.GetBytes("fRAc"),
            Encoding.ASCII.GetBytes("gIF*"),

            //APNG Support
            Encoding.ASCII.GetBytes("acTL"),
            Encoding.ASCII.GetBytes("fcTL"),
            Encoding.ASCII.GetBytes("fdAT")
        };

        private static readonly byte[][] mngChunkTypes =
        {
            //Critical
            Encoding.ASCII.GetBytes("MHDR"),
            Encoding.ASCII.GetBytes("IHDR"),
            Encoding.ASCII.GetBytes("PLTE"),
            Encoding.ASCII.GetBytes("IDAT"),
            Encoding.ASCII.GetBytes("LOOP"),
            Encoding.ASCII.GetBytes("ENDL"),
            Encoding.ASCII.GetBytes("MEND"),
            Encoding.ASCII.GetBytes("IHDR"),
            Encoding.ASCII.GetBytes("JHDR"),
            Encoding.ASCII.GetBytes("TERM"),
            Encoding.ASCII.GetBytes("BACK"),
            Encoding.ASCII.GetBytes("SAVE"),
            Encoding.ASCII.GetBytes("SEEK"),
            Encoding.ASCII.GetBytes("DEFI"),
            Encoding.ASCII.GetBytes("JDAT"),
            Encoding.ASCII.GetBytes("JDAA"),
            Encoding.ASCII.GetBytes("JSEP"),

            //Ancillary
            Encoding.ASCII.GetBytes("tRNS"),
            Encoding.ASCII.GetBytes("eXPI"),
            Encoding.ASCII.GetBytes("pHYg"),
            Encoding.ASCII.GetBytes("gAMA"),
            Encoding.ASCII.GetBytes("cHRM"),
            Encoding.ASCII.GetBytes("sRGB"),
            Encoding.ASCII.GetBytes("iCCP"),
            Encoding.ASCII.GetBytes("iTXt"),
            Encoding.ASCII.GetBytes("tEXt"),
            Encoding.ASCII.GetBytes("zTXt"),
            Encoding.ASCII.GetBytes("bKGD"),
            Encoding.ASCII.GetBytes("pHYs"),
            Encoding.ASCII.GetBytes("sBIT"),
            Encoding.ASCII.GetBytes("tIME"),
        };

        private static readonly byte[][] jngChunkTypes =
        {
            //Critical
            Encoding.ASCII.GetBytes("JHDR"),
            Encoding.ASCII.GetBytes("IDAT"),
            Encoding.ASCII.GetBytes("JDAT"),
            Encoding.ASCII.GetBytes("JDAA"),
            Encoding.ASCII.GetBytes("JSEP"),
            Encoding.ASCII.GetBytes("IEND"),

            //Ancillary
            Encoding.ASCII.GetBytes("gAMA"),
            Encoding.ASCII.GetBytes("cHRM"),
            Encoding.ASCII.GetBytes("sRGB"),
            Encoding.ASCII.GetBytes("iCCP"),
            Encoding.ASCII.GetBytes("iTXt"),
            Encoding.ASCII.GetBytes("tEXt"),
            Encoding.ASCII.GetBytes("zTXt"),
            Encoding.ASCII.GetBytes("bKGD"),
            Encoding.ASCII.GetBytes("pHYs"),
            Encoding.ASCII.GetBytes("tIME"),
            Encoding.ASCII.GetBytes("sCAL"),
            Encoding.ASCII.GetBytes("oFFs"),
            Encoding.ASCII.GetBytes("pCAL"),
        };

        #endregion

        //TODO have the crc table be declared inline to save on re-calculating every time the user starts the program (might be a terrible idea)
        /// <summary>
        /// Table containing the crc of all possible byte values
        /// </summary>
        private static uint[] crc_table = null;

        private class chunk
        {
            /// <summary>
            /// The length of the chunk's data
            /// </summary>
            public int length { get; set; }
            /// <summary>
            /// The chunk's type
            /// </summary>
            public byte[] type { get; set; }
            /// <summary>
            /// The chunk's data
            /// </summary>
            public byte[] data { get; set; }
            /// <summary>
            /// The crc of the chunk (calculated using the chunk's type and data)
            /// </summary>
            public byte[] crc { get; set; }


            public chunk()
            {
                length = 0;
                //type = Encoding.ASCII.GetBytes("IEND");
                type = new byte[] { 0x49, 0x45, 0x4e, 0x44 };
                data = new byte[0];
                crc = new byte[] { 174, 66, 96, 130 };
            }
            public chunk(int length, byte[] type, byte[] data, byte[] crc)
            {
                this.length = length;
                this.type = type;
                this.data = data;
                this.crc = crc;
            }
        }

        private class NetworkGraphic
        {
            public FileType fileType { get; set; }
            public byte[] header { get; set; }
            public List<chunk> data { get; set; }

            /// <summary>
            /// Initializes a new png using the given byte array
            /// </summary>
            /// <param name="input">the byte array to sort into a png</param>
            public NetworkGraphic(byte[] input)
            {
                header = input.Take(8).ToArray();
                input = input.Skip(8).ToArray();

                if (Enumerable.SequenceEqual(header, pngHeader))
                    fileType = FileType.png;
                else if (Enumerable.SequenceEqual(header, mngHeader))
                    fileType = FileType.mng;
                else if (Enumerable.SequenceEqual(header, jngHeader))
                    fileType = FileType.jng;
                else
                    fileType = FileType.unknown;

                data = new List<chunk>();
                while (input.Length > 0)
                {
                    int length = BitConverter.ToInt32(input.Take(4).Reverse().ToArray(),0);
                    var workingchunk = new chunk(
                        length,
                        input.Skip(4).Take(4).ToArray(),
                        input.Skip(8).Take(length).ToArray(),
                        input.Skip(8).Skip(length).Take(4).ToArray()
                        );

                    data.Add(workingchunk);
                    input = input.Skip(12).Skip(length).ToArray();
                    
                    /*Make a new working chunk
                    var workingchunk = new chunk(
                        BitConverter.ToInt32(input.Take(4).Reverse().ToArray(), 0),
                        input.Skip(4).Take(4).ToArray(),
                        input.Skip(8).Take(BitConverter.ToInt32(input.Take(4).Reverse().ToArray(), 0)).ToArray(),
                        input.Skip(12).Skip(BitConverter.ToInt32(input.Take(4).Reverse().ToArray(), 0)).ToArray()
                        );
                    
                    //Store the chunk's length as an int
                    workingchunk.length = BitConverter.ToInt32(input.Take(4).Reverse().ToArray(), 0);
                    //Store the chunk type as a byte array
                    workingchunk.type = input.Skip(4).Take(4).ToArray();
                    //Store the data of the chunk.
                    workingchunk.data = input.Skip(8).Take(workingchunk.length).ToArray();
                    //Store the chunk crc as a byte array
                    workingchunk.crc = input.Skip(8).Skip(workingchunk.length).Take(4).ToArray();
                    //Subtract all previous chunk data from the input
                    input = input.Skip(12).Skip(workingchunk.length).ToArray();

                    data.Add(workingchunk);*/
                }
            }
        }
        
        /// <summary>
        /// Loads a given byte[] into memory as a NetworkGraphic
        /// </summary>
        /// <param name="selectedBytes">The bytes to load into memory</param>
        /// <returns>Filter for use with saving the final file</returns>
        public string Load(byte[] selectedBytes)
        {
            string returnFilter = "";

            //Try and find out what kind of Network Graphic we're opening
            if(Enumerable.SequenceEqual(selectedBytes.Take(8), pngHeader))
                returnFilter = "Portable Network Graphics (*.png)|*.png";
            else if (Enumerable.SequenceEqual(selectedBytes.Take(8), mngHeader))
                returnFilter = "Multiple Network Graphics (*.mng)|*.mng";
            else if (Enumerable.SequenceEqual(selectedBytes.Take(8), jngHeader))
                returnFilter = "JPEG Network Graphics (*.jng)|*.jng";
            else
            {
                DialogResult warningDR = MessageBox.Show("The selected file's header does not match with any Network Graphics header (png, mng, or jng.\nWould you like to open the file anyways?", "Warning", MessageBoxButtons.YesNo);
                if (warningDR != DialogResult.Yes)
                    return null;
                else
                    returnFilter = "Portable Network Graphics (*.png)|*.png";
            }
            openedFile = new NetworkGraphic(selectedBytes);
            return returnFilter;
        }

        /*
        public void PopulateRows(DataGridViewColumnCollection columns, DataGridViewRowCollection rows)
        {


            //dgv.Columns.Add("length", "Length");
            //dgv.Columns["length"].ValueType = typeof(int);
            //dgv.Columns["length"].SortMode = DataGridViewColumnSortMode.NotSortable;

            columns.AddRange(new DataGridViewColumn[]
            {
            new DataGridViewTextBoxColumn()
            {
                Name = "length",
                HeaderText = "Length",
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ValueType = typeof(int)
            },
            new DataGridViewTextBoxColumn()
            {
                Name = "type",
                HeaderText = "Type",
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ValueType = typeof(string)
            },
            new DataGridViewTextBoxColumn()
            {
                Name = "data",
                HeaderText = "Data",
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ValueType = typeof(string),
                ReadOnly = true
            },
            new DataGridViewTextBoxColumn()
            {
                Name = "crc",
                HeaderText = "CRC",
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ValueType = typeof(string),
                //ReadOnly = true
            }
            });
            /*
            dgv.Columns.Add(new DataGridViewColumn()
            {
                Name = "length",
                HeaderText = "Length",
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ValueType = typeof(int)
            });

            //dgv.Columns.Add("type", "Type");
            //dgv.Columns["type"].ValueType = typeof(string);

            dgv.Columns.Add(new DataGridViewColumn()
            {
                Name = "type",
                HeaderText = "Type",
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ValueType = typeof(string)
            });

            //dgv.Columns.Add("data", "Data");
            //dgv.Columns["data"].ValueType = typeof(string);
            //dgv.Columns["data"].ReadOnly = true;

            dgv.Columns.Add(new DataGridViewColumn()
            {
                Name = "data",
                HeaderText = "Data",
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ValueType = typeof(string),
                ReadOnly = true
            });


            //dgv.Columns.Add("crc", "CRC");
            //dgv.Columns["crc"].ValueType = typeof(string);
            //dgv.Columns["crc"].ReadOnly = true;

            dgv.Columns.Add(new DataGridViewColumn()
            {
                Name = "crc",
                HeaderText = "CRC",
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ValueType = typeof(string),
                ReadOnly = true
            });
            */
        /*
        var cms = dgv.ContextMenuStrip;
        cms.Items.Add("Re-Calculate CRC", null, delegate { ReCalculateCRCContextMenuStripButton_Click(dgv); });
        dgv.ContextMenuStrip = cms;
        */
        //dgv.Columns["crc"].ContextMenuStrip.Items.Add("Re-Calculate CRC", null, delegate { ReCalculateCRCContextMenuStripButton_Click(dgv); });
        /*
        foreach (DataGridViewColumn column in dgv.Columns)
        {
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
        }
        * /
        for (int i = 0; i < openedFile.data.Count; i++)
        {
            rows.Add(openedFile.data[i].length,
                         Encoding.ASCII.GetString(openedFile.data[i].type),
                         $"<{openedFile.data[i].data.Length}> bytes long",
                         BitConverter.ToString(openedFile.data[i].crc.ToArray()).Replace("-", ", ")
                         );
        }
    }
    */
        public DataGridViewRow[] GetRows()
        {
            List<DataGridViewRow> rows = new List<DataGridViewRow>(); 
            for (int i = 0; i < openedFile.data.Count; i++)
            {
                DataGridViewRow row = new DataGridViewRow();
                row.Cells.AddRange(new DataGridViewCell[] {
                    new DataGridViewTextBoxCell
                    {
                        Value = openedFile.data[i].length
                    },
                    new DataGridViewTextBoxCell
                    {
                        Value = Encoding.ASCII.GetString(openedFile.data[i].type)
                    },
                    new DataGridViewTextBoxCell
                    {
                        Value = $"<{openedFile.data[i].data.Length}> bytes long"
                    },
                    new DataGridViewTextBoxCell
                    {
                        Value = BitConverter.ToString(openedFile.data[i].crc.ToArray()).Replace("-", ", ")
                    }
                });
                rows.Add(row);

                /*
                rows.Add(openedFile.data[i].length,
                             Encoding.ASCII.GetString(openedFile.data[i].type),
                             $"<{openedFile.data[i].data.Length}> bytes long",
                             BitConverter.ToString(openedFile.data[i].crc.ToArray()).Replace("-", ", ")
                             );
                */
            }
            return rows.ToArray();
        }

        /*
        public void PopulateRows(DataGridViewRowCollection rows)
        {
            for (int i = 0; i < openedFile.data.Count; i++)
            {
                rows.Add(openedFile.data[i].length,
                             Encoding.ASCII.GetString(openedFile.data[i].type),
                             $"<{openedFile.data[i].data.Length}> bytes long",
                             BitConverter.ToString(openedFile.data[i].crc.ToArray()).Replace("-", ", ")
                             );
            }
        }
        */

        /* OLD
        public void UpdateRows(DataGridViewRow row)
        {
            row.Cells[0].Value = openedFile.data[row.Index].length;
            row.Cells[1].Value = Encoding.ASCII.GetString(openedFile.data[row.Index].type);
            row.Cells[2].Value = $"<{openedFile.data[row.Index].data.Length}> bytes long";
            row.Cells[3].Value = BitConverter.ToString(openedFile.data[row.Index].crc.ToArray()).Replace("-", ", ");
        }
        */

        public void UpdateRows(DataGridViewRow row)
        {
            row.Cells[0].Value = openedFile.data[row.Index].length;
            if (openedFile.data[row.Cells[0].RowIndex].length != openedFile.data[row.Cells[0].RowIndex].data.Length)
                row.Cells[0].Style.BackColor = Color.Red;
            else
                row.Cells[0].Style.BackColor = Color.White;

            row.Cells[1].Value = Encoding.ASCII.GetString(openedFile.data[row.Index].type);
            switch (openedFile.fileType)
            {
                case (FileType.png):
                    if (!pngChunkTypes.Any(x => x.SequenceEqual(openedFile.data[row.Cells[1].RowIndex].type)))
                        row.Cells[1].Style.BackColor = Color.Red;
                    else
                        row.Cells[1].Style.BackColor = Color.White;
                    break;
                case (FileType.mng):
                    if (!mngChunkTypes.Any(x => x.SequenceEqual(openedFile.data[row.Cells[1].RowIndex].type)))
                        row.Cells[1].Style.BackColor = Color.Red;
                    else
                        row.Cells[1].Style.BackColor = Color.White;
                    break;
                case (FileType.jng):
                    if (!jngChunkTypes.Any(x => x.SequenceEqual(openedFile.data[row.Cells[1].RowIndex].type)))
                        row.Cells[1].Style.BackColor = Color.Red;
                    else
                        row.Cells[1].Style.BackColor = Color.White;
                    break;
            }

            row.Cells[2].Value = $"<{openedFile.data[row.Index].data.Length}> bytes long";
            if (openedFile.data[row.Cells[1].RowIndex].length != openedFile.data[row.Cells[1].RowIndex].data.Length)
                row.Cells[1].Style.BackColor = Color.Red;
            else
                row.Cells[1].Style.BackColor = Color.White;

            var crc = CalculateCRCOf(openedFile.data[row.Cells[3].RowIndex].type.Concat(openedFile.data[row.Cells[3].RowIndex].data).ToArray());

            /* Fruitless attempt at refactoring
            e.CellStyle.BackColor = Color.White;

            if ((!Enumerable.SequenceEqual(crc, openedFile.data[e.RowIndex].crc)) && autoCalculateCRC)
                openedFile.data[e.RowIndex].crc = crc;
            else
                e.CellStyle.BackColor = Color.Red;
            */

            if (!Enumerable.SequenceEqual(crc, openedFile.data[row.Cells[1].RowIndex].crc))
            {
                if (autoCalculateCRC)
                {
                    openedFile.data[row.Cells[1].RowIndex].crc = crc;
                    row.Cells[1].Value = BitConverter.ToString(crc).Replace("-", ", ");
                    row.Cells[1].Style.BackColor = Color.White;
                }
                else
                    row.Cells[1].Style.BackColor = Color.Red;
            }
            else
                row.Cells[1].Style.BackColor = Color.White;
            
            row.Cells[3].Value = BitConverter.ToString(openedFile.data[row.Index].crc.ToArray()).Replace("-", ", ");
        }
        public void UpdateRows(DataGridViewRow[] rows)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                //Is this even good practice?
                UpdateRows(rows[i]);
                /*
                rows[i].Cells[0].Value = openedFile.data[rows[i].Index].length;
                rows[i].Cells[1].Value = Encoding.ASCII.GetString(openedFile.data[rows[i].Index].type);
                rows[i].Cells[2].Value = $"<{openedFile.data[rows[i].Index].data.Length}> bytes long";
                rows[i].Cells[3].Value = BitConverter.ToString(openedFile.data[rows[i].Index].crc.ToArray()).Replace("-", ", ");
                */
            }
        }

        /*
        public ToolStripItem[] getCustomContextMenuStripItems()
        {
            return new ToolStripItem[]
            {
                //new ToolStripMenuItem("Re-Calculate CRC", null, delegate { ReCalculateCRCContextMenuStripButton_Click(ref cells); })
                new ToolStripMenuItem("Re-Calculate CRC", null, delegate { ReCalculateCRCContextMenuStripButton_Click(); })
            };
        }
        */
        /*
        public ToolStripItemCollection removeCustomContextMenuStripItems(ToolStripItemCollection ic)
        {
            ic.RemoveByKey("Re-Calculate CRC");
            return ic;
        }
        */
        /* MISGUIDED
        public void Populate(DataTable dt)
        {
            dt.Columns.Add("Length");
            dt.Columns["Length"].DataType = typeof(int);
            
            dt.Columns.Add("Type");
            dt.Columns["Type"].DataType = typeof(string);

            dt.Columns.Add("Data");
            dt.Columns["Data"].DataType = typeof(string);
            dt.Columns["Data"].ReadOnly = true;

            dt.Columns.Add("CRC");
            dt.Columns["CRC"].DataType = typeof(string);
            dt.Columns["CRC"].ReadOnly = true;

            /*
            var cms = dt.ContextMenuStrip;
            cms.Items.Add("Re-Calculate CRC", null, delegate { ReCalculateCRCContextMenuStripButton_Click(dt); });
            dt.ContextMenuStrip = cms;
            /
            //dt.Columns["crc"].ContextMenuStrip.Items.Add("Re-Calculate CRC", null, delegate { ReCalculateCRCContextMenuStripButton_Click(dt); });

            /*
            foreach (DataColumn column in dt.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
            /

            for (int i = 0; i < openedFile.data.Count; i++)
            {
                dt.Rows.Add(openedFile.data[i].length,
                             Encoding.ASCII.GetString(openedFile.data[i].type),
                             $"<{openedFile.data[i].data.Length}> bytes long",
                             BitConverter.ToString(openedFile.data[i].crc.ToArray()).Replace("-", ", ")
                             );
            }
        }
        */


        #region Exporting Stuff

        /*
        public byte[] ExportSelected(DataGridViewCellCollection cells)
        {
            if (cells.Count > 0)
            {
                /*
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Text Files (*.txt;)|*.txt|All Files (*.*)|*.*";
                sfd.AddExtension = true;

                if (sfd.ShowDialog() == DialogResult.OK)
                {* /
                    List<byte> bytesToWrite = new List<byte>();
                    var orderedCells = cells.Cast<DataGridViewCell>().OrderBy(c => c.ColumnIndex).OrderBy(c => c.RowIndex).ToArray();
                    for (int i = 0; i < orderedCells.Length; i++)
                    {
                        switch (orderedCells[i].ColumnIndex)
                        {
                            case (0):
                                bytesToWrite.AddRange(BitConverter.GetBytes(openedFile.data[orderedCells[i].RowIndex].length).Reverse().ToArray());
                                break;
                            case (1):
                                bytesToWrite.AddRange(openedFile.data[orderedCells[i].RowIndex].type);
                                break;
                            case (2):
                                bytesToWrite.AddRange(openedFile.data[orderedCells[i].RowIndex].data);
                                break;
                            case (3):
                                bytesToWrite.AddRange(openedFile.data[orderedCells[i].RowIndex].crc);
                                break;
                        }
                    }
                return bytesToWrite.ToArray();
                   /* File.WriteAllBytes(sfd.FileName, bytesToWrite.ToArray());
                }* /
            }
            return null;
        }
        */

        public byte[] ExportAll()
        {
            List<byte> bytesToWrite = new List<byte>(openedFile.header);
            for (int i = 0; i < openedFile.data.Count; i++)
            {
                bytesToWrite.AddRange(BitConverter.GetBytes(openedFile.data[i].length).Reverse().ToArray());
                bytesToWrite.AddRange(openedFile.data[i].type);
                bytesToWrite.AddRange(openedFile.data[i].data);
                bytesToWrite.AddRange(openedFile.data[i].crc);
            }
            return bytesToWrite.ToArray();
        }

        //-----------------------------------------------------------------\\

        /*
        public List<byte[]> ExportAsList(DataGridViewCell[] cells)
        {
            List<byte[]> output = new List<byte[]>();
            for (int i = 0; i < cells.Length; i++)
                output.Add(GetCellData(cells[i]));
            return output;
        }
        */

        public List<byte[]> ExportSelectedAsList(DataGridViewCell[] cells = null)
        {
            if (cells == null)
                cells = selectedCells;

            //TODO maybe replace with linq?
            List<byte[]> output = new List<byte[]>();
            for (int i = 0; i < cells.Length; i++)
                output.Add(GetCellData(cells[i]));
            return output;
        }
        
        /*
        public byte[] ExportAsArray(DataGridViewCell[] cells)
        {
            List<byte> output = new List<byte>();
            for (int i = 0; i < cells.Length; i++)
                output.AddRange(GetCellData(cells[i]));
            return output.ToArray();
        }
        */

        public byte[] ExportSelectedAsArray(DataGridViewCell[] cells = null)
        {
            if (cells == null)
                cells = selectedCells;
                        
            //TODO maybe replace with linq?
            List<byte> output = new List<byte>();
            for (int i = 0; i < cells.Length; i++)
                output.AddRange(GetCellData(cells[i]));
            return output.ToArray();
        }

        private byte[] GetCellData(DataGridViewCell cell)
        {
            switch (cell.ColumnIndex)
            {
                case (0):
                    return BitConverter.GetBytes(openedFile.data[cell.RowIndex].length).Reverse().ToArray();
                case (1):
                    return openedFile.data[cell.RowIndex].type;
                case (2):
                    return openedFile.data[cell.RowIndex].data;
                case (3):
                    return openedFile.data[cell.RowIndex].crc;
                default:
                    return null;
            }
        }

        #endregion

        /*
        public void ReplaceSelected(DataGridViewCell[] cells, byte[] replacementBytes)
        {
            //byte[] replacementBytes = File.ReadAllBytes(ofd.FileName);

            var orderedCells = cells.Cast<DataGridViewCell>().OrderBy(c => c.ColumnIndex).OrderBy(c => c.RowIndex).ToArray();

            double lengthOfSelectedCells = 0;

            for (int i = 0; i < orderedCells.Length; i++)
            {
                switch (orderedCells[i].ColumnIndex)
                {
                    case (0):
                        lengthOfSelectedCells += BitConverter.GetBytes(openedFile.data[orderedCells[i].RowIndex].length).Length;
                        break;
                    case (1):
                        lengthOfSelectedCells += openedFile.data[orderedCells[i].RowIndex].type.Length;
                        break;
                    case (2):
                        lengthOfSelectedCells += openedFile.data[orderedCells[i].RowIndex].data.Length;
                        break;
                    case (3):
                        lengthOfSelectedCells += openedFile.data[orderedCells[i].RowIndex].crc.Length;
                        break;
                }
            }

            if (replacementBytes.Length != lengthOfSelectedCells)
            {
                DialogResult warningDR = MessageBox.Show("The file you have selected is not the same length as the chunk sections you wish to replace. Are you sure you want to continue?", "Warning", MessageBoxButtons.YesNo);
                if (warningDR == DialogResult.No)
                {
                    MessageBox.Show("Ok.","Oh.");
                    return;
                }
            }

            for (int i = 0; i < orderedCells.Length; i++)
            {
                switch (orderedCells[i].ColumnIndex)
                {
                    case (0):
                        openedFile.data[orderedCells[i].RowIndex].length = BitConverter.ToInt32(replacementBytes.Take(4).Reverse().ToArray(), 0);
                        orderedCells[i].Value = openedFile.data[orderedCells[i].RowIndex].length;
                        replacementBytes = replacementBytes.Skip(4).ToArray();
                        break;
                    case (1):
                        openedFile.data[orderedCells[i].RowIndex].type = replacementBytes.Take(4).ToArray();
                        orderedCells[i].Value = Encoding.ASCII.GetString(openedFile.data[orderedCells[i].RowIndex].type);
                        replacementBytes = replacementBytes.Skip(4).ToArray();
                        break;
                    case (2):
                        openedFile.data[orderedCells[i].RowIndex].data = replacementBytes.Take(openedFile.data[orderedCells[i].RowIndex].data.Length).ToArray();
                        orderedCells[i].Value = $"<{openedFile.data[orderedCells[i].RowIndex].data.Length}> bytes long";
                        replacementBytes = replacementBytes.Skip(openedFile.data[orderedCells[i].RowIndex].data.Length).ToArray();
                        break;
                    case (3):
                        openedFile.data[orderedCells[i].RowIndex].crc = replacementBytes.Take(4).ToArray();
                        orderedCells[i].Value = BitConverter.ToString(openedFile.data[orderedCells[i].RowIndex].crc).Replace("-", ", ");
                        replacementBytes = replacementBytes.Skip(4).ToArray();
                        break;
                }
            }

            /*
            if(autoCalculateCRC)
            {
                byte[] input = openedFile.data[editedCell.RowIndex].type.Concat(openedFile.data[editedCell.RowIndex].data).ToArray();
                byte[] crc = CalculateCRCOf(input);
                MessageBox.Show($"Chunk {editedCell.RowIndex + 1}'s CRC is {Enumerable.SequenceEqual(crc, openedFile.data[editedCell.RowIndex].crc)}.".Replace("True.", "OK").Replace("False.", "BAD"));
                openedFile.data[editedCell.RowIndex].crc = crc;
                editedCell.DataGridView["crc", editedCell.RowIndex].Value = BitConverter.ToString(crc).Replace("-", ", ");
            }
            * /
            
        }
        */

        public void ReplaceSelectedWith(byte[] replacementBytes, DataGridViewCell[] cells = null)
        {
            /*
            if (cells == null)
                cells = selectedCells;
            else
              cells = cells.OrderBy(c => c.ColumnIndex).OrderBy(c => c.RowIndex).ToArray();
            */
            cells = (cells != null) ? cells.OrderBy(c => c.ColumnIndex).OrderBy(r => r.RowIndex).ToArray() : selectedCells;

            #region Valid cell finder
            int badCells = cells.Where(x => x.RowIndex >= openedFile.data.Count).Count();
            if (badCells > 0)
            {
                //TODO replace all MessageBox.show stuff here (maybe use exceptions?)
                if (badCells == cells.Length)
                {
                    MessageBox.Show("The cells you have selected to replace are not valid for replacing.", "Error");
                    return;
                }
                else
                {
                    if (MessageBox.Show($"{badCells} of the cells you've selected for replacing are not valid to repalce. Would you like to continue using all remaining valid cells?", "Warning", MessageBoxButtons.YesNo) != DialogResult.Yes)
                        return;
                    else
                        cells = cells.Where(x => x.RowIndex < openedFile.data.Count).ToArray();
                }
            }
            #endregion

            #region Length of replacement finder

            long lengthOfReplacementBytes = replacementBytes.Length;
            
            long lengthOfcells = 0;

            for (int i = 0; i < cells.Length; i++)
            {
                switch (cells[i].ColumnIndex)
                {
                    case (0):
                        lengthOfcells += BitConverter.GetBytes(openedFile.data[cells[i].RowIndex].length).Length;
                        break;
                    case (1):
                        lengthOfcells += openedFile.data[cells[i].RowIndex].type.Length;
                        break;
                    case (2):
                        lengthOfcells += openedFile.data[cells[i].RowIndex].data.Length;
                        break;
                    case (3):
                        lengthOfcells += openedFile.data[cells[i].RowIndex].crc.Length;
                        break;
                }
            }
            #endregion

            #region Proportional cell replacer (only for data chunks)
            if (cells.All(x => x.ColumnIndex == 2))
            {
                for (int i = 0; i < cells.Length; i++)
                {
                    /* Explanation of that one line down below
                    
                    decimal percent = decimal.Divide(openedFile.data[cells[i].RowIndex].data.Length, lengthOfcells);
                                        
                    decimal amount = percent * lengthOfReplacementBytes;

                    //TODO maybe use AwayFromZero?
                    //TODO add support for when chunk length exceeds int max size
                    int amountToTake = (int)Math.Round(amount);

                    openedFile.data[cells[i].RowIndex].data = replacementBytes.Take(amountToTake).ToArray();

                    */
                    openedFile.data[cells[i].RowIndex].data = replacementBytes.Take((int)Math.Round(decimal.Divide(openedFile.data[cells[i].RowIndex].data.Length, lengthOfcells) * lengthOfReplacementBytes)).ToArray();

                    openedFile.data[cells[i].RowIndex].length = openedFile.data[cells[i].RowIndex].data.Length;

                    cells[i].Value = $"<{openedFile.data[cells[i].RowIndex].data.Length}> bytes long";
                    replacementBytes = replacementBytes.Skip(openedFile.data[cells[i].RowIndex].data.Length).ToArray();
                }
            }
            #endregion
            #region Non-proportional cell replacer (for mixed cell types)
            else
            {
                if (replacementBytes.Length != lengthOfcells)
                {
                    //DialogResult warningDR = MessageBox.Show("The file you have selected is not the same length as the chunk sections you wish to replace. Are you sure you want to continue?", "Warning", MessageBoxButtons.YesNo);
                    if (MessageBox.Show("The file you have selected is not the same length as the chunk sections you wish to replace. Are you sure you want to continue?", "Warning", MessageBoxButtons.YesNo) == DialogResult.No)
                    {
                        MessageBox.Show("Ok.", "Oh.");
                        return;
                    }
                    else
                    {
                        MessageBox.Show("STAP");
                        return;
                    }
                }

                for (int i = 0; i < cells.Length; i++)
                {
                    switch (cells[i].ColumnIndex)
                    {
                        case (0):
                            openedFile.data[cells[i].RowIndex].length = BitConverter.ToInt32(replacementBytes.Take(4).Reverse().ToArray(), 0);
                            cells[i].Value = openedFile.data[cells[i].RowIndex].length;
                            replacementBytes = replacementBytes.Skip(4).ToArray();
                            break;
                        case (1):
                            openedFile.data[cells[i].RowIndex].type = replacementBytes.Take(4).ToArray();
                            cells[i].Value = Encoding.ASCII.GetString(openedFile.data[cells[i].RowIndex].type);
                            replacementBytes = replacementBytes.Skip(4).ToArray();
                            break;
                        case (2):
                            openedFile.data[cells[i].RowIndex].data = replacementBytes.Take(openedFile.data[cells[i].RowIndex].data.Length).ToArray();
                            cells[i].Value = $"<{openedFile.data[cells[i].RowIndex].data.Length}> bytes long";
                            replacementBytes = replacementBytes.Skip(openedFile.data[cells[i].RowIndex].data.Length).ToArray();
                            break;
                        case (3):
                            openedFile.data[cells[i].RowIndex].crc = replacementBytes.Take(4).ToArray();
                            cells[i].Value = BitConverter.ToString(openedFile.data[cells[i].RowIndex].crc).Replace("-", ", ");
                            replacementBytes = replacementBytes.Skip(4).ToArray();
                            break;
                    }
                }                
            }
            #endregion

            #region CRC calculater
            if (autoCalculateCRC)
            {
                //TODO figure out which on of these is faster
                /*
                List<int> chunksToReCalculate = new List<int>();
                for (int i = 0; i < cells.Length; i++)
                    if (!chunksToReCalculate.Contains(cells[i].RowIndex))
                        chunksToReCalculate.Add(cells[i].RowIndex);
                */
                int[] chunksToReCalculate = selectedCells.Select(x => x.RowIndex).OrderBy(x => x).Distinct().ToArray();

                ReCalculateCRCOfChunk(chunksToReCalculate.ToArray());
            }
            #endregion
        }

        #region CRC Stuff

        /// <summary>
        /// Generates a table containing the crc of every byte value
        /// </summary>
        public static void GenerateCRCTable()
        {
            crc_table = new uint[256];

            for (uint n = 0; n < 256; n++)
            {
                uint c = n;
                for (int k = 0; k < 8; k++)
                {
                    if ((c & 1) == 1)
                        c = 0xedb88320 ^ (c >> 1);
                    else
                        c >>= 1;
                }
                crc_table[n] = c;
            }
        }

        /// <summary>
        /// Returns the crc of any byte array passed as input.
        /// </summary>
        /// <param name="input">The bytes to get the crc of.</param>
        /// <returns>The 4 byte crc of the given byte array.</returns>
        public static byte[] CalculateCRCOf(byte[] input)
        {
            uint c = 0xffffffff;

            if (crc_table == null)
                GenerateCRCTable();
            for (int i = 0; i < input.Length; i++)
            {
                c = crc_table[(c ^ input[i]) & 0xff] ^ (c >> 8);
            }
            c ^= 0xffffffff;

            return BitConverter.GetBytes(c).Reverse().ToArray();
        }

        /// <summary>
        /// Activates when the custom button "Re-Calculate CRC" is clicked
        /// </summary>
        /// <param name="selectedCells"> The selected cells of the DataGridView</param>
        /*
        public void ReCalculateCRCContextMenuStripButton_Click(ref DataGridViewCell[] cells)
        {
            //var selectedCells = dgv.SelectedCells;
            if (cells.Length > 0)
            {
                List<int> chunksToReCalculate = new List<int>();
                for(int i = 0; i < cells.Length; i++)
                    if (!chunksToReCalculate.Contains(cells[i].RowIndex))
                        chunksToReCalculate.Add(cells[i].RowIndex);

                for (int i = 0; i < chunksToReCalculate.Count; i++)
                {
                    byte[] input = openedFile.data[chunksToReCalculate[i]].type.Concat(openedFile.data[chunksToReCalculate[i]].data).ToArray();
                    byte[] crc = CalculateCRCOf(input);

                    MessageBox.Show($"Chunk {chunksToReCalculate[i] + 1}'s CRC is {Enumerable.SequenceEqual(crc, openedFile.data[chunksToReCalculate[i]].crc)}.".Replace("True", "OK").Replace("False", "BAD"));
                    /*
                    if (Enumerable.SequenceEqual(crc, openedFile.data[chunksToReCalculate[i]].crc))
                        MessageBox.Show($"Chunk {chunksToReCalculate[i] + 1}'s CRC is OK.");
                    else
                        MessageBox.Show($"Chunk {chunksToReCalculate[i] + 1}'s CRC is BAD.");
                    * /
                    openedFile.data[chunksToReCalculate[i]].crc = crc;
                    //dgv["crc",chunksToReCalculate[i]].Value = BitConverter.ToString(crc).Replace("-", ", ");

                }
            }
            
        }
        */

        public void ReCalculateCRCContextMenuStripButton_Click()
        {
            //var selectedselectedCells = dgv.SelectedselectedCells;
            if (selectedCells.Length > 0)
            {
                /*
                List<int> chunksToReCalculate = new List<int>();
                for (int i = 0; i < selectedCells.Length; i++)
                    if (!chunksToReCalculate.Contains(selectedCells[i].RowIndex))
                        chunksToReCalculate.Add(selectedCells[i].RowIndex);
                */
                
                int[] chunksToReCalculate = selectedCells.Select(x => x.RowIndex).OrderBy(x => x).Distinct().ToArray();


                ReCalculateCRCOfChunk(chunksToReCalculate.ToArray());
                /*
                for (int i = 0; i < chunksToReCalculate.Count; i++)
                {
                    byte[] input = openedFile.data[chunksToReCalculate[i]].type.Concat(openedFile.data[chunksToReCalculate[i]].data).ToArray();
                    byte[] crc = CalculateCRCOf(input);

                    MessageBox.Show($"Chunk {chunksToReCalculate[i] + 1}'s CRC is {Enumerable.SequenceEqual(crc, openedFile.data[chunksToReCalculate[i]].crc)}.".Replace("True", "OK").Replace("False", "BAD"));
                    /*
                    if (Enumerable.SequenceEqual(crc, openedFile.data[chunksToReCalculate[i]].crc))
                        MessageBox.Show($"Chunk {chunksToReCalculate[i] + 1}'s CRC is OK.");
                    else
                        MessageBox.Show($"Chunk {chunksToReCalculate[i] + 1}'s CRC is BAD.");
                    * /
                    openedFile.data[chunksToReCalculate[i]].crc = crc;
                    //dgv["crc",chunksToReCalculate[i]].Value = BitConverter.ToString(crc).Replace("-", ", ");

                }
                */
            }
        }

        private void ReCalculateCRCOfChunk(int chunk)
        {
            var newCRC = CalculateCRCOf(openedFile.data[chunk].type.Concat(openedFile.data[chunk].data).ToArray());
            if(verboseCRCChecks)
                MessageBox.Show($"Chunk {chunk + 1}'s CRC is {Enumerable.SequenceEqual(newCRC, openedFile.data[chunk].crc)}.".Replace("True", "OK").Replace("False", "BAD"));
            openedFile.data[chunk].crc = newCRC;
        }
        private void ReCalculateCRCOfChunk(int[] chunks)
        {
            for(int i = 0; i < chunks.Length; i++)
            {
                ReCalculateCRCOfChunk(chunks[i]);
                /*
                var newCRC = CalculateCRCOf(openedFile.data[chunks[i]].type.Concat(openedFile.data[chunks[i]].data).ToArray());
                if (verboseCRCChecks)
                    MessageBox.Show($"Chunk {chunks[i] + 1}'s CRC is {Enumerable.SequenceEqual(newCRC, openedFile.data[chunks[i]].crc)}.".Replace("True", "OK").Replace("False", "BAD"));
                openedFile.data[chunks[i]].crc = newCRC;
                */
            }
            
        }
        
        #endregion

        /// <summary>
        /// Fires whenever a cell is edited.
        /// </summary>
        /// <param name="editedCell">The cell that has just been edited.</param>
        public void CellEndEdit(DataGridViewCell editedCell)
        {
            //If the edited cell's value is not null, set the chunk's data to be equal to it
            if (editedCell.Value != null && editedCell.Value.ToString() != null) //TODO might not need the ToString variant...
            {
                switch (editedCell.ColumnIndex)
                {
                    case (0):
                        //Set the chunk length
                        openedFile.data[editedCell.RowIndex].length = int.Parse(editedCell.Value.ToString());
                        break;
                    case (1):
                        //Set the chunk type
                        openedFile.data[editedCell.RowIndex].type = Encoding.ASCII.GetBytes(editedCell.Value.ToString());
                        //If autoCalculateCRC is on, recalculate the crc
                        if (autoCalculateCRC)
                        {
                            ReCalculateCRCOfChunk(editedCell.RowIndex);
                            //editedCell.DataGridView["crc", editedCell.RowIndex].Value = BitConverter.ToString(openedFile.data[editedCell.RowIndex].crc).Replace("-", ", ");
                        }
                        break;
                    case (2):
                        //Edit machine broke
                        MessageBox.Show("What?! How?! (Please contact /u/Brayconn.)");
                        break;
                    case (3):
                        switch (autoCalculateCRC)
                        {
                            //If autoCalculateCRC is on, recalculate the crc
                            case (true):
                                ReCalculateCRCOfChunk(editedCell.RowIndex);
                                break;
                            //If not, se the crcto be the one the user entered
                            case (false):
                                //HACK probably slow & has other flaws...
                                //Remove the seperator from the crc (might not be the best way of doing things...)
                                string hexValues = editedCell.Value.ToString().Replace(@", ", null);
                                //Set the crc using code from here: https://stackoverflow.com/questions/321370/how-can-i-convert-a-hex-string-to-a-byte-array
                                openedFile.data[editedCell.RowIndex].crc = Enumerable.Range(0, hexValues.Length)
                                        .Where(x => x % 2 == 0)
                                        .Select(x => Convert.ToByte(hexValues.Substring(x, 2), 16))
                                        .ToArray();
                                break;
                        }
                        //Set the edited cell's value to be the new crc
                        //editedCell.Value = BitConverter.ToString(openedFile.data[editedCell.RowIndex].crc).Replace("-", ", ");
                        break;

                }
            }
            #region old code or something
            /*If not, set the cell's value equal to their respective part from the chunk list
            else
            {
                switch (editedCell.ColumnIndex)
                {
                    case (0):
                        editedCell.Value = openedFile.data[editedCell.RowIndex].length;
                        break;
                    case (1):
                        editedCell.Value = Encoding.ASCII.GetString(openedFile.data[editedCell.RowIndex].type);
                        break;
                    case (2):
                        editedCell.Value = $"<{openedFile.data[editedCell.RowIndex].data.Length}> bytes long";
                        break;
                    case (3):
                        editedCell.Value = BitConverter.ToString(openedFile.data[editedCell.RowIndex].crc).Replace("-", ", ");
                        break;
                }
            }
            */
            /*
            //If the edited cell's row index is less than the amount of chunks, then we're editing an existing chunk
            if (editedCell.RowIndex < openedFile.data.Count)
            {
                if (editedCell.Value != null)
                {
                    switch (editedCell.ColumnIndex)
                    {
                        case (0):
                            openedFile.data[editedCell.RowIndex].length = int.Parse(editedCell.Value.ToString());
                            break;
                        case (1):
                            openedFile.data[editedCell.RowIndex].type = Encoding.ASCII.GetBytes(editedCell.Value.ToString());
                            break;
                        case (2):
                            MessageBox.Show("What?! How?!");
                            break;
                        case (3):
                            switch (autoCalculateCRC)
                            {
                                case (true):
                                    /*
                                    byte[] input = openedFile.data[editedCell.RowIndex].type.Concat(openedFile.data[editedCell.RowIndex].data).ToArray();
                                    byte[] crc = CalculateCRCOf(input);
                                    MessageBox.Show($"Chunk {editedCell.RowIndex + 1}'s CRC is {Enumerable.SequenceEqual(crc, openedFile.data[editedCell.RowIndex].crc)}.".Replace("True.", "OK").Replace("False.", "BAD"));
                                    openedFile.data[editedCell.RowIndex].crc = crc;
                                    * /
                                    ReCalculateCRCOfChunk(editedCell.RowIndex);
                                    break;
                                case (false):
                                    string hexValues = editedCell.Value.ToString().Replace(@", ", null);
                                    openedFile.data[editedCell.RowIndex].crc = Enumerable.Range(0, hexValues.Length)
                                            .Where(x => x % 2 == 0)
                                            .Select(x => Convert.ToByte(hexValues.Substring(x, 2), 16))
                                            .ToArray();
                                    break;
                            }
                            editedCell.DataGridView["crc", editedCell.RowIndex].Value = BitConverter.ToString(openedFile.data[editedCell.RowIndex].crc).Replace("-", ", ");
                            break;

                    }
                    /*
                    if (autoCalculateCRC)
                        ReCalculateCRCOfChunk(editedCell.RowIndex);
                    * /
                    /*
                    if(editedCell.ColumnIndex >= 3 && autoCalculateCRC)
                    {
                        byte[] input = openedFile.data[editedCell.RowIndex].type.Concat(openedFile.data[editedCell.RowIndex].data).ToArray();
                        byte[] crc = CalculateCRCOf(input);

                        MessageBox.Show($"Chunk {editedCell.RowIndex + 1}'s CRC is {Enumerable.SequenceEqual(crc, openedFile.data[editedCell.RowIndex].crc)}.".Replace("true", "OK").Replace("false", "BAD"));
                        /*
                        if (Enumerable.SequenceEqual(crc, openedFile.data[chunksToReCalculate[i]].crc))
                            MessageBox.Show($"Chunk {chunksToReCalculate[i] + 1}'s CRC is OK.");
                        else
                            MessageBox.Show($"Chunk {chunksToReCalculate[i] + 1}'s CRC is BAD.");
                        * /
                        openedFile.data[editedCell.RowIndex].crc = crc;
                        editedCell.DataGridView["crc", editedCell.RowIndex].Value = BitConverter.ToString(crc).Replace("-", ", ");
                    }
                    * /
                }
                else
                {
                    switch (editedCell.ColumnIndex)
                    {
                        case (0):
                            editedCell.Value = openedFile.data[editedCell.RowIndex].length;
                            break;
                        case (1):
                            editedCell.Value = Encoding.ASCII.GetString(openedFile.data[editedCell.RowIndex].type);
                            break;
                        case (2):
                            editedCell.Value = $"<{openedFile.data[editedCell.RowIndex].data.Length}> bytes long";
                            break;
                        case (3):
                            editedCell.Value = BitConverter.ToString(openedFile.data[editedCell.RowIndex].crc).Replace("-", ", ");
                            break;
                    }
                }
            }
            else
            {
                chunk workingChunk = new chunk();

                switch (editedCell.ColumnIndex)
                {
                    case (0):
                        workingChunk.length = int.Parse(editedCell.Value.ToString());
                        break;
                    case (1):
                        workingChunk.type = Encoding.ASCII.GetBytes(editedCell.Value.ToString());
                        break;
                    case (2):
                        MessageBox.Show("What?! How?! (Please contact /u/Brayconn, 'cause you broke something)");
                        break;
                    case (3):
                        switch (autoCalculateCRC)
                        {
                            case (true):
                                /*
                                byte[] input = workingChunk.type.Concat(workingChunk.data).ToArray();
                                byte[] crc = CalculateCRCOf(input);
                                MessageBox.Show($"Chunk {editedCell.RowIndex + 1}'s CRC is {Enumerable.SequenceEqual(crc, workingChunk.crc)}.".Replace("True.", "OK").Replace("False.", "BAD"));
                                workingChunk.crc = crc;
                                * /
                                ReCalculateCRCOfChunk(editedCell.RowIndex);
                                break;
                            case (false):
                                string hexValues = editedCell.Value.ToString().Replace(@", ", null);
                                workingChunk.crc = Enumerable.Range(0, hexValues.Length)
                                        .Where(x => x % 2 == 0)
                                        .Select(x => Convert.ToByte(hexValues.Substring(x, 2), 16))
                                        .ToArray();
                                break;
                        }
                        editedCell.DataGridView["crc", editedCell.RowIndex].Value = BitConverter.ToString(workingChunk.crc).Replace("-", ", ");
                        break;

                }

                openedFile.data.Add(workingChunk);

                if (autoCalculateCRC)
                    ReCalculateCRCOfChunk(editedCell.RowIndex);
            }
            */
            #endregion
        }

        public void UserAddedRow(DataGridViewRowEventArgs e)
        {
            openedFile.data.Add(new chunk());
        }

        public void UserDeletingRow(DataGridViewRowCancelEventArgs e)
        {
            openedFile.data.RemoveAt(e.Row.Index);
        }

        public void MoveChunk(int rowIndexFromMouseDown, int rowIndexOfItemUnderMouseToDrop)
        {
            var chunktoMove = openedFile.data[rowIndexFromMouseDown];
            openedFile.data.RemoveAt(rowIndexFromMouseDown);
            openedFile.data.Insert(rowIndexOfItemUnderMouseToDrop, chunktoMove);
        }

        public void CellFormating(DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < openedFile.data.Count)
            {
                switch (e.ColumnIndex)
                {
                    case (0):
                        e.Value = openedFile.data[e.RowIndex].length;
                        if (openedFile.data[e.RowIndex].length != openedFile.data[e.RowIndex].data.Length)
                            e.CellStyle.BackColor = Color.Red;
                        else
                            e.CellStyle.BackColor = Color.White;
                        break;
                    case (1):
                        e.Value = Encoding.ASCII.GetString(openedFile.data[e.RowIndex].type);
                        switch (openedFile.fileType)
                        {
                            case (FileType.png):
                                if (!pngChunkTypes.Any(x => x.SequenceEqual(openedFile.data[e.RowIndex].type)))
                                    e.CellStyle.BackColor = Color.Red;
                                else
                                    e.CellStyle.BackColor = Color.White;
                                break;
                            case (FileType.mng):
                                if (!mngChunkTypes.Any(x => x.SequenceEqual(openedFile.data[e.RowIndex].type)))
                                    e.CellStyle.BackColor = Color.Red;
                                else
                                    e.CellStyle.BackColor = Color.White;
                                break;
                            case (FileType.jng):
                                if (!jngChunkTypes.Any(x => x.SequenceEqual(openedFile.data[e.RowIndex].type)))
                                    e.CellStyle.BackColor = Color.Red;
                                else
                                    e.CellStyle.BackColor = Color.White;
                                break;
                        }
                        break;
                    case (2):
                        e.Value = $"<{openedFile.data[e.RowIndex].data.Length}> bytes long";
                        if (openedFile.data[e.RowIndex].length != openedFile.data[e.RowIndex].data.Length)
                            e.CellStyle.BackColor = Color.Red;
                        else
                            e.CellStyle.BackColor = Color.White;
                        break;
                    case (3):

                        var crc = CalculateCRCOf(openedFile.data[e.RowIndex].type.Concat(openedFile.data[e.RowIndex].data).ToArray());

                        /* Fruitless attempt at refactoring
                        e.CellStyle.BackColor = Color.White;

                        if ((!Enumerable.SequenceEqual(crc, openedFile.data[e.RowIndex].crc)) && autoCalculateCRC)
                            openedFile.data[e.RowIndex].crc = crc;
                        else
                            e.CellStyle.BackColor = Color.Red;
                        */

                        /* Another fruitless attempt at refactoring
                        if ((!Enumerable.SequenceEqual(crc, openedFile.data[e.RowIndex].crc)) && autoCalculateCRC)
                        {
                            openedFile.data[e.RowIndex].crc = crc;
                            e.Value = BitConverter.ToString(crc).Replace("-", ", ");
                        }

                        e.CellStyle.BackColor = (!Enumerable.SequenceEqual(crc, openedFile.data[e.RowIndex].crc)) ? Color.Red : Color.White;
                        */

                        //TODO attempt to refactor this
                        if (!Enumerable.SequenceEqual(crc, openedFile.data[e.RowIndex].crc))
                        {
                            if (autoCalculateCRC)
                            {
                                openedFile.data[e.RowIndex].crc = crc;
                                e.Value = BitConverter.ToString(crc).Replace("-", ", ");
                                e.CellStyle.BackColor = Color.White;
                            }
                            else
                                e.CellStyle.BackColor = Color.Red;
                        }
                        else
                            e.CellStyle.BackColor = Color.White;
                        
                        e.Value = BitConverter.ToString(openedFile.data[e.RowIndex].crc).Replace("-", ", ");

                        break;
                }
            }
        }

        public TreeNode GetHotfileInfo(DataGridViewCell cell)
        {
            string nodeName = "Chunk " + (cell.RowIndex + 1);
            switch (cell.ColumnIndex)
            {
                case (0):
                    nodeName += " Length";
                    break;
                case (1):
                    nodeName += " Type";
                    break;
                case (2):
                    nodeName += " Data";
                    break;
                case (3):
                    nodeName += " CRC";
                    break;
            }
            return new TreeNode(nodeName);
        }
        public TreeNode[] GetHotfileInfo(DataGridViewCell[] cells)
        {
            TreeNode[] nodes = new TreeNode[cells.Length];
            for (int i = 0; i < cells.Length; i++)
                nodes[i] = GetHotfileInfo(cells[i]);
            return nodes;
        }
    }
}
