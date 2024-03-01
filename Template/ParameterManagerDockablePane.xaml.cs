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
using System.Configuration.Assemblies;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
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
using static CarsonsAddins.Util;

using Binding = System.Windows.Data.Binding;
//using DataGrid = System.Windows.Controls.DataGrid;

namespace CarsonsAddins
{
    /// <summary>
    /// Interaction logic for SimpleFilterDockablePane.xaml
    /// </summary>
    public partial class ParameterManagerDockablePane : Page, IDockablePaneProvider, ISettingsUIComponent, ISettingsUpdaterComponent
    {
        public const bool IsWIP = false;

        private UIDocument uidoc;
        
        private ParameterTable table;
        private StaleReferenceUpdater updater;
        
        public ParameterManagerDockablePane()
        {
            InitializeComponent();
            table = new ParameterTable(SelectionDataGrid);
            ParameterGroupComboBox.ItemsSource = table.columnNames;
        }
        
        public void Init(UIDocument uidoc)
        {
            table.Clear();
            this.uidoc = uidoc;
            new ParametersByTypeId(uidoc);
        }
        public PushButtonData RegisterButton(Assembly assembly)
        {
            PushButtonData pushButtonData = new PushButtonData("Parameter Manager", "Parameter Manager", assembly.Location, typeof(ShowDockablePane<ParameterManagerDockablePane>).FullName);
            pushButtonData.ToolTip = "An element parameter manager which can be used to sort and set element parameter values.";
            return pushButtonData;
        }
        public void RegisterUpdater(AddInId addinId)
        {
            updater = new StaleReferenceUpdater(addinId, ref table.ids);
            updater.Link(this);
        }

