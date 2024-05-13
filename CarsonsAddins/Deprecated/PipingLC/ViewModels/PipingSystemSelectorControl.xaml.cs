using Autodesk.Revit.UI;
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

namespace CarsonsAddins
{
    /// <summary>
    /// A WPF UserControl that lets the User select a Piping System
    /// </summary>
    public partial class PipingSystemSelectorControl : UserControl
    {
        public delegate void OnSelectPipingSystem(PipingSystemLC system);
        public static event OnSelectPipingSystem SelectPipingSystemEvent;
        public PipingSystemSelectorControl()
        {
            InitializeComponent();
        }

        public void Init(ObservableCollection<PipingSystemLC> list)
        {
            TaskDialog.Show("Piping System Selector Control", "Initialized");
            PipingSystemSelector.ItemsSource = list;
        }

        private void OnSelect(object sender, RoutedEventArgs e)
        {
            PipingSystemLC system = (PipingSystemLC)((Button)sender).DataContext;
            SelectPipingSystemEvent(system);
        }
    }
}
