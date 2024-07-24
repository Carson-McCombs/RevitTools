using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CarsonsAddins.Standalone.ParameterManager.Models
{
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
        public void Refresh(string parameterName)
        {
            if (cells.ContainsKey(parameterName)) cells[parameterName].Refresh();
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
        public void PushUpdatesToElement(string parameterName)
        {
            try
            {
                if (!cells.ContainsKey(parameterName)) return;
                ParameterCell cell = cells[parameterName];
                if (cell.IsSynced) return;
                if (cell.IsNull) return;
                cell.PushValueToParameter();

            }
            catch (Exception ex)
            {
                TaskDialog.Show("Parameter Row Error", ex.Message);
            }
        }
        public void PushUpdatesToElement()
        {
            foreach (string parameterName in cells.Keys)
            {
                PushUpdatesToElement(parameterName);


            }
        }

        protected void OnNotifyPropertyChanged([CallerMemberName] string memberName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
        }
    }
}
