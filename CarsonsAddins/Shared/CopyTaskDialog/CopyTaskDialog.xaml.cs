using CarsonsAddins.GenericCommands;
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

namespace CarsonsAddins.Shared.CopyTaskDialog
{
    /// <summary>
    /// Interaction logic for CopyTaskDialog.xaml
    /// </summary>
    public partial class CopyTaskDialog : Window
    {

        public CopyTaskDialog(string title, string text)
        {
            InitializeComponent();
            Title = title;
            MainText.Text = text;
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(MainText.Text);
        }
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public static void Show(string title, string text)
        {
            CopyTaskDialog instance = new CopyTaskDialog(title, text);
            instance.Show();
        }
    }
}
