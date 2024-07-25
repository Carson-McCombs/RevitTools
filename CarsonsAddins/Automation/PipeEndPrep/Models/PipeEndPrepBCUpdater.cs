using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static CarsonsAddins.Utils.ConnectionUtils;

namespace CarsonsAddins
{
    class PipeEndPrepBCUpdater : IUpdater
    {

        public delegate void UpdateEndPrepEvent(Document doc, Pipe[] pipes);
        public UpdateEndPrepEvent UpdateEndPrep;
        private readonly UpdaterId updaterId;
        private bool isRunning = false;
        public PipeEndPrepBCUpdater(AddInId addinId)
        {
            updaterId = new UpdaterId(addinId, ApplicationIds.GetId(GetType()));

            Register();
            RegisterTriggers();
        }

        public void SetState(bool state)
        {
            isRunning = state;
        }
        
        private void Register()
        {
            if (UpdaterRegistry.IsUpdaterRegistered(updaterId)) Unregister();

            UpdaterRegistry.RegisterUpdater(this);
        }
        private void RegisterTriggers()
        {
            if (updaterId == null) return;
            if (!UpdaterRegistry.IsUpdaterRegistered(updaterId)) return;
            UpdaterRegistry.RemoveAllTriggers(updaterId);
            ElementFilter elementFilter = new ElementCategoryFilter(BuiltInCategory.OST_PipeCurves);
            UpdaterRegistry.AddTrigger(updaterId, elementFilter, Element.GetChangeTypeGeometry());
            UpdaterRegistry.AddTrigger(updaterId, elementFilter, Element.GetChangeTypeElementAddition());
        }
        public void Unregister()
        {
            UpdaterRegistry.RemoveAllTriggers(updaterId);
            UpdaterRegistry.UnregisterUpdater(updaterId);
        }
        

        public void Execute(UpdaterData data)
        {
            if (!isRunning) return;
            try
            {
                Document doc = data.GetDocument();
                List<ElementId> elementIds = data.GetAddedElementIds().ToList();
                elementIds.AddRange(data.GetModifiedElementIds());
                Pipe[] pipes = elementIds.Select(id => doc.GetElement(id)).Cast<Pipe>().ToArray();
                UpdateEndPrep?.Invoke(doc, pipes);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Pipe End Prep By Connectors - ERROR", ex.Message);
            }
            
        }

        public string GetAdditionalInformation()
        {
            return "NA";
        }

        public ChangePriority GetChangePriority()
        {
            return ChangePriority.Connections;
        }

        public UpdaterId GetUpdaterId()
        {
            return updaterId;
        }

        public string GetUpdaterName()
        {
            return "Piping Connections Updater By Connector";
        }
    }
}
