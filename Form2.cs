using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesignManagerAddins
{
    public partial class Form2 : Form
    {
        List<string> unitTypes = new List<string>();
        List<double> unitConstants = new List<double>();
        static public double units;
        public Form2()
        {
            InitializeComponent();
            unitTypes.Add("Centimeters");
            unitTypes.Add("Millimeters");
            unitTypes.Add("Meters");
            //set Up the unit combobox
            foreach (string unitType in unitTypes)
            {
                comboBox.Items.Add(unitType);
            }

            unitConstants.Add(30.48);
            unitConstants.Add(304.8);
            unitConstants.Add(0.3048);
            comboBox1.DataSource = unitTypes;





        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void ok_btn_fm2_Click(object sender, EventArgs e)
        {
            foreach (string unitType in unitTypes)
            {
                if (unitType == comboBox.SelectedItem.ToString())
                {
                    ok_btn_fm2.DialogResult = DialogResult.OK;
                    Close();
                    units = unitConstants[comboBox.SelectedIndex];
                }
            }
            return;
        }

        private void comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
