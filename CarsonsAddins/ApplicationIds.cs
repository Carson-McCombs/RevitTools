using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarsonsAddins
{
    /// <summary>
    /// This class is used to store IDs for UI ( such as windows and dockable panes ) and for updaters, all of which are required to be registered through Revit.
    /// These are stored here instead of all of them being generated at runtime so that Revit doesn't ask the user to allow each component everytime Revit is launched or everytime the addin is updated.
    /// 
    /// Originally, this class just stored IDs in the form of static/const variables. 
    /// But after the the usuage of ComponentStates began, retrieving IDs by type became necessary to retrieve references to Dockable Panes and Updaters.
    /// </summary>
    public static class ApplicationIds
    {

        private static Dictionary<Type, Guid> guidsTypeMap; 
        public static void Init()
        {
            guidsTypeMap = new Dictionary<Type, Guid>
            {
                { typeof(PipingLCDockablePane), new Guid("{1524F3C0-B6EB-4A32-985F-E3A7071F32B2}") },
                { typeof(PipingLCUpdater), new Guid("{BD82E5B2-D984-49F9-8442-ED572680656D}") },
                { typeof(PipingEndPrepUpdater), new Guid("{892AC63B-A827-466F-A8EA-F4418C27B7C2}") },
                { typeof(PipeEndPrepWindow), new Guid("{7FAA23DB-AAFD-4F47-857E-C06D21B2717E}") },
                { typeof(SimpleFilterDockablePane), new Guid("{5539CADC-1545-4427-9495-3E74FAB6071C}") },
                { typeof(ParameterManagerDockablePane), new Guid("{DEF29C25-F7BA-4BE6-A9CD-B1702328043E}") },
                { typeof(StaleReferenceUpdater), new Guid("E00DAD85-B1A0-4C8D-84A8-CEE2BFBBB2E5") }
            };
            
        }

        /// <summary>
        /// Retrieves the GUID corresponding to a given type, if there isn't a corresponding GUID found, a new one is generated.
        /// </summary>
        /// <param name="t">The type of the class - which should extend any class that can be registered through Revit ( i.e. IDockablePaneProver, IUpdater, etc. )</param>
        /// <example>GetId(typeof(this));</example>
        /// <returns>GUID corresponding the the given class type.</returns>
        public static Guid GetId(Type t)
        {
            if (guidsTypeMap.ContainsKey(t)) return guidsTypeMap[t];
            return AddNewGuidForType(t); //this should never be called
            //return Guid.Empty;
        }

        /// <summary>
        /// Registers a new GUID for a given type
        /// </summary>
        /// <param name="t">The type of the class being registered.</param>
        /// <returns></returns>
        private static Guid AddNewGuidForType(Type t)
        {
            Guid guid = Guid.NewGuid();
            guidsTypeMap.Add(t, guid);
            TaskDialog.Show("issue", guidsTypeMap.ToString());
            TaskDialog.Show("Unable to Find GUID for " + t.Name + ", generating new ID.", "Please add a GUID for the specified type within the guidsTypeMap in the ApplicationIds class.");
            return guid;
        }
    }
}
