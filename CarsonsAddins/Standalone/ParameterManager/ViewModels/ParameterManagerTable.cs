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
using CarsonsAddins.Utils;
using System.Windows.Media;
using CarsonsAddins.Shared.EventHandlers;
using CarsonsAddins.Standalone.ParameterManager.Models;

namespace CarsonsAddins.Standalone.ParameterManager.ViewModels
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
        private ExternalEvent updateParametersEvent;
        public ParameterTable(DataGrid dataGrid, CollectionViewSource collectionViewSource)
        {
            this.dataGrid = dataGrid;
            this.collectionViewSource = collectionViewSource;
            collectionViewSource.Source = rows;
            ParameterCell.UpdateParameterEventHandler = new SingleCallFunctionEventHandler();
            updateParametersEvent = ExternalEvent.Create(ParameterCell.UpdateParameterEventHandler);
            dataGrid.CurrentCellChanged += UpdateChangedCells;
            dataGrid.MouseLeave += UpdateChangedCells;
        }

        private void UpdateChangedCells(object sender, EventArgs e)
        {
            updateParametersEvent.Raise();
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
                Parameter parameter = rows[i].element.LookupParameter(parameterName);
                if (parameter == null) continue;
                StorageType st = parameter.StorageType;
                definition = parameter.Definition;
                parameterReadOnly.Add((st == StorageType.ElementId) || (parameter.IsReadOnly));
                parameterReturnTypes.Add(st);
                parameterDefinitions.Add(definition);
                break;
            }
            if (definition == null) return;
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].AddParameter(parameterName);
            }
            
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
            
            Style style = new Style();
            Setter backgroundSetter = new Setter
            {
                Property = System.Windows.Controls.Control.BackgroundProperty,
                Value = new Binding("cells[" + parameterName + "].BackgroundColor")
                {
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                }
            };
            style.Setters.Add(backgroundSetter);
            Setter fontColorSetter = new Setter
            {
                Property = System.Windows.Controls.Control.ForegroundProperty,
                Value = new Binding("cells[" + parameterName + "].FontColor")
                {
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                }
            };
            style.Setters.Add(fontColorSetter);

            DataGridTextColumn column = new DataGridTextColumn
            {
                //Sets the parameter column generic binding to each table cell in the column to show its parameter value
                Binding = new Binding("cells[" + parameterName + "].ParameterValue")
                {
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                },
                
                Header = parameterName,
                IsReadOnly = isReadOnly,
                CellStyle = style,
            };
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
}