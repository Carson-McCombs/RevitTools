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

namespace CarsonsAddins
{
    [Regeneration(RegenerationOption.Manual)]
    [Transaction(TransactionMode.Manual)]
    public partial class ThisApplication : IExternalApplication
    {
        public static string tmplog = string.Empty;
        private List<ComponentState> componentStates = new List<ComponentState>();
        private List<ISettingsComponent> settingsComponents = new List<ISettingsComponent>();
        public static ThisApplication instance {  get; private set; }
        public ThisApplication() { instance = this; }
        public Result OnStartup(UIControlledApplication app)
        {
            ApplicationIds.Init();
            app.CreateRibbonTab("Carsons Addins");
            RibbonPanel panel = app.CreateRibbonPanel("Carsons Addins", "Piping Preferences");
            Assembly assembly = Assembly.GetExecutingAssembly();

            MyApplicationSettings.Instance = new MyApplicationSettings();

            // Always loads in the Application Settings Window
            panel.AddItem(MyApplicationSettingsWindow.RegisterButton(assembly));

            // Then loads in ComponentStates based on the user's saved preference on which Component should be enabled at launch ( or loads in whichever is not a work in progress component by default )
            componentStates = MyApplicationSettings.Instance.InitComponentStates(assembly);

            // Includes all components with UI, such as Windows and DockablePanes
            PulldownButtonData uiComponentsPulldownButtonData = new PulldownButtonData("UIComponentsPullDownButton","Windows");
            uiComponentsPulldownButtonData.ToolTip = "All tools with their own dedicated window or dockable pane.";
            uiComponentsPulldownButtonData.Image = Util.GetImage(assembly, "CarsonsAddins.Resources.blockC_32.png");
            uiComponentsPulldownButtonData.LargeImage = Util.GetImage(assembly, "CarsonsAddins.Resources.blockC_32.png");
            PulldownButton uiComponentsPulldownButton = panel.AddItem(uiComponentsPulldownButtonData) as PulldownButton;
            
            // Includes all components without UI
            PulldownButtonData miscComponentsPulldownButtonData = new PulldownButtonData("MiscComponentsPullDownButton", "Misc Tools");
            miscComponentsPulldownButtonData.ToolTip = "All tools without their own dedicated window or dockable pane.";
            miscComponentsPulldownButtonData.Image = Util.GetImage(assembly, "CarsonsAddins.Resources.blockA_16.png");
            miscComponentsPulldownButtonData.LargeImage = Util.GetImage(assembly, "CarsonsAddins.Resources.blockA_32.png");
            PulldownButton miscComponentsPulldownButton = panel.AddItem(miscComponentsPulldownButtonData) as PulldownButton;

            // Get each ComponentState and if they are enabled, then attempts to generate a new instance of each component, registering them in the progress
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
                    if (component is ISettingsUIComponent uiComponent) uiComponentsPulldownButton.AddPushButton(buttonData);
                    else miscComponentsPulldownButton.AddPushButton(buttonData);
                }
            }

