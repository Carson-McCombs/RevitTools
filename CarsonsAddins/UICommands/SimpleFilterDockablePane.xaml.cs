using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using CarsonsAddins;
using CarsonsAddins.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
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
using static CarsonsAddins.PipeEndPrepWindow;

namespace CarsonsAddins
{
    /// <summary>
    /// Interaction logic for SimpleFilterDockablePane.xaml
    /// </summary>
    public partial class SimpleFilterDockablePane : Page, IDockablePaneProvider
    {
        public const bool IsWIP = true;

        UIDocument uidoc;
        public ObservableCollection<CategorySelectionItem> categorySelectionItems = new ObservableCollection<CategorySelectionItem>();
        private int selectedCount = 0;
        //private int totalSelectedCount = 0;
        
        //public string TotalSelectedCount { get => "Total Selected Items:            "  + totalSelectedCount.ToString(); }
        public SimpleFilterDockablePane()
        {
            InitializeComponent();
            //SelectionDataGrid.ItemsSource = categorySelectionItems;
            //TotalSelectedLabel.DataContext = TotalSelectedCount;
            SelectionDataGrid.ItemsSource = categorySelectionItems;
            //categorySelectionItems.CollectionChanged += RefreshSelectedCount;


        }
        public void Init(UIDocument uidoc)
        {
            this.uidoc = uidoc;
        }

        
        public void RefreshSelection()
        {
            if (uidoc == null) return;
            categorySelectionItems.Clear();
            selectedCount = 0;
            Dictionary<BuiltInCategory, CategorySelectionItem> categoriesDictionary = new Dictionary<BuiltInCategory, CategorySelectionItem>();

            List<ElementId> elementIds = uidoc.Selection.GetElementIds() as List<ElementId>;
            if (elementIds.Count == 0)
            {
                return;

            }

            foreach (ElementId id in elementIds)
            {
                try
                {
                    if (id == null) continue;
                    if (ElementId.InvalidElementId.Equals(id)) continue;
                    Element elem = uidoc.Document.GetElement(id);
                    if (elem == null) continue;
                    BuiltInCategory category = elem.Category.BuiltInCategory;

                    if (!categoriesDictionary.ContainsKey(category)) categoriesDictionary.Add(category, new CategorySelectionItem(category.ToString()));
                    categoriesDictionary[category].Add(id);
                }
                catch
                {

                }
                //TaskDialog.Show("Now linked to pane " + categorySelectionItems.Count, "");

            }

            foreach(CategorySelectionItem item in categoriesDictionary.Values)
            {
                categorySelectionItems.Add(item);
            }
        }

        //private void RefreshSelectedCount(object sender, RoutedEventArgs e) => TotalSelectedLabel.Content = "Total Selected Items:            " + CalculateTotalSelectedCount().ToString();

        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.FrameworkElement = this;
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Floating,

            };
        }

        private void RefreshButtonPress(object sender, RoutedEventArgs e)
        {
            RefreshSelection();
        }
        private void CheckAllButtonPress(object sender, RoutedEventArgs e)
        {
            string a = "";
            string b = "";
            foreach (CategorySelectionItem item in categorySelectionItems) a += item.ToString() + '\n';

            SetAllCategoriesToSelectionState(true);
            foreach (CategorySelectionItem item in categorySelectionItems) b += item.ToString() + '\n';

            TaskDialog.Show("Comparing Check All Action", "BEFORE: \n " + a + "\n\nAFTER: \n" + b);
        }

        private void CheckNoneButtonPress(object sender, RoutedEventArgs e)
        {
            string a = "";
            string b = "";
            foreach (CategorySelectionItem item in categorySelectionItems) a += item.ToString() + '\n';


            SetAllCategoriesToSelectionState(false);
            foreach (CategorySelectionItem item in categorySelectionItems) b += item.ToString() + '\n';

            TaskDialog.Show("Comparing Check None Action", "BEFORE: \n " + a + "\n\nAFTER: \n" + b);
        }

        private void ApplySelectionButtonPress(object sender, RoutedEventArgs e)
        {
            List<ElementId> elementIds = GetAllSelectedItems();
            if (elementIds.Count == 0) return;
            uidoc.Selection.SetElementIds(elementIds);
        }
        
        private void OkSelectionButtonPress(object sender, RoutedEventArgs e)
        {
            List<ElementId> elementIds = GetAllSelectedItems();
            if (elementIds.Count == 0) return;
            uidoc.Selection.SetElementIds(elementIds);
            RefreshSelection();
            
        }
        private void CancelSelectionButtonPress(object sender, RoutedEventArgs e)
        {
            categorySelectionItems.Clear();
        }
        private void SetAllCategoriesToSelectionState(bool isSelected)
        {
            for (int i = 0; i < categorySelectionItems.Count; i++)
            {
                CategorySelectionItem item = categorySelectionItems[i];
                item.IsSelected = isSelected;
                categorySelectionItems[i] = item;
            }
            
        }
        private List<ElementId> GetAllSelectedItems()
        {
            List<ElementId> ids = new List<ElementId>();
            foreach (CategorySelectionItem item in categorySelectionItems)
            {
                if (item.IsSelected) ids.AddRange(item.ElementIds);
            }
            return ids;
        }

        private void Category_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            CategorySelectionItem item = cb.DataContext as CategorySelectionItem;
            item.IsSelected = false;

            selectedCount -= item.Count;
            TotalSelectedLabel.Content = "Total Selected Elements    " + selectedCount.ToString();
            //TaskDialog.Show("VALUE CHANGED", "Event: " + item.ToString() + "\nDB: " + GetItem(item.CategoryName).ToString());
        }

        private void Category_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            CategorySelectionItem item = cb.DataContext as CategorySelectionItem;
            item.IsSelected = true;
            selectedCount += item.Count;
            TotalSelectedLabel.Content = "Total Selected Elements    " + selectedCount.ToString();
        }
        
    }

    public class CategorySelectionItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private bool isSelected = false;
        public bool IsSelected { 
            get => isSelected; 
            set {
                if (isSelected == value) return;
                isSelected = value; 
                OnNotifyPropertyChanged();
            }
        }
        private readonly string categoryName = "Null Name";
        public string CategoryName { get => categoryName; }
        public int Count { get => elementIds.Count; }
        private readonly List<ElementId> elementIds = new List<ElementId>();

        

        public List<ElementId> ElementIds { get => elementIds; }
        public CategorySelectionItem(string categoryName)
        {
            this.categoryName = categoryName;
        }
        public CategorySelectionItem(bool isSelected, string categoryName)
        {
            this.isSelected = isSelected;
            this.categoryName = categoryName;
        }
        public void Add(ElementId id)
        {
            elementIds.Add(id);
        }
        protected void OnNotifyPropertyChanged([CallerMemberName] string memberName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
        }
        public override string ToString()
        {
            return categoryName + ": " + (IsSelected ? "Selected" : "Not Selected") + " with " + Count.ToString() + " items";
        }
    }

}
