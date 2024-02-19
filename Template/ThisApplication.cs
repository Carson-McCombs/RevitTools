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
    //[Autodesk.Revit.DB.Macros.AddInId("82283650-43D8-4B1F-ABAF-C8BDCF5EF3A3")]
    public partial class ThisApplication : IExternalApplication
    {

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
                    ISettingsComponent component = (ISettingsComponent)state.ComponentType.GetConstructor(new Type[0]).Invoke(new object[0]);

                    settingsComponents.Add(component);
                    PushButtonData buttonData = component.RegisterButton(assembly);
                    if (component is ISettingsUIComponent uiComponent) uiComponentsPulldownButton.AddPushButton(buttonData);
                    else miscComponentsPulldownButton.AddPushButton(buttonData);
                }
            }
            /* Below is the method I was originally using to load in external commands
             * - Reason for changing to ComponentStates: 
             *  -> I didn't like having to recompile everytime I wanted to enable or disable a command,
             *  -> I didn't like having all of this information, specific to each command and window, all located in the main application class
             *  
            if (PreferenceWindow.LoadWindow || PipeEndPrepWindow.LoadWindow || SimpleFilterDockablePane.LoadWindow || ComplexFilterDockablePane.LoadWindow)
            {
                PulldownButtonData dockablesPulldownData = new PulldownButtonData("Open Dockable Panes", "Dockables");
                dockablesPulldownData.Image = GetImage(assembly, "CarsonsAddins.Resources.blockA_16.png");
                dockablesPulldownData.LargeImage = GetImage(assembly, "CarsonsAddins.Resources.blockA_32.png");
                PulldownButton dockablesPulldown = panel.AddItem(dockablesPulldownData) as PulldownButton;

                if (PreferenceWindow.LoadWindow)
                {
                    PushButtonData openPreferencesButton = new PushButtonData("OpenPreferencesWindow", "Open Preferences Window", assemblyPath, "CarsonsAddins.ShowPreferenceWindow");
                    openPreferencesButton.ToolTip = "Opens Dockable Preference Window";
                    dockablesPulldown.AddPushButton(openPreferencesButton);
                }
                if (PipeEndPrepWindow.LoadWindow)
                {
                    PushButtonData openPEP = new PushButtonData("OpenPipeEndPrepWindow", "Open Pipe End Prep Window", assemblyPath, "CarsonsAddins.ShowPipeEndPrepWindow");
                    openPEP.ToolTip = "Opens Dockable Pipe End Prep Window";
                    dockablesPulldown.AddPushButton(openPEP);
                }
                if (SimpleFilterDockablePane.LoadWindow)
                {
                    PushButtonData simpleFilterButton = new PushButtonData("ShowSimpleFilterPane", "Simple Element Filter Pane", assemblyPath, "CarsonsAddins.ShowSimpleFilterPane");
                    simpleFilterButton.LargeImage = GetImage(assembly, "CarsonsAddins.Resources.cloud_32.png");
                    simpleFilterButton.Image = GetImage(assembly, "CarsonsAddins.Resources.cloud_16.png");
                    simpleFilterButton.ToolTip = "Simple Filter.";
                    dockablesPulldown.AddPushButton(simpleFilterButton);
                }
                if (ComplexFilterDockablePane.LoadWindow)
                {
                    PushButtonData complexFilterButton = new PushButtonData("ShowFilterPane", "Element Filter Pane", assemblyPath, "CarsonsAddins.ShowComplexFilterPane");
                    //complexFilterButton.LargeImage = GetImage(assembly, "CarsonsAddins.Resources.coin_32.png");
                    //complexFilterButton.Image = GetImage(assembly, "CarsonsAddins.Resources.coin_16.png");
                    complexFilterButton.ToolTip = "A Complex Filter which can be used to sort and set element parameter values.";
                    dockablesPulldown.AddPushButton(complexFilterButton);
                }

                


            }
            
            if (DimensionPipeLineCommand.LoadCommand || SelectPipeLineCommand.LoadCommand || DimensionBendCommand.LoadCommand)
            {
                PulldownButtonData pipeLinePulldownData = new PulldownButtonData("Pipe Line", "Pipe Line Tools");
                pipeLinePulldownData.Image = GetImage(assembly, "CarsonsAddins.Resources.blockB_16.png");
                pipeLinePulldownData.LargeImage = GetImage(assembly, "CarsonsAddins.Resources.blockB_32.png");
                pipeLinePulldownData.ToolTip = "A pipe line is defined here as all elements connected to either side of a pipe until either a change in direction, size, or the line ends.";
                PulldownButton pipeLinePulldown = panel.AddItem(pipeLinePulldownData) as PulldownButton;

                if (DimensionPipeLineCommand.LoadCommand)
                {
                    PushButtonData dimensionPipeLineButton = new PushButtonData("DimensionPipeLineCommand (WIP)", "Dimensions Elements in Pipe Line (WIP)", assemblyPath, "CarsonsAddins.DimensionPipeLineCommand");
                    dimensionPipeLineButton.ToolTip = "Gets the dimensions of all elements in pipe line.";
                    pipeLinePulldown.AddPushButton(dimensionPipeLineButton);
                }
                if (SelectPipeLineCommand.LoadCommand)
                {
                    PushButtonData selectPipeLineButton = new PushButtonData("SelectPipeLineCommand", "Select Elements in Pipe Line", assemblyPath, "CarsonsAddins.SelectPipeLineCommand");
                    selectPipeLineButton.ToolTip = "Selects all pipes connected to selected pipe.";
                    pipeLinePulldown.AddPushButton(selectPipeLineButton);
                }
                if (DimensionBendCommand.LoadCommand)
                {
                    PushButtonData dimensionBendCommand = new PushButtonData("DimensionBendCommand (WIP)", "Dimension Bend (WIP)", assemblyPath, "CarsonsAddins.DimensionBendCommand");
                    dimensionBendCommand.ToolTip = "Dimensions selected bend.";
                    pipeLinePulldown.AddPushButton(dimensionBendCommand);
                }

            }
            
            if (GetTotalPipeLengthCommand.LoadCommand || SmartFlipCommand.LoadCommand || SelectAllElementsOfSystemCommand.LoadCommand || FilterSelectionCommand.LoadCommand || QuestionMarkDimensionsCommand.LoadCommand)
            {
                PulldownButtonData miscToolsPulldownData = new PulldownButtonData("Misc Tools", "Misc Tools");
                miscToolsPulldownData.Image = GetImage(assembly, "CarsonsAddins.Resources.blockC_16.png");
                miscToolsPulldownData.LargeImage = GetImage(assembly, "CarsonsAddins.Resources.blockC_32.png");
                miscToolsPulldownData.ToolTip = "An Assortment of Utility Commands";
                PulldownButton miscToolsButton = panel.AddItem(miscToolsPulldownData) as PulldownButton;

                if (GetTotalPipeLengthCommand.LoadCommand)
                {
                    PushButtonData getTotalPipeLengthButton = new PushButtonData("GetTotalPipeLengthCommand", "Get Total Pipe Length", assemblyPath, "CarsonsAddins.GetTotalPipeLengthCommand");
                    getTotalPipeLengthButton.ToolTip = "Gets the total length of all selected pipe.";
                    miscToolsButton.AddPushButton(getTotalPipeLengthButton);
                }
                
                if (SmartFlipCommand.LoadCommand)
                {
                    PushButtonData smartFlipButton = new PushButtonData("SmartFlipFittingCommand", "Smart Flip Pipe Fitting", assemblyPath, "CarsonsAddins.SmartFlipCommand");
                    smartFlipButton.Image = GetImage(assembly, "CarsonsAddins.Resources.flip_32.png");
                    smartFlipButton.LargeImage = GetImage(assembly, "CarsonsAddins.Resources.flip_32.png");
                    smartFlipButton.ToolTip = "Disconnects Selected Fitting Before Flipping it and Reconnecting it";
                    miscToolsButton.AddPushButton(smartFlipButton);
                }
                
                if (FilterSelectionCommand.LoadCommand)
                {
                    PushButtonData filterSelectionButton = new PushButtonData("FilterSelectionCommand", "Filters Selection", assemblyPath, "CarsonsAddins.FilterSelectionCommand");
                    filterSelectionButton.LargeImage = GetImage(assembly, "CarsonsAddins.Resources.coin_32.png");
                    filterSelectionButton.Image = GetImage(assembly, "CarsonsAddins.Resources.coin_16.png");
                    filterSelectionButton.ToolTip = "Filters Selection.";
                    miscToolsButton.AddPushButton(filterSelectionButton);
                }
                
                if (SelectAllElementsOfSystemCommand.LoadCommand)
                {
                    PushButtonData selectAllElementsInPipingSystemButton = new PushButtonData("SelectAllElementsOfSystemCommand", "Selects all Elements in Piping System", assemblyPath, "CarsonsAddins.SelectAllElementsOfSystemCommand");
                    selectAllElementsInPipingSystemButton.ToolTip = "Selects all Elements in Piping System.";
                    miscToolsButton.AddPushButton(selectAllElementsInPipingSystemButton);
                }
                
                if (QuestionMarkDimensionsCommand.LoadCommand)
                {
                    PushButtonData questionMarkDimensionButton = new PushButtonData("QuestionMarkDimensionsCommand", "Question Mark Dimensions", assemblyPath, "CarsonsAddins.QuestionMarkDimensionsCommand");
                    questionMarkDimensionButton.ToolTip = "Overrides selected dimensions' value with a question mark.";
                    miscToolsButton.AddPushButton(questionMarkDimensionButton);
                }
                
            }
            RegisterUpdaters(app);
            */





            app.ControlledApplication.ApplicationInitialized += RegisterDockablePanes;
            return Result.Succeeded;
        }


        

        public Result OnShutdown(UIControlledApplication app)
        {
            return Result.Succeeded;
        }

        
        private void RegisterDockablePanes(object sender, ApplicationInitializedEventArgs e)
        {
            UIApplication uiapp = new UIApplication(sender as Autodesk.Revit.ApplicationServices.Application);
            foreach (ISettingsComponent component in settingsComponents)
            {
                if (component is ISettingsUIComponent uiComponent) 
                {
                    var registerCommandType = typeof( RegisterDockablePane<>).MakeGenericType(component.GetType());
                    var registerCommand = Activator.CreateInstance(registerCommandType);
                    if (registerCommand is IExecuteWithUIApplication command) command.Execute(uiapp);
                    //if (registerCommand is ISettingUpdaterComponent updaterComponent) updaterComponent.RegisterUpdater(ActiveAddInId); //updater is now registered on command register

                }
            }
            /* Registering logic now located and information located in each command's class and ComponentState respectively
             * 
            //if (PreferenceWindow.LoadWindow)
            //{
            //    RegisterPreferenceWindow registerPreferenceWindow = new RegisterPreferenceWindow();
            //    registerPreferenceWindow.Execute(uiapp);
            //    pipingLCUpdater.LinkToPreferenceWindow(registerPreferenceWindow.windowInstance);
            //}
            //if (PipeEndPrepWindow.LoadWindow)
            //{
            //    RegisterPipeEndPrepWindow registerPipeEndPrepWindow = new RegisterPipeEndPrepWindow();
            //    registerPipeEndPrepWindow.Execute(uiapp);
            //    pipingConnectionsUpdater.Link(registerPipeEndPrepWindow.windowInstance);
            //}
            //if (SimpleFilterDockablePane.LoadWindow)
            //{
            //    RegisterSimpleFilterPane registerSimpleFilterPane = new RegisterSimpleFilterPane();
            //    registerSimpleFilterPane.Execute(uiapp);
            //}
            //if (ComplexFilterDockablePane.LoadWindow)
            //{
            //    RegisterComplexFilterPane registerComplexFilterPane = new RegisterComplexFilterPane();
            //    registerComplexFilterPane.Execute(uiapp);
            //}
            
            */
        }
        
        private void UnregisterUpdaters()
        {
            foreach (ISettingsComponent component in settingsComponents)
            {
                if (component is ISettingUpdaterComponent uiComponent) uiComponent.UnregisterUpdater();
            }
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



    #region LCPreferenceWindow Setup
    /*
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class RegisterPreferenceWindow : IExternalCommand
    {
        public PreferenceWindow windowInstance = null;
        UIApplication uiapp = null;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return Execute(commandData.Application);
            
        }
        
        public Result Execute(UIApplication uiapp)
        {
            this.uiapp = uiapp;
            DockablePaneProviderData data = new DockablePaneProviderData();
            
            windowInstance = new PreferenceWindow();
            windowInstance.SetupDockablePane(data);

            DockablePaneId id = new DockablePaneId(ApplicationIds.preferenceWindowId);
            uiapp.RegisterDockablePane(id, "Dockable Preference Window", windowInstance as IDockablePaneProvider);
            uiapp.Application.DocumentOpened += new EventHandler<DocumentOpenedEventArgs>(OnDocumentOpened);

            return Result.Succeeded;
        }

        public void OnDocumentOpened(object sender, DocumentOpenedEventArgs e)
        {
            //TaskDialog.Show("OnViewActivated", "View has been activated.");
            if (uiapp.ActiveUIDocument == null)
            {
                TaskDialog.Show("ERROR", "UIAPP IS NULL");
                return;
            }
            windowInstance.Init(uiapp.ActiveUIDocument);
        }

    }
    [Transaction(TransactionMode.Manual)]
    public class ShowPreferenceWindow : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                DockablePaneId id = new DockablePaneId(ApplicationIds.preferenceWindowId);
                DockablePane dockablePane = commandData.Application.GetDockablePane(id);
                dockablePane.Show();
            }
            catch (Exception e)
            {
                TaskDialog.Show("Show Preference Window Error", e.Message);

            }
            return Result.Succeeded;
        }
    }*/
    #endregion

    #region PEP Window Setup
    //[Transaction(TransactionMode.Manual)]
    //[Regeneration(RegenerationOption.Manual)]
    //public class RegisterPipeEndPrepWindow : IExternalCommand
    //{
    //    public PipeEndPrepWindow windowInstance = null;
    //    UIApplication uiapp = null;
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        return Execute(commandData.Application);

    //    }

    //    public Result Execute(UIApplication uiapp)
    //    {
    //        this.uiapp = uiapp;
    //        DockablePaneProviderData data = new DockablePaneProviderData();

    //        windowInstance = new PipeEndPrepWindow();
    //        windowInstance.SetupDockablePane(data);

    //        DockablePaneId id = new DockablePaneId(ApplicationIds.pipeEndPrepWindowId);
    //        uiapp.RegisterDockablePane(id, "Dockable Pipe End Prep Window", windowInstance as IDockablePaneProvider);
    //        uiapp.Application.DocumentOpened += new EventHandler<DocumentOpenedEventArgs>(OnDocumentOpened);

    //        return Result.Succeeded;
    //    }

    //    public void OnDocumentOpened(object sender, DocumentOpenedEventArgs e)
    //    {
    //        //TaskDialog.Show("OnViewActivated", "View has been activated.");
    //        if (uiapp.ActiveUIDocument == null)
    //        {
    //            TaskDialog.Show("ERROR", "UIAPP IS NULL");
    //            return;
    //        }
    //        windowInstance.Init(uiapp.ActiveUIDocument);
    //    }

    //}
    //[Transaction(TransactionMode.Manual)]
    //public class ShowPipeEndPrepWindow : IExternalCommand
    //{
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        try
    //        {
    //            DockablePaneId id = new DockablePaneId(ApplicationIds.pipeEndPrepWindowId);
    //            DockablePane dockablePane = commandData.Application.GetDockablePane(id);
    //            dockablePane.Show();
    //        }
    //        catch (Exception e)
    //        {
    //            TaskDialog.Show("Show Pipe End Prep Window Error", e.Message);

    //        }
    //        return Result.Succeeded;
    //    }
    //}
    //#endregion

    //#region SimpleFilter Window Setup
    //[Transaction(TransactionMode.Manual)]
    //[Regeneration(RegenerationOption.Manual)]
    //public class RegisterSimpleFilterPane : IExternalCommand
    //{
    //    public SimpleFilterDockablePane windowInstance = null;
    //    UIApplication uiapp = null;
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        return Execute(commandData.Application);

    //    }

    //    public Result Execute(UIApplication uiapp)
    //    {
    //        this.uiapp = uiapp;
    //        DockablePaneProviderData data = new DockablePaneProviderData();

    //        windowInstance = new SimpleFilterDockablePane();
    //        windowInstance.SetupDockablePane(data);

    //        DockablePaneId id = new DockablePaneId(ApplicationIds.simpleFilterPaneId);
    //        uiapp.RegisterDockablePane(id, "Dockable Simple Filter Window", windowInstance as IDockablePaneProvider);
    //        uiapp.Application.DocumentOpened += new EventHandler<DocumentOpenedEventArgs>(OnDocumentOpened);

    //        return Result.Succeeded;
    //    }

    //    public void OnDocumentOpened(object sender, DocumentOpenedEventArgs e)
    //    {
    //        //TaskDialog.Show("OnViewActivated", "View has been activated.");
    //        if (uiapp.ActiveUIDocument == null)
    //        {
    //            TaskDialog.Show("ERROR", "UIAPP IS NULL");
    //            return;
    //        }
    //        windowInstance.Init(uiapp.ActiveUIDocument);
    //    }
    //}
    //[Transaction(TransactionMode.Manual)]
    //public class ShowSimpleFilterPane : IExternalCommand
    //{
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        try
    //        {
    //            DockablePaneId id = new DockablePaneId(ApplicationIds.simpleFilterPaneId);
    //            DockablePane dockablePane = commandData.Application.GetDockablePane(id);
    //            dockablePane.Show();
    //        }
    //        catch (Exception e)
    //        {
    //            TaskDialog.Show("Show Simple Filter Pane Error", e.Message);

    //        }
    //        return Result.Succeeded;
    //    }
    //}
    //#endregion

    //#region ComplexFilter Setup
    //[Transaction(TransactionMode.Manual)]
    //[Regeneration(RegenerationOption.Manual)]
    //public class RegisterComplexFilterPane : IExternalCommand
    //{
    //    public ComplexFilterDockablePane windowInstance = null;
    //    UIApplication uiapp = null;
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        return Execute(commandData.Application);

    //    }

    //    public Result Execute(UIApplication uiapp)
    //    {
    //        this.uiapp = uiapp;
    //        DockablePaneProviderData data = new DockablePaneProviderData();

    //        windowInstance = new ComplexFilterDockablePane();
    //        windowInstance.SetupDockablePane(data);

    //        DockablePaneId id = new DockablePaneId(ApplicationIds.complexFilterPaneId);
    //        uiapp.RegisterDockablePane(id, "Dockable Simple Filter Window", windowInstance as IDockablePaneProvider);
    //        uiapp.Application.DocumentOpened += new EventHandler<DocumentOpenedEventArgs>(OnDocumentOpened);

    //        return Result.Succeeded;
    //    }

    //    public void OnDocumentOpened(object sender, DocumentOpenedEventArgs e)
    //    {
    //        //TaskDialog.Show("OnViewActivated", "View has been activated.");
    //        if (uiapp.ActiveUIDocument == null)
    //        {
    //            TaskDialog.Show("ERROR", "UIAPP IS NULL");
    //            return;
    //        }
    //        windowInstance.Init(uiapp.ActiveUIDocument);
    //    }
    //}
    //[Transaction(TransactionMode.Manual)]
    //public class ShowComplexFilterPane : IExternalCommand
    //{
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        try
    //        {
    //            DockablePaneId id = new DockablePaneId(ApplicationIds.complexFilterPaneId);
    //            DockablePane dockablePane = commandData.Application.GetDockablePane(id);
    //            dockablePane.Show();
    //        }
    //        catch (Exception e)
    //        {
    //            TaskDialog.Show("Show Simple Filter Pane Error", e.Message);

    //        }
    //        return Result.Succeeded;
    //    }
    //}

    interface IExecuteWithUIApplication
    {
        Result Execute(UIApplication uiapp);
    }


    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class RegisterDockablePane<T> : IExternalCommand, IExecuteWithUIApplication where T : ISettingsUIComponent, IDockablePaneProvider, new()
    {
        public T windowInstance;
        UIApplication uiapp = null;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return Execute(commandData.Application);

        }

        public Result Execute(UIApplication uiapp)
        {
            this.uiapp = uiapp;
            DockablePaneProviderData data = new DockablePaneProviderData();

            windowInstance = new T();
            windowInstance.SetupDockablePane(data);
            if (windowInstance is ISettingUpdaterComponent updaterComponent) updaterComponent.RegisterUpdater(uiapp.ActiveAddInId);
            DockablePaneId id = new DockablePaneId(ApplicationIds.GetId(typeof(T)));

            //DockablePaneId id = new DockablePaneId(ApplicationIds.GetId<T>());
            uiapp.RegisterDockablePane(id, typeof(T).Name, windowInstance as IDockablePaneProvider);
            uiapp.Application.DocumentOpened += new EventHandler<DocumentOpenedEventArgs>(OnDocumentOpened);
            uiapp.ApplicationClosing += new EventHandler<ApplicationClosingEventArgs>(OnApplicationClosing);
            return Result.Succeeded;
        }

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
        private void OnApplicationClosing(object sender, ApplicationClosingEventArgs e)
        {
            if (windowInstance is ISettingUpdaterComponent updaterComponent) updaterComponent.UnregisterUpdater();
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class ShowDockablePane<T> : IExternalCommand where T : ISettingsUIComponent, IDockablePaneProvider
    {        
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                DockablePaneId id = new DockablePaneId(ApplicationIds.GetId<T>());
                DockablePane dockablePane = commandData.Application.GetDockablePane(id);
                dockablePane.Show();

                return Result.Succeeded;

            }
            catch (Exception e)
            {
                TaskDialog.Show("Error Showing " + typeof(T).Name, ApplicationIds.GetId<T>().ToString() + '\n' + e.Message);
                return Result.Failed;
            }

        }
    }


    #endregion

    #endregion
}


