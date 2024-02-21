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
    /// Made to combine a checkbox with its label and a corresponding textbox into a single component. 
    /// 
    /// Less necessary than, but made to be consistent with the TextboxWithLabel class
    /// </summary>
    public partial class ToggleableCheckBox : UserControl
    {
        public static readonly DependencyProperty IsClickableProperty =
        DependencyProperty.Register(
            name: "IsClickable",
            propertyType: typeof(bool),
            ownerType: typeof(ToggleableCheckBox),
            typeMetadata: new FrameworkPropertyMetadata(defaultValue: true));

        public static readonly DependencyProperty IsTextEditableProperty =
        DependencyProperty.Register(
            name: "IsTextEditable",
            propertyType: typeof(bool),
            ownerType: typeof(ToggleableCheckBox),
            typeMetadata: new FrameworkPropertyMetadata(defaultValue: true));

        public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(
            name: "IsChecked",
            propertyType: typeof(bool),
            ownerType: typeof(ToggleableCheckBox),
            typeMetadata: new FrameworkPropertyMetadata(defaultValue: false));

        public static readonly DependencyProperty LabelTextProperty =
        DependencyProperty.Register(
            name: "LabelText",
            propertyType: typeof(string),
            ownerType: typeof(ToggleableCheckBox),
            typeMetadata: new FrameworkPropertyMetadata(defaultValue: "Label"));

        public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            name: "Text",
            propertyType: typeof(string),
            ownerType: typeof(ToggleableCheckBox),
            typeMetadata: new FrameworkPropertyMetadata(defaultValue: ""));

        public bool IsClickable
        {
            get => (bool)GetValue(IsClickableProperty);
            set => SetValue(IsClickableProperty, value);
        }
        public bool IsTextEditable
        {
            get => (bool)GetValue(IsTextEditableProperty);
            set => SetValue(IsTextEditableProperty, value);
        }
        public bool IsChecked
        {
            get => (bool)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }
        public string LabelText
        {
            get => (string) GetValue(LabelTextProperty);
            set => SetValue(LabelTextProperty, value);
        }
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        

        public ToggleableCheckBox()
        {
            InitializeComponent();
        }
    }
}
