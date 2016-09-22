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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfLightNovelClient
{
    /// <summary>
    /// Interaction logic for LabeledProgressBar.xaml
    /// </summary>
    public partial class LabeledProgressBar : UserControl
    {
        public LabeledProgressBar()
        {
            InitializeComponent();
            
        }

        public void SetMouseLeftButtonDownHandler(MouseButtonEventHandler handler)
        {
            this.MouseLeftButtonDown += handler;
        }

        public double Progress
        {
            get { return ProgressBar.Value; }
            set { ProgressBar.Value = value; }
        }

        public new string Content
        {
            get { return ContentLabel.Content as string; }
            set { ContentLabel.Content = value; }
        }
    }
}
