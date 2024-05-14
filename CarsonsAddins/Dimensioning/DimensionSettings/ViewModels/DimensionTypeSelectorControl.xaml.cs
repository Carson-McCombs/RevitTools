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

        public DimensionType PrimaryDimensionType
        {
            get => dimensionStyles.primaryDimensionType;
            set
            {
                if (value == null || value == dimensionStyles.primaryDimensionType) return;
                dimensionStyles.primaryDimensionType = value;
                OnNotifyPropertyChanged();
            }
        }
        public DimensionType SecondaryPipeDimensionType
        {
            get => dimensionStyles.secondaryPipeDimensionType;
            set
            {
                if (value == null || value == dimensionStyles.secondaryPipeDimensionType) return;
                dimensionStyles.secondaryPipeDimensionType = value;
                OnNotifyPropertyChanged();
            }
        }
        public DimensionType SecondaryFittingDimensionType
        {
            get => dimensionStyles.secondaryFittingDimensionType;
            set
            {
                if (value == null || value == dimensionStyles.secondaryFittingDimensionType) return;
                dimensionStyles.secondaryFittingDimensionType = value;
                OnNotifyPropertyChanged();
            }
        }
        public DimensionType SecondaryAccessoryDimensionType
        {
            get => dimensionStyles.secondaryAccessoryDimensionType;
            set
            {
                if (value == null || value == dimensionStyles.secondaryAccessoryDimensionType) return;
                dimensionStyles.secondaryAccessoryDimensionType = value;
                OnNotifyPropertyChanged();
            }
        }
        public DimensionType SecondaryOtherDimensionType
        {
            get => dimensionStyles.secondaryOtherDimensionType;
            set
            {
                if (value == null || value == dimensionStyles.secondaryOtherDimensionType) return;
                dimensionStyles.secondaryOtherDimensionType = value;
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
            LoadFromDimensionStyles();
        }

        private void LoadFromDimensionStyles()
        {
            if (dimensionStyles == null) return;
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
