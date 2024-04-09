using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using CarsonsAddins.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static CarsonsAddins.Utils.ConnectionUtils;

namespace CarsonsAddins
{
    /// <summary>
    /// Interaction logic for PipeEndPrepBCWindow.xaml
    /// </summary>
    public partial class PipeEndPrepBCWindow : Page, IDockablePaneProvider, ISettingsUIComponent, ISettingsUpdaterComponent
    {
        public const bool IsWIP = false;
        private PipeEndPrepBCUpdater updater;
        private UIDocument uidoc;
        public PipeEndPrepBCWindow()
        {
            InitializeComponent();
        }

        public PushButtonData RegisterButton(Assembly assembly)
        {
            return new PushButtonData("Pipe End Prep BC", "Pipe End Prep BC", assembly.Location, typeof(GenericCommands.ShowDockablePane<PipeEndPrepBCWindow>).FullName)
            {
                ToolTip = "Opens Pipe End Prep By Connectors DockablePane"
            };
        }

        public void RegisterUpdater(AddInId addinId)
        {
            updater = new PipeEndPrepBCUpdater(addinId);
            
        }

        public void UnregisterUpdater()
        {
            if (updater == null) return;
            updater.Unregister();
        }

        public void Init(UIDocument uidoc)
        {
            this.uidoc = uidoc;
        }

        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.FrameworkElement = this;
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Bottom,
            };
        }

    }
}
