using Autodesk.Revit.DB;
using System;
using System.Collections;
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

namespace CarsonsAddins
{
    /// <summary>
    /// Interaction logic for GraphicsStyleListControl.xaml
    /// </summary>
    public partial class GraphicsStyleListControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private string[] allGraphicStyleNames = new string[0];
        public string[] AllGraphicStyleNames
        {
            get => allGraphicStyleNames;
            set
            {
                if (value == null || value == allGraphicStyleNames) return;
                allGraphicStyleNames = value;
                OnNotifyPropertyChanged();
            }
        }
        private ObservableCollection<string> selectedGraphicStyleNames = new ObservableCollection<string>();
        public ObservableCollection<string> SelectedGraphicStyleNames
        {
            get => selectedGraphicStyleNames;
            set
            {
                if (value == null || selectedGraphicStyleNames == value) return;
                selectedGraphicStyleNames = value;
            }
        }
        private ObservableCollection<string> notSelectedGraphicStyleNames = new ObservableCollection<string>();
        public ObservableCollection<string> NotSelectedGraphicStyleNames
        {
            get => notSelectedGraphicStyleNames;
            set
            {
                if (value == null || notSelectedGraphicStyleNames == value) return;
                notSelectedGraphicStyleNames = value;
            }
        }
        private string comboboxSelectedGraphicStyleName;
        public string ComboboxSelectedGraphicStyleName
        {
            get => comboboxSelectedGraphicStyleName;
            set
            {
                comboboxSelectedGraphicStyleName = value;
                OnNotifyPropertyChanged();
            }
        }
        private Dictionary<string, GraphicsStyle[]> graphicStylesNameDictionary = new Dictionary<string, GraphicsStyle[]>();
        private List<GraphicsStyle> selectedGraphicStyles = new List<GraphicsStyle>();
        public GraphicsStyleListControl()
        {
            InitializeComponent();
            DataContext = this;    
        }
        public void Init(GraphicsStyle[] allGraphicStyles, ref List<GraphicsStyle> selectedGraphicStyles)
        {
            
            List<string> nameList = allGraphicStyles.Select(gs => gs.Name).ToList();
            nameList.Sort();
            AllGraphicStyleNames = nameList.ToArray();
            this.selectedGraphicStyles = selectedGraphicStyles;
            graphicStylesNameDictionary = allGraphicStyles.GroupBy(gs => gs.Name).ToDictionary(group => group.Key, group => group.ToArray());
            SelectedGraphicStyleNames.Clear();
            NotSelectedGraphicStyleNames = new ObservableCollection<string>(graphicStylesNameDictionary.Keys.OrderBy(s => s));          
        }
        private void AddStyle_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ComboboxSelectedGraphicStyleName)) return;
            if (graphicStylesNameDictionary.ContainsKey(ComboboxSelectedGraphicStyleName))
            {
                selectedGraphicStyles.AddRange(graphicStylesNameDictionary[ComboboxSelectedGraphicStyleName]);
            }
            SelectedGraphicStyleNames.Add(ComboboxSelectedGraphicStyleName);
            NotSelectedGraphicStyleNames.Remove(ComboboxSelectedGraphicStyleName);
        }
        private void RemoveStyle_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (!(button.DataContext is string name)) return;
            if (graphicStylesNameDictionary.ContainsKey(name))
            {
                foreach (GraphicsStyle style in graphicStylesNameDictionary[name]) selectedGraphicStyles.Remove(style);
            }
            List<string> names = NotSelectedGraphicStyleNames.ToList();
            names.Add(name);
            names.Sort();
            NotSelectedGraphicStyleNames = new ObservableCollection<string>(names);
            //NotSelectedGraphicStyleNames.Add(name);
            //NotSelectedGraphicStyleNames = new ObservableCollection<string>(NotSelectedGraphicStyleNames.OrderBy(s => s));
            SelectedGraphicStyleNames.Remove(name);
        }

        protected void OnNotifyPropertyChanged([CallerMemberName] string memberName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
        }

    }
}