        public void UnregisterUpdater()
        {
            updater.Unregister();
        }
        private void LoadSelectionButtonPress(object sender, RoutedEventArgs e)
        {
            LoadSelection();
        }
        public void LoadSelection()
        {
            if (uidoc == null) return;
            table.Clear();
            ParameterGroupComboBox.SelectedItem = null;
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


        public void RemoveStaleReference(ElementId[] elementIds )
        {
            foreach (ElementId id in elementIds)
            {
                table.RemoveRow(id);
            }
        }

        private void AddParameterButton(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ParameterNameControl.Text == "") return;
                table.AddParameter(ParameterNameControl.Text);
            } catch (Exception ex)
            {
                TaskDialog.Show("Parameter Manager - Error Adding Parameter", ex.Message);

            }
        }

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
            List<ElementId> selectedIds = new List<ElementId>();
            foreach(var item in SelectionDataGrid.SelectedItems)
            {
                selectedIds.Add((item as ParameterRow).Id);
            }
            if (selectedIds == null)
            {
                TaskDialog.Show("SELECT HIGHLIGHTED ELEMENTS", "SELECTED ITEMS IS NULL");
                return;
            };
            if (selectedIds.Count == 0)
            {
                TaskDialog.Show("SELECT HIGHLIGHTED ELEMENTS", "SELECTED ITEMS IS EMPTY");
                return;
            }
            uidoc.Selection.SetElementIds(selectedIds);
        }

        private void AddGroupButton(object sender, RoutedEventArgs e)
        {
            ICollectionView cvTasks = CollectionViewSource.GetDefaultView(SelectionDataGrid.ItemsSource);
            if (cvTasks == null) return;
            if (ParameterGroupComboBox.SelectedItem == null) return;
            cvTasks.GroupDescriptions.Clear();
            cvTasks.GroupDescriptions.Add(new GroupParameterValueProperty("cells[" + ParameterGroupComboBox.SelectedItem.ToString() + "]"));
        }

        
    }

    //  CURRENTLY UNUSED
    //  Purpose: to act as a buffer for retrieving element parameters. Will attempt to retrieve the a via parameter if no definition is saved, otherwise retrieving the parameter through the definition.
    class ParametersByTypeId
    {
        public static ParametersByTypeId instance;
        private UIDocument uidoc;
        private Dictionary<ElementId, List<Definition>> parameterDictionary = new Dictionary<ElementId, List<Definition>>();
        public ParametersByTypeId(UIDocument uidoc)
        {
            instance = this;
            this.uidoc = uidoc;
        }

        public List<Definition> GetParameterDefinitions(ElementId id, ElementId familyId, Definition[] ignore )
        {
            
            if (familyId == null) return null;
            if (parameterDictionary.ContainsKey(familyId))
            {
                List<Definition> definitions = parameterDictionary[familyId];
                definitions.RemoveAll(it => ignore.Contains(it));
                return definitions;
            }
            if (id == null) return null;
            if (uidoc == null) return null;
            Element element = uidoc.Document.GetElement(id);
            if (element == null) return null;
            List<Parameter> parameters = element.GetOrderedParameters() as List<Parameter>;
            if (parameters == null) return null;
            if (parameters.Count == 0) return null;
            List<Definition> parameterDefinitions = new List<Definition>();
            foreach(Parameter parameter in parameters)
            {
                parameterDefinitions.Add(parameter.Definition);
            }
            parameterDictionary.Add(familyId, parameterDefinitions);
            parameterDefinitions.RemoveAll(it => ignore.Contains(it));
            return parameterDefinitions;
        }
        
    }

    
    /// <summary>
    /// Abstracts the parameters and element data and interactions with the WPF DataGrid into its own class.
    /// </summary>
    class ParameterTable
    {
        public List<ElementId> ids = new List<ElementId>();
        public ParameterRowsCollection rows = new ParameterRowsCollection();
        public ObservableCollection<string> columnNames = new ObservableCollection<string>();
        List<Definition> parameterDefinitions = new List<Definition>();
        List<bool> parameterReadOnly = new List<bool>(); 
        List<StorageType> parameterReturnTypes = new List<StorageType>();
        private DataGrid dataGrid;
        public ParameterTable(DataGrid dataGrid)
        {
            this.dataGrid = dataGrid;
            dataGrid.ItemsSource = rows;
        }

        
        public bool ContainsColumn(string columnName) => columnNames.Contains(columnName);
        public void Clear()
        {
            ids.Clear();
            rows.Clear();
            columnNames.Clear();
            parameterDefinitions.Clear();
            parameterReadOnly.Clear();
            
            CollectionViewSource.GetDefaultView(rows).GroupDescriptions.Clear();
            for (int i = dataGrid.Columns.Count - 1; i >= 1; i--)
            { 
                dataGrid.Columns.RemoveAt(i);
                
            }
        }
        public void AddElement(Element element)
        {
            if (element is null) return;
            if (ids.Contains(element.Id)) return;
            ids.Add(element.Id);
            ParameterRow row = new ParameterRow(element);
            
           
            foreach (Definition definition in parameterDefinitions)
            {
                row.AddParameter(definition);
            }
            rows.Add(row);
            
        }
        public void AddParameter(Definition definition)
        {
            if (definition is null) return;
            
            for (int i = 0; i < rows.Count; i++)
            {
                ParameterRow row = rows[i];
                Parameter parameter = row.AddParameter(definition);
                if ( parameter == null) return;
                if (i == 0) {
                   
                    StorageType st = parameter.StorageType;
                    bool readOnly = (st == StorageType.ElementId) ? true : parameter.IsReadOnly;
                    parameterReadOnly.Add(readOnly);
                    parameterReturnTypes.Add(st);

                }
                rows[i] = row;
            }
            columnNames.Add(definition.Name);
            parameterDefinitions.Add(definition);
            AddColumn(definition.Name, parameterReadOnly.Last());
        }
        public void AddParameter(string parameterName)
        {
            if (parameterName is null) return;
            Definition definition = null;
            for (int i = 0; i < rows.Count; i++)
            {
                
                ParameterRow row = rows[i];
                if (definition == null)
                {
                    Parameter parameter = row.element.LookupParameter(parameterName);
                    if (parameter == null) continue;
                    definition = parameter.Definition;
                    row.AddParameter(definition);
                    rows[i] = row;
                    StorageType st = parameter.StorageType;
                    bool readOnly = (st == StorageType.ElementId) ? true : parameter.IsReadOnly;
                    parameterReadOnly.Add(readOnly);
                    parameterReturnTypes.Add(st);
                    parameterDefinitions.Add(definition);
                }
                else
                {
                    Parameter parameter = row.AddParameter(definition);
                    if (parameter == null) continue;
                    
                }
                rows[i] = row;
            }
            if (definition == null) return;
            columnNames.Add(definition.Name);
            parameterDefinitions.Add(definition);
            AddColumn(definition.Name, parameterReadOnly.Last());
        }
        /// <summary>
        /// Adds a new column to the datagrid. This new column will be bound to a specific parameter, such that each cell within that column corresponds to a different element's parameter value.
        /// </summary>
        /// <param name="parameterName">Name of the Parameter that the column will bind to. Also is the header of the column.</param>
        /// <param name="isReadOnly">Whether or not the Parameter being bound to is readonly.</param>
        private void AddColumn(string parameterName, bool isReadOnly)
        {
            DataGridTextColumn column = new DataGridTextColumn();
            //Sets the parameter column generic binding to each table cell in the column to show its parameter value
            column.Binding = new Binding("cells[" + parameterName + "].ParameterValue");
            column.Header = parameterName;
            column.IsReadOnly = isReadOnly;
            //If the parameter is not readonly, bind to the cell object's backrgound color property
            if (!isReadOnly)
            {
                Style style = new Style();
                Setter backgroundSetter = new Setter();
                backgroundSetter.Property = DataGridCell.BackgroundProperty;
                backgroundSetter.Value = new Binding("cells[" + parameterName + "].BackgroundColor");
                style.Setters.Add(backgroundSetter);
                column.CellStyle = style;
            }
            dataGrid.Columns.Add(column);
        }

        /// <summary>
        /// Recursive update element parameter values.
        /// </summary>
        public void PushUpdatesToElements()
        {
            try
            {
                foreach (ParameterRow row in rows)
                {
                    row.PushUpdatesToElement();
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Parameter Table Error", ex.Message);
            }
            
        }

        /// <summary>
        /// Removes element from parameter. This can be due to altering the selection or removing a stale reference.
        /// </summary>
        /// <param name="id">ElementId of the element to be removed.</param>
        public void RemoveRow(ElementId id)
        {
            if (!ids.Contains(id)) return;
            rows.RemoveAt(ids.IndexOf(id));
            ids.Remove(id);
        }
    }

    class ParameterRowsCollection : ObservableCollection<ParameterRow> { }

    /// <summary>
    /// Currently binding and tracking changes via the INotifyPropertyChanged base class. Will eventually move to binding via DependencyProperties for speed increase.
    /// </summary>
    class ParameterRow : INotifyPropertyChanged // 1 to 1: Element to Row
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Element element { get; private set; }
        private ElementId id;
        public ElementId Id 
        { 
            get => id; 
            set{ 
                if (id == value) return;
                id = value;
            }
        }
        public Dictionary<string, ParameterCell> cells { private set; get; }
        public ParameterRow(Element element)
        {
            cells = new Dictionary<string, ParameterCell>();
            this.element = element;
            Id = element.Id;
        }
        public Parameter AddParameter(Definition definition)
        {
            if (element == null) return null;
            Parameter parameter = element.get_Parameter(definition); 
            cells.Add(definition.Name, new ParameterCell(parameter));
            return parameter;
        }
        public string GetCellValue(string parameterName)
        {
            if (parameterName == null) return "";
            ParameterCell cell;
            if (!cells.TryGetValue(parameterName, out cell)) return "";
            return cell.ParameterValue;
        }

        public void PushUpdatesToElement()
        {
            try
            {
                foreach (string parameterName in cells.Keys)
                {


                    ParameterCell cell = cells[parameterName];
                    if (cell.IsSynced) continue;
                    if (cell.IsNull) continue; 
                    Parameter parameter = cell.PushValueToParameter();
                }
            }
            catch(Exception ex) {
                TaskDialog.Show("Parameter Row Error", ex.Message);
            }
        }



        protected void OnNotifyPropertyChanged([CallerMemberName] string memberName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(memberName));
            }
        }
    }


    class ParameterCell : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool isSynced = true;
        public bool IsSynced
        {
            get => isSynced;
            set
            {
                if (isSynced == value) return;
                isSynced = value;
                if (isSynced) BackgroundColor = Brushes.White; 
                else BackgroundColor = Brushes.Orange;
                OnNotifyPropertyChanged();
            }
        }
        private Brush backgroundColor = Brushes.White;
        public Brush BackgroundColor 
        { 
            get => backgroundColor;
            set
            {
                if (value == null) return;
                if (backgroundColor == value) return;
                backgroundColor = value;
                OnNotifyPropertyChanged();
            }
        }
        public bool IsNull = true;
        private Parameter parameter;
        private string parameterValue = "";
        public string ParameterValue { 
            get => parameterValue;
            set
            {
                if (value == null) return;
                if (parameterValue.Equals(value)) return;
                if (IsNull) return;
                parameterValue = value;
                BackgroundColor = Brushes.Orange;
                IsSynced = false;
                OnNotifyPropertyChanged();
            }
        }
        

        public ParameterCell(Parameter parameter)
        {
            this.parameter = parameter;
            IsNull = parameter == null;
            ParameterValue = GetParameterValue(parameter);
            IsSynced = true;
            if (IsNull) BackgroundColor = Brushes.Gray;
        }

        private string GetParameterValue(Parameter parameter)
        {
            if (parameter == null) return null;
            IsSynced = true;
            switch (parameter.StorageType)
            {
                case StorageType.String: return parameter.AsValueString();
                case StorageType.Integer: return parameter.AsInteger().ToString();
                case StorageType.Double: return parameter.AsDouble().ToString();
                case StorageType.ElementId: return parameter.AsValueString().ToString();
                default: return null;
            }

        }
        public Parameter PushValueToParameter()
        {
            if (IsNull) return null;
            IsSynced = true;
            try
            {
                switch (parameter.StorageType)
                {
                    case StorageType.String:
                        {
                            parameter.Set(parameterValue);
                            return parameter;
                        }
                    case StorageType.Integer:
                        {
                            int value;
                            int.TryParse(parameterValue, out value);
                            parameter.Set(value);
                            return parameter;
                        }
                    case StorageType.Double:
                        {
                            double value;
                            double.TryParse(parameterValue, out value);
                            parameter.Set(value);
                            return parameter;
                        }
                    case StorageType.ElementId:
                        {
                            ElementId value;
                            ElementId.TryParse(parameterValue, out value);
                            parameter.Set(value);
                            return parameter;
                        }
                    default: return parameter;
                }
            }catch (Exception ex) {
                TaskDialog.Show("ParameterCell Error", ex.Message);
            }
            return parameter;
        }
        protected void OnNotifyPropertyChanged([CallerMemberName] string memberName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(memberName));
            }
        }
    }


    class GroupParameterValueProperty : PropertyGroupDescription
    { 
        public GroupParameterValueProperty (string groupName) : base(groupName) { }

        public override object GroupNameFromItem(object item, int level, CultureInfo culture)
        {
            var baseItem = base.GroupNameFromItem(item, level, culture);
            ParameterCell parameterCell = baseItem as ParameterCell;
            if (parameterCell.IsNull) return "No Parameter";
            else if (string.IsNullOrEmpty(parameterCell.ParameterValue)) return "Empty";
            return parameterCell.ParameterValue;
        }

    }




}
