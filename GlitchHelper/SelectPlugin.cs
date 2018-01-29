using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GlitchHelper
{
    //HACK this is not a pretty looking form
    public partial class SelectPlugin : Form
    {
        public int SelectedIndex { get; private set; } = 0;
        private string[] options { get; }


        public SelectPlugin(string[] options)
        {
            this.options = options;
            InitializeComponent();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectedIndex = comboBox1.SelectedIndex;
        }

        private void SelectPlugin_Load(object sender, EventArgs e)
        {
            comboBox1.Items.AddRange(options);
            comboBox1.SelectedItem = comboBox1.Items[0];
            comboBox1.DropDownWidth = DropDownWidth(comboBox1);
        }

        //https://stackoverflow.com/questions/4842160/auto-width-of-comboboxs-content
        int DropDownWidth(ComboBox myCombo)
        {
            int maxWidth = 0, temp = 0;
            foreach (var obj in myCombo.Items)
            {
                temp = TextRenderer.MeasureText(obj.ToString(), myCombo.Font).Width;
                if (temp > maxWidth)
                {
                    maxWidth = temp;
                }
            }
            return maxWidth;
        }
    }
}
