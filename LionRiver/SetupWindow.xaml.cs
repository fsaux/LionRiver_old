using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.IO.Ports;

namespace LionRiver
{
    /// <summary>
    /// Interaction logic for SetupWindow.xaml
    /// </summary>
    public partial class SetupWindow : Window
    {

        NMEASentence RouteSentence = new NMEASentence();
        NMEASentence NavSentence = new NMEASentence();
        NMEASentence AppWindSentence = new NMEASentence();
        NMEASentence HullSpeedSentence = new NMEASentence();
        NMEASentence HeadingSentence = new NMEASentence();
        NMEASentence DepthSentence = new NMEASentence();
        NMEASentence WaterTempSentence = new NMEASentence();
        NMEASentence TacktickPerformanceSentence = new NMEASentence();

        List<NMEASentence> NMEASentences = new List<NMEASentence>();

        public ObservableString port1 { get; set; }
        public ObservableString port2 { get; set; }
        public ObservableString port3 { get; set; }
        public ObservableString port4 { get; set; }
        
        public SetupWindow()
        {
            port1 = new ObservableString();
            port2 = new ObservableString();
            port3 = new ObservableString();
            port4 = new ObservableString();

            InitializeComponent();

            this.DataContext = this;            

            if (Properties.Settings.Default.RouteSentence != null) RouteSentence = Properties.Settings.Default.RouteSentence;
            if (Properties.Settings.Default.NavSentence != null) NavSentence = Properties.Settings.Default.NavSentence;
            if (Properties.Settings.Default.DepthSentence != null) DepthSentence = Properties.Settings.Default.DepthSentence;
            if (Properties.Settings.Default.HeadingSentence != null) HeadingSentence = Properties.Settings.Default.HeadingSentence;
            if (Properties.Settings.Default.WaterTempSentence != null) WaterTempSentence = Properties.Settings.Default.WaterTempSentence;
            if (Properties.Settings.Default.HullSpeedSentence != null) HullSpeedSentence = Properties.Settings.Default.HullSpeedSentence;
            if (Properties.Settings.Default.AppWindSentence != null) AppWindSentence = Properties.Settings.Default.AppWindSentence;
            if (Properties.Settings.Default.TacktickPerformanceSentence != null) TacktickPerformanceSentence = Properties.Settings.Default.TacktickPerformanceSentence;

            RouteSentence.Name = "Route"; RouteSentence.Comments = "RMB";
            NavSentence.Name = "Navigation"; NavSentence.Comments = "RMC";
            DepthSentence.Name = "Depth"; DepthSentence.Comments = "DPT, DBT";
            HeadingSentence.Name = "Heading"; HeadingSentence.Comments = "HDG, HDT";
            WaterTempSentence.Name = "Water Temp"; WaterTempSentence.Comments = "MTW";
            HullSpeedSentence.Name = "Hull Speed"; HullSpeedSentence.Comments = "VHW";
            AppWindSentence.Name = "Apparent Wind"; AppWindSentence.Comments = "MWV";
            TacktickPerformanceSentence.Name = "Tacktick performance"; TacktickPerformanceSentence.Comments = "PTAK";

            NMEASentences.Add(RouteSentence);
            NMEASentences.Add(NavSentence); 
            NMEASentences.Add(DepthSentence);
            NMEASentences.Add(HeadingSentence);
            NMEASentences.Add(WaterTempSentence);
            NMEASentences.Add(HullSpeedSentence);
            NMEASentences.Add(AppWindSentence);
            NMEASentences.Add(TacktickPerformanceSentence);


            foreach (string s in SerialPort.GetPortNames())
            {
                Combobox1.Items.Add(s);
                Combobox2.Items.Add(s);
                Combobox3.Items.Add(s);
                Combobox4.Items.Add(s);
            }

            Combobox1.Items.Add("None");
            Combobox2.Items.Add("None");
            Combobox3.Items.Add("None");
            Combobox4.Items.Add("None");

            if (Properties.Settings.Default.Port1 != "") Combobox1.SelectedValue = Properties.Settings.Default.Port1; else Combobox1.SelectedIndex = 0;
            if (Properties.Settings.Default.Port2 != "") Combobox2.SelectedValue = Properties.Settings.Default.Port2; else Combobox2.SelectedIndex = 0;
            if (Properties.Settings.Default.Port3 != "") Combobox3.SelectedValue = Properties.Settings.Default.Port3; else Combobox3.SelectedIndex = 0;
            if (Properties.Settings.Default.Port4 != "") Combobox4.SelectedValue = Properties.Settings.Default.Port4; else Combobox4.SelectedIndex = 0;

            if (Combobox1.SelectedValue != null) port1.Value = Combobox1.SelectedValue.ToString();
            if (Combobox2.SelectedValue != null) port2.Value = Combobox2.SelectedValue.ToString();
            if (Combobox3.SelectedValue != null) port3.Value = Combobox3.SelectedValue.ToString();
            if (Combobox4.SelectedValue != null) port4.Value = Combobox4.SelectedValue.ToString(); 

            dataGrid1.DataContext = NMEASentences;

            textBox1.Text = Properties.Settings.Default.MagVar.ToString();

        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Port1 = (string)Combobox1.SelectedValue;
            Properties.Settings.Default.Port2 = (string)Combobox2.SelectedValue;
            Properties.Settings.Default.Port3 = (string)Combobox3.SelectedValue;
            Properties.Settings.Default.Port4 = (string)Combobox4.SelectedValue;

            // Sentences assigned to InPort=None shoud be generated based on available data
            if (Properties.Settings.Default.Port1 == "None") SetToGenerate(1);
            if (Properties.Settings.Default.Port2 == "None") SetToGenerate(2);
            if (Properties.Settings.Default.Port3 == "None") SetToGenerate(3);
            if (Properties.Settings.Default.Port4 == "None") SetToGenerate(4);

            Properties.Settings.Default.RouteSentence = RouteSentence;
            Properties.Settings.Default.NavSentence = NavSentence;
            Properties.Settings.Default.DepthSentence = DepthSentence;
            Properties.Settings.Default.HeadingSentence = HeadingSentence;
            Properties.Settings.Default.WaterTempSentence = WaterTempSentence;
            Properties.Settings.Default.HullSpeedSentence = HullSpeedSentence;
            Properties.Settings.Default.AppWindSentence = AppWindSentence;
            Properties.Settings.Default.TacktickPerformanceSentence = TacktickPerformanceSentence;

            Properties.Settings.Default.MagVar = Convert.ToDouble(textBox1.Text);

            Properties.Settings.Default.Save();

            this.DialogResult = true;
        }

