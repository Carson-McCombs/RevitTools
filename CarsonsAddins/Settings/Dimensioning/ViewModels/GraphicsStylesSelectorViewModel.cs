﻿using Autodesk.Revit.DB;
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

namespace CarsonsAddins.Settings.Dimensioning.ViewModels
{
    public partial class GraphicStylesSelectorViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private GraphicsStyle[] allGraphicStyles;

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
                OnNotifyPropertyChanged();

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
                OnNotifyPropertyChanged();
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
        public GraphicStylesSelectorViewModel(ref GraphicsStyle[] allGraphicStyles, ref List<GraphicsStyle> selectedGraphicStyles)
        {
            this.allGraphicStyles = allGraphicStyles;
            this.selectedGraphicStyles = selectedGraphicStyles;
            Refresh();
        }

        public void Refresh()
        {
            List<string> nameList = allGraphicStyles.Select(gs => gs.Name).ToList();
            nameList.Sort();
            AllGraphicStyleNames = nameList.ToArray();
            graphicStylesNameDictionary = allGraphicStyles.GroupBy(gs => gs.Name).ToDictionary(group => group.Key, group => group.ToArray());
            SelectedGraphicStyleNames = new ObservableCollection<string>(selectedGraphicStyles.Select(gs => gs.Name).Distinct());
            NotSelectedGraphicStyleNames = new ObservableCollection<string>(graphicStylesNameDictionary.Keys.Where(name => !SelectedGraphicStyleNames.Contains(name)).OrderBy(s => s));
        }
        public void AddStyle(string styleName)
        {
            if (string.IsNullOrWhiteSpace(styleName)) return;
            if (graphicStylesNameDictionary.ContainsKey(styleName))
            {
                selectedGraphicStyles.AddRange(graphicStylesNameDictionary[styleName]);
            }
            SelectedGraphicStyleNames.Add(styleName);
            NotSelectedGraphicStyleNames.Remove(styleName);
        }


        public void RemoveStyle(string styleName)
        {
            if (graphicStylesNameDictionary.ContainsKey(styleName))
            {
                foreach (GraphicsStyle style in graphicStylesNameDictionary[styleName]) selectedGraphicStyles.Remove(style);
            }
            List<string> names = NotSelectedGraphicStyleNames.ToList();
            names.Add(styleName);
            names.Sort();
            NotSelectedGraphicStyleNames = new ObservableCollection<string>(names);
            SelectedGraphicStyleNames.Remove(styleName);
        }


        protected void OnNotifyPropertyChanged([CallerMemberName] string memberName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
        }

    }
}
