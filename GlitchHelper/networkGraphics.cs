using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GlitchHelper
{

    public class chunk
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
    }

    public enum networkGraphicsTypes
    {
        png,
        mng,
        jng
    }

    public class networkGraphics
    { 
        /// <summary>
        /// Table containing the crc of all possible byte values
        /// </summary>
        private static uint[] crc_table = null;

        /// <summary>
        /// The standard headers for pngs, mngs, and jngs
        /// </summary>
        public static readonly byte[] pngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        public static readonly byte[] mngHeader = { 0x8A, 0x4D, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        public static readonly byte[] jngHeader = { 0x8B, 0x4A, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        public List<byte[]> open(byte[] input, networkGraphicsTypes headerType)
        {
            List<byte[]> output = new List<byte[]>() { input.Take(8).ToArray() };
            input = input.Skip(8).ToArray();

            while (input.Length > 0)
            {
                //Store the chunk's length as an int
                output.Add(input.Take(4).ToArray());
                int length = BitConverter.ToInt32(input.Take(4).Reverse().ToArray(),0);

                //Store the chunk type as a byte array
                output.Add(input.Skip(4).Take(4).ToArray());

                //Store the data of the chunk.
                output.Add(input.Skip(8).Take(length).ToArray());
                
                //Store the chunk crc as a byte array
                output.Add(input.Skip(8).Skip(length).Take(4).ToArray());
                
                //Subtract all previous chunk data from the input
                input = input.Skip(12).Skip(length).ToArray();
            }

            return output;
        }

        /// <summary>
        /// The actual header of the file
        /// </summary>
        public byte[] header;
        /// <summary>
        /// List of chunks in the file
        /// </summary>
        public BindingList<chunk> data = new BindingList<chunk>();
        /// <summary>
        /// Initializes a new png with the default header
        /// </summary>
        public networkGraphics()
        {
            //Hi!
        }
        /// <summary>
        /// Initializes a new png using the given byte array
        /// </summary>
        /// <param name="input">the byte array to sort into a png</param>
        public networkGraphics(byte[] input)
        {
            header = input.Take(8).ToArray();           
            input = input.Skip(8).ToArray(); 

            while(input.Length > 0)
            {
                //Make a new working chunk
                var workingchunk = new chunk();

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

                data.Add(workingchunk);
            }
        }

        /// <summary>
        /// Generates a table containing the crc of every byte value
        /// </summary>
        public static void generatecrctable()
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
        /// Returns the crc of any byte array passed as input
        /// </summary>
        /// <param name="input">The bytes to get the crc of</param>
        /// <returns>byte[4]</returns>
        public static byte[] calculatecrcof(byte[] input)
        {
            uint c = 0xffffffff;

            if (crc_table == null)
                generatecrctable();
            for (int n = 0; n < input.Length; n++)
            {
                c = crc_table[(c ^ input[n]) & 0xff] ^ (c >> 8);
            }
            c ^= 0xffffffff;

            return BitConverter.GetBytes(c).Reverse().ToArray();
        }
    }
}
