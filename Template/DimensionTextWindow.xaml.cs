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
using static CarsonsAddins.Util;

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

        

        private static SelectIndividualDimensionsEventHandler selectDimensionsHandler;
        private static SetDimensionsTextEventHandler setDimensionsTextHandler;
        
        private static ExternalEvent selectDimensionsEvent;
        private static ExternalEvent setDimensionsTextEvent;

        private List<(Dimension, DimensionSegment)> dimensionsAndSegments;

        private IntPtr windowHandle;

        public DimensionTextWindow()
        {
            InitializeComponent();
            ShowInTaskbar = false;
            selectDimensionsHandler = new SelectIndividualDimensionsEventHandler();
            setDimensionsTextHandler = new SetDimensionsTextEventHandler();
            selectDimensionsEvent = ExternalEvent.Create(selectDimensionsHandler);
            setDimensionsTextEvent = ExternalEvent.Create(setDimensionsTextHandler);
            selectDimensionsHandler.SelectionUpdatedEvent += UpdateSelection;
        }

        public void Init(UIDocument uidoc)
        {
            this.uidoc = uidoc;
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

        private void SetDefaultTextsBySelected(List<(Dimension, DimensionSegment)> selected)
        {
            if (selected == null || selected.Count == 0) return;

            bool similarAboveText = true;
            bool similarBelowText = true;

            bool similarValueText = true;
            bool similarValueOverrideText = true;
            bool similarLabelText = true;


            bool similarPrefixText = true;
            bool similarSuffixText = true;

            DimensionAndSegment current;
            DimensionAndSegment next = new DimensionAndSegment(selected[0]);
            if (selected.Count > 1)
            {
                for (int i = 0; i < selected.Count - 1; i++)
                {
                    current = next;
                    next = new DimensionAndSegment(selected[i]);
                    if (similarAboveText && current.Above == next.Above) similarAboveText = false;
                    if (similarBelowText && current.Below == next.Below) similarBelowText = false;

                    if (similarValueText && current.ValueString != next.ValueString) similarValueText = false;
                    if (similarValueOverrideText && current.ValueOverride !=next.ValueOverride) similarValueOverrideText = false;
                    if (similarLabelText && current.LabelText != next.LabelText) similarLabelText = false;

                    if (similarPrefixText && current.Prefix != next.Prefix) similarPrefixText = false;
                    if (similarSuffixText && current.Suffix != next.Suffix) similarSuffixText = false;

                }
            }
            AboveText = similarAboveText ? EmptyIfNull(next.Above) : "";
            BelowText = similarBelowText ? EmptyIfNull(next.Below) : "";

            ValueText = similarValueText ? EmptyIfNull(next.ValueString) : "";
            ValueOverrideText = similarValueOverrideText ? EmptyIfNull(next.ValueOverride) : "";
            LabelText = similarLabelText ? EmptyIfNull(next.LabelText) : "";

            PrefixText = similarPrefixText ? EmptyIfNull(next.Prefix) : "";
            SuffixText = similarAboveText ? EmptyIfNull(next.Suffix) : "";


        
        }

        private string EmptyIfNull(string value) => (value == null) ? "": value;

        /// <summary>
        /// Called when the SelectDimensionEvent finished executing. The current default texts within the UserControl are then updated to match the new selection.
        /// </summary>
        /// <param name="selection">List of Dimension/DimensionSegments that are selected</param>
        public void UpdateSelection(List<(Dimension,DimensionSegment)> selection)
        {
            dimensionsAndSegments = selection;
            SetDefaultTextsBySelected(dimensionsAndSegments);
            if (windowHandle != null ) SetForegroundWindow(windowHandle);
            ShowDialog();
            
        }
        private void ReselectButton_Click(object sender, RoutedEventArgs e)
        {
            selectDimensionsEvent.Raise();
            windowHandle = GetForegroundWindow();
            Hide();
            SetForegroundWindow(uidoc.Application.MainWindowHandle);

        }
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {

            Close();
        }
        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            setDimensionsTextHandler.Inject(dimensionsAndSegments);
            setDimensionsTextHandler.Inject(AboveText, BelowText, ValueOverrideText, PrefixText, SuffixText);
            setDimensionsTextEvent.Raise();
            Close();
        }

        
    }

    class UpdateDimensionTextEventHandler : IExternalEventHandler
    {
        private List<Dimension> selectedDimensions;
        private string above, below, valueOverride, prefix, suffix;
        public void Inject(List<Dimension> selectedDimensions, string above, string below, string valueOverride, string prefix, string suffix) 
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
            if (selectedDimensions == null) return;
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
