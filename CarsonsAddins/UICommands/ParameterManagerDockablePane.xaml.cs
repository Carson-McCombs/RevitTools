using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CarsonsAddins.UICommands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;


namespace CarsonsAddins
{
    /// <summary>
    /// Allows for Pipe End Prep to be set for each side of each Flange / Union FamilyInstance that is currently loaded.
    /// </summary>
    public partial class ParameterManagerDockablePane : Page, IDockablePaneProvider, ISettingsUIComponent, ISettingsUpdaterComponent
    {
        public const bool IsWIP = false;
        public const string FolderName = "";

        private UIDocument uidoc;

        private readonly ParameterTable table;
        private ParameterManagerUpdater updater;

        //private CollectionViewSource collectionViewSource;
        public ParameterManagerDockablePane()
        {
            InitializeComponent();
            CollectionViewSource collectionViewSource = FindResource("ElementRowCollectionViewSource") as CollectionViewSource;
            table = new ParameterTable(SelectionDataGrid, collectionViewSource);
            
        }

        public void Init(UIDocument uidoc)
        {
            table.Clear();
            this.uidoc = uidoc;
        }
        public PushButtonData RegisterButton(Assembly assembly)
        {
            PushButtonData pushButtonData = new PushButtonData("Parameter Manager", "Parameter Manager", assembly.Location, typeof(GenericCommands.ShowDockablePane<ParameterManagerDockablePane>).FullName)
            {
                ToolTip = "An element parameter manager which can be used to sort and set element parameter values."
            };
            return pushButtonData;
        }
        public void RegisterUpdater(AddInId addinId)
        {
            updater = new ParameterManagerUpdater(addinId, ref table.ids);
            updater.Link(this);
        }
        /// <summary>
        /// Unregisters the Stale Reference Updater
        /// </summary>
        public void UnregisterUpdater()
        {
            updater.Unregister();
        }


        /// <summary>
        /// On button press, loads the Elements that the User currently has selected into the Parameter Manager.
        /// </summary>
        private void LoadSelectionButtonPress(object sender, RoutedEventArgs e)
        {
            LoadSelection();
        }

        /// <summary>
        /// Loads the Elements that the User currently has selected through Revit into the Parameter Manager.
        /// </summary>
        public void LoadSelection()
        {
            if (uidoc == null) return;
            table.Clear();
            ParameterNameControl.Text = "";
            List<ElementId> elementIds = uidoc.Selection.GetElementIds() as List<ElementId>;
            if (elementIds.Count == 0) return;

            foreach (ElementId id in elementIds)
            {
                Element elem = uidoc.Document.GetElement(id);
                if (elem == null) continue;
                table.AddElement(elem);
            }

        }


        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.FrameworkElement = this;
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Floating,
            };
        }

        public void RefreshElements(ElementId[] elementIds)
        {
            table.RefreshElements(elementIds);
        }

        /// <summary>
        /// Removes Elements from the Parameter Manager. Called for purposes such as removing stale references.
        /// </summary>
        /// <param name="elementIds">array of ElementIds to be removed.</param>
        public void RemoveElements(ElementId[] elementIds)
        {
            foreach (ElementId id in elementIds)
            {
                table.RemoveRow(id);
            }
        }

        /// <summary>
        /// Adds a new parameter into the table based on the text currently entered into the ParameterNameControl TextField.
        /// </summary>
        private void AddParameterButton(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ParameterNameControl.Text == "") return;
                table.AddParameter(ParameterNameControl.Text);
                ParameterNameControl.Text = "";

            }
            catch (Exception ex)
            {
                TaskDialog.Show("Parameter Manager - Error Adding Parameter", ex.Message);

            }
        }
        /// <summary>
        /// Updates the Elements within the ParameterManager.
        /// </summary>
        private void ApplyButtonPress(object sender, RoutedEventArgs e)
        {
            try
            {
                table.PushUpdatesToElements();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Complex Filter Apply Parameter Changes Error", ex.Message);
            }
        }

        private void SelectHighlightedElements(object sender, RoutedEventArgs e)
        {
            //List<ElementId> selectedIds = SelectionDataGrid.SelectedItems.Cast<ElementRow>().Select(row => row.Id).ToList();

            ElementId[] selectedIds = table.GetSelectedElements();
            if (selectedIds != null && selectedIds.Length != 0) uidoc.Selection.SetElementIds(selectedIds);
        }




        /// <summary>
        /// Gets the ColumnHeader of the MenuItem provided. Will show a TaskDialog if the MenuItem is null or the ContextMenu of the MenuItem is null.
        /// </summary>
        /// <param name="menuItem">The MenuItem whose ColumnHeader will be returned.</param>
        /// <returns>the ColumnHeader of the MenuItem or an empty string if the MenuItem or the ContextMenu of the MenuItem is null.</returns>
        private string GetMenuItemColumnHeader(MenuItem menuItem)
        {
            if (menuItem == null)
            {
                TaskDialog.Show("Menu Item is Null", "");
                return "";
            }
            ContextMenu contextMenu = menuItem.Parent as ContextMenu;
            if (menuItem == null)
            {
                TaskDialog.Show("Context Menu is Null", "");
                return "";
            }
            if (contextMenu.DataContext != null && contextMenu.DataContext.ToString() != null) return contextMenu.DataContext.ToString();
            return "IsSelected";
        }

        /// <summary>
        /// Called from a ContextMenu on a Parameter. Sets the table of elements to be grouped by this parameter.
        /// </summary>
        private void GroupBy_Click(object sender, RoutedEventArgs e)
        {
            string parameterName = GetMenuItemColumnHeader(sender as MenuItem);
            table.SetGroup(parameterName);
        }

        /// <summary>
        /// Clears the current groupings from the table.
        /// </summary>
        private void ClearGroups_Click(object sender, RoutedEventArgs e)
        {
            table.ClearGroups();
        }

        /// <summary>
        /// Called from a ContextMenu on a Parameter. Deletes the Parameter that was clicked.
        /// </summary>
        private void DeleteParameter_Click(object sender, RoutedEventArgs e)
        {
            string parameterName = GetMenuItemColumnHeader(sender as MenuItem);
            if (string.IsNullOrEmpty(parameterName)) return;
            table.RemoveParameter(parameterName);

        }
        private void SelectGroup_CheckBox(object sender, RoutedEventArgs e)
        {
            if (!(sender is CheckBox checkbox)) return;
            if (checkbox.IsChecked == null) return;
            if (!(checkbox.DataContext is CollectionViewGroup group)) return;
            if (group.Items is null) return;
            table.SetSelectedStateOfElements(group.Items.Cast<ElementRow>().ToArray(), (bool)checkbox.IsChecked);
        }


        
        
    }

    class ParameterTracker
    {
        private List<Parameter> allPossibleParameters = new List<Parameter>();
        private ObservableCollection<Parameter> filteredParameterList = new ObservableCollection<Parameter>();
        public ParameterTracker() { }
    }
}