using Dto;
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
    /// Interaction logic for AddChapterDialog.xaml
    /// </summary>
    public partial class AddChapterDialog : Window
    {
        public AddChapterDialog()
        {
            InitializeComponent();
            Success = false;
        }

        private void addChapterBtn_Click(object sender, RoutedEventArgs e)
        {
            ChapterDto c = new ChapterDto
            {
                ChapterUrl = linkTxtBox.Text,
                DisplayName = nameTxtBox.Text
            };
            Chapter = c;
            Success = true;
            Close();
        }
        public ChapterDto Chapter { get; set; }
        public bool Success { get; set; }
    }
}
