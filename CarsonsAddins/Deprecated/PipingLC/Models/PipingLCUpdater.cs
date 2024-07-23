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
    /// <summary>
    /// WIP - 
    /// Updater in charge of communicating with the Linings & Coatings Preference Window
    /// </summary>
    class PipingLCUpdater : IUpdater
    {
        private UpdaterId updaterId;
        private PipingLCDockablePane preferenceWindow;
        public PipingLCUpdater(AddInId addinId)
        {
            updaterId = new UpdaterId(addinId, ApplicationIds.GetId(typeof(PipingLCUpdater)));
            RegisterUpdater();
            RegisterTriggers();
        }
        public void LinkToPreferenceWindow(PipingLCDockablePane preferenceWindow)
        {
            this.preferenceWindow = preferenceWindow;
        }
        public void RegisterUpdater()
        {
            if (UpdaterRegistry.IsUpdaterRegistered(updaterId)) Unregister();
            UpdaterRegistry.RegisterUpdater(this);
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
            ElementFilter elementFilter = new ElementMulticategoryFilter(new List<BuiltInCategory>() { BuiltInCategory.OST_PipeCurves, BuiltInCategory.OST_PipeFitting }); //new ElementCategoryFilter(BuiltInCategory.OST_PipeCurves);//
            UpdaterRegistry.AddTrigger(updaterId, elementFilter, Element.GetChangeTypeElementAddition());

        }

        public void Execute(UpdaterData data)
        {
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
