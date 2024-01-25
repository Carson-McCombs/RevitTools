using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarsonsAddins
{
    public static class ApplicationIds
    {
        /*public static Guid preferenceWindowId;
        public static Guid pipingUpdaterId;
        public static Guid pipingConnectionsUpdaterId;
        public static Guid pipeEndPrepWindowId;
        public static Guid simpleFilterPaneId;
        public static Guid complexFilterPaneId;*/
        private static Dictionary<Type, Guid> guidsTypeMap; 
        public static void Init()
        {
            guidsTypeMap = new Dictionary<Type, Guid>();
            guidsTypeMap.Add(typeof(PreferenceWindow), new Guid("{1524F3C0-B6EB-4A32-985F-E3A7071F32B2}"));
            guidsTypeMap.Add(typeof(PipingLCUpdater), new Guid("{BD82E5B2-D984-49F9-8442-ED572680656D}"));
            guidsTypeMap.Add(typeof(PipingEndPrepUpdater), new Guid("{892AC63B-A827-466F-A8EA-F4418C27B7C2}"));
            guidsTypeMap.Add(typeof(PipeEndPrepWindow), new Guid("{7FAA23DB-AAFD-4F47-857E-C06D21B2717E}"));
            guidsTypeMap.Add(typeof(SimpleFilterDockablePane), new Guid("{5539CADC-1545-4427-9495-3E74FAB6071C}"));
            guidsTypeMap.Add(typeof(ComplexFilterDockablePane), new Guid("{DEF29C25-F7BA-4BE6-A9CD-B1702328043E}"));
            /*preferenceWindowId = new Guid("{1524F3C0-B6EB-4A32-985F-E3A7071F32B2}");
            pipingUpdaterId = new Guid("{BD82E5B2-D984-49F9-8442-ED572680656D}");
            pipingConnectionsUpdaterId = new Guid("{892AC63B-A827-466F-A8EA-F4418C27B7C2}");
            pipeEndPrepWindowId = new Guid("{7FAA23DB-AAFD-4F47-857E-C06D21B2717E}");
            simpleFilterPaneId = new Guid("{5539CADC-1545-4427-9495-3E74FAB6071C}");
            complexFilterPaneId = new Guid("{DEF29C25-F7BA-4BE6-A9CD-B1702328043E}");*/

        }
        public static Guid GetId<T>()
        {
            Type t = typeof(T);
            if (guidsTypeMap.ContainsKey(t)) return guidsTypeMap[t];

            return Guid.Empty;
        }
        public static Guid GetId(Type t)
        {
            if (guidsTypeMap.ContainsKey(t)) return guidsTypeMap[t];

            return Guid.Empty;
        }
    }
}
