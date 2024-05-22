/*
 * Created by SharpDevelop.
 * User: CMcCombs
 * Date: 10/30/2023
 * Time: 3:06 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */




using System;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.Attributes;
using System.IO;
using CarsonsAddins;
using Autodesk.Revit.UI.Events;
using Autodesk.Revit.DB.Events;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Controls;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.Plumbing;
using CarsonsAddins.Properties;
using Newtonsoft.Json;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using CarsonsAddins.GenericCommands;

namespace CarsonsAddins
{
    [Regeneration(RegenerationOption.Manual)]
    [Transaction(TransactionMode.Manual)]
    public partial class CarsonsAddinsApplication : IExternalApplication
    {
        public static string tmplog = string.Empty;
        private List<ComponentState> componentStates = new List<ComponentState>();
        private readonly List<ISettingsComponent> settingsComponents = new List<ISettingsComponent>();
        public Result OnStartup(UIControlledApplication app)
        {
            ApplicationIds.Init();
            app.CreateRibbonTab("Carsons Addins");
            RibbonPanel panel = app.CreateRibbonPanel("Carsons Addins", "Tools");
            Assembly assembly = Assembly.GetExecutingAssembly();

            MyApplicationSettings.Instance = new MyApplicationSettings();

            // Always loads in the Application Settings Window
            panel.AddItem(MyApplicationSettingsWindow.RegisterButton(assembly));

            // Then loads in ComponentStates based on the user's saved preference on which Component should be enabled at launch ( or loads in whichever is not a work in progress component by default )
            componentStates = MyApplicationSettings.Instance.InitComponentStates(assembly);
            Dictionary<string, PulldownButton> pulldownButtonDictionary = new Dictionary<string, PulldownButton>();

            PulldownButtonData automationPullDownButtonData = new PulldownButtonData("AutomationPullDownButton", "Automation")
            {
                ToolTip = "All tools with their own dedicated window or dockable pane.",
                Image = Utils.MediaUtils.GetImage(assembly, "CarsonsAddins.Resources.automation_icon_32.png"),
                LargeImage = Utils.MediaUtils.GetImage(assembly, "CarsonsAddins.Resources.automation_icon_32.png")
            };
            PulldownButton automationPullDownButton = panel.AddItem(automationPullDownButtonData) as PulldownButton;
            pulldownButtonDictionary.Add("Automation", automationPullDownButton);
            // Includes all components with UI, such as Windows and DockablePanes
            PulldownButtonData dimensioningPulldownButtonData = new PulldownButtonData("UIComponentsPullDownButton", "Dimensioning")
            {
                ToolTip = "All tools with their own dedicated window or dockable pane.",
                Image = Utils.MediaUtils.GetImage(assembly, "CarsonsAddins.Resources.dimension_icon_32.png"),
                LargeImage = Utils.MediaUtils.GetImage(assembly, "CarsonsAddins.Resources.dimension_icon_32.png")
            };
            PulldownButton dimensioningPulldownButton = panel.AddItem(dimensioningPulldownButtonData) as PulldownButton;
            pulldownButtonDictionary.Add("Dimensioning", dimensioningPulldownButton);
            // Includes all components without UI
            PulldownButtonData miscComponentsPulldownButtonData = new PulldownButtonData("MiscComponentsPullDownButton", "Misc")
            {
                ToolTip = "All tools without their own dedicated window or dockable pane.",
               
            };
            PulldownButton miscComponentsPulldownButton = panel.AddItem(miscComponentsPulldownButtonData) as PulldownButton;
            pulldownButtonDictionary.Add("Misc", miscComponentsPulldownButton);
            RegisterComponentPushButtons(assembly, panel, pulldownButtonDictionary);
            app.ControlledApplication.ApplicationInitialized += RegisterDockablePanes;
            app.ControlledApplication.ApplicationInitialized += DummyLaunchDimensionSettingsWindow;
            return Result.Succeeded;
        }

        
        public Result OnShutdown(UIControlledApplication app)
        {
            return Result.Succeeded;
        }

        /// <summary>
        /// Get each ComponentState and if they are enabled, then attempts to generate a new instance of each component, registering them in the progress
        /// </summary>
        /// <param name="assembly">Assembly where the Addin is located.</param>
        /// <param name="uiComponentsPulldownButton">Pulldown Button for classes with a UI component such as Windows or DockablePanes.</param>
        /// <param name="miscComponentsPulldownButton">Pulldown Button for classes without a UI component.</param>
        private void RegisterComponentPushButtons(Assembly assembly, RibbonPanel panel, Dictionary<string, PulldownButton> pulldownButtonDictionary)
        {
            foreach (ComponentState state in componentStates)
            {
                if (state == null) continue;
                if (state.IsEnabled)
                {
                    //uses reflection to find each class by type and generate a new instance
                    ConstructorInfo constructor = state.ComponentType.GetConstructor(new Type[0]);
                    ISettingsComponent component = (ISettingsComponent)constructor.Invoke(new object[0]);

                    settingsComponents.Add(component);
                    PushButtonData buttonData = component.RegisterButton(assembly);
                    if (pulldownButtonDictionary.ContainsKey(state.FolderName))
                    {
                        pulldownButtonDictionary[state.FolderName].AddPushButton(buttonData);
                    }
                    else panel.AddItem(buttonData);
                }
            }
        }
        /// <summary>
        /// Registers all of the classes with a SettingsComponent that contain a DockablePane via reflection.
        /// </summary>
        private void DummyLaunchDimensionSettingsWindow(object sender, ApplicationInitializedEventArgs e)
        {
            UIApplication uiapp = new UIApplication(sender as Autodesk.Revit.ApplicationServices.Application);
            new ShowWindow<DimensionTextWindow>().InitWindow(uiapp);
        }
            /// <summary>
            /// Registers all of the classes with a SettingsComponent that contain a DockablePane via reflection.
            /// </summary>
        private void RegisterDockablePanes(object sender, ApplicationInitializedEventArgs e)
        {
            UIApplication uiapp = new UIApplication(sender as Autodesk.Revit.ApplicationServices.Application);
            foreach (ISettingsComponent component in settingsComponents)
            {
                if (!(component is IDockablePaneProvider))  continue;
                if (component is ISettingsUIComponent uiComponent) 
                {
                    Type registerCommandType = typeof( RegisterDockablePane<>).MakeGenericType(uiComponent.GetType());
                    var registerCommand = Activator.CreateInstance(registerCommandType);
                    if (registerCommand is IExecuteWithUIApplication command) command.Execute(uiapp);
                }
            }
        }
    }


    
    


    
}


