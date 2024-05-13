using Autodesk.Revit.DB;
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
using static CarsonsAddins.Utils.DimensioningUtils;

namespace CarsonsAddins
{
    /// <summary>
    /// Interaction logic for DimensionTypeSelectorControl.xaml
    /// </summary>
    public partial class DimensionTypeSelectorControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private DimensionStyles dimensionStyles;

        private DimensionType primaryDimensionType;
        public DimensionType PrimaryDimensionType
        {
            get => primaryDimensionType;
            set
            {
                if (value == null || value == primaryDimensionType) return;
                primaryDimensionType = value;
                dimensionStyles.primaryDimensionType = primaryDimensionType;
                OnNotifyPropertyChanged();
            }
        }
        private DimensionType secondaryPipeDimensionType;
        public DimensionType SecondaryPipeDimensionType
        {
            get => secondaryPipeDimensionType;
            set
            {
                if (value == null || value == secondaryPipeDimensionType) return;
                secondaryPipeDimensionType = value;
                dimensionStyles.secondaryPipeDimensionType = secondaryPipeDimensionType;
                OnNotifyPropertyChanged();
            }
        }
        private DimensionType secondaryFittingDimensionType;
        public DimensionType SecondaryFittingDimensionType
        {
            get => secondaryFittingDimensionType;
            set
            {
                if (value == null || value == secondaryFittingDimensionType) return;
                secondaryFittingDimensionType = value;
                dimensionStyles.secondaryFittingDimensionType = secondaryFittingDimensionType;
                OnNotifyPropertyChanged();
            }
        }
        private DimensionType secondaryAccessoryDimensionType;
        public DimensionType SecondaryAccessoryDimensionType
        {
            get => secondaryAccessoryDimensionType;
            set
            {
                if (value == null || value == secondaryAccessoryDimensionType) return;
                secondaryAccessoryDimensionType = value;
                dimensionStyles.secondaryAccessoryDimensionType = secondaryAccessoryDimensionType;
                OnNotifyPropertyChanged();
            }
        }
        private DimensionType secondaryOtherDimensionType;
        public DimensionType SecondaryOtherDimensionType
        {
            get => secondaryOtherDimensionType;
            set
            {
                if (value == null || value == secondaryOtherDimensionType) return;
                secondaryOtherDimensionType = value;
                dimensionStyles.secondaryOtherDimensionType = secondaryOtherDimensionType;
                OnNotifyPropertyChanged();
            }
        }

        public ObservableCollection<DimensionType> dimensionTypes = new ObservableCollection<DimensionType>();
        public ObservableCollection<DimensionType> DimensionTypes { get => dimensionTypes; set => dimensionTypes = value; }

        public DimensionTypeSelectorControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        public void Init(DimensionType[] dimensionTypes, ref DimensionStyles currentPreferences)
        {
            DimensionTypes = new ObservableCollection<DimensionType>(dimensionTypes);
            dimensionStyles = currentPreferences;

        }

        protected void OnNotifyPropertyChanged([CallerMemberName] string memberName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
        }
    }
}
