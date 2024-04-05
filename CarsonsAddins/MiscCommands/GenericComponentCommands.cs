using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Events;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CarsonsAddins.GenericCommands
{

    /// <summary>
    /// Interface used for referencing generic register commands via reflection.
    /// </summary>
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
            if (windowInstance is ISettingsUpdaterComponent updaterComponent) updaterComponent.RegisterUpdater(uiapp.ActiveAddInId);
            DockablePaneId id = new DockablePaneId(ApplicationIds.GetId(typeof(T)));
            uiapp.RegisterDockablePane(id, typeof(T).Name, windowInstance);
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
            if (windowInstance is ISettingsUpdaterComponent updaterComponent) updaterComponent.UnregisterUpdater();
        }
    }

    /// <summary>
    ///Retrieves an instance of the Dockable Pane of Type T and shows it.
    /// </summary>
    /// <typeparam name="T">Generic Type for Dockable Pane that also extends the ISettingsUIComponent ( most usecases will also have a parameterless constructor ).</typeparam>
    [Transaction(TransactionMode.Manual)]
    public class ShowDockablePane<T> : IExternalCommand where T : ISettingsUIComponent, IDockablePaneProvider
    {
        /// <summary>
        /// Retrieves the GUID of the DockablePane within the Revit Context. Retrieves the instance of the DockablePane from Revit. Then shows the instance of the DockablePane.
        /// </summary>
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
                message = "Error Showing " + typeof(T).Name + " - " + ApplicationIds.GetId(typeof(T)).ToString() + '\n' + e.Message;
                return Result.Failed;
            }

        }
    }

    /// <summary>
    /// Stores a static instance of each class, creating and initializing a new instance if one doesn't exist.
    /// </summary>
    /// <typeparam name="T">Class type that extends the Window class and ISettingsUIComponent interface with a parameterless constructor.</typeparam>
    [Transaction(TransactionMode.Manual)]
    public class ShowWindow<T> : IExternalCommand where T : Window, ISettingsUIComponent, new()
    {
        /// <summary>
        /// Static instance of the generic type. One instance exists seperately for each type.
        /// </summary>
        private static T instance;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                instance = new T();
                instance.Init(commandData.Application.ActiveUIDocument);
                instance.ShowDialog();

                return Result.Succeeded;

            }
            catch
            {
                message = "Error showing window ( " + nameof(T) + " )";
                return Result.Failed;
            }

        }
    }
}
