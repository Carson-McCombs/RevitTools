using Autodesk.Revit.DB;
using CarsonsAddins.Settings.Dimensioning.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace CarsonsAddins.Settings.Dimensioning.Views
{
    /// <summary>
    /// Interaction logic for GraphicsStylesSelectorControl.xaml
    /// </summary>
    public partial class GraphicsStylesSelectorControl : UserControl
    {
        private GraphicStylesSelectorViewModel viewModel;
        public GraphicsStylesSelectorControl()
        {
            InitializeComponent();
        }
        public void Init(GraphicStylesSelectorViewModel viewModel)
        {
            this.viewModel = viewModel;
            DataContext = viewModel;
        }
        private void AddStyle_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel == null) return;
            viewModel.AddStyle(viewModel.ComboboxSelectedGraphicStyleName);
        }
        private void RemoveStyle_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel == null) return;
            Button button = sender as Button;
            if (!(button.DataContext is string styleName)) return;
            viewModel.RemoveStyle(styleName);
        }
    }
}
