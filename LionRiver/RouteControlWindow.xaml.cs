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
    /// Interaction logic for RouteControlWindow.xaml
    /// </summary>
    public partial class RouteControlWindow : Window
    {
        public RouteControlWindow()
        {
            InitializeComponent();            
        }

        public event RouteCtrlEventHandler RouteCtrlHd;

        protected virtual void OnRouteCtrl(RouteCtrlEventArgs e)
        {
            RouteCtrlEventHandler handler = RouteCtrlHd;
            if (handler != null)
            {
                // Invokes the delegates.
                handler(this, e);
            }
        }

        private void RouteListComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RouteListComboBox.SelectedIndex != -1)
                RenameButton.IsEnabled = true;
            else
                RenameButton.IsEnabled = false;
            
            RouteCtrlEventArgs ea = new RouteCtrlEventArgs((Route)RouteListComboBox.SelectedItem, RouteCtrlCmd.SelectionChanged);
            OnRouteCtrl(ea);

        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Hide();
            RouteCtrlEventArgs ea = new RouteCtrlEventArgs(null, RouteCtrlCmd.Hiding);
            OnRouteCtrl(ea);
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void RenameButton_Click(object sender, RoutedEventArgs e)
        {
            RouteListComboBox.Visibility = Visibility.Collapsed;
            RenameTextBox.Visibility = Visibility.Visible;
            var rte = RouteListComboBox.SelectedItem as Route;
            RenameTextBox.Text = rte.Name;
            Keyboard.Focus(RenameTextBox);
            RenameTextBox.SelectAll();
        }

        private void RenameTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                RouteListComboBox.Visibility = Visibility.Visible;
                RenameTextBox.Visibility = Visibility.Collapsed;
                var rte = RouteListComboBox.SelectedItem as Route;
                rte.Name = RenameTextBox.Text;
                e.Handled = true;
            }
        }


    }


    public class RouteCtrlEventArgs : EventArgs
    {
        private Route rte;
        private RouteCtrlCmd command;

        public RouteCtrlEventArgs(Route rtn, RouteCtrlCmd cmd)
        {
            this.rte = rtn;
            this.command = cmd;
        }

        public Route RouteTarget
        {
            get { return rte; }
        }

        public RouteCtrlCmd Command
        {
            get { return command; }
        }
    }

    public delegate void RouteCtrlEventHandler(object sender, RouteCtrlEventArgs e);

    public enum RouteCtrlCmd
    {
        SelectionChanged,
        Hiding
    }

}
