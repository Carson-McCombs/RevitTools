using CarsonsAddins.Properties;
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
using System.Reflection;
using Autodesk.Revit.UI;
using System.Configuration.Assemblies;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using System.CodeDom;
using System.Threading;
using Autodesk.Revit.DB.ExtensibleStorage;
using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace CarsonsAddins
{

    /// <summary>
    /// Controls the loading, setting, and saving of ComponentState information. There is one ComponentState per Command and each State can be enabled and disabled within the Window UI.
    /// </summary>
    public partial class MyApplicationSettingsWindow : Window
    {
        public MyApplicationSettingsWindow()
        {
            InitializeComponent();
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
        
        /// <summary>
        /// When the "Apply" button is pressed, saves the current SettingState to Revit.
        /// </summary>
        private void ApplyButtonClick(object sender, RoutedEventArgs e)
        {
            MyApplicationSettings.Instance.SaveToDB();
            TaskDialog.Show("Preferences Updated", "Please restart Revit to apply changes to preferences.");
        }

        /// <summary>
        /// When the "Revert" button is pressed, reloads SettingsState to the state which is saved within Revit.
        /// </summary>
        private void RevertButtonClick(object sender, RoutedEventArgs e)
        {
            MyApplicationSettings.Instance.LoadFromDB(Assembly.GetExecutingAssembly());
        }

        /// <summary>
        /// Shows the SettingsWindow. The ShowSettingsWindow class is needed so that the MyApplicationSettingsWindow doesn't need extend the ISettingsComponent to prevent feedback loops on register
        /// </summary>
        [Transaction(TransactionMode.Manual)]
        public class ShowSettingsWindow : IExternalCommand
        {
            private MyApplicationSettingsWindow instance;

            public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
            {
                try
                {
                    if (instance == null) instance = new MyApplicationSettingsWindow();
                    instance.SettingsDataGrid.ItemsSource = MyApplicationSettings.Instance.settingsState;
                    instance.ShowDialog();

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
