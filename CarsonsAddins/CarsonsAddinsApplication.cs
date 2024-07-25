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
using CarsonsAddins.Settings.ComponentStates.Models;
using CarsonsAddins.Settings.Main.Views;
using CarsonsAddins.Settings.Dimensioning.Models;

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

            
            ComponentStatePreferences.Instance = new ComponentStatePreferences();

            // Always loads in the Application Settings Window
            panel.AddItem(MyApplicationSettingsWindow.RegisterButton(assembly));

            // Then loads in ComponentStates based on the user's saved preference on which Component should be enabled at launch ( or loads in whichever is not a work in progress component by default )
            componentStates = ComponentStatePreferences.Instance.InitComponentStates(assembly);
            Dictionary<string, PulldownButton> pulldownButtonDictionary = new Dictionary<string, PulldownButton>();
            if (componentStates.Any(component => component.FolderName == "Automation" && component.IsEnabled))
            {
                PulldownButtonData automationPullDownButtonData = new PulldownButtonData("AutomationPullDownButton", "Automation")
                {
                    ToolTip = "All tools for automating tasks.",
                    Image = Utils.MediaUtils.GetImage(assembly, "CarsonsAddins.Resources.automation_icon_32.png"),
                    LargeImage = Utils.MediaUtils.GetImage(assembly, "CarsonsAddins.Resources.automation_icon_32.png")
                };
                PulldownButton automationPullDownButton = panel.AddItem(automationPullDownButtonData) as PulldownButton;
                pulldownButtonDictionary.Add("Automation", automationPullDownButton);
            }
            if (componentStates.Any(component => component.FolderName == "Dimensioning" && component.IsEnabled))
            {
                PulldownButtonData dimensioningPulldownButtonData = new PulldownButtonData("UIComponentsPullDownButton", "Dimensioning")
                {
                    ToolTip = "All tools for creating or manipulating dimensions.",
                    Image = Utils.MediaUtils.GetImage(assembly, "CarsonsAddins.Resources.dimension_icon_32.png"),
                    LargeImage = Utils.MediaUtils.GetImage(assembly, "CarsonsAddins.Resources.dimension_icon_32.png")
                };
                PulldownButton dimensioningPulldownButton = panel.AddItem(dimensioningPulldownButtonData) as PulldownButton;
                pulldownButtonDictionary.Add("Dimensioning", dimensioningPulldownButton);
            }
            if (componentStates.Any(component => component.FolderName == "Debug" && component.IsEnabled))
            {
                PulldownButtonData debugPulldownButtonData = new PulldownButtonData("DebugPullDownButton", "Debug")
                {
                    ToolTip = "All tools for development purposes.",
                };
                PulldownButton debugPulldownButton = panel.AddItem(debugPulldownButtonData) as PulldownButton;
                pulldownButtonDictionary.Add("Debug", debugPulldownButton);
            }
            if (componentStates.Any(component => component.FolderName == "Misc" && component.IsEnabled))
            {
                PulldownButtonData miscComponentsPulldownButtonData = new PulldownButtonData("MiscComponentsPullDownButton", "Misc")
                {
                    ToolTip = "All tools without their own dedicated window or dockable pane.",

                };
                PulldownButton miscComponentsPulldownButton = panel.AddItem(miscComponentsPulldownButtonData) as PulldownButton;
                pulldownButtonDictionary.Add("Misc", miscComponentsPulldownButton);
            }
            RegisterComponentPushButtons(assembly, panel, pulldownButtonDictionary);
            app.ControlledApplication.ApplicationInitialized += RegisterDockablePanes;
            app.ViewActivated += CheckIfFocusingOnNewDocument;
            return Result.Succeeded;
        }

        private void CheckIfFocusingOnNewDocument(object sender, ViewActivatedEventArgs e)
        {
            if (e.CurrentActiveView?.Document == null || e.CurrentActiveView?.Document == e.PreviousActiveView?.Document) return;
           
            DimensionPreferences.Instance = DimensionPreferences.CreateFromPreferences(e.CurrentActiveView.Document);
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


