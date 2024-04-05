using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CarsonsAddins
{
    /// <summary>
    /// Stores information on all the classes that extend the ISettingsComponent interface and are located within the Assembly. 
    /// In other words, all classes with commands and UI should have their type information stored here. 
    /// The information stored is used with reflection to more easily register components to Revit. 
    /// It is also used to set the default state on whether or not Revit will register and load each command as a button by checking whether or not it is WIP. 
    /// </summary>
    public class ComponentState : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Name of the location where the Component's button is located.
        /// </summary>
        public string ParentName;

        /// <summary>
        /// Type of the Component.
        /// </summary>
        public Type ComponentType;

        /// <summary>
        /// Name of the Component.
        /// </summary>
        public string ComponentName
        {
            get
            {
                if (ComponentType == null) return "Null Type";
                return ComponentType.Name + ((IsWIP) ? " ( WIP )" : "");
            }
        }

        /// <summary>
        /// "Work in Progress". Set by the class' constant bool variable of the same name.
        /// </summary>
        public bool IsWIP = false;

        /// <summary>
        /// Stores a boolean value that determines whether or not the Component is enabled to be registered on application launch. By default is equal to the opposite of IsWIP. Can be set by within the ApplicationSettings Window.
        /// </summary>
        private bool isEnabled;

        /// <summary>
        /// Public boolean value that updates UI on change. It determines whether or not the Component is enabled to be registered on application launch. By default is equal to the opposite of IsWIP. Can be set by within the ApplicationSettings Window.
        /// </summary>
        public bool IsEnabled
        {
            get => isEnabled;
            set
            {
                if (value == isEnabled) return;
                isEnabled = value;
                OnNotifyPropertyChanged();
            }
        }

        /// <param name="componentType">Type of the Component.</param>
        /// <param name="isEnabled">Determines if the Component should be loaded in on application launch. Value is either loaded from preferences and equals the opposite of isWIP by default.</param>
        /// <param name="isWIP">Whether or not the Component is a "Work in Progress"</param>
        public ComponentState(Type componentType, bool isEnabled, bool isWIP)
        {
            ComponentType = componentType;
            IsEnabled = isEnabled;
            IsWIP = isWIP;
        }


        /* Below was considered to be held within the ComponentState class such that a more generic method of registering a component ( a class and their respective buttons, updates, and windows ), 
         * but the added complexity using lowered readability. Thus just loading in the Command's buttons, updaters, windows, etc. are now located within the ThisApplication class
         * 
         * public static void RegisterComponent<T>(Assembly assembly)
        {
            Type componentType = typeof(T);
            if (!(componentType is ISettingsComponent)) return;
            componentType.GetMethod(componentType.FullName + ".RegisterButton()")?.Invoke(null, new object[]{ assembly });
        }*/

        /// <summary>
        /// This is used for databinding to the Settings Window, detecting when the user changes a detail of the ComponentStates ( i.e. enabling / disabling a component )
        /// </summary>
        protected void OnNotifyPropertyChanged([CallerMemberName] string memberName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
        }

    }

    /// <summary>
    /// All commands that require a button on the ribbon panel, are created by this addin, and have their own corresponding Updater should implement this interface.
    /// </summary>
    public interface ISettingsUpdaterComponent
    {
        /// <summary>
        /// Registers the Updater within the Revit context. Called on application startup.
        /// </summary>
        /// <param name="addinId">Id of the Addin, in this case, the Id of CarsonsAddins within Revit.</param>
        void RegisterUpdater(AddInId addinId);

        /// <summary>
        /// Unregisters the Updater within the Revit context.
        /// </summary>
        void UnregisterUpdater();
    }

    /// <summary>
    /// All commands that require a button on the ribbon panel, are created by this Addin, and have dedicated XAML UI ( such as a Window or Dockable Pane ) should implement this interface.
    /// </summary>
    public interface ISettingsUIComponent : ISettingsComponent
    {
        /// <summary>
        /// Initializes the UIComponent within the Revit Context. Used to save a reference to the UIDocument and load anything that may not have been initialized on application startup.
        /// </summary>
        /// <param name="uidoc">Revit's active UIDocument</param>
        void Init(UIDocument uidoc);
    }
}

/*
      Purpose: 
       ->To store data related to loading in commands as ribbon buttons and where those buttons are located
      -Pro: 
       -> Can easily enable and disable commands ( and their updaters ) without having to recompile.
       -> Can easily add new commands with less code
       -> Abstracts away how and which commands should be loaded and registered
       -> Abstracts away a command and their dependent buttons, updaters, and windows
      -Cons: 
       -> Now each command ( with a button ) must implement ISettingsComponent and have a const bool IsWIP to be loaded in properly. This is unless the command is just set to always be loaded in, such as the open settings window command.
     */

/// <summary>
/// All commands that require a button on the ribbon panel, and are created by this Addin should implement this interface.
/// </summary>
public interface ISettingsComponent
{
    /// <summary>
    /// Creates a button for this SettingsComponent. Has information such as image for button icon and tooltip. Used to register within the Addin Ribbon Panel.
    /// </summary>
    /// <param name="assembly">The assembly where the Revit Addin is located.</param>
    /// <returns>The button data for the respective class.</returns>
    /// <in
    PushButtonData RegisterButton(Assembly assembly);
}
