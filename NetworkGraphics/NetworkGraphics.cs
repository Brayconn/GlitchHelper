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
        public event EventHandler<AskToContinueEventArgs> AskToContinue = new EventHandler<AskToContinueEventArgs>((o,e) => { });
        public event EventHandler<DisplayInformationEventArgs> DisplayInformation = new EventHandler<DisplayInformationEventArgs>((o, e) => { });

        public string sfdFilter { get; set; }

        public string ofdFilter
        {
            get
            {
                return "Network Graphics Files (*.png;*.apng;*.mng;*.jng)|*.png;*.apng;*.mng;*.jng";
            }
        }

        public Reference[] defaultReference
        {
            get
            {
                string path = Path.GetTempFileName();
                File.WriteAllBytes(path, new byte[] { 0, 0, 0, 0 });
                Reference length = new Reference("0", path, 0, 4, Reference.LengthType.invariable, true);

                path = Path.GetTempFileName();
                File.WriteAllText(path, "IEND");
                Reference type = new Reference("IEND", path, 0, 4, Reference.LengthType.invariable, true);

                path = Path.GetTempFileName();
                Reference data = new Reference("<0> bytes long", path, 0, 0, Reference.LengthType.variable, false);

                path = Path.GetTempFileName();
                File.WriteAllBytes(path, new byte[] { 0xAE, 0x42, 0x60, 0x82 }); //{174, 66, 96, 130}
                Reference crc = new Reference("AE, 42, 60, 82", path, 0, 4, Reference.LengthType.invariable, true);

                return new Reference[]
                {
                    length,
                    type,
                    data,
                    crc
                };
            }
        }

        public string[] columnNames
        {
            get
            {
                return new string[]
                {
                    "Length",
                    "Type",
                    "Data",
                    "CRC"
                };
            }
        }

        public Option[] customOptions
        {
            get
            {
                unsafe
                {
                    fixed (bool* a = &autoCalculateCRC)
                    fixed(bool* v = &verboseCRCChecks)
                    {
                        return new Option[]
                            {
                                    new Option("Auto-Fix CRC", a),
                                    new Option("Verbose CRC Checks",a)
                            };
                    }
                }
            }
        }

        #region Default Headers
        private static readonly Dictionary<byte[], string> saveableFileTypes = new Dictionary<byte[], string>()
        {
            { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "Portable Network Graphics (*.png)|*.png"},
            { new byte[] { 0x8A, 0x4D, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "Multiple Network Graphics (*.mng)|*.mng" },
            { new byte[] { 0x8B, 0x4A, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "JPEG Network Graphics (*.jng)|*.jng"}
        };

        private static readonly byte[] pngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly byte[] mngHeader = { 0x8A, 0x4D, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly byte[] jngHeader = { 0x8B, 0x4A, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        #endregion

        public unsafe bool verboseCRCChecks = false;
        public unsafe bool autoCalculateCRC = false;
        private enum FileType
        {
            png,
            mng,
            jng,
            unknown
        }
        private FileType fileType { get; set; } = FileType.unknown;

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

        public bool IsFileValid(FileStream stream)
        {
            using (BinaryReader br = new BinaryReader(stream, Encoding.Default, true))
            {
                var returnFilter = saveableFileTypes.Where(x => x.Key.SequenceEqual(br.ReadBytes(8))).ToArray(); //UNSURE I'd like to use .Single, but then I'd have to use exceptions, so...

                if (returnFilter.Length == 1)
                {
                    sfdFilter = returnFilter[0].Value;

                    if (returnFilter[0].Key.SequenceEqual(pngHeader))
                        fileType = FileType.png;
                    else if (returnFilter[0].Key.SequenceEqual(jngHeader))
                        fileType = FileType.jng;
                    else if (returnFilter[0].Key.SequenceEqual(mngHeader))
                        fileType = FileType.mng;
                    else
                        fileType = FileType.unknown;

                    return true;
                }
                else
                {
                    sfdFilter = saveableFileTypes.Where( x => x.Key.SequenceEqual(pngHeader)).Single().Value; //HACK this is messy
                    fileType = FileType.unknown;

                    AskToContinueEventArgs eventArgs = new AskToContinueEventArgs("The selected file's header does not match with any Network Graphics header (png, mng, or jng).\nWould you like to open the file anyways?", "File Validity Checker");
                    AskToContinue(this, eventArgs);
                    return eventArgs.result;
                }
            }
        }

        private bool IsBufferChunk(byte[] buffer)
        {
            switch (fileType)
            {
                case (FileType.unknown):
                case (FileType.png):
                    return pngChunkTypes.Any(x => x.SequenceEqual(buffer));
                case (FileType.mng):
                    return mngChunkTypes.Any(x => x.SequenceEqual(buffer));
                case (FileType.jng):
                    return jngChunkTypes.Any(x => x.SequenceEqual(buffer));
                default:
                    return false;
            }
        }

        public Tuple<Reference,List<Reference[]>> Load(FileStream stream)
        {
            Reference header;
            List<Reference[]> output = new List<Reference[]>();
            using (BinaryReader br = new BinaryReader(stream))
            {

                header = new Reference(
                    "",
                    stream.Name,
                    0,
                    8,
                    Reference.LengthType.invariable,
                    true
                    );

                stream.Seek(8, SeekOrigin.Begin);
                //var output = new List<reference[]>();
                while (stream.Position < stream.Length)
                {
                    //TODO redo opening to not actually care about what the dataL is, and just go until the next recognized chunk type
                    long lengthO = stream.Position;
                    int lengthI = BitConverter.ToInt32(br.ReadBytes(4).Reverse().ToArray(), 0);

                    long typeO = stream.Position; //TODO implement some sort of checking system here
                    string typeS = new string(br.ReadChars(4));

                    long dataO = stream.Position;
                    int dataL = 0;

                    /*
                    if (lengthI < 0)
                    {
                        stream.Seek(12, SeekOrigin.Current);
                        byte[] buffer = br.ReadBytes(4);
                        while(!IsBufferChunk(buffer))
                        {
                            buffer[0] = buffer[1];
                            buffer[1] = buffer[2];
                            buffer[2] = buffer[3];
                            buffer[3] = br.ReadByte();
                            dataL++;
                        }
                        stream.Seek(-12, SeekOrigin.Current);
                    }
                    else
                    {
                        dataL = lengthI;             
                        stream.Seek(lengthI, SeekOrigin.Current);
                    }
                    */
                    if (lengthI != 0)
                    {
                        stream.Seek(8, SeekOrigin.Current);
                        byte[] buffer = br.ReadBytes(4); //TODO investigate using something other than an array?
                        while (!IsBufferChunk(buffer.ToArray()) && br.BaseStream.Position < br.BaseStream.Length)
                        {
                            buffer[0] = buffer[1];
                            buffer[1] = buffer[2];
                            buffer[2] = buffer[3];
                            buffer[3] = br.ReadByte();
                            dataL++;
                        }
                        stream.Seek(-12, SeekOrigin.Current);
                    }

                    long CRCO = stream.Position;
                    byte[] CRCD = br.ReadBytes(4);

                    Reference[] referencesToAdd = new Reference[]
                    {
                        new Reference(lengthI.ToString(),stream.Name,lengthO,4,Reference.LengthType.invariable,true),
                        new Reference(typeS,stream.Name,typeO,4,Reference.LengthType.invariable,true),
                        new Reference($"<{dataL}> bytes long" ,stream.Name,dataO,dataL,Reference.LengthType.variable,false),
                        new Reference(BitConverter.ToString(CRCD).Replace(@"-",@", "),stream.Name,CRCO,4,Reference.LengthType.invariable,false)
                    };
                    //UpdateReferenceValidity(referencesToAdd);
                    output.Add(referencesToAdd);
                }
            }
                for(int i = 0; i < output.Count; i++)
                    UpdateReferenceValidity(output[i],stream.Name);
                return new Tuple<Reference, List<Reference[]>>(header, output);
            //}
        }

        public void UpdateReferenceValidity(Reference[] references, string defaultFile)
        {
            #region CRC Validation

            if (references[1].validity == Reference.ReferenceValidity.unknown ||
                references[2].validity == Reference.ReferenceValidity.unknown ||
                references[3].validity == Reference.ReferenceValidity.unknown ||
                ((autoCalculateCRC) ? references[3].validity == Reference.ReferenceValidity.invalid : false)
                )
            {
                using (FileStream fst = new FileStream(references[1].file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (BinaryReader brt = new BinaryReader(fst))
                using (FileStream fsd = new FileStream(references[2].file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (BinaryReader brd = new BinaryReader(fsd))
                using (FileStream fsc = new FileStream(references[3].file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (BinaryReader brc = new BinaryReader(fsc))
                {
                    /* Non insane translation of that line below (RIP)
                     * 
                     * fsc.Seek(references[3].offset, SeekOrigin.Begin);
                     * byte[] oldcrc = brc.ReadBytes(4);
                     *
                     * fst.Seek(references[1].offset, SeekOrigin.Begin);
                     * fsd.Seek(references[2].offset, SeekOrigin.Begin);
                     * byte[] input = brt.ReadBytes(references[1].length).Concat(brd.ReadBytes(references[2].length)).ToArray();
                     * byte[] newcrc = CalculateCRCOf(input);
                     * 
                     * if(autoCalculateCRC)
                     *     
                     * 
                     * if(oldcrc.SequenceEqual(newcrc))
                     *      references[3].validity = reference.ReferenceValidity.valid;
                     * else
                     *      references[3].validity = reference.ReferenceValidity.invalid;
                     * 
                     * 
                     * references[3].validity = brc.ReadBytes(4).SequenceEqual(CalculateCRCOf(brt.ReadBytes(references[1].length).Concat(brd.ReadBytes(references[2].length)).ToArray())) ? reference.ReferenceValidity.valid : reference.ReferenceValidity.invalid;
                     */

                    fst.Seek(references[1].offset, SeekOrigin.Begin);
                    fsd.Seek(references[2].offset, SeekOrigin.Begin);
                    fsc.Seek(references[3].offset, SeekOrigin.Begin);

                    byte[] oldcrc = brc.ReadBytes(4);
                    byte[] newcrc = CalculateCRCOf(brt.ReadBytes(references[1].length).Concat(brd.ReadBytes(references[2].length)).ToArray());

                    //TODO refactor this
                    if (!oldcrc.SequenceEqual(newcrc))
                    {
                        if (autoCalculateCRC)
                        {
                            if (references[3].file == defaultFile)
                            {
                                references[3].file = Path.GetTempFileName();
                                references[3].offset = 0;
                            }

                            using (BinaryWriter bw = new BinaryWriter(new FileStream(references[3].file, FileMode.Open, FileAccess.Write)))
                            {
                                fsc.Seek(references[3].offset, SeekOrigin.Begin);
                                bw.Write(newcrc);
                            }
                            references[3].text = BitConverter.ToString(newcrc).Replace("-", ", ");
                            references[3].validity = Reference.ReferenceValidity.valid;
                        }
                        else
                            references[3].validity = Reference.ReferenceValidity.invalid;
                    }
                    else
                        references[3].validity = Reference.ReferenceValidity.valid;
                }
            }

            #endregion
            
            #region Length/Data Validation

            if (references[0].validity == Reference.ReferenceValidity.unknown || references[2].validity == Reference.ReferenceValidity.unknown)
            {
                using (FileStream fsr = new FileStream(references[0].file, FileMode.Open, FileAccess.Read))
                using (BinaryReader br = new BinaryReader(fsr))
                {
                    fsr.Seek(references[0].offset, SeekOrigin.Begin);
                    int length = BitConverter.ToInt32(br.ReadBytes(references[0].length).Reverse().ToArray(), 0);

                    references[0].text = length.ToString();
                    references[2].text = $"<{references[2].length}> bytes long";
                                        
                    if (length != references[2].length)
                    {
                        references[0].validity = Reference.ReferenceValidity.invalid;
                        references[2].validity = Reference.ReferenceValidity.invalid;
                    }
                    else
                    {
                        references[0].validity = Reference.ReferenceValidity.valid;
                        references[2].validity = Reference.ReferenceValidity.valid;
                    }
                }
            }

            #endregion

            #region Type Validation

            if (references[1].validity == Reference.ReferenceValidity.unknown)
            {
                using (FileStream fsr = new FileStream(references[1].file, FileMode.Open, FileAccess.Read))
                using (BinaryReader br = new BinaryReader(fsr))
                {
                    fsr.Seek(references[1].offset, SeekOrigin.Begin);
                    byte[] type = br.ReadBytes(references[1].length);
                    references[1].text = Encoding.ASCII.GetString(type);

                    Reference.ReferenceValidity rv = Reference.ReferenceValidity.invalid;
                    switch(fileType)
                    {
                        case (FileType.png):
                            if (pngChunkTypes.Any(x => x.SequenceEqual(type)))
                                rv = Reference.ReferenceValidity.valid;
                            break;
                        case (FileType.mng):
                            if (mngChunkTypes.Any(x => x.SequenceEqual(type)))
                                rv = Reference.ReferenceValidity.valid;
                            break;
                        case (FileType.jng):
                            if (jngChunkTypes.Any(x => x.SequenceEqual(type)))
                                rv = Reference.ReferenceValidity.valid;
                            break;
                    }
                    references[1].validity =  rv;
                }
            }

            #endregion
        }

        #region CRC Stuff

        //TODO have the crc table be declared inline to save on re-calculating every time the user starts the program (might be a terrible idea)
        /// <summary>
        /// Table containing the crc of all possible byte values
        /// </summary>
        private static uint[] crc_table = null;
        
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

        #endregion

        public byte[] FormatReplacementInput(int column, object input)
        {
            switch (column)
            {
                case (0):
                    int output;
                    return (int.TryParse(input.ToString(), out output)) ? BitConverter.GetBytes(output).Reverse().ToArray() : null;
                case (1):
                    return (input.ToString().Length == 4) ? Encoding.ASCII.GetBytes(input.ToString()) : null;
                case (2):
                    return null; //You can't edit the data directly
                case (3):
                    string inputS = input.ToString().Replace(@", ", null);
                    byte[] crcOutput = Enumerable.Range(0, inputS.Length)
                                    .Where(x => x % 2 == 0)
                                    .Select(x => Convert.ToByte(inputS.Substring(x, 2), 16))
                                    .ToArray();

                    return (crcOutput.Length == 4) ? crcOutput : null;
                default:
                    return null;
            }
        }


        public string GetHotfileInfo(Tuple<int, int> reference)
        {
            string name = "Chunk " + (reference.Item1 + 1);
            switch (reference.Item2)
            {
                case (0):
                    name += " Length";
                    break;
                case (1):
                    name += " Type";
                    break;
                case (2):
                    name += " Data";
                    break;
                case (3):
                    name += "CRC";
                    break;
            }
            return name;
        }
        public string[] GetHotfileInfo(Tuple<int, int>[] references)
        {
            string[] names = new string[references.Length];
            for (int i = 0; i < references.Length; i++)
                names[i] = GetHotfileInfo(references[i]);
            return names;
        }
    }
}
