using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CarsonsAddins
{


    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    class SmartFlipCommand : IExternalCommand, ISettingsComponent
    {
        public const bool IsWIP = false;
        public PushButtonData RegisterButton(Assembly assembly)
        {
            PushButtonData pushButtonData = new PushButtonData("SmartFlipFittingCommand", "Smart Flip Pipe Fitting", assembly.Location, "CarsonsAddins.SmartFlipCommand");
            pushButtonData.Image = Util.GetImage(assembly, "CarsonsAddins.Resources.flip_32.png");
            pushButtonData.LargeImage = Util.GetImage(assembly, "CarsonsAddins.Resources.flip_32.png");
            pushButtonData.ToolTip = "Disconnects Selected Fitting Before Flipping it and Reconnecting it";
            return pushButtonData;
        }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return Execute(commandData.Application, ref message, elements);
        }
        public Result Execute(UIApplication uiapp, ref string message, ElementSet elements)
        {
            int flag = 0;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            if (doc.IsFamilyDocument)
            {
                TaskDialog.Show("Question Mark Dimensions Command", "Command should not be used within a family document.");
                return Result.Failed;
            }
            while (true)
            {
                Transaction transaction = new Transaction(doc, "SmartFlipFittingCommand");
                transaction.Start();
                try
                {
                    //SelectionFilter
                    Reference elemReference = uidoc.Selection.PickObject(ObjectType.Element, new SelectionFilter_PipeFittingPartType(PartType.PipeFlange), "Please select the pipe flange or bell you wish to flip.");
                    if (elemReference == null)
                    {
                        transaction.RollBack();
                        return Result.Succeeded;
                    }
                    flag++;
                    Element elem = doc.GetElement(elemReference);
                    if (elem == null)
                    {
                        transaction.RollBack();
                        return Result.Cancelled;
                    }
                    flag++;

                    FamilyInstance familyInstance = elem as FamilyInstance;
                    if (familyInstance == null)
                    {
                        transaction.RollBack();
                        return Result.Cancelled;
                    }
                    flag++;

                    if (!Flip(doc, familyInstance))
                    {
                        transaction.RollBack();
                        return Result.Failed;
                    }
                    flag++;
                    transaction.Commit();
                    //return Result.Succeeded;
                }
                catch (Exception ex)
                {
                    transaction.RollBack();
                    return Result.Failed;
                }

            }


        }

        

        private bool Flip(Document doc, FamilyInstance fitting)
        {
            List<Connector> connectors = Util.GetConnectors(fitting);
            Connector primaryConnected = null;
            Connector secondaryConnected = null;
            
            Connector primaryConnector = null;
            Connector secondaryConnector = null;
            bool isPinned = fitting.Pinned;
            if (isPinned) fitting.Pinned = false;
            for (int i = 0; i < connectors.Count; i++)
            {
                
                
                SubTransaction subA = new SubTransaction(doc);
                subA.Start();
                Connector other = Util.TryGetConnected(connectors[i]);
                if (connectors[i].GetMEPConnectorInfo().IsPrimary) 
                {
                    primaryConnector = connectors[i];
                    primaryConnected = other;
                }

                else 
                {
                    secondaryConnector = connectors[i]; 
                    secondaryConnected = other; 
                }
                if (!connectors[i].IsConnected || other == null) { subA.RollBack(); continue; };
                
                try
                {
                    connectors[i].DisconnectFrom(other);
                    subA.Commit();
                }
                catch(Exception ex)
                {
                    TaskDialog.Show("ERR Disconnecting",i.ToString());
                    subA.RollBack();
                }

                
            }
            SubTransaction subB = new SubTransaction(doc);
            subB.Start();
            if (!fitting.flipHand())
            {
                TaskDialog.Show("FLIP ERROR", "CANNOT FLIP");
                subB.RollBack();
                fitting.Pinned = isPinned;
                return false;
            }
            subB.Commit();
            SubTransaction subC = new SubTransaction(doc);
            subC.Start();
            try
            {
                ElementTransformUtils.MoveElement(doc, fitting.Id, primaryConnector.Origin - secondaryConnector.Origin );
                subC.Commit();
            }
            catch (Exception e)
            {
                TaskDialog.Show("ERR Moving", e.Message);
                subC.RollBack();
            }

            SubTransaction subD = null;
            try
            {
                subD = new SubTransaction(doc);

                subD.Start();
                if (secondaryConnected != null) primaryConnector.ConnectTo(secondaryConnected);
                if (primaryConnected != null) secondaryConnector.ConnectTo(primaryConnected);
                subD.Commit();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("ERR Reconnecting", "");
                subD.RollBack();
            }
            fitting.Pinned = isPinned;
            return true;
            //flag++;
        }


    }
    
}
