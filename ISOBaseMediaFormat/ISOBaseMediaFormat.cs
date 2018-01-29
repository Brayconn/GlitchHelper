using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PluginBase;
using System.Drawing;
using System.Data;
using System.Text.RegularExpressions;
using System.IO;

//TODO reorganize this?
namespace ISOBaseMediaFormat
{
    public class ISOBaseMediaFormat : IGHPlugin
    {
        public event EventHandler<AskToContinueEventArgs> AskToContinue = new EventHandler<AskToContinueEventArgs>((o, e) => { });
        public event EventHandler<DisplayInformationEventArgs> DisplayInformation = new EventHandler<DisplayInformationEventArgs>((o, e) => { });

        public string[] columnNames
        {
            get
            {
                return new string[]
                {
                    "Length",
                    "Type",
                    "Data",
                };
            }
        }

        public Reference[] defaultReference
        {
            get
            {
                string path = Path.GetTempFileName();
                File.WriteAllBytes(path, BitConverter.GetBytes(8).Reverse().ToArray());
                Reference length =  new Reference("8", path, 0, 4, Reference.LengthType.invariable, true);

                path = Path.GetTempFileName();
                File.WriteAllText(path, "free");
                Reference type = new Reference("free", path, 0, 4, Reference.LengthType.invariable, true);

                path = Path.GetTempFileName();
                Reference data = new Reference("<0> bytes long", path, 0, 4, Reference.LengthType.variable, false);

                return new Reference[]
                {
                    length,
                    type,
                    data
                };                
            }
        }

        public Option[] customOptions
        {
            get
            {
                return new Option[0];
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

        #region Header/Save Filters

        //TODO make this better... somehow...
        #region Dictionary version

        private static readonly Dictionary<string, Tuple<string, string>> ftyps = new Dictionary<string, Tuple<string, string>>()
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

        public string ofdFilter
        {
            get
            {
                string filetypes = string.Join(";", ftyps.Values.Select(x => x.Item2).Where(x => x != null).Distinct().ToArray());
                return $"ISO base media file format ({filetypes})|{filetypes}";
            }
        }
        public string sfdFilter { get; set; }
        
        public bool IsFileValid(FileStream stream)
        {
            using (BinaryReader br = new BinaryReader(stream,Encoding.Default,true))
            {
                stream.Seek(4, SeekOrigin.Begin);
                string fileType = new string(br.ReadChars(4));
                if (fileType != "moov")
                    fileType = new string(br.ReadChars(4));

                var ftyp = ftyps.Where(x => x.Key.ToLower() == fileType).Select(x => x.Value).ToArray();

                if (ftyp.Length == 1)
                {
                    string extention = ftyp[0].Item2 ?? "*.mp4";
                    sfdFilter = $"{ftyp[0].Item1} ({extention})|{extention}";
                    return true;
                }
                else
                {
                    AskToContinueEventArgs eventArgs = new AskToContinueEventArgs("The selected file's header does not match with any recognized ISO Base Media Format or Apple Quicktime headers.", "File Validity Checker");
                    AskToContinue(this, eventArgs);
                    return eventArgs.result;
                }
            }
        }

        public void UpdateReferenceValidity(Reference[] references, string defaultFile)
        {
            if(references[0].validity == Reference.ReferenceValidity.unknown || references[2].validity == Reference.ReferenceValidity.unknown)
            {
                /* TODO new code doesn't trigger Microsoft's Managed Recommended rules, but I'm not really sure what advantage that gives...
                using (FileStream fsr = new FileStream(references[0].file, FileMode.Open, FileAccess.Read))
                using (BinaryReader br = new BinaryReader(fsr))
                {
                    fsr.Seek(references[0].offset,SeekOrigin.Begin);
                    if ( BitConverter.ToInt32(br.ReadBytes(references[0].length).Reverse().ToArray(),0) - 8 != references[2].length)
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
                */

                using (BinaryReader br = new BinaryReader(new FileStream(references[0].file, FileMode.Open, FileAccess.Read)))
                {
                    br.BaseStream.Seek(references[0].offset, SeekOrigin.Begin);
                    if (BitConverter.ToInt32(br.ReadBytes(references[0].length).Reverse().ToArray(), 0) - 8 != references[2].length)
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
            if(references[1].validity == Reference.ReferenceValidity.unknown)
            {
                /*
                using (FileStream fsr = new FileStream(references[1].file, FileMode.Open, FileAccess.Read))
                using (BinaryReader br = new BinaryReader(fsr))
                {
                    fsr.Seek(references[1].offset,SeekOrigin.Begin);
                    byte[] type = br.ReadBytes(references[1].length);
                    if (validChunkTypes.Any(x => x.SequenceEqual(type)))
                        references[1].validity = Reference.ReferenceValidity.valid;
                    else
                        references[1].validity = Reference.ReferenceValidity.invalid;
                }
                */

                using (BinaryReader br = new BinaryReader(new FileStream(references[1].file, FileMode.Open, FileAccess.Read)))
                {
                    br.BaseStream.Seek(references[1].offset, SeekOrigin.Begin);
                    byte[] type = br.ReadBytes(references[1].length);
                    /*
                    if (validChunkTypes.Any(x => x.SequenceEqual(type)))
                        references[1].validity = Reference.ReferenceValidity.valid;
                    else
                        references[1].validity = Reference.ReferenceValidity.invalid;
                        */
                    references[1].validity = (validChunkTypes.Any(x => x.SequenceEqual(type))) ? Reference.ReferenceValidity.valid : Reference.ReferenceValidity.invalid;
                }

            }
        }

        public Tuple<Reference,List<Reference[]>> Load(FileStream stream)
        {
            using (BinaryReader br = new BinaryReader(stream))
            {
                stream.Seek(0, SeekOrigin.Begin);
                var output = new List<Reference[]>();
                long streamPosition = 0;
                while (streamPosition < stream.Length)
                {
                    long lengthO = stream.Position;
                    int dataL = BitConverter.ToInt32(br.ReadBytes(4).Reverse().ToArray(), 0) - 8;
                    long typeO = stream.Position; //TODO implement some sort of checking system here
                    string typeS = new string(br.ReadChars(4));
                    long dataO = stream.Position;
                    streamPosition = stream.Seek(dataL, SeekOrigin.Current);

                    Reference[] referencesToAdd = new Reference[]
                    {
                        new Reference((dataL + 8).ToString(),stream.Name,lengthO,4,Reference.LengthType.invariable,true),
                        new Reference(typeS,stream.Name,typeO,4,Reference.LengthType.invariable,true),
                        new Reference($"<{dataL}> bytes long",stream.Name,dataO,dataL,Reference.LengthType.variable,false),
                    };
                    UpdateReferenceValidity(referencesToAdd,stream.Name);
                    output.Add(referencesToAdd);
                }
                return new Tuple<Reference, List<Reference[]>>(null,output);
            };
        }

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
            }
            return null;
        }

        public string GetHotfileInfo(Tuple<int,int> reference)
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
            }
            return name;
        }
        public string[] GetHotfileInfo(Tuple<int,int>[] references)
        {
            string[] names = new string[references.Length];
            for (int i = 0; i < references.Length; i++)
                names[i] = GetHotfileInfo(references[i]);
            return names;
        }       
    }
}
