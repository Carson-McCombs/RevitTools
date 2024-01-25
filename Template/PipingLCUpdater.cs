using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CarsonsAddins
{
    class PipingLCUpdater : IUpdater
    {
        private UpdaterId updaterId;
        private PreferenceWindow preferenceWindow;
        //Document doc;
        public PipingLCUpdater(AddInId addinId)
        {
            updaterId = new UpdaterId(addinId, ApplicationIds.GetId<PipingLCUpdater>());
            //updaterId = new UpdaterId(addinId, ApplicationIds.pipingUpdaterId);
            //id = new UpdaterId(addinId, new Guid("{BD82E5B2-D984-49F9-8442-ED572680656D}"));

            RegisterUpdater();
            RegisterTriggers();
        }
        public void LinkToPreferenceWindow(PreferenceWindow preferenceWindow)
        {
            this.preferenceWindow = preferenceWindow;
        }
        public void RegisterUpdater()
        {
            if (UpdaterRegistry.IsUpdaterRegistered(updaterId)) Unregister();

            UpdaterRegistry.RegisterUpdater(this);
            //TaskDialog.Show("REGISTERING UPDATER", id.ToString());
        }
        public void Unregister()
        {
            UpdaterRegistry.RemoveAllTriggers(updaterId);
            UpdaterRegistry.UnregisterUpdater(updaterId);
        }
        public void RegisterTriggers()
        {
            if (updaterId == null) return;
            if (!UpdaterRegistry.IsUpdaterRegistered(updaterId)) return;
            UpdaterRegistry.RemoveAllTriggers(updaterId);
            ElementFilter elementFilter = new ElementCategoryFilter(BuiltInCategory.OST_PipeCurves);//new ElementMulticategoryFilter(new List<BuiltInCategory>() { BuiltInCategory.OST_PipeCurves, BuiltInCategory.OST_PipeFitting });
            UpdaterRegistry.AddTrigger(updaterId, elementFilter, Element.GetChangeTypeElementAddition());

        }

        public void Execute(UpdaterData data)
        {
            //Document doc = data.GetDocument();
            List<ElementId> elemIds = data.GetAddedElementIds() as List<ElementId>;
            if (preferenceWindow == null)
            {
                TaskDialog.Show("ERROR IN PipingLCUpdater", "Preference Window Reference is Null!");
                return;
            };
            preferenceWindow.SetToPipeTypeLCs(elemIds);

        }

        public string GetAdditionalInformation()
        {
            return "Updates Each Time a Pipe or Fitting is Placed.";
        }

        public ChangePriority GetChangePriority()
        {
            return ChangePriority.MEPSystems;
        }

        public UpdaterId GetUpdaterId()
        {
            return updaterId;
        }

        public string GetUpdaterName()
        {
            return "PipingLCUpdater";
        }
    }

}
