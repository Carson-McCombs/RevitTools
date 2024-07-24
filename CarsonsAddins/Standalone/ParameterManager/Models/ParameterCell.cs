using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CarsonsAddins.Shared.EventHandlers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CarsonsAddins.Standalone.ParameterManager.Models
{
    class ParameterCell : INotifyPropertyChanged
    {
        public static SingleCallFunctionEventHandler UpdateParameterEventHandler;
        public event PropertyChangedEventHandler PropertyChanged;
        private bool isSynced = true;
        public bool IsSynced
        {
            get => isSynced;
            set
            {
                if (isSynced == value) return;
                isSynced = value;
            }
        }
        private SolidColorBrush fontColor = new SolidColorBrush(Colors.Black);
        public SolidColorBrush FontColor
        {
            get => fontColor;
            set
            {
                if (value == null) return;
                if (fontColor == value) return;
                fontColor = value;
                OnNotifyPropertyChanged();
            }
        }
        private SolidColorBrush backgroundColor = new SolidColorBrush(Colors.White);
        public SolidColorBrush BackgroundColor
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
                IsSynced = false;
                PushValueToParameter();
                OnNotifyPropertyChanged();
            }
        }


        public ParameterCell(Parameter parameter)
        {
            this.parameter = parameter;
            IsNull = parameter == null;
            // parameterValue = GetParameterValue(parameter);
            ParameterValue = GetParameterValue(parameter);
            IsSynced = true;
            if (IsNull) BackgroundColor = new SolidColorBrush(Colors.LightGray);
            if (!IsNull && parameter.IsReadOnly) FontColor = new SolidColorBrush(Colors.Gray);
        }

        private string GetParameterValue(Parameter parameter)
        {
            if (parameter == null) return null;
            if (StorageType.String.Equals(parameter.StorageType)) return parameter.AsString();
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
        //public void PushValueToParameterEvent()
        //{
        //    UpdateParameterHandler.Instance.handler.Functions += PushValueToParameter;
        //}
        public void PushValueToParameter()
        {
            if (IsNull) return;
            if (parameter.IsReadOnly) return;
            if (ParameterValue == GetParameterValue(parameter)) return;
            //Transaction transaction = new Transaction(doc);
            //transaction.Start("Update " + parameter.Definition.Name + " Cell");
            try
            {
                UpdateParameterEventHandler.Functions += PushValueFromHandler;
                //transaction.Commit();
                //return;
            }
            catch (Exception ex)
            {
                //transaction.RollBack();
                TaskDialog.Show("ParameterCell Error", ex.Message);
                //return;
            }

        }
        public void PushValueFromHandler(Document doc)
        {
            Transaction transaction = new Transaction(doc);
            transaction.Start("Update Parameter");
            try
            {
                if (parameter.StorageType != StorageType.String) parameter.SetValueString(ParameterValue);
                else parameter.Set(ParameterValue);
                IsSynced = true;
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.RollBack();
                TaskDialog.Show("Error Updating Element", ex.Message);
            }
        }
        protected void OnNotifyPropertyChanged([CallerMemberName] string memberName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
        }
    }
}
