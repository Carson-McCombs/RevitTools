﻿using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarsonsAddins
{
    /// <summary>
    /// Used to track elements being used to avoid stale reference errors. Currently filtering within the updater instead of with an ElementFilter. 
    /// This is because I currently can not find documentation or know if it is possible to extend the ElementFilter, QuickElementFilter, or ElementSetIdFilter classes. 
    /// Ideally an ElementSetIdFilter with a dynamic set of ElementIds would be used.
    /// </summary>
    class ParameterManagerUpdater : IUpdater
    {
        private readonly UpdaterId updaterId;
        private List<ElementId> elementIds;
        private ParameterManagerDockablePane parameterManager = null;
        public ParameterManagerUpdater(AddInId addinId, ref List<ElementId> elementIds)
        {
            updaterId = new UpdaterId(addinId, ApplicationIds.GetId(GetType()));
            this.elementIds = elementIds;

            RegisterUpdater();
            RegisterTriggers();

        }

        public void UpdateElementList()
        {
            RegisterTriggers();
        }
        public void Link(ParameterManagerDockablePane parameterManager)
        {
            this.parameterManager = parameterManager;
        }
        private void RegisterUpdater()
        {
            if (UpdaterRegistry.IsUpdaterRegistered(updaterId)) Unregister();
            UpdaterRegistry.RegisterUpdater(this);
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
            ElementFilter filter = new ElementIsElementTypeFilter(true);
            
            UpdaterRegistry.AddTrigger(updaterId, filter, Element.GetChangeTypeAny()); //unfortunately until I figure out how to extend an ElementFilter to track a dynamic set of ElementIds, this is what I will be using (it is slow).
        }
        public void Execute(UpdaterData data)
        {
            if (parameterManager == null || elementIds.Count == 0) return;
            ElementId[] deletedElementIds = data.GetModifiedElementIds().Where(elementId => elementIds.Contains(elementId)).ToArray();
            ElementId[] modifiedElementIds = data.GetModifiedElementIds().Where(elementId => elementIds.Contains(elementId)).ToArray();

            if (deletedElementIds.Length > 0) parameterManager.RemoveElements(deletedElementIds);
            if (modifiedElementIds.Length > 0) parameterManager.RefreshElements(modifiedElementIds);
        }

        public string GetAdditionalInformation()
        {
            return "Tracks when Elements within the Parameter Manager have changed within Revit.";
        }

        public ChangePriority GetChangePriority()
        {
            return ChangePriority.Annotations;
        }

        public UpdaterId GetUpdaterId()
        {
            return updaterId;
        }

        public string GetUpdaterName()
        {
            return "Parameter Manager Updater";
        }
    }

    
}
