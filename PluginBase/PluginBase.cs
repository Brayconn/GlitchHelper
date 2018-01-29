using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PluginBase
{  
    public class Reference
    {
        /// <summary>
        /// The text that gets displayed
        /// </summary>
        public string text { get; set; }

        /// <summary>
        /// The file where the data can be located
        /// </summary>
        public string file { get; set; }

        /// <summary>
        /// The offset of the data from the start of the file
        /// </summary>
        public long offset { get; set; }
        
        /// <summary>
        /// The length of the data
        /// </summary>
        public int length { get; set; }
                
        public enum LengthType
        {
            invariable,
            variable
        }
        /// <summary>
        /// Whether the length of the refrence can change
        /// </summary>
        public LengthType lengthType { get; }

        /// <summary>
        /// Whether or not the reference can be DIRECTLY edited (if not, it must be exported/replaced)
        /// </summary>
        public bool editable { get; }
        
        public bool exportable { get; }

        public enum ReferenceValidity
        {
            valid,
            unknown,
            invalid
        }
        /// <summary>
        /// Whether or not the reference is valid
        /// </summary>
        public ReferenceValidity validity { get; set; }      

        public Reference(string text, string file, long offset, int length, LengthType lengthType, bool editable, bool exportable = true, ReferenceValidity validity = ReferenceValidity.unknown)
        {
            this.text = text;
            this.file = file;
            this.offset = offset;
            this.length = length;
            this.lengthType = lengthType;
            this.editable = editable;
            this.exportable = exportable;
            this.validity = validity;
        }
    }

    unsafe public class Option
    {
        /// <summary>
        /// The name of the Option
        /// </summary>
        public string name { get; }

        public Type type { get; }
        /// <summary>
        /// The value to change
        /// </summary>
        public void* value { get; } //UNSURE this works fine but... pointers in C# man... I get the idea that's not right.

        private Option(string name)
        {
            this.name = name;
        }
        public Option(string name, void* value, Type type) : this(name)
        {
            this.value = value;
            this.type = type;
        }
        public Option(string name, bool* value) : this(name,value,typeof(bool)) { }
        public Option(string name, int* value) : this(name, value, typeof(int)) { }


    }

    public class DisplayInformationEventArgs : EventArgs
    {
        public string text { get; }
        public string header { get; }

        public DisplayInformationEventArgs(string text, string header)
        {
            this.text = text;
            this.header = header;
        }
    }

    public class AskToContinueEventArgs : DisplayInformationEventArgs
    {
        public bool result { get; set; }

        public AskToContinueEventArgs(string text, string header, bool result = false) : base(text, header)
        {
            this.result = result;
        }
    }

    public interface IGHPlugin
    {
        event EventHandler<AskToContinueEventArgs> AskToContinue;
        event EventHandler<DisplayInformationEventArgs> DisplayInformation;

        /// <summary>
        /// Filter meant for OpenFileDialog.Filter
        /// </summary>
        string ofdFilter { get; }
        /// <summary>
        /// Filter meant for SaveFileDialog.Filter
        /// </summary>
        string sfdFilter { get; }

        /// <summary>
        /// The default reference[] to be added if the user decides to add new row
        /// </summary>
        Reference[] defaultReference { get; }

        /// <summary>
        /// The names of each column
        /// </summary>
        string[] columnNames { get; }

        /// <summary>
        /// Array containing any custom options a plugin may have
        /// </summary>
        Option[] customOptions { get; }

        /// <summary>
        /// Checks if the given FileStream is valid for opening
        /// </summary>
        /// <param name="stream">The stream of the file to check</param>
        /// <returns>Whether or not the file is valid</returns>
        bool IsFileValid(FileStream stream);

        /// <summary>
        /// Loads the given bytes into a useable/sorted format
        /// </summary>
        /// <param name="selectedBytes">The bytes of the file to load</param>
        /// <returns>Filter meant for telling the program what file the user is allowed to save.</returns>
        Tuple<Reference,List<Reference[]>> Load(FileStream stream);

        //HACK don't like the fact I'm now having to include the default file everywhere, all because plugins need it if they need to replace any data during validation time...
        /// <summary>
        /// Updates the "validity" property of the provided references
        /// </summary>
        /// <param name="references">The references to update</param>
        /// <param name="defaultFile">The opened file</param>
        void UpdateReferenceValidity(Reference[] references, string defaultFile);

        //Reference[] GetHeaderChunks(Reference[] references);

        /// <summary>
        /// Convert an object into a byte[] useable for replacement
        /// </summary>
        /// <param name="column">The column the input was entered in</param>
        /// <param name="input">The object itself</param>
        /// <returns>Formatted byte[]</returns>
        byte[] FormatReplacementInput(int column, object input);

        /// <summary>
        /// Get the name of the references provided. (Meant for Hotfile displays)
        /// </summary>
        /// <param name="input">The references to get the names of</param>
        /// <returns>The names of the provided references</returns>
        string[] GetHotfileInfo(Tuple<int, int>[] input);
        /// <summary>
        /// Get the name of the reference provided. (Meant for Hotfile displays)
        /// </summary>
        /// <param name="input">The reference to get the name of</param>
        /// <returns>The name of the provided reference</returns>
        string GetHotfileInfo(Tuple<int, int> input);
    }
}
