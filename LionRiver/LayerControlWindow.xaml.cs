using System;
using System.Collections.Generic;
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

namespace LionRiver
{
    /// <summary>
    /// Interaction logic for LayerControl.xaml
    /// </summary>
    public partial class LayerControlWindow : Window
    {
        public LayerControlWindow()
        {
            InitializeComponent();
        }

        public event LayerCtrlEventHandler LayerCtrlHd;

        protected virtual void OnLayerCtrl(LayerCtrlEventArgs e)
        {
            LayerCtrlEventHandler handler = LayerCtrlHd;
            if (handler != null)
            {
                // Invokes the delegates.
                handler(this, e);
            }
        }

        public class LayerCtrlEventArgs : EventArgs
        {
            private LayerCtrlCmd command;
            private Double value;
            private Visibility visible;
            

            public LayerCtrlEventArgs(LayerCtrlCmd cmd, Double val=0,Visibility vis=Visibility.Visible)
            {
                this.command = cmd;
                this.value = val;
                this.visible = vis;
            }

            public LayerCtrlCmd Command
            {
                get { return command; }
            }

            public Double Value
            {
                get { return value; }
            }

            public Visibility Visible
            {
                get { return visible; }
            }
        }

        public delegate void LayerCtrlEventHandler(object sender, LayerCtrlEventArgs e);

        public enum LayerCtrlCmd
        {
            Layer1Changed,
            Layer2Changed,
            TrackResolutionChanged,
            Layer1OpacityChanged,
            Layer2OpacityChanged, 
            LaylinesChanged,
            TargetBearingsChanged,
            Hiding
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Hide();
            LayerCtrlEventArgs ea = new LayerCtrlEventArgs(LayerCtrlCmd.Hiding);
            OnLayerCtrl(ea);
        }

        private void Layer1CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            LayerCtrlEventArgs ea = new LayerCtrlEventArgs(LayerCtrlCmd.Layer1Changed,0,Visibility.Hidden);
            OnLayerCtrl(ea);
        }

        private void Layer2CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            LayerCtrlEventArgs ea = new LayerCtrlEventArgs(LayerCtrlCmd.Layer2Changed,0,Visibility.Hidden);
            OnLayerCtrl(ea);
        }

        private void Layer1CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            LayerCtrlEventArgs ea = new LayerCtrlEventArgs(LayerCtrlCmd.Layer1Changed,0,Visibility.Visible);
            OnLayerCtrl(ea);
        }

        private void Layer2CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            LayerCtrlEventArgs ea = new LayerCtrlEventArgs(LayerCtrlCmd.Layer2Changed,0,Visibility.Visible);
            OnLayerCtrl(ea);
        }

        private void LaylinesCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            LayerCtrlEventArgs ea = new LayerCtrlEventArgs(LayerCtrlCmd.LaylinesChanged, 0, Visibility.Visible);
            OnLayerCtrl(ea);
        }

        private void LaylinesCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            LayerCtrlEventArgs ea = new LayerCtrlEventArgs(LayerCtrlCmd.LaylinesChanged, 0, Visibility.Hidden);
            OnLayerCtrl(ea);
        }

        private void TrackResolutionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;

            LayerCtrlEventArgs ea = new LayerCtrlEventArgs(LayerCtrlCmd.TrackResolutionChanged, slider.Value);
            OnLayerCtrl(ea);
        }

        private void Layer1OpacitySliderSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;

            LayerCtrlEventArgs ea = new LayerCtrlEventArgs(LayerCtrlCmd.Layer1OpacityChanged, slider.Value);
            OnLayerCtrl(ea);
        }

        private void Layer2OpacitySliderSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;

            LayerCtrlEventArgs ea = new LayerCtrlEventArgs(LayerCtrlCmd.Layer2OpacityChanged, slider.Value);
            OnLayerCtrl(ea);

        }

        private void TargetBearingsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            LayerCtrlEventArgs ea = new LayerCtrlEventArgs(LayerCtrlCmd.TargetBearingsChanged, 0, Visibility.Visible);
            OnLayerCtrl(ea);
        }

        private void TargetBearingsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            LayerCtrlEventArgs ea = new LayerCtrlEventArgs(LayerCtrlCmd.TargetBearingsChanged, 0, Visibility.Hidden);
            OnLayerCtrl(ea);
        }


    }
}
