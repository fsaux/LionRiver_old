using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LionRiver
{
    /// <summary>
    /// Interaction logic for MarkControlWindow.xaml
    /// </summary>
    public partial class MarkControlWindow : Window
    {
        public MarkControlWindow()
        {
            InitializeComponent();
        }

        public event MarkCtrlEventHandler MarkCtrlHd;

        protected virtual void OnMarkCtrl(MarkCtrlEventArgs e)
        {
            MarkCtrlEventHandler handler = MarkCtrlHd;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Hide();
            MarkCtrlEventArgs ea = new MarkCtrlEventArgs(MarkCtrlCmd.Hiding);
            OnMarkCtrl(ea);
        }

    }


    public class MarkCtrlEventArgs : EventArgs
    {
        private MarkCtrlCmd command;

        public MarkCtrlEventArgs(MarkCtrlCmd cmd)
        {
            this.command = cmd;
        }

        public MarkCtrlCmd Command
        {
            get { return command; }
        }
    }

    public delegate void MarkCtrlEventHandler(object sender, MarkCtrlEventArgs e);

    public enum MarkCtrlCmd
    {
        Hiding
    }
}
