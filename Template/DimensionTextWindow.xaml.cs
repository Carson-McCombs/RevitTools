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

namespace CarsonsAddins
{
    /// <summary>
    /// Remade Revit's Dimension Text tool. Meant to be incorporated with the QuestionMarkDimensionsCommand for more options.
    /// </summary>
    public partial class DimensionTextWindow : Window
    {
        public DimensionTextWindow()
        {
            InitializeComponent();
        }

        

        private void UseActualValue_Checked(object sender, RoutedEventArgs e)
        {

        }
        private void ReplaceWithText_Checked(object sender, RoutedEventArgs e)
        {

        }
        private void ShowLabelInView_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void LeaderVisibility_Selected(object sender, RoutedEventArgs e)
        {

        }
    }
}
