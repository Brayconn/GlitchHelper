using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PluginBase;
using System.Windows.Forms;
using System.Drawing;
using System.Data;
using System.Text.RegularExpressions;

//TODO rename/reorganize this
namespace ISOBaseMediaFormat
{
    public class ISOBaseMediaFormat : IGHPlugin
    {
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
                };
            }
        }

        public ToolStripItem[] customContextMenuStripButtons
        {
            get
            {
                return new ToolStripItem[0];
            }
        }

        public ToolStripItem[] customMenuStripOptions
        {
            get
            {
                return new ToolStripItem[0];
            }
        }

        private byte[][] validChunkTypes
        {
            get
            {
                return new byte[][]
                {
                    Encoding.ASCII.GetBytes("ftyp"),
                    Encoding.ASCII.GetBytes("mdat"),
                    Encoding.ASCII.GetBytes("moov"),
                    Encoding.ASCII.GetBytes("pnot"),
                    Encoding.ASCII.GetBytes("udta"),
                    Encoding.ASCII.GetBytes("uuid"),
                    Encoding.ASCII.GetBytes("moof"),
                    Encoding.ASCII.GetBytes("free"),
                    Encoding.ASCII.GetBytes("skip"),
                    Encoding.ASCII.GetBytes("jP2 "),
                    Encoding.ASCII.GetBytes("wide"),
                    Encoding.ASCII.GetBytes("load"),
                    Encoding.ASCII.GetBytes("ctab"),
                    Encoding.ASCII.GetBytes("imap"),
                    Encoding.ASCII.GetBytes("matt"),
                    Encoding.ASCII.GetBytes("kmat"),
                    Encoding.ASCII.GetBytes("clip"),
                    Encoding.ASCII.GetBytes("crgn"),
                    Encoding.ASCII.GetBytes("sync"),
                    Encoding.ASCII.GetBytes("chap"),
                    Encoding.ASCII.GetBytes("tmcd"),
                    Encoding.ASCII.GetBytes("scpt"),
                    Encoding.ASCII.GetBytes("ssrc"),
                    Encoding.ASCII.GetBytes("PICT")
                };
            }
        }

        private class chunk
        {
            /// <summary>
            /// The length of the entire chunk
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


            public chunk()
            {
                length = 0;
                //type = Encoding.ASCII.GetBytes("IEND");
                type = new byte[] { 0x49, 0x45, 0x4e, 0x44 };
                data = new byte[0];
            }
            public chunk(int length, byte[] type, byte[] data)
            {
                this.length = length;
                this.type = type;
                this.data = data;
            }
        }

        private List<chunk> openedData { get; set; }

        #region Header/Save Filters

        //TODO make this better... somehow...
        #region Dictionary version

        private static Dictionary<string, Tuple<string, string>> ftyps = new Dictionary<string, Tuple<string, string>>()
        {
            { "3g2a", new Tuple<string,string>("3GPP2 Media compliant with 3GPP2 C.S0050-0 V1.0", "*.3g2") },
			{ "3g2b", new Tuple<string,string>("3GPP2 Media compliant with 3GPP2 C.S0050-A V1.0.0", "*.3g2") },
			{ "3g2c", new Tuple<string,string>("3GPP2 Media compliant with 3GPP2 C.S0050-B v1.0", "*.3g2") },
			{ "3ge6", new Tuple<string,string>("3GPP Release 6 MBMS Extended Presentations", "*.3gp") },
			{ "3ge7", new Tuple<string,string>("3GPP Release 7 MBMS Extended Presentations", "*.3gp") },
			{ "3ge9", new Tuple<string,string>("3GPP Release 9 MBMS Extended Presentations", "*.3gp") },
			{ "3gg6", new Tuple<string,string>("3GPP Release 6 General Profile", "*.3gp") },
			{ "3gg9", new Tuple<string,string>("3GPP Release 9 General Profile", "*.3gp") },
			{ "3gh9", new Tuple<string,string>("3GPP Release 9 Adaptive Streaming Profile", "*.3gp") },
			{ "3gm9", new Tuple<string,string>("3GPP Release 9 Media Segment Profile", "*.3gp") },
			{ "3gp1", new Tuple<string,string>("3GPP Media Release 1 (probably non-existent)", "*.3gp") },
			{ "3gp2", new Tuple<string,string>("3GPP Media Release 2 (probably non-existent)", "*.3gp") },
			{ "3gp3", new Tuple<string,string>("3GPP Media Release 3 (probably non-existent)", "*.3gp") },
			{ "3gp4", new Tuple<string,string>("3GPP Media Release 4", "*.3gp") },
			{ "3gp5", new Tuple<string,string>("3GPP Media Release 5", "*.3gp") },
			{ "3gp6", new Tuple<string,string>("3GPP Media Release 6 Basic Profile", "*.3gp") },
			{ "3gp7", new Tuple<string,string>("3GPP Media Release 7", "*.3gp") },
			{ "3gp8", new Tuple<string,string>("3GPP Media Release 8", "*.3gp") },
			{ "3gp9", new Tuple<string,string>("3GPP Media Release 9 Basic Profile", "*.3gp") },
			{ "3gr6", new Tuple<string,string>("3GPP Media Release 6 Progressive Download Profile", "*.3gp") },
			{ "3gr9", new Tuple<string,string>("3GPP Media Release 9 Progressive Download Profile", "*.3gp") },
			{ "3gs6", new Tuple<string,string>("3GPP Media Release 6 Streaming Servers", "*.3gp") },
			{ "3gs7", new Tuple<string,string>("3GPP Media Release 7 Streaming Servers", "*.3gp") },
			{ "3gs9", new Tuple<string,string>("3GPP Media Release 9 Streaming Servers", "*.3gp") },
			{ "3gt9", new Tuple<string,string>("3GPP Media Release 9 Media Stream Recording Profile", "*.3gp") },
			{ "ARRI", new Tuple<string,string>("ARRI Digital Camera", null ) },
			{ "avc1", new Tuple<string,string>("MP4 Base w/ AVC ext [ISO 14496-12:2005]", "*.mp4") },
			{ "bbxm", new Tuple<string,string>("Blinkbox Master File: H.264 video and 16-bit little-endian LPCM audio", null ) },
			{ "CAEP", new Tuple<string,string>("Canon Digital Camera", null ) },
			{ "caqv", new Tuple<string,string>("Casio Digital Camera", null ) },
			{ "CDes", new Tuple<string,string>("Convergent Design", null ) },
			{ "da0a", new Tuple<string,string>("DMB MAF w/ MPEG Layer II aud, MOT slides, DLS, JPG/PNG/MNG images", null ) },
			{ "da0b", new Tuple<string,string>("DMB MAF, extending DA0A, with 3GPP timed text, DID, TVA, REL, IPMP", null ) },
			{ "da1a", new Tuple<string,string>("DMB MAF audio with ER-BSAC audio, JPG/PNG/MNG images", null ) },
			{ "da1b", new Tuple<string,string>("DMB MAF, extending da1a, with 3GPP timed text, DID, TVA, REL, IPMP", null ) },
			{ "da2a", new Tuple<string,string>("DMB MAF aud w/ HE-AAC v2 aud, MOT slides, DLS, JPG/PNG/MNG images", null ) },
			{ "da2b", new Tuple<string,string>("DMB MAF, extending da2a, with 3GPP timed text, DID, TVA, REL, IPMP", null ) },
			{ "da3a", new Tuple<string,string>("DMB MAF aud with HE-AAC aud, JPG/PNG/MNG images", null ) },
			{ "da3b", new Tuple<string,string>("DMB MAF, extending da3a w/ BIFS, 3GPP timed text, DID, TVA, REL, IPMP", null ) },
			{ "dash", new Tuple<string,string>("ISO base media file format file specifically designed for DASH including movie fragments and Segment Index.", null ) },
			{ "dby1", new Tuple<string,string>("MP4 files with Dolby content", "*.mp4") },
			{ "dmb1", new Tuple<string,string>("DMB MAF supporting all the components defined in the specification", null ) },
			{ "dmpf", new Tuple<string,string>("Digital Media Project", null ) },
			{ "drc1", new Tuple<string,string>("Dirac (wavelet compression), encapsulated in ISO base media (MP4)", null ) },
			{ "dsms", new Tuple<string,string>("Media Segment conforming to the DASH Self-Initializing Media Segment format type", null ) },
			{ "dv1a", new Tuple<string,string>("DMB MAF vid w/ AVC vid, ER-BSAC aud, BIFS, JPG/PNG/MNG images, TS", null ) },
			{ "dv1b", new Tuple<string,string>("DMB MAF, extending dv1a, with 3GPP timed text, DID, TVA, REL, IPMP", null ) },
			{ "dv2a", new Tuple<string,string>("DMB MAF vid w/ AVC vid, HE-AAC v2 aud, BIFS, JPG/PNG/MNG images, TS", null ) },
			{ "dv2b", new Tuple<string,string>("DMB MAF, extending dv2a, with 3GPP timed text, DID, TVA, REL, IPMP", null ) },
			{ "dv3a", new Tuple<string,string>("DMB MAF vid w/ AVC vid, HE-AAC aud, BIFS, JPG/PNG/MNG images, TS", null ) },
			{ "dv3b", new Tuple<string,string>("DMB MAF, extending dv3a, with 3GPP timed text, DID, TVA, REL, IPMP", null ) },
			{ "dvr1", new Tuple<string,string>("DVB over RTP", "*.dvb") },
			{ "dvt1", new Tuple<string,string>("DVB over MPEG-2 Transport Stream", "*.dvb") },
			{ "dxo", new Tuple<string,string>("DxO ONE camera", null ) },
			{ "emsg", new Tuple<string,string>("Event message box present", null ) },
			{ "F4V", new Tuple<string,string>("Video for Adobe Flash Player 9+", "*.f4v") },
			{ "F4P", new Tuple<string,string>("Protected Video for Adobe Flash Player 9+", "*.f4p") },
			{ "F4A", new Tuple<string,string>("Audio for Adobe Flash Player 9+", "*.f4a") },
			{ "F4B", new Tuple<string,string>("Audio Book for Adobe Flash Player 9+", "*.f4b") },
			{ "ifrm", new Tuple<string,string>("Apple iFrame Specification, Version 8.1 (Jan 2013)", null ) },
			{ "isc2", new Tuple<string,string>("ISMACryp 2.0 Encrypted File", null ) },
			{ "iso2", new Tuple<string,string>("MP4 Base Media v2 [ISO 14496-12:2005]", "*.mp4") },
			{ "iso3", new Tuple<string,string>("Version of the ISO file format", "*.mp4") },
			{ "iso4", new Tuple<string,string>("Version of the ISO file format", "*.mp4") },
			{ "iso5", new Tuple<string,string>("Version of the ISO file format", "*.mp4") },
			{ "iso6", new Tuple<string,string>("Version of the ISO file format", "*.mp4") },
			{ "isom", new Tuple<string,string>("MP4 Base Media v1 [IS0 14496-12:2003]", "*.mp4") },
			{ "J2P0", new Tuple<string,string>("JPEG2000 Profile 0", "*.jp2") },
			{ "J2P1", new Tuple<string,string>("JPEG2000 Profile 1", "*.jp2") },
			{ "JP2", new Tuple<string,string>("JPEG 2000 Image [ISO 15444-1 ?]", "*.jp2") },
			{ "JP20", new Tuple<string,string>("Unknown, from GPAC samples (prob non-existent)", null ) },
			{ "jpm", new Tuple<string,string>("JPEG 2000 Compound Image [ISO 15444-6]", "*.jpm") },
			{ "jpsi", new Tuple<string,string>("JPSearch data interchange format", null ) },
			{ "jpx", new Tuple<string,string>("JPEG 2000 w/ extensions [ISO 15444-2]", "*.jpx") },
			{ "jpxb", new Tuple<string,string>("JPEG Part 2", null ) },
			{ "KDDI", new Tuple<string,string>("3GPP2 EZmovie for KDDI 3G cellphones", "*.3g2;*.3gp") },
			{ "LCAG", new Tuple<string,string>("Leica digital camera", null ) },
			{ "lmsg", new Tuple<string,string>("last Media Segment indicator for ISO base media file format", null ) },
			{ "M4A ", new Tuple<string,string>("Apple iTunes AAC-LC Audio", "*.m4a") },
			{ "M4B ", new Tuple<string,string>("Apple iTunes AAC-LC Audio Book", "*.m4b") },
			{ "M4P ", new Tuple<string,string>("Apple iTunes AAC-LC AES Protected Audio", "*.m4p") },
			{ "M4V ", new Tuple<string,string>("Apple iTunes Video Video", "*.m4v") },
			{ "M4VH", new Tuple<string,string>("Apple TV", "*.m4v") },
			{ "M4VP", new Tuple<string,string>("Apple iPhone", "*.m4v") },
			{ "MFSM", new Tuple<string,string>("Media File for Samsung video Metadata", null ) },
			{ "MGSV", new Tuple<string,string>("Home and Mobile Multimedia Platform (HMMP)", null ) },
			{ "mj2s", new Tuple<string,string>("Motion JPEG 2000 [ISO 15444-3] Simple Profile", null ) },
			{ "mjp2", new Tuple<string,string>("Motion JPEG 2000 [ISO 15444-3] General Profile", null ) },
			{ "mmp4", new Tuple<string,string>("MPEG-4/3GPP Mobile Profile (for NTT)", "*.mp4;*.3gp") },
            { "moov", new Tuple<string,string>("Apple QuickTime (Legacy)", "*.mov;*.qt") },
            { "mp21", new Tuple<string,string>("MPEG-21 [ISO/IEC 21000-9]", "*.mp4") },
			{ "mp41", new Tuple<string,string>("MP4 v1 [ISO 14496-1:ch13]", "*.mp4") },
			{ "mp42", new Tuple<string,string>("MP4 v2 [ISO 14496-14]", "*.mp4") },
			{ "mp71", new Tuple<string,string>("MP4 w/ MPEG-7 Metadata [per ISO 14496-12]", "*.mp4") },
			{ "MPPI", new Tuple<string,string>("Photo Player, MAF [ISO/IEC 23000-3]", null ) },
			{ "mpuf", new Tuple<string,string>("Compliance with the MMT Processing Unit format", null ) },
			{ "mqt", new Tuple<string,string>("Sony / Mobile QuickTime (.MQV)  US Patent 7,477,830 (Sony Corp)", "*.mqv") },
			{ "msdh", new Tuple<string,string>("Media Segment conforming to the general format type for ISO base media file format.", null ) },
			{ "msix", new Tuple<string,string>("Media Segment conforming to the Indexed Media Segment format type for ISO base media file format.", null ) },
			{ "MSNV", new Tuple<string,string>("MPEG-4 for SonyPSP", "*.mp4") },
			{ "NDAS", new Tuple<string,string>("MP4 v2 [ISO 14496-14] Nero Digital AAC Audio", "*.mp4") },
			{ "NDSC", new Tuple<string,string>("MPEG-4 Nero Cinema Profile", "*.mp4") },
			{ "NDSH", new Tuple<string,string>("MPEG-4 Nero HDTV Profile", "*.mp4") },
			{ "NDSM", new Tuple<string,string>("MPEG-4 Nero Mobile Profile", "*.mp4") },
			{ "NDSP", new Tuple<string,string>("MPEG-4 Nero Portable Profile", "*.mp4") },
			{ "NDSS", new Tuple<string,string>("MPEG-4 Nero Standard Profile", "*.mp4") },
			{ "NDXC", new Tuple<string,string>("H.264/MPEG-4 AVC Nero Cinema Profile", "*.mp4") },
			{ "NDXH", new Tuple<string,string>("H.264/MPEG-4 AVC Nero HDTV Profile", "*.mp4") },
			{ "NDXM", new Tuple<string,string>("H.264/MPEG-4 AVC Nero Mobile Profile", "*.mp4") },
			{ "NDXP", new Tuple<string,string>("H.264/MPEG-4 AVC Nero Portable Profile", "*.mp4") },
			{ "NDXS", new Tuple<string,string>("H.264/MPEG-4 AVC Nero Standard Profile", "*.mp4") },
			{ "niko", new Tuple<string,string>("Nikon Digital Camera", null ) },
			{ "odcf ", new Tuple<string,string>("OMA DCF DRM Format 2.0 (OMA-TS-DRM-DCF-V2_0-20060303-A)", null ) },
			{ "opf2 ", new Tuple<string,string>("OMA PDCF DRM Format 2.1 (OMA-TS-DRM-DCF-V2_1-20070724-C)", null ) },
			{ "opx2 ", new Tuple<string,string>("OMA PDCF DRM + XBS extensions (OMA-TS-DRM_XBS-V1_0-20070529-C)", null ) },
			{ "pana", new Tuple<string,string>("Panasonic Digital Camera", null ) },
			{ "piff", new Tuple<string,string>("Protected Interoperable File Format", "*.isma;*.ismv") },
			{ "pnvi", new Tuple<string,string>("Panasonic Video Intercom", null ) },
			{ "qt ", new Tuple<string,string>("Apple QuickTime", "*.mov;*.qt") },
			{ "risx", new Tuple<string,string>("Representation Index Segment used to index MPEG-2 TS based Media Segments", null ) },
			{ "ROSS", new Tuple<string,string>("Ross Video", null ) },
			{ "sdv", new Tuple<string,string>("SD Memory Card Video", null ) },
			{ "SEAU", new Tuple<string,string>("Home and Mobile Multimedia Platform (HMMP)", null ) },
			{ "SEBK", new Tuple<string,string>("Home and Mobile Multimedia Platform (HMMP)", null ) },
			{ "senv", new Tuple<string,string>("Video contents Sony Entertainment Network provides by using MP4 file format", "*.mp4") },
			{ "sims", new Tuple<string,string>("conforming to the Sub-Indexed Media Segment format type", null ) },
			{ "sisx", new Tuple<string,string>("Single Index Segment used to index MPEG-2 TS based Media Segments", null ) },
			{ "ssc1", new Tuple<string,string>("Samsung stereoscopic, single stream (patent pending, see notes)", null ) },
			{ "ssc2", new Tuple<string,string>("Samsung stereoscopic, dual stream (patent pending, see notes)", null ) },
			{ "ssss", new Tuple<string,string>("Subsegment Index Segment used to index MPEG-2 TS based Media Segments", null ) },
			{ "uvvu", new Tuple<string,string>("UltraViolet file brand – conforming to the DECE Common File Format spec, Annex E.", null ) },
			{ "XAVC", new Tuple<string,string>("XAVC File Format", null ) },
        };

        #endregion

        #region Regex version

        #region 3GP
        private static string _3GPRegex = @"3g((e|r|s)(6|7)|g6|p[1-7])|kddi";
        private static List<byte[]> _3GPHeaders = new List<byte[]>()
        {
            //3ge(6,7)
            new byte[] { 0x33, 0x67, 0x65, 0x36 },
            new byte[] { 0x33, 0x67, 0x65, 0x37 },

            //3gg6
            new byte[] { 0x33, 0x67, 0x67, 0x36 },

            //3gp(1-7)
            new byte[] { 0x33, 0x67, 0x70, 0x31 },
            new byte[] { 0x33, 0x67, 0x70, 0x32 },
            new byte[] { 0x33, 0x67, 0x70, 0x33 },
            new byte[] { 0x33, 0x67, 0x70, 0x34 },
            new byte[] { 0x33, 0x67, 0x70, 0x35 },
            new byte[] { 0x33, 0x67, 0x70, 0x36 },
            new byte[] { 0x33, 0x67, 0x70, 0x37 },

            //3gr(6,7)
            new byte[] { 0x33, 0x67, 0x72, 0x36 },
            new byte[] { 0x33, 0x67, 0x72, 0x37 },

            //3gs(6,7)
            new byte[] { 0x33, 0x67, 0x73, 0x36 },
            new byte[] { 0x33, 0x67, 0x73, 0x37 },

            //kddi
            new byte[] { 0x6b, 0x64, 0x64, 0x69 }
        };
        private static string _3GPSaveFilter = "3GPP (*.3gp;)|*.3gp;";
        #endregion

        #region 3G2
        private static string _3G2Regex = @"3g2[a-c]";
        private static List<byte[]> _3G2Headers = new List<byte[]>()
        {
            //3g2(a-c)
            new byte[] { 0x33, 0x67, 0x32, 0x61 },
            new byte[] { 0x33, 0x67, 0x32, 0x62 },
            new byte[] { 0x33, 0x67, 0x32, 0x63 },
        };
        private static string _3G2SaveFilter = "3GPP2 (*.3g2;)|*.3g2;";
        #endregion

        #region QuickTime File Format
        private static string QTRegex = "qt  ";
        private static string QTSaveFilter = "QuickTime File Format (*.mov;*.qt;)|*.mov;*.qt;";
        #endregion

        #region MPEG-4 Part 14
        private static string MP4Regex = @"avc1|iso(2|m)|(m((mp4)|(p(4(1|2)|(71)))|(snv)))|nd((as)|((s|x)(c|h|m|p|s)))";
        private static string MP4SaveFilter = "MPEG-4 Part 14 (*.mp4;*.m4a;*.m4p;*.m4b;*.m4r;*.m4v;)|*.mp4;*.m4a;*.m4p;*.m4b;*.m4r;*.m4v;";

        #endregion

        #endregion

        #endregion

        private static Dictionary<string,string> saveableFileTypes
            {
                get
                {
                Dictionary<string, string> output = new Dictionary<string, string>
                {
                    { _3GPRegex, _3GPSaveFilter },
                    { _3G2Regex, _3G2SaveFilter },
                    { QTRegex, QTSaveFilter },
                    { MP4Regex, MP4SaveFilter }
                };
                return output;
                }
            }

        public string filter
        {
            get
            {
                return "ISO base media file format (*.3gp;*.3gpp;*.3g2;*.mj2;*.mjp2;*.mov;*.mp4;*.m4a;*.m4p;*.m4b;*.m4r;*.m4v)|*.3gp;*.3gpp;*.3g2;*.mj2;*.mjp2;*.mov;*.mp4;*.m4a;*.m4p;*.m4b;*.m4r;*.m4v";
            }
        }

        public DataGridViewCell[] selectedCells { get; set; }

        public DataGridViewRow[] GetRows()
        {
            List<DataGridViewRow> rows = new List<DataGridViewRow>();
            for (int i = 0; i < openedData.Count; i++)
            {
                DataGridViewRow row = new DataGridViewRow();
                row.Cells.AddRange(new DataGridViewCell[] {
                    new DataGridViewTextBoxCell
                    {
                        Value = openedData[i].length
                    },
                    new DataGridViewTextBoxCell
                    {
                        Value = Encoding.ASCII.GetString(openedData[i].type)
                    },
                    new DataGridViewTextBoxCell
                    {
                        Value = $"<{openedData[i].data.Length}> bytes long"
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


        public string Load(byte[] selectedBytes)
        {
            string fileType = Encoding.ASCII.GetString(selectedBytes.Skip(4).Take(4).ToArray()).ToLower();
            if(fileType != "moov")
                fileType = Encoding.ASCII.GetString(selectedBytes.Skip(8).Take(4).ToArray()).ToLower();

            /*
            string[] returnFilter = saveableFileTypes.Where(x => Regex.IsMatch(fileType, x.Key,RegexOptions.IgnoreCase)).Select(x => x.Value).ToArray();

            if (returnFilter == null || returnFilter.Length != 1)
            {
                DialogResult warningDR = MessageBox.Show("The selected file's header does not match with any recognized \nWould you like to open the file anyways?", "Warning", MessageBoxButtons.YesNo);
                if (warningDR != DialogResult.Yes)
                    return null;
                else
                    returnFilter[0] = "";
            }
            */

            string returnFilter;

            var ftyp = ftyps.Where(x => x.Key.ToLower() == fileType).Select(x => x.Value).ToArray();

            if(ftyp.Length == 1)
            {
                string extention = ftyp[0].Item2 ?? "*.mp4"; 
                returnFilter = $"{ftyp[0].Item1} ({extention})|{extention}";
            }
            else
            {
                DialogResult warningDR = MessageBox.Show("The selected file's header does not match with any recognized \nWould you like to open the file anyways?", "Warning", MessageBoxButtons.YesNo);
                if (warningDR != DialogResult.Yes)
                    return null;
                else
                    returnFilter = "";
            }

            openedData = new List<chunk>();
            while (selectedBytes.Length > 0)
            {
                int length = BitConverter.ToInt32(selectedBytes.Take(4).Reverse().ToArray(), 0);
                var workingchunk = new chunk(
                    length,
                    selectedBytes.Skip(4).Take(4).ToArray(),
                    selectedBytes.Skip(8).Take(length - 8).ToArray()
                    );

                openedData.Add(workingchunk);
                selectedBytes = selectedBytes.Skip(length).ToArray();
            }
            return returnFilter;
        }

        #region Exporting stuff

        public byte[] ExportAll()
        {
            List<byte> output = new List<byte>();
            for (int i = 0; i < openedData.Count; i++)
            {
                output.AddRange(BitConverter.GetBytes(openedData[i].length).Reverse());
                output.AddRange(openedData[i].type);
                output.AddRange(openedData[i].data);
            }
            return output.ToArray();
        }

        public byte[] ExportSelectedAsArray(DataGridViewCell[] cells = null)
        {
            List<byte> output = new List<byte>();
            for (int i = 0; i < cells.Length; i++)
                output.AddRange(GetCellData(cells[i]));
            return output.ToArray();
        }

        public List<byte[]> ExportSelectedAsList(DataGridViewCell[] cells = null)
        {
            List<byte[]> output = new List<byte[]>();
            for (int i = 0; i < cells.Length; i++)
                output.Add(GetCellData(cells[i]));
            return output;
        }

        private byte[] GetCellData(DataGridViewCell cell)
        {
            switch (cell.ColumnIndex)
            {
                case (0):
                    return BitConverter.GetBytes(openedData[cell.RowIndex].length).ToArray();
                case (1):
                    return openedData[cell.RowIndex].type;
                case (2):
                    return openedData[cell.RowIndex].data;
                default:
                    return null;
            }
        }

        #endregion

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
            int badCells = cells.Where(x => x.RowIndex >= openedData.Count).Count();
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
                        cells = cells.Where(x => x.RowIndex < openedData.Count).ToArray();
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
                        lengthOfcells += BitConverter.GetBytes(openedData[cells[i].RowIndex].length).Length;
                        break;
                    case (1):
                        lengthOfcells += openedData[cells[i].RowIndex].type.Length;
                        break;
                    case (2):
                        lengthOfcells += openedData[cells[i].RowIndex].data.Length;
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
                    
                    decimal percent = decimal.Divide(data[cells[i].RowIndex].data.Length, lengthOfcells);
                                        
                    decimal amount = percent * lengthOfReplacementBytes;

                    //TODO maybe use AwayFromZero?
                    //TODO add support for when chunk length exceeds int max size
                    int amountToTake = (int)Math.Round(amount);

                    data[cells[i].RowIndex].data = replacementBytes.Take(amountToTake).ToArray();

                    */
                    openedData[cells[i].RowIndex].data = replacementBytes.Take((int)Math.Round(decimal.Divide(openedData[cells[i].RowIndex].data.Length, lengthOfcells) * lengthOfReplacementBytes)).ToArray();

                    openedData[cells[i].RowIndex].length = openedData[cells[i].RowIndex].data.Length + 8; //TODO

                    cells[i].Value = $"<{openedData[cells[i].RowIndex].data.Length}> bytes long";
                    replacementBytes = replacementBytes.Skip(openedData[cells[i].RowIndex].data.Length).ToArray();
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
                            openedData[cells[i].RowIndex].length = BitConverter.ToInt32(replacementBytes.Take(4).Reverse().ToArray(), 0);
                            cells[i].Value = openedData[cells[i].RowIndex].length;
                            replacementBytes = replacementBytes.Skip(4).ToArray();
                            break;
                        case (1):
                            openedData[cells[i].RowIndex].type = replacementBytes.Take(4).ToArray();
                            cells[i].Value = Encoding.ASCII.GetString(openedData[cells[i].RowIndex].type);
                            replacementBytes = replacementBytes.Skip(4).ToArray();
                            break;
                        case (2):
                            openedData[cells[i].RowIndex].data = replacementBytes.Take(openedData[cells[i].RowIndex].data.Length).ToArray();
                            cells[i].Value = $"<{openedData[cells[i].RowIndex].data.Length}> bytes long";
                            replacementBytes = replacementBytes.Skip(openedData[cells[i].RowIndex].data.Length).ToArray();
                            break;
                    }
                }
            }
            #endregion
        }

        public void CellEndEdit(DataGridViewCell editedCell)
        {
            //If the edited cell's value is not null, set the chunk's data to be equal to it
            if (editedCell.Value != null && editedCell.Value.ToString() != null) //TODO might not need the ToString variant...
            {
                switch (editedCell.ColumnIndex)
                {
                    case (0):
                        //Set the chunk length
                        openedData[editedCell.RowIndex].length = int.Parse(editedCell.Value.ToString());
                        break;
                    case (1):
                        //Set the chunk type
                        openedData[editedCell.RowIndex].type = Encoding.ASCII.GetBytes(editedCell.Value.ToString());
                        break;
                    case (2):
                        //Edit machine broke
                        MessageBox.Show("What?! How?! (Please contact /u/Brayconn.)");
                        break;
                }
            }
        }


        public void CellFormating(DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < openedData.Count)
            {
                switch (e.ColumnIndex)
                {
                    case (0):
                        e.Value = openedData[e.RowIndex].length;
                        if (openedData[e.RowIndex].length != BitConverter.GetBytes(openedData[e.RowIndex].length).Length + openedData[e.RowIndex].type.Length + openedData[e.RowIndex].data.Length)
                            e.CellStyle.BackColor = Color.Red;
                        else
                            e.CellStyle.BackColor = Color.White;
                        break;
                    case (1):
                        e.Value = Encoding.ASCII.GetString(openedData[e.RowIndex].type);
                        if (!validChunkTypes.Any(x => x.SequenceEqual(openedData[e.RowIndex].type)))
                            e.CellStyle.BackColor = Color.Red;
                        else
                            e.CellStyle.BackColor = Color.White;
                        break;
                    case (2):
                        e.Value = $"<{openedData[e.RowIndex].data.Length}> bytes long";
                        if (openedData[e.RowIndex].length != BitConverter.GetBytes(openedData[e.RowIndex].length).Length + openedData[e.RowIndex].type.Length + openedData[e.RowIndex].data.Length)
                            e.CellStyle.BackColor = Color.Red;
                        else
                            e.CellStyle.BackColor = Color.White;
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


        public void MoveChunk(int rowIndexFromMouseDown, int rowIndexOfItemUnderMouseToDrop)
        {
            var chunktoMove = openedData[rowIndexFromMouseDown];
            openedData.RemoveAt(rowIndexFromMouseDown);
            openedData.Insert(rowIndexOfItemUnderMouseToDrop, chunktoMove);
        }

        

        public void UpdateRows(DataGridViewRow[] rows)
        {
            throw new NotImplementedException();
        }

        public void UpdateRows(DataGridViewRow row)
        {
            throw new NotImplementedException();
        }


        public void UserAddedRow(DataGridViewRowEventArgs e)
        {
            openedData.Add(new chunk());
        }

        public void UserDeletingRow(DataGridViewRowCancelEventArgs e)
        {
            openedData.RemoveAt(e.Row.Index);
        }
    }
}
