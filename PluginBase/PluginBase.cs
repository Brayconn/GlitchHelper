using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PluginBase
{
    public interface IPlugin
    {
        /// <summary>
        /// Filter meant for OpenFileDialog.Filter
        /// </summary>
        string filter { get; }

        DataGridViewCell[] selectedCells { get; set; }
        DataGridViewColumn[] columns { get; }


        ToolStripItem[] customMenuStripOptions { get; }
        ToolStripItem[] customContextMenuStripButtons { get; }

        /// <summary>
        /// Array containing all custom ToolStripItems the plugin wants to add to the DataGridView's Context Menu Strip
        /// </summary>
        //ToolStripItem[] contextMenuStripItems { get; }

        /// <summary>
        /// Array containing all custom ToolStripItems the plugin wants to add to the DataGridView's Menu Strip
        /// </summary>
        //ToolStripItem[] menuStripItems { get; }

        /// <summary>
        /// Loads the given bytes into a useable/sorted format
        /// </summary>
        /// <param name="selectedBytes">The bytes of the file to load</param>
        /// <returns>Filter meant for telling the program what file the user is allowed to save.</returns>
        string Load(byte[] selectedBytes);

        /// <summary>
        /// Displays the loaded file's contents onto a given DataGridView
        /// </summary>
        /// <param name="dgv">The DataGridView to display to.</param>
        //void PopulateRows(DataGridViewColumnCollection columns, DataGridViewRowCollection rows);
        void PopulateRows(DataGridViewRowCollection rows);
        DataGridViewRow[] GetRows();

        void UpdateRows(DataGridViewRow row);
        void UpdateRows(DataGridViewRow[] rows);

        //Add/remove any custom menustrip items
        //ToolStripItem[] getCustomContextMenuStripItems(DataGridViewCell[] cells);
        //ToolStripItem[] getCustomContextMenuStripItems();
        //ToolStripItemCollection removeCustomContextMenuStripItems(ToolStripItemCollection ic);

        //byte[] ExportSelected(DataGridViewCellCollection selectedCells);

        /// <summary>
        /// Exports the entire file.
        /// </summary>
        /// <returns>The entire file.</returns>
        byte[] ExportAll();

        //-----------------------------------------------------------\\

        /// <summary>
        /// Return the bytes corresponding to the given DataGridViewCells as a List of byte[]
        /// </summary>
        /// <param name="cells">The cells to get the data of</param>
        /// <returns>The data of the given cells</returns>
        //List<byte[]> ExportAsList(DataGridViewCell[] cells = null);
        List<byte[]> ExportSelectedAsList(DataGridViewCell[] cells = null);

        /// <summary>
        /// Return the bytes corresponding to the given DataGridViewCells as a byte[]
        /// </summary>
        /// <param name="cells">The cells to get the data of</param>
        /// <returns>The data of the given cells</returns>
        //byte[] ExportAsArray(DataGridViewCell[] cells = null);
        byte[] ExportSelectedAsArray(DataGridViewCell[] cells = null);

        /// <summary>
        /// Replace the data of the given cells with the given data
        /// </summary>
        /// <param name="selectedCells">The cells whose data will be replaced</param>
        /// <param name="replacementBytes">The data to be used as a replacement</param>
        //void ReplaceSelected(DataGridViewCell[] selectedCells, byte[] replacementBytes);
        void ReplaceSelectedWith(byte[] replacementBytes, DataGridViewCell[] cells = null);

        /// <summary>
        /// Updates the given cells data
        /// </summary>
        /// <param name="editedCell">The cell whose data needs updating</param>
        void CellEndEdit(DataGridViewCell editedCell);

        /// <summary>
        /// Moves a given chunk to another place
        /// </summary>
        /// <param name="rowIndexFromMouseDown">Row of chunk to move</param>
        /// <param name="rowIndexOfItemUnderMouseToDrop">Row of chunk to displace</param>
        void Move(int rowIndexFromMouseDown, int rowIndexOfItemUnderMouseToDrop);

        /// <summary>
        /// Formats the given cell
        /// </summary>
        /// <param name="e">The cell to format</param>
        void CellFormating(DataGridViewCellFormattingEventArgs e);

        //Was a good idea, since it forced plugin makers to have a function that added a new chunk/row/whatever, but it really just didn't pan out.
        //void RowsAdded(DataGridViewRowsAddedEventArgs e);

        /// <summary>
        /// ...Pretty self explanitory... Displays the given hotfiles into the given TreeView.
        /// </summary>
        /// <param name="tv">The TreeView to populate.</param>
        /// <param name="hotfiles">The hotfiles to add.</param>
        void DisplayHotfilesInManager(TreeView tv, Dictionary<string, DataGridViewCell[]> hotfiles);
    }
}