            app.ControlledApplication.ApplicationInitialized += RegisterDockablePanes;
            return Result.Succeeded;
        }
        

        public Result OnShutdown(UIControlledApplication app)
        {
            return Result.Succeeded;
        }

        
        private void RegisterDockablePanes(object sender, ApplicationInitializedEventArgs e)
        {
            string s = "";
            UIApplication uiapp = new UIApplication(sender as Autodesk.Revit.ApplicationServices.Application);
            foreach (ISettingsComponent component in settingsComponents)
            {
                if (component is ISettingsUIComponent uiComponent) 
                {
                    s += uiComponent.GetType().Name + '\n';
                    var registerCommandType = typeof( RegisterDockablePane<>).MakeGenericType(uiComponent.GetType());
                    var registerCommand = Activator.CreateInstance(registerCommandType);
                    if (registerCommand is IExecuteWithUIApplication command) command.Execute(uiapp);
                }
            }
            TaskDialog.Show("RegisterDockablePanes", s);
        }

    }
    
    #region Window Setup
    public class Availability_NoActiveUIDocument : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
        {
            if (applicationData.ActiveUIDocument == null)
            {
                return true;
            }

            return false;
        }
    }



    interface IExecuteWithUIApplication
    {
        Result Execute(UIApplication uiapp);
    }



    /// <summary>
    /// Registers a Generic Dockable Pane to Revit.
    /// </summary>
    /// <typeparam name="T">Generic Type for a Dockable Pane that also extends ISettingsUIComponent and has a parameterless constructor.</typeparam>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class RegisterDockablePane<T> : IExternalCommand, IExecuteWithUIApplication where T : ISettingsUIComponent, IDockablePaneProvider, new()
    {
        public T windowInstance;
        UIApplication uiapp = null;

        //By passing the Execute function to one with only the UIApplication parameter, this allows for the extenal command to be called easier and less reliant on Revit
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return Execute(commandData.Application);

        }


        /// <summary>
        /// Registers the dockable pane of a generic type, along with any updaters.
        /// </summary>
        /// <param name="uiapp">The UIApplication</param>
        /// <returns>Always returns Result.Succeeded.</returns>
        public Result Execute(UIApplication uiapp)
        {
            this.uiapp = uiapp;
            DockablePaneProviderData data = new DockablePaneProviderData();
            windowInstance = new T();
            windowInstance.SetupDockablePane(data);
            if (windowInstance is ISettingUpdaterComponent updaterComponent) updaterComponent.RegisterUpdater(uiapp.ActiveAddInId);
            DockablePaneId id = new DockablePaneId(ApplicationIds.GetId(typeof(T)));
            uiapp.RegisterDockablePane(id, typeof(T).Name, windowInstance as IDockablePaneProvider);
            uiapp.Application.DocumentOpened += new EventHandler<DocumentOpenedEventArgs>(OnDocumentOpened);
            uiapp.ApplicationClosing += new EventHandler<ApplicationClosingEventArgs>(OnApplicationClosing);
            return Result.Succeeded;
        }

        // Makes sure that the Dockable pane instance is initialized each time a new document is opened.
        private void OnDocumentOpened(object sender, DocumentOpenedEventArgs e)
        {
            //TaskDialog.Show("OnViewActivated", "View has been activated.");
            if (uiapp.ActiveUIDocument == null)
            {
                TaskDialog.Show("ERROR", typeof(T).Name + " IS NULL");
                return;
            }
            windowInstance.Init(uiapp.ActiveUIDocument);
        }
        //If the Dockable Pane has an updater, unregister it on application closing
        private void OnApplicationClosing(object sender, ApplicationClosingEventArgs e)
        {
            if (windowInstance is ISettingUpdaterComponent updaterComponent) updaterComponent.UnregisterUpdater();
        }
    }

    /// <summary>
    ///Retrieves an instance of the Dockable Pane of Type T and shows it.
    /// </summary>
    /// <typeparam name="T">Generic Type for Dockable Pane that also extends the ISettingsUIComponent ( most usecases will also have a parameterless constructor ).</typeparam>
    [Transaction(TransactionMode.Manual)]
    public class ShowDockablePane<T> : IExternalCommand where T : ISettingsUIComponent, IDockablePaneProvider
    {        
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                DockablePaneId id = new DockablePaneId(ApplicationIds.GetId(typeof(T)));
                DockablePane dockablePane = commandData.Application.GetDockablePane(id);
                dockablePane.Show();

                return Result.Succeeded;

            }
            catch (Exception e)
            {
                TaskDialog.Show("Error Showing " + typeof(T).Name, ApplicationIds.GetId(typeof(T)).ToString() + '\n' + e.Message);
                return Result.Failed;
            }

        }
    }


    #endregion


}


