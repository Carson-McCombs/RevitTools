using Autodesk.Revit.DB;
using CarsonsAddins.Settings.Dimensioning.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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


namespace CarsonsAddins.Settings.Dimensioning.ViewModels
{
    public partial class DimensionTypeSelectorViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private DimensionPreferences dimensionPreferences;

        public DimensionType PrimaryDimensionType
        {
            get => dimensionPreferences?.primaryDimensionType;
            set
            {
                if (value == null || value == dimensionPreferences.primaryDimensionType) return;
                dimensionPreferences.primaryDimensionType = value;
                OnNotifyPropertyChanged();
            }
        }
        public DimensionType SecondaryPipeDimensionType
        {
            get => dimensionPreferences?.secondaryPipeDimensionType;
            set
            {
                if (value == null || value == dimensionPreferences.secondaryPipeDimensionType) return;
                dimensionPreferences.secondaryPipeDimensionType = value;
                OnNotifyPropertyChanged();
            }
        }
        public DimensionType SecondaryFittingDimensionType
        {
            get => dimensionPreferences?.secondaryFittingDimensionType;
            set
            {
                if (value == null || value == dimensionPreferences.secondaryFittingDimensionType) return;
                dimensionPreferences.secondaryFittingDimensionType = value;
                OnNotifyPropertyChanged();
            }
        }
        public DimensionType SecondaryAccessoryDimensionType
        {
            get => dimensionPreferences?.secondaryAccessoryDimensionType;
            set
            {
                if (value == null || value == dimensionPreferences.secondaryAccessoryDimensionType) return;
                dimensionPreferences.secondaryAccessoryDimensionType = value;
                OnNotifyPropertyChanged();
            }
        }
        public DimensionType SecondaryOtherDimensionType
        {
            get => dimensionPreferences?.secondaryOtherDimensionType;
            set
            {
                if (value == null || value == dimensionPreferences.secondaryOtherDimensionType) return;
                dimensionPreferences.secondaryOtherDimensionType = value;
                OnNotifyPropertyChanged();
            }
        }

        public DimensionType[] dimensionTypes = new DimensionType[0];
        public DimensionType[] DimensionTypes 
        { 
            get => dimensionTypes; 
            set 
            {
                if (value == null || dimensionTypes == value) return;
                dimensionTypes = value; 
                OnNotifyPropertyChanged();
            }

        }
        public DimensionTypeSelectorViewModel(ref DimensionType[] dimensionTypes, ref DimensionPreferences currentPreferences)
        {
            DimensionTypes = dimensionTypes;
            dimensionPreferences = currentPreferences;
            NotifyIntialized();
        }

        private void NotifyIntialized()
        {
            if (dimensionPreferences == null) return;
            OnNotifyPropertyChanged(nameof(PrimaryDimensionType));
            OnNotifyPropertyChanged(nameof(SecondaryPipeDimensionType));
            OnNotifyPropertyChanged(nameof(SecondaryFittingDimensionType));
            OnNotifyPropertyChanged(nameof(SecondaryAccessoryDimensionType));
            OnNotifyPropertyChanged(nameof(SecondaryOtherDimensionType));
        }

        protected void OnNotifyPropertyChanged([CallerMemberName] string memberName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
        }
    }
}
