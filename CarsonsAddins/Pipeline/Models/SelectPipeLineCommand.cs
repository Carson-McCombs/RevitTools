﻿using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CarsonsAddins
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    class SelectPipeLineCommand : IExternalCommand, ISettingsComponent
    {
        public const string FolderName = "";
        public const bool IsWIP = false;

        public PushButtonData RegisterButton(Assembly assembly)
        {
            PushButtonData pushbuttonData = new PushButtonData("SelectPipeLineCommand", "Select Pipe Line", assembly.Location, "CarsonsAddins.SelectPipeLineCommand")
            {
                AvailabilityClassName = typeof(Setup.Availablity.Availability_ProjectDocumentAndActiveView).FullName,
                Image = Utils.MediaUtils.GetImage(assembly, "CarsonsAddins.Resources.select_pipeline_icon_32.png"),
                LargeImage = Utils.MediaUtils.GetImage(assembly, "CarsonsAddins.Resources.select_pipeline_icon_32.png"),
                ToolTip = "Selects all pipes connected to selected pipe."
            };
            return pushbuttonData;
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            if (doc.IsFamilyDocument)
            {
                message = "Command should not be used within a family document.";
                return Result.Failed;
            }
            Transaction transaction = new Transaction(doc);
            transaction.Start("SelectPipeLineCommand");
            try
            {
                Reference pipeReference = uidoc.Selection.PickObject(ObjectType.Element, new SelectionFilters.SelectionFilter_Pipe(), "Please select a Pipe.");
                if (!(doc.GetElement(pipeReference.ElementId) is Pipe pipe))
                {
                    transaction.RollBack();
                    return Result.Cancelled;
                }
                PipeLine pipeLine = new PipeLine(doc.ActiveView, pipe);
                Element[] elementList = pipeLine.GetElements();

                ElementId[] elementIds = elementList.Select(element => element.Id).ToArray();
                Array.ForEach(elementList, element => elements.Insert(element));

                uidoc.Selection.SetElementIds(elementIds);
                transaction.Commit();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                transaction.RollBack();
                message = ex.Message;
                return Result.Failed;
            }
        }

    }

 
    
}
