using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;
using System.Drawing;
using Binding = System.Windows.Data.Binding;

namespace CarsonsAddins.UICommands
{

    /// <summary>
    /// Abstracts the parameters and element data and interactions with the WPF DataGrid into its own class.
    /// </summary>
    class ParameterTable
    {

        public List<ElementId> ids = new List<ElementId>();
        public ElementRowCollection rows = new ElementRowCollection();
        public ObservableCollection<string> columnNames = new ObservableCollection<string>();
        private readonly List<Definition> parameterDefinitions = new List<Definition>();
        private readonly List<bool> parameterReadOnly = new List<bool>();
        private readonly List<StorageType> parameterReturnTypes = new List<StorageType>();
        private readonly DataGrid dataGrid;
        private readonly CollectionViewSource collectionViewSource;
        private string currentGroupName = "";
        public ParameterTable(DataGrid dataGrid, CollectionViewSource collectionViewSource)
        {
            this.dataGrid = dataGrid;
            this.collectionViewSource = collectionViewSource;
            //dataGrid.ItemsSource = rows;
            collectionViewSource.Source = rows;
        }

        /// <summary>
        /// Checks if the Table contains a Column with the provided column name.
        /// </summary>
        /// <param name="columnName">the name of the Column being checked.</param>
        /// <returns>True if the Table contains the corresponding Column and false if not.</returns>
        public bool ContainsColumn(string columnName) => columnNames.Contains(columnName);

        /// <summary>
        /// Clears the Table of all of its data.
        /// </summary>
        public void Clear()
        {
            ids.Clear();
            rows.Clear();
            columnNames.Clear();
            parameterDefinitions.Clear();
            parameterReadOnly.Clear();
            collectionViewSource.GroupDescriptions.Clear();
            currentGroupName = "";
            //CollectionViewSource.GetDefaultView(rows).GroupDescriptions.Clear();
            for (int i = dataGrid.Columns.Count - 1; i >= 2; i--)
            {
                dataGrid.Columns.RemoveAt(i);

            }
        }
        public void SetGroup(string groupName)
        {
            if (groupName == "ElementId") return;
            if (collectionViewSource == null) return;


            ClearGroups();
            PropertyGroupDescription groupDescription;
            if ((groupName == "IsSelected"))
            {
                groupDescription = (new GroupIsSelectedProperty());
            }
            else
            {
                groupDescription = (new GroupParameterValueProperty("cells[" + groupName + "]"));
            }

            collectionViewSource.GroupDescriptions.Add(groupDescription);
            currentGroupName = groupName;
        }
        public void ClearGroups()
        {
            if (collectionViewSource == null) return;
            currentGroupName = "";
            collectionViewSource.GroupDescriptions.Clear();
        }
        public void RefreshElements(params ElementId[] elementIds)
        {
            Array.ForEach(elementIds, id => RefreshElement(id));
        }
        public void RefreshElement(ElementId id)
        {
            if (ElementId.InvalidElementId.Equals(id)) return;
            int index = ids.IndexOf(id);
            if (index == -1) return;
            rows[index].Refresh();
        }
        public void Refresh()
        {
            foreach (ElementRow row in rows)
            {
                row.Refresh();
            }
        }
        public void SetSelectedStateOfElements(ElementRow[] elementRows, bool isSelected)
        {
            Array.ForEach(elementRows, row => row.IsSelected = isSelected);
        }
        public ElementId[] GetSelectedElements()
        {
            return rows.Where(row => row.IsSelected && !ElementId.InvalidElementId.Equals(row.Id)).Select(row => row.Id).ToArray();
        }

        /// <summary>
        /// Adds an Element to the Table as a Row.
        /// </summary>
        /// <param name="element">Element being added to the Table.</param>
        public void AddElement(Element element)
        {
            if (element is null) return;
            if (ids.Contains(element.Id)) return;
            ids.Add(element.Id);
            ElementRow row = new ElementRow(element);

            foreach (Definition definition in parameterDefinitions)
            {
                row.AddParameter(definition);
            }
            rows.Add(row);
        }