        private void SetToGenerate(int port)
        {
            foreach (NMEASentence sentence in NMEASentences)
                if (sentence.InPort == port) sentence.Generate = true;
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();

            dlg.SelectedPath = Properties.Settings.Default.Layer1Directory;

            // Show browse file dialog box

            DialogResult result = dlg.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                // Save document
                string foldername = dlg.SelectedPath;
                Properties.Settings.Default.Layer1Directory = foldername;
            }
        }

        private void Combobox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            port1.Value = Combobox1.SelectedItem.ToString();
        }

        private void Combobox2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            port2.Value = Combobox2.SelectedItem.ToString();
        }

        private void Combobox3_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            port3.Value = Combobox3.SelectedItem.ToString();
        }

        private void Combobox4_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            port4.Value = Combobox4.SelectedItem.ToString();
        }

        private void button4_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.Filter = "Polar files|*.pol";
            try
            {
                dlg.InitialDirectory = System.IO.Path.GetDirectoryName(Properties.Settings.Default.PolarFile);
            }
            catch { }

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                // Read Polar
                Properties.Settings.Default.PolarFile = dlg.FileName;
            }
        }

        private void button3_Copy_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();

            dlg.SelectedPath = Properties.Settings.Default.Layer2Directory;

            // Show browse file dialog box

            DialogResult result = dlg.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                // Save document
                string foldername = dlg.SelectedPath;
                Properties.Settings.Default.Layer2Directory = foldername;
            }
        }

        private void button5_Click(object sender, RoutedEventArgs e)
        {         
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

            try
            {
                dlg.InitialDirectory = System.IO.Path.GetDirectoryName(Properties.Settings.Default.LogFile);
            }
            catch { }

            dlg.Filter = "Lof files|*.log";

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                // Save document
                string filename = dlg.FileName;
                Properties.Settings.Default.LogFile = filename;
            }

        }

    }
}
