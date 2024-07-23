using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CarsonsAddins.Settings.ComponentStates.ViewModels;
using CarsonsAddins.Settings.Main.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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

namespace CarsonsAddins.Settings.Main.Views
{
    /// <summary>
    /// Interaction logic for MyApplicationSettingsWindow.xaml
    /// </summary>
    public partial class MyApplicationSettingsWindow : Window
    {
        private MyApplicationSettingsViewModel viewModel;
        public MyApplicationSettingsWindow()
        {
            InitializeComponent();
        }
        public void Init(MyApplicationSettingsViewModel viewModel)
        {
            this.viewModel = viewModel;
            PluginControl.Init(viewModel.pluginsViewModel);
            DimensionsControl.Init(viewModel.dimensionSettingsViewModel);
        }
        /// <summary>
        /// Creates a PushButtonData for the Application Settings Window.
        /// </summary>
        /// <param name="assembly">Assembly where the Addin is located.</param>
        /// <returns></returns>
        public static PushButtonData RegisterButton(Assembly assembly)
        {
            PushButtonData pushButtonData = new PushButtonData("Carsons Addins Settings", "Carson's Addins Settings", assembly.Location, typeof(ShowSettingsWindow).FullName)
            {
                Image = Utils.MediaUtils.GetImage(assembly, "CarsonsAddins.Resources.settings_icon_32.png"),
                LargeImage = Utils.MediaUtils.GetImage(assembly, "CarsonsAddins.Resources.settings_icon_32.png"),
                ToolTip = "Opens Carson's Addins Settings Window"
            };
            return pushButtonData;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            viewModel.Save();
            e.Cancel = true;
            Visibility = System.Windows.Visibility.Hidden;
        }

        /// <summary>
        /// Shows the SettingsWindow. The ShowSettingsWindow class is needed so that the MyApplicationSettingsWindow doesn't need extend the ISettingsComponent to prevent feedback loops on register
        /// </summary>
        [Transaction(TransactionMode.Manual)]
        public class ShowSettingsWindow : IExternalCommand
        {
            private static MyApplicationSettingsWindow instance;
            private static MyApplicationSettingsViewModel viewModel;

            public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
            {
                try
                {
                    if (instance == null) instance = new MyApplicationSettingsWindow();
                    if (viewModel == null) viewModel = new MyApplicationSettingsViewModel(new Dimensioning.ViewModels.DimensionSettingsViewModel(commandData.Application.ActiveUIDocument));
                    instance.Init(viewModel);
                    switch (instance.Visibility)
                    {
                        case System.Windows.Visibility.Collapsed: instance.Visibility = System.Windows.Visibility.Visible; break;
                        case System.Windows.Visibility.Hidden: instance.ShowDialog(); break;
                        default: break;
                    }
                    return Result.Succeeded;

                }
                catch (Exception ex)
                {
                    message = ex.Message;
                    return Result.Failed;
                }

            }
        }
    }
}
