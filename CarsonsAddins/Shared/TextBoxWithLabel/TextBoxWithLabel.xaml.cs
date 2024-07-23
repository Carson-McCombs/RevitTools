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
    /// A simple WPF User Control meant to allow the user to toggle and set the text override settings within the Dimension Text Window
    /// </summary>
    public partial class TextBoxWithLabel : UserControl
    {
        public static readonly DependencyProperty LabelTextProperty =
        DependencyProperty.Register(
            name: "LabelText",
            propertyType: typeof(string),
            ownerType: typeof(TextBoxWithLabel),
            typeMetadata: new FrameworkPropertyMetadata(defaultValue: "Label"));
        public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            name: "Text",
            propertyType: typeof(string),
            ownerType: typeof(TextBoxWithLabel),
            typeMetadata: new FrameworkPropertyMetadata(defaultValue: "TextField"));

        public static readonly DependencyProperty IsEditableProperty =
        DependencyProperty.Register(
            name: "IsEditable",
            propertyType: typeof(bool),
            ownerType: typeof(TextBoxWithLabel),
            typeMetadata: new FrameworkPropertyMetadata(defaultValue: true));

        public string LabelText
        {
            get => (string)GetValue(LabelTextProperty);
            set => SetValue(LabelTextProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        public bool IsEditable
        {
            get => (bool)GetValue(IsEditableProperty);
            set => SetValue(IsEditableProperty, value);
        }


        public TextBoxWithLabel()
        {
            InitializeComponent();
        }
    }
}
