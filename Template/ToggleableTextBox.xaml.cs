using Autodesk.Revit.UI;
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
    public partial class ToggleableTextBox : UserControl
    {
        public static readonly DependencyProperty IsClickableProperty =
            DependencyProperty.Register(
            name: "IsClickable",
            propertyType: typeof(bool),
            ownerType: typeof(ToggleableTextBox),
            typeMetadata: new FrameworkPropertyMetadata(defaultValue: true));

        public static readonly DependencyProperty IsTextEditableProperty =
            DependencyProperty.Register(
            name: "IsTextEditable",
            propertyType: typeof(bool),
            ownerType: typeof(ToggleableTextBox),
            typeMetadata: new FrameworkPropertyMetadata( defaultValue: true));

        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register(
            name: "IsChecked",
            propertyType: typeof(bool),
            ownerType: typeof(ToggleableTextBox),
            typeMetadata: new FrameworkPropertyMetadata(defaultValue: false));

        public static readonly RoutedEvent OnCheckedEvent = EventManager.RegisterRoutedEvent(
            name: "OnChecked",
            routingStrategy: RoutingStrategy.Bubble,
            handlerType: typeof(RoutedEventHandler),
            ownerType: typeof(ToggleableTextBox));

        public static readonly DependencyProperty LabelTextProperty =
            DependencyProperty.Register(
            name: "LabelText",
            propertyType: typeof(string),
            ownerType: typeof(ToggleableTextBox),
            typeMetadata: new FrameworkPropertyMetadata(defaultValue: "Label"));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
            name: "Text",
            propertyType: typeof(string),
            ownerType: typeof(ToggleableTextBox),
            typeMetadata: new FrameworkPropertyMetadata(defaultValue: ""));


        public event RoutedEventHandler OnChecked
        {
            add { 
                AddHandler(OnCheckedEvent, value);
                TaskDialog.Show("ToggleableTextBox - Added to handler","");
            }
            remove { RemoveHandler(OnCheckedEvent, value); }
        }

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



        public ToggleableTextBox()
        {
            InitializeComponent();
        }

        private void CheckBoxControl_Click(object sender, RoutedEventArgs e)
        {
            if (IsChecked) 
            {
                RoutedEventArgs args = new RoutedEventArgs(ToggleableTextBox.OnCheckedEvent);
                RaiseEvent(args);
            }
        }
    }
}
