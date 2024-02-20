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
    /// Interaction logic for TextBoxWithLabel.xaml
    /// </summary>
    public partial class TextBoxWithLabel : UserControl
    {
        public static readonly DependencyProperty labelTextProperty =
        DependencyProperty.Register(
            name: "labelText",
            propertyType: typeof(string),
            ownerType: typeof(UserControl),
            typeMetadata: new FrameworkPropertyMetadata(defaultValue: "Label"));
        public static readonly DependencyProperty textProperty =
        DependencyProperty.Register(
            name: "text",
            propertyType: typeof(string),
            ownerType: typeof(UserControl),
            typeMetadata: new FrameworkPropertyMetadata(defaultValue: "TextField"));

        public static readonly DependencyProperty canEditProperty =
        DependencyProperty.Register(
            name: "canEdit",
            propertyType: typeof(bool),
            ownerType: typeof(UserControl),
            typeMetadata: new FrameworkPropertyMetadata(defaultValue: true));

        public string labelText 
        {
            get => (string)GetValue(labelTextProperty);
            set => SetValue(labelTextProperty, value);
        }

        public string text
        {
            get => (string)GetValue(textProperty);
            set => SetValue(textProperty, value);
        }
        public bool canEdit
        {
            get => (bool)GetValue(canEditProperty);
            set => SetValue(canEditProperty, value);
        }


        public TextBoxWithLabel()
        {
            InitializeComponent();
        }
    }
}
