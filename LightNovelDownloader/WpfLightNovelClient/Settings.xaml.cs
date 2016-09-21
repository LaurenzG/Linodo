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

namespace WpfLightNovelClient
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        public Settings()
        {
            InitializeComponent();
            pathTxt.Text = (Properties.Settings.Default["Path"].Equals(""))
                ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                : (string)Properties.Settings.Default["Path"];
            SaveAsBtn.Content = ((bool)Properties.Settings.Default["AsEpub"])
                ? "Save as Epub"
                : "Save as Html";
            ToolTipCheckBox.IsChecked = ((bool)Properties.Settings.Default["ShowToolTips"]);
        }

        private void FolderBtn_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dlg.ShowDialog();
            if (result.Equals(System.Windows.Forms.DialogResult.OK))
            {
                Properties.Settings.Default["Path"]= dlg.SelectedPath;
                pathTxt.Text = dlg.SelectedPath;
                Properties.Settings.Default.Save();
            }
        }



        private void ReturnBtn_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default["AsEpub"] = SaveAsBtn.Content.Equals("Save as Epub") 
                ? true
                : false;
            Properties.Settings.Default.Save();
            Close();
        }

        private void SaveAsBtn_Click(object sender, RoutedEventArgs e)
        {
            
            if (((string)SaveAsBtn.Content).Contains("Epub"))
            {
                SaveAsBtn.Content = "Save as Html";
            }
            else
            {
                SaveAsBtn.Content = "Save as Epub";
            }
        }

        private void ToolTipCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default["ShowToolTips"] = ToolTipCheckBox.IsChecked;
        }
    }
}