        /// <summary>
        /// Adds a Parameter by name to the Table as a Column. WARNING: it is possible that there are two different Parameters with the same name, in which case the one found first is used.
        /// </summary>
        /// <param name="parameterName">The name of the Parameter being added.</param>
        public void AddParameter(string parameterName)
        {
            if (parameterName is null) return;
            Definition definition = null;

            //Iterates through each Element in the Table until an Element containing a Parameter sharing the name is found.
            for (int i = 0; i < rows.Count; i++)
            {

                ElementRow row = rows[i];
                if (definition == null)
                {
                    Parameter parameter = row.element.LookupParameter(parameterName);
                    if (parameter == null) continue;
                    definition = parameter.Definition;
                    row.AddParameter(definition);
                    rows[i] = row;
                    StorageType st = parameter.StorageType;
                    bool readOnly = (st == StorageType.ElementId) || parameter.IsReadOnly;
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
            DataGridTextColumn column = new DataGridTextColumn
            {
                //Sets the parameter column generic binding to each table cell in the column to show its parameter value
                Binding = new Binding("cells[" + parameterName + "].ParameterValue"),
                Header = parameterName,
                IsReadOnly = isReadOnly
            };
            Style style = new Style();
            Setter backgroundSetter = new Setter
            {
                Property = DataGridCell.BackgroundProperty,
                Value = new Binding("cells[" + parameterName + "].BackgroundColor")
            };

            style.Setters.Add(backgroundSetter);
            column.CellStyle = style;
            dataGrid.Columns.Add(column);
        }


        /// <summary>
        /// Removes a parameter column from the datagrid.
        /// </summary>
        /// <param name="parameterName">Name of the Parameter that will be removed. Also is the header of the column that will be removed alongside.</param>
        public void RemoveParameter(string parameterName)
        {
            if (!columnNames.Contains(parameterName)) return;
            if (currentGroupName == parameterName) return;
            int parameterIndex = columnNames.IndexOf(parameterName);
            DataGridColumn column = dataGrid.Columns.Where(col => col.Header != null && GroupParameterValueProperty.GetGroupName(parameterName).Equals(col.Header.ToString())).First();
            if (column is null) return;
            dataGrid.Columns.Remove(column);
            parameterDefinitions.RemoveAt(parameterIndex);
            columnNames.RemoveAt(parameterIndex);
            parameterReturnTypes.RemoveAt(parameterIndex);
            parameterReadOnly.RemoveAt(parameterIndex);
            foreach (ElementRow row in rows)
            {
                row.RemoveParameter(parameterName);
            }

        }
        /// <summary>
        /// Recursive update element parameter values.
        /// </summary>
        public void PushUpdatesToElements()
        {
            try
            {
                foreach (ElementRow row in rows)
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

    class ElementRowCollection : ObservableCollection<ElementRow> { }

    /// <summary>
    /// Stores and References data of a single Element. Currently binding and tracking changes via the INotifyPropertyChanged base class. Will eventually move to binding via DependencyProperties for speed increase.
    /// </summary>
    class ElementRow : INotifyPropertyChanged // 1 to 1: Element to Row
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Element element { get; private set; }
        private readonly ElementId id = ElementId.InvalidElementId;
        public ElementId Id { get => id; }
        private bool isSelected = false;
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected == value) return;
                isSelected = value;
                OnNotifyPropertyChanged();
            }
        }

        public Dictionary<string, ParameterCell> cells { private set; get; }
        public ElementRow(Element element)
        {
            cells = new Dictionary<string, ParameterCell>();
            this.element = element;
            id = element.Id;
        }

        public void Refresh()
        {
            foreach (ParameterCell cell in cells.Values)
            {
                cell.Refresh();
            }
        }

        public bool HasParameterValue(string parameterName, string valueString)
        {
            if (valueString is null) return false;
            if (parameterName == "IsSelected") return (GroupIsSelectedProperty.GetGroupName(IsSelected) == valueString);
            if (!cells.ContainsKey(parameterName)) return false;
            ParameterCell cell = cells[parameterName];
            if (cell is null) return false;
            return valueString.Equals(GroupParameterValueProperty.GetGroupName(cell));
        }

       

        public Parameter AddParameter(Definition definition)
        {
            if (element == null) return null;
            Parameter parameter = element.get_Parameter(definition);
            cells.Add(definition.Name, new ParameterCell(parameter));
            return parameter;
        }
        public Parameter AddParameter(string parameterName)
        {
            if (element == null) return null;
            if (parameterName == null) return null;
            Parameter parameter = element.LookupParameter(parameterName);
            cells.Add(parameterName, new ParameterCell(parameter));
            return parameter;
        }
        public void RemoveParameter(string parameterName)
        {
            if (!cells.ContainsKey(parameterName)) return;
            cells.Remove(parameterName);
        }
        public string GetCellValue(string parameterName)
        {
            if (parameterName == null) return "";
            if (!cells.TryGetValue(parameterName, out ParameterCell cell)) return "";
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
            catch (Exception ex)
            {
                TaskDialog.Show("Parameter Row Error", ex.Message);
            }
        }



        protected void OnNotifyPropertyChanged([CallerMemberName] string memberName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
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
                //OnNotifyPropertyChanged();
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
        private readonly Parameter parameter;
        private string parameterValue = "";
        public string ParameterValue
        {
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
            return parameter.AsValueString();
            /*switch (parameter.StorageType)
            {
                case StorageType.String: return parameter.AsValueString();
                case StorageType.Integer: return parameter.AsInteger().ToString();
                case StorageType.Double: return parameter.AsDouble().ToString();
                case StorageType.ElementId: return parameter.AsValueString().ToString();
                default: return null;
            }*/

        }
        public void Refresh()
        {
            if (IsNull) return;
            ParameterValue = GetParameterValue(parameter);
            IsSynced = true;
        }
        public Parameter PushValueToParameter()
        {
            if (IsNull) return null;
            IsSynced = true;

            try
            {
                if (parameter.StorageType != StorageType.String) parameter.SetValueString(ParameterValue);
                else parameter.Set(ParameterValue);
                /*
                switch (parameter.StorageType)
                {
                    case StorageType.String:
                        {
                            parameter.Set(parameterValue);
                            return parameter;
                        }
                    case StorageType.Integer:
                        {
                            int.TryParse(parameterValue, out int value);
                            parameter.Set(value);
                            return parameter;
                        }
                    case StorageType.Double:
                        {
                            double.TryParse(parameterValue, out double value);
                            parameter.Set(value);
                            return parameter;
                        }
                    case StorageType.ElementId:
                        {
                            ElementId.TryParse(parameterValue, out ElementId value);
                            parameter.Set(value);
                            return parameter;
                        }
                    default: return parameter;
                }*/
            }
            catch (Exception ex)
            {
                TaskDialog.Show("ParameterCell Error", ex.Message);
            }
            return parameter;
        }
        protected void OnNotifyPropertyChanged([CallerMemberName] string memberName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
        }
    }
    class GroupParameterValueProperty : PropertyGroupDescription
    {
        public GroupParameterValueProperty(string propertyName) : base(propertyName) 
        { 
            PropertyName = propertyName; 
        }

        public override object GroupNameFromItem(object item, int level, CultureInfo culture)
        {
            var baseItem = base.GroupNameFromItem(item, level, culture);
            ParameterCell parameterCell = baseItem as ParameterCell;
            return GetGroupName(parameterCell);
        }

        public static string GetGroupName(ParameterCell parameterCell)
        {
            if (parameterCell.IsNull) return "No Parameter";
            else if (string.IsNullOrEmpty(parameterCell.ParameterValue)) return "Empty";
            return parameterCell.ParameterValue;
        }
        public static string GetGroupName(string parameterName)
        {
            return (string.IsNullOrEmpty(parameterName)) ? "Empty" : parameterName;
        }

    }

    class GroupIsSelectedProperty : PropertyGroupDescription
    {
        public GroupIsSelectedProperty() : base("IsSelected") { }
        public override object GroupNameFromItem(object item, int level, CultureInfo culture)
        {
            var baseItem = base.GroupNameFromItem(item, level, culture);
            if (!(baseItem is bool isSelected) ) return "NULL";
            return GetGroupName(isSelected);
        }
        public static string GetGroupName(bool isSelected) 
        {
            return isSelected ? "Selected" : "Not Selected";
        }
    }




}