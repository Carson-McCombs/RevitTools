using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
    enum DimensionTextOptions { None, UseActualValue, ReplaceWithText, ShowLabelInView };

    /// <summary>
    /// Remade Revit's Dimension Text tool. Meant to be incorporated with the QuestionMarkDimensionsCommand for more options.
    /// </summary>
    public partial class DimensionTextWindow : Window, ISettingsUIComponent
    {
        public const bool IsWIP = false;

        public static readonly DependencyProperty DimensionTextOptionsProperty =
           DependencyProperty.Register(
           name: "DimensionTextOptions",
           propertyType: typeof(int),
           ownerType: typeof(DimensionTextWindow),
           typeMetadata: new FrameworkPropertyMetadata(defaultValue: 0));



        public static readonly DependencyProperty AboveTextProperty =
            DependencyProperty.Register(
            name: "AboveText",
            propertyType: typeof(string),
            ownerType: typeof(DimensionTextWindow),
            typeMetadata: new FrameworkPropertyMetadata(defaultValue: ""));
        public static readonly DependencyProperty BelowTextProperty =
            DependencyProperty.Register(
            name: "BelowText",
            propertyType: typeof(string),
            ownerType: typeof(DimensionTextWindow),
            typeMetadata: new FrameworkPropertyMetadata(defaultValue: ""));
        public static readonly DependencyProperty ValueTextProperty =
            DependencyProperty.Register(
            name: "ValueText",
            propertyType: typeof(string),
            ownerType: typeof(DimensionTextWindow),
            typeMetadata: new FrameworkPropertyMetadata(defaultValue: ""));
        public static readonly DependencyProperty ValueOverrideTextProperty =
            DependencyProperty.Register(
            name: "ValueOverrideText",
            propertyType: typeof(string),
            ownerType: typeof(DimensionTextWindow),
            typeMetadata: new FrameworkPropertyMetadata(defaultValue: ""));
        public static readonly DependencyProperty LabelTextProperty =
            DependencyProperty.Register(
            name: "LabelText",
            propertyType: typeof(string),
            ownerType: typeof(DimensionTextWindow),
            typeMetadata: new FrameworkPropertyMetadata(defaultValue: ""));
        public static readonly DependencyProperty PrefixTextProperty =
            DependencyProperty.Register(
            name: "PrefixText",
            propertyType: typeof(string),
            ownerType: typeof(DimensionTextWindow),
            typeMetadata: new FrameworkPropertyMetadata(defaultValue: ""));
        public static readonly DependencyProperty SuffixTextProperty =
            DependencyProperty.Register(
            name: "SuffixText",
            propertyType: typeof(string),
            ownerType: typeof(DimensionTextWindow),
            typeMetadata: new FrameworkPropertyMetadata(defaultValue: ""));

        private UIDocument uidoc;
        private List<Dimension> selectedDimensions = new List<Dimension>();
        private DimensionTextOptions selectedTextOption
        {
            get => (DimensionTextOptions) GetValue( DimensionTextOptionsProperty );
            set => SetValue( DimensionTextOptionsProperty, (int) value );
        }



        private string AboveText
        {
            get => (string)GetValue(AboveTextProperty);
            set => SetValue(AboveTextProperty, value);
        }

        private string BelowText
        {
            get => (string)GetValue(BelowTextProperty);
            set => SetValue(BelowTextProperty, value);
        }
        private string ValueText
        {
            get => (string)GetValue(ValueTextProperty);
            set => SetValue(ValueTextProperty, value);
        }
        private string ValueOverrideText
        {
            get => (string)GetValue(ValueOverrideTextProperty);
            set => SetValue(ValueOverrideTextProperty, value);
        }
        private string LabelText
        {
            get => (string)GetValue(LabelTextProperty);
            set => SetValue(LabelTextProperty, value);
        }
        private string PrefixText
        {
            get => (string)GetValue(PrefixTextProperty);
            set => SetValue(PrefixTextProperty, value);
        }
        private string SuffixText
        {
            get => (string)GetValue(SuffixTextProperty);
            set => SetValue(SuffixTextProperty, value);
        }



        public DimensionTextWindow()
        {
            InitializeComponent();
        }

        public void Init(UIDocument uidoc)
        {
            this.uidoc = uidoc;
            List<Element> dimensionElements = new FilteredElementCollector(uidoc.Document, uidoc.Selection.GetElementIds()).OfCategory(BuiltInCategory.OST_Dimensions).ToElements() as List<Element>;
            selectedDimensions = new List<Dimension>();
            foreach (Element element in dimensionElements)
            {
                if (element is Dimension dimension )selectedDimensions.Add(dimension);
            }
            
            SetDefaultTextsBySelected(selectedDimensions);
        }

        public PushButtonData RegisterButton(Assembly assembly)
        {
            PushButtonData pushButtonData = new PushButtonData("DimensionsTextWindow", "Dimensions Text Window", assembly.Location, typeof(ShowWindow<DimensionTextWindow>).FullName);
            pushButtonData.ToolTip = "Dimension Text Window";
            return pushButtonData;
        }
    





        private void UseActualValueControl_OnChecked(object sender, RoutedEventArgs e)
        {
            selectedTextOption = DimensionTextOptions.UseActualValue;
            ReplaceWithTextControl.IsChecked = false;
            ShowLabelInViewControl.IsChecked = false;

            PrefixControl.IsEditable = true;
            SuffixControl.IsEditable = true;
        }

        private void ReplaceWithTextControl_OnChecked(object sender, RoutedEventArgs e)
        {
            selectedTextOption = DimensionTextOptions.ReplaceWithText;
            UseActualValueControl.IsChecked = false;
            ShowLabelInViewControl.IsChecked = false;

            PrefixControl.IsEditable = false;
            SuffixControl.IsEditable = false;
        }

        private void ShowLabelInViewControl_OnChecked(object sender, RoutedEventArgs e)
        {
            selectedTextOption = DimensionTextOptions.ShowLabelInView;
            UseActualValueControl.IsChecked = false;
            ReplaceWithTextControl.IsChecked = false;

            PrefixControl.IsEditable = false;
            SuffixControl.IsEditable = false;
        }

        private void LeaderVisibility_Selected(object sender, RoutedEventArgs e)
        {
            //nothing for now
        }

        private void SetDefaultTextsBySelected(List<Dimension> dimensions)
        {
            if (dimensions == null || dimensions.Count == 0) return;

            bool similarAboveText = true;
            bool similarBelowText = true;

            bool similarValueText = true;
            bool similarValueOverrideText = true;
            bool similarLabelText = true;


            bool similarPrefixText = true;
            bool similarSuffixText = true;

            if (dimensions.Count > 1)
            {
                for (int i = 0; i < dimensions.Count - 1; i++)
                {
                    if (similarAboveText && !dimensions[i].Above.Equals(dimensions[i + 1].Above)) similarAboveText = false;
                    if (similarBelowText && !dimensions[i].Below.Equals(dimensions[i + 1].Below)) similarBelowText = false;

                    if (similarValueText && !dimensions[i].ValueString.Equals(dimensions[i + 1].ValueString)) similarValueText = false;
                    if (similarValueOverrideText && !dimensions[i].ValueOverride.Equals(dimensions[i + 1].ValueOverride)) similarValueOverrideText = false;
                    if (similarLabelText && !dimensions[i].get_Parameter(BuiltInParameter.DIM_LABEL).AsValueString().Equals(dimensions[i + 1].get_Parameter(BuiltInParameter.DIM_LABEL).AsValueString())) similarLabelText = false;

                    if (similarPrefixText && !dimensions[i].Prefix.Equals(dimensions[i + 1].Prefix)) similarPrefixText = false;
                    if (similarSuffixText && !dimensions[i].Suffix.Equals(dimensions[i + 1].Suffix)) similarSuffixText = false;

                }
            }
            AboveText = similarAboveText ? dimensions[0].Above : "";
            BelowText = similarAboveText ? dimensions[0].Below : "";

            ValueText = similarAboveText ? dimensions[0].ValueString : "";
            ValueOverrideText = similarAboveText ? dimensions[0].ValueOverride : "";
            LabelText = similarLabelText ? dimensions[0].get_Parameter(BuiltInParameter.DIM_LABEL).AsValueString() : "";

            PrefixText = similarAboveText ? dimensions[0].Prefix : "";
            SuffixText = similarAboveText ? dimensions[0].Suffix : "";


        }
        private void UpdateSelectedDimensions()
        {
            string above = AboveText;
            string below = BelowText;
            string valueOverride = ValueOverrideText;
            string prefix = PrefixText;
            string suffix = SuffixText;
            for (int i = 0; i < selectedDimensions.Count; i++)
            {
                //selectedDimensions[i].Above = above;
                //selectedDimensions[i].Below = below;
                selectedDimensions[i].ValueOverride = valueOverride;
               // selectedDimensions[i].Prefix = prefix;
               // selectedDimensions[i].Suffix = suffix;
            }
            //Transaction transaction = new Transaction(uidoc.Document);
            //transaction.Start("Update Selected Dimensions");
            //try
            //{
            //    string above = AboveText;
            //    string below = BelowText;
            //    string valueOverride = ValueOverrideText;
            //    string prefix = PrefixText;
            //    string suffix = SuffixText;
            //    for (int i = 0; i < selectedDimensions.Count; i++)
            //    {
            //        selectedDimensions[i].Above = above;
            //        selectedDimensions[i].Below = below;
            //        selectedDimensions[i].ValueOverride = valueOverride;
            //        selectedDimensions[i].Prefix = prefix;
            //        selectedDimensions[i].Suffix = suffix;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    TaskDialog.Show("Error", ex.Message);
            //    transaction.RollBack();
            //}
            //transaction.Commit();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateDimensionTextCommand handler = new UpdateDimensionTextCommand(selectedDimensions, AboveText, BelowText, ValueOverrideText, PrefixText, SuffixText);
            handler.Execute(uidoc.Application);
            Close();
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {

            Close();
        }
        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateDimensionTextCommand handler = new UpdateDimensionTextCommand(selectedDimensions, AboveText, BelowText, ValueOverrideText, PrefixText, SuffixText);
            handler.Execute(uidoc.Application);
            
            //UpdateSelectedDimensions();
            Close();
        }
    }


    class UpdateDimensionTextCommand : IExternalEventHandler
    {
        private List<Dimension> selectedDimensions;
        private string above, below, valueOverride, prefix, suffix;
        public UpdateDimensionTextCommand(List<Dimension> selectedDimensions, string above, string below, string valueOverride, string prefix, string suffix) 
        { 
            this.selectedDimensions = selectedDimensions;
            this.above = above;
            this.below = below;
            this.valueOverride = valueOverride;
            this.prefix = prefix;
            this.suffix = suffix;
        }


        public void Execute(UIApplication app)
        {
            if (selectedDimensions.Count == 0) return;
            Transaction transaction = new Transaction(app.ActiveUIDocument.Document);
            transaction.Start("Update Dimensions Text");
            try
            {
                for (int i = 0; i < selectedDimensions.Count; i++)
                {
                    selectedDimensions[i].Above = above;
                    selectedDimensions[i].Below = below;
                    selectedDimensions[i].ValueOverride = valueOverride;
                    selectedDimensions[i].Prefix = prefix;
                    selectedDimensions[i].Suffix = suffix;
                }
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.RollBack();
            }
        }

        public string GetName()
        {
            return "Update Dimensions Text Event Handler";
        }
    }
}
