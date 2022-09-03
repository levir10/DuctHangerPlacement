using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using View = Autodesk.Revit.DB.View;

namespace DesignManagerAddins
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public partial class Form1 : System.Windows.Forms.Form//MAIN Form script
    {


        //define variables
        ExternalCommandData commandData;
        Document doc;
        Level pickedLevel;
        public static string chosenLevel;
        string selectedItemText;
        int selectedItemIndex;
        List<string> levelNames = new List<string>();
        List<Level> levels = new List<Level>();
        IEnumerable<Element> views;
        public double unitsConstant;// units for tab 2 
        List<double> unitConstants = new List<double>();
        public static double constantDistance;
        public static Boolean isLinked = false;
        public static Boolean stopScript = false;

        public Form1(ExternalCommandData commandDatax, Document doc)
        {
            InitializeComponent();
            commandData = commandDatax;
            // list of al the levels
            IEnumerable<Element> levelCollector = new FilteredElementCollector(doc)//filter the floor category to get a floor list
                                .OfCategory(BuiltInCategory.OST_Levels)
                                .WhereElementIsNotElementType()
                                .ToElements();


            //add level names to levelName list
            foreach (Element level in levelCollector)
            {
                levelNames.Add(level.Name);
                levels.Add(level as Level);
            }
            //sort the levels 
            levelNames.Sort();




            //adds the level names to the listbox
            foreach (string levelName in levelNames)
            {
                listBox1.Items.Add(levelName);
            }

            views = new FilteredElementCollector(doc)
           .OfCategory(BuiltInCategory.OST_Views)
           .WhereElementIsNotElementType().ToElements();

            //set the data source 
            listBox1.DataSource = levelNames;
            //inicialize second tab:
            hangerDistTabInicializer();
            this.Select();

        }

        private void dataBindings()
        {
            listBox1.DataSource = null;
            listBox1.DataSource = levelNames;
        }

        private void Form1_Load_1(object sender, EventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //listBox2.Items.Clear();
        }
        // OKbutton to execute
        private void btn_ok_Click(object sender, EventArgs e)
        {
            int tst;

            if (!int.TryParse(textBox1.Text, out tst))// if user input is NOT valid integers
            {

                //TaskDialog.Show("Wrong format", "Pleas insert valid integer number(100,97,205... without letters, symbols etc..)");
                TaskDialog.Show("Wrong format", "הלו הלו.. לא הכנסתם גודל מרחק מינימלי בין מתלים! (נמצא בלשונית השניה בחלון הזה, לשים לב שמכניסים מספר ולא שטויות)");

            }
            else if (radioButton3.Checked == false && radioButton4.Checked == false)
            {
                //TaskDialog.Show("Check Link Status", "Please check the project's linking status (check the check box: linked or not linked)");
                TaskDialog.Show("Check Link Status", "בעיה אבויה: לא בחרת האם המודל האדריכלי הוא בלינק או לא. לחץ על הצ'קבוקס המתאים לפני לחצה על אישור");

            }
            else if (listBox2.SelectedItem == null)
            {
                //TaskDialog.Show("Pick view", "In: ''View & Level'' tab, pick the desired floor(the left list box), \n " +
                //    "press the 'right' arrow, and then pick desired view (right list box) \n " );

                TaskDialog.Show("בעיה כפרה", "בלשונית הראשונה בחלון הזה, אחרי בחירת מפלס ברשימה השמאלית, \n " +
                    "תלחץ על החץ ימינה לבחירת מבט לפי שם \n ");

            }
            else
            {
                stopScript = false;
                foreach (Element view in views)
                {
                    View v = view as View;
                    if (v.GenLevel != null)//check if the view is indeed a level ( if it's a section it won't work)
                    {
                        if (v.ViewType == ViewType.CeilingPlan && v.GenLevel.Name == pickedLevel.Name && v.Name == listBox2.SelectedItem.ToString())//check if the plan is cielling, and if it has the same level as chosen
                        {
                            commandData.Application.ActiveUIDocument.ActiveView = v;


                            TaskDialog.Show("new view", "The view named:" + view.Name + " is now active.");

                            //stopScript = true;
                        }
                    }

                }
                chosenLevel = listBox1.SelectedItem.ToString();

                if (unitsConstant == 0)
                {
                    unitsConstant = 1;
                }
                constantDistance = Int32.Parse(textBox1.Text) * unitsConstant;// the constant distance in which we will devide the hangers
                                                                              //example: constantDistance ==300--> numberofducts=ductlengh/300
                btn_ok.DialogResult = DialogResult.OK;
                Close();
                return;
            }

        }

        private void btn_cancel_Click(object sender, EventArgs e)
        {
            btn_cancel.DialogResult = DialogResult.Cancel;
            stopScript = true;
            Close();
            return;
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            Form2 form2 = new Form2();
            form2.ShowDialog();
        }



        // button add view to listbox[tab1]
        private void addView1_Click(object sender, EventArgs e)
        {
            listBox2.Items.Clear();
            selectedItemText = listBox1.SelectedItem.ToString();
            selectedItemIndex = listBox1.SelectedIndex;
            foreach (Level lev in levels)
            {
                if (lev.Name == selectedItemText)
                {
                    pickedLevel = lev;
                }
            }


            foreach (Element view in views)
            {
                View v = view as View;

                if (v.GenLevel != null)//check if the view is indeed a level ( if it's a section it won't work)
                {
                    if (v.ViewType == ViewType.CeilingPlan && v.GenLevel.Name == pickedLevel.Name)//check if the plan is cielling, and if it has the same level as chosen
                    {
                        listBox2.Items.Add(v.Name);

                    }
                }

            }
            dataBindings();
        }

        // button decrese views from view list[tab1]
        private void lessView1_Click(object sender, EventArgs e)
        {
            listBox2.Items.Clear();

            dataBindings();
        }



        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            unitsConstant = unitConstants[comboBox1.SelectedIndex];
        }

        private void tabPage2_Click(object sender, EventArgs e)
        {


        }


        private void tabPage1_Click(object sender, EventArgs e)
        {

        }
        //iniciating unit combobox for units
        void hangerDistTabInicializer()
        {
            List<string> unitTypes = new List<string>();


            unitTypes.Add("Centimeters");
            unitTypes.Add("Millimeters");
            unitTypes.Add("Meters");
            //set Up the unit combobox
            foreach (string unitType in unitTypes)
            {
                comboBox1.Items.Add(unitType);
            }

            unitConstants.Add(1);
            unitConstants.Add(0.1);
            unitConstants.Add(100);
            comboBox1.DataSource = unitTypes;
            radioButton1.Checked = true;
            unitsConstant = comboBox1.SelectedIndex;// the constant that has been selected
        }

        //constant radiobuttons
        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
            {
                radioButton1.Checked = false;
                textBox1.Enabled = false;
                textBox1.Clear();
                textBox2.Enabled = true;
                textBox3.Enabled = true;
                textBox4.Enabled = true;
                textBox5.Enabled = true;
                textBox6.Enabled = true;
                textBox7.Enabled = true;
                l1.Enabled = false;
                l2.Enabled = false;
                l3.Enabled = true;
                l4.Enabled = true;
                l5.Enabled = true;
                l6.Enabled = true;
                l7.Enabled = true;
                l8.Enabled = true;
                l9.Enabled = true;
                l10.Enabled = true;
                l11.Enabled = true;

            }

        }
        //conditios radiobuttons
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                radioButton2.Checked = false;
                textBox1.Enabled = true;
                textBox2.Clear();
                textBox3.Clear();
                textBox4.Clear();
                textBox5.Clear();
                textBox6.Clear();
                textBox7.Clear();
                textBox2.Enabled = false;
                textBox3.Enabled = false;
                textBox4.Enabled = false;
                textBox5.Enabled = false;
                textBox6.Enabled = false;
                textBox7.Enabled = false;
                l1.Enabled = true;
                l2.Enabled = true;
                l3.Enabled = false;
                l4.Enabled = false;
                l5.Enabled = false;
                l6.Enabled = false;
                l7.Enabled = false;
                l8.Enabled = false;
                l9.Enabled = false;
                l10.Enabled = false;
                l11.Enabled = false;


            }

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {


        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked)//the model is linked
            {
                radioButton4.Checked = false;
                isLinked = true;
            }
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton4.Checked)//model is not linked
            {
                radioButton3.Checked = false;
                isLinked = false;
            }
        }
    }
}
