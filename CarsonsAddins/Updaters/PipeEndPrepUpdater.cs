using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarsonsAddins
{
    class PipeEndPrepUpdater : IUpdater
    {
        private readonly UpdaterId updaterId;
        private PipeEndPrepWindow pipeEndPrepWindow;
        private bool enabled = false;
        public PipeEndPrepUpdater(AddInId addinId)
        {
            //updaterId = new UpdaterId(addinId, ApplicationIds.pipingConnectionsUpdaterId);
            updaterId = new UpdaterId(addinId, ApplicationIds.GetId(GetType()));
            
            Register();
            RegisterTriggers();
        }
        public void Link(PipeEndPrepWindow pipeEndPrepWindow)
        {
            //TaskDialog.Show("PEP Updater Linked", "Linked");
            this.pipeEndPrepWindow = pipeEndPrepWindow;
            pipeEndPrepWindow.ToggleUpdater += ToggleEnabled;
        }
        public void ToggleEnabled(bool enabled)
        {
            //TaskDialog.Show("PEP Updater Toggled", enabled.ToString());
            //if (!UpdaterRegistry.IsUpdaterRegistered(updaterId)) return;
            //if (UpdaterRegistry.IsUpdaterEnabled(updaterId) == enabled) return;
            
            //if (enabled) UpdaterRegistry.EnableUpdater(updaterId);
            //else UpdaterRegistry.DisableUpdater(updaterId);
            this.enabled = enabled;


        }
        private void Register()
        {
            if (UpdaterRegistry.IsUpdaterRegistered(updaterId)) Unregister();

            UpdaterRegistry.RegisterUpdater(this);
            //UpdaterRegistry.DisableUpdater(updaterId);
            //TaskDialog.Show("REGISTERING UPDATER", id.ToString());
        }
        public void Unregister()
        {
            UpdaterRegistry.RemoveAllTriggers(updaterId);
            UpdaterRegistry.UnregisterUpdater(updaterId);
        }
        private void RegisterTriggers()
        {
            if (updaterId == null) return;
            if (!UpdaterRegistry.IsUpdaterRegistered(updaterId)) return;
            UpdaterRegistry.RemoveAllTriggers(updaterId);
            ElementFilter elementFilter = new ElementCategoryFilter(BuiltInCategory.OST_PipeCurves);//new ElementMulticategoryFilter(new List<BuiltInCategory>() { BuiltInCategory.OST_PipeCurves, BuiltInCategory.OST_PipeFitting });//new ElementCategoryFilter(BuiltInCategory.OST_ConnectorElem);//
            UpdaterRegistry.AddTrigger(updaterId, elementFilter, Element.GetChangeTypeGeometry());
            UpdaterRegistry.AddTrigger(updaterId, elementFilter, Element.GetChangeTypeElementAddition());
        }
        public void Execute(UpdaterData data)
        {
            //TaskDialog.Show("PEP Updater Triggered", "Executed");
            if (!enabled) return;
            if (pipeEndPrepWindow == null) return;
            //ThisApplication.instance.ActiveUIDocument.Selection.SetElementIds(data.GetModifiedElementIds());
            //string s = "Added Elements: \n";
            
            //Transaction trans = new Transaction(data.GetDocument());
            //trans.Start("UpdateConnections");

            List<ElementId> elementIds = new List<ElementId>();
            elementIds.AddRange(data.GetAddedElementIds());
            elementIds.AddRange(data.GetModifiedElementIds());
            Document doc = data.GetDocument();
            try
            {
                foreach (ElementId elementId in elementIds)
                {
                    if (elementId == null) continue; 
                    if (elementId == ElementId.InvalidElementId) continue; 
                    Element elem = doc.GetElement(elementId);
                    if (elem == null) continue; 
                    Pipe pipe = (Pipe)elem;
                    if (pipe == null)  continue; 
                    pipeEndPrepWindow.UpdatePipeEndPrep(pipe);
                    //s = s + pipe.ToString() + '\n';
                    //s = s + GetConnectorNames(pipe) + "\n\n";
                }
            }catch (Exception ex)
            {
                TaskDialog.Show("PEP Updater Error", "Execute: \n" + ex.Message);

            }
            
            //trans.Commit();
            //TaskDialog.Show("SYSTEM UPDATED", s);
            
            //ThisApplication.instance.ActiveUIDocument.ShowElements);
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
            return "Piping Connections Updater ";
        }
    }
}
