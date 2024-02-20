using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarsonsAddins
{
    /*
     * This class is used to store IDs for UI ( such as windows and dockable panes ) and for updaters, all of which are required to be registered through Revit
     * 
     * Originally, this class just stored IDs in the form of static/const variables. But after the the usuage of ComponentStates began, retrieving IDs by type became necessary to retrieve ( 
     */
    public static class ApplicationIds
    {

        private static Dictionary<Type, Guid> guidsTypeMap; 
        public static void Init()
        {
            guidsTypeMap = new Dictionary<Type, Guid>
            {
                { typeof(PreferenceWindow), new Guid("{1524F3C0-B6EB-4A32-985F-E3A7071F32B2}") },
                { typeof(PipingLCUpdater), new Guid("{BD82E5B2-D984-49F9-8442-ED572680656D}") },
                { typeof(PipingEndPrepUpdater), new Guid("{892AC63B-A827-466F-A8EA-F4418C27B7C2}") },
                { typeof(PipeEndPrepWindow), new Guid("{7FAA23DB-AAFD-4F47-857E-C06D21B2717E}") },
                { typeof(SimpleFilterDockablePane), new Guid("{5539CADC-1545-4427-9495-3E74FAB6071C}") },
                { typeof(ParameterManagerDockablePane), new Guid("{DEF29C25-F7BA-4BE6-A9CD-B1702328043E}") }
            };
            
        }
        /*public static Guid GetId<T>() where T : Type
        {
            Type t = typeof(T);
            if (guidsTypeMap.ContainsKey(t)) return guidsTypeMap[t];
            return Guid.Empty;//AddNewGuidForType(t);
        }*/
        public static Guid GetId(Type t)
        {
            if (guidsTypeMap.ContainsKey(t)) return guidsTypeMap[t];
            return Guid.Empty;//AddNewGuidForType(t);

        }
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
