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
            PushButtonData pushButtonData = new PushButtonData("SmartFlipFittingCommand", "Smart Flip Pipe Fitting", assembly.Location, "CarsonsAddins.SmartFlipCommand")
            {
                Image = Utils.MediaUtils.GetImage(assembly, "CarsonsAddins.Resources.flip_32.png"),
                LargeImage = Utils.MediaUtils.GetImage(assembly, "CarsonsAddins.Resources.flip_32.png"),
                ToolTip = "Disconnects Selected Fitting Before Flipping it and Reconnecting it"
            };
            return pushButtonData;
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
            
            while (true) //this is not good practice, but it is the simpliest way to achieve this loop without using something like recursion. This loop is only exited on either the user cancelling their action or an error.
            {
                Transaction transaction = new Transaction(doc, "SmartFlipFittingCommand");
                transaction.Start();
                try
                {
                    
                    Reference elemReference = uidoc.Selection.PickObject(ObjectType.Element, new SelectionFilters.SelectionFilter_PipeFittingPartType(PartType.PipeFlange), "Please select the pipe flange or bell you wish to flip.");
                    if (elemReference == null)
                    {
                        transaction.RollBack();
                        return Result.Cancelled;
                    }
                    Element elem = doc.GetElement(elemReference);
                    elements.Insert(elem);
                    if (elem == null)
                    {
                        transaction.RollBack();
                        return Result.Cancelled;
                    }

                    else if (!(elem is FamilyInstance familyInstance))
                    {
                        transaction.RollBack();
                        return Result.Cancelled;
                    }

                    else if (!Flip(doc, familyInstance))
                    {
                        transaction.RollBack();
                        message += "Could not flip element.\n";
                        return Result.Failed;
                    }

                    transaction.Commit();
                    
                }
                catch (Exception ex)
                {
                    message += ex.ToString() + '\n';
                    transaction.RollBack();
                    return Result.Cancelled;
                }

            }

        }

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="fitting"></param>
        /// <returns></returns>
        private bool Flip(Document doc, FamilyInstance fitting)
        {
            Connector[] connectors = Utils.ConnectionUtils.GetConnectors(fitting);
            Connector primaryConnected = null;
            Connector secondaryConnected = null;
            
            Connector primaryConnector = null;
            Connector secondaryConnector = null;
            bool isPinned = fitting.Pinned;
            if (isPinned) fitting.Pinned = false;
            for (int i = 0; i < connectors.Length; i++)
            {
                
                
                SubTransaction subA = new SubTransaction(doc);
                subA.Start();
                Connector other = Utils.ConnectionUtils.TryGetConnected(connectors[i]);
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
                catch (Exception ex)
                {
                    TaskDialog.Show("ERR Disconnecting ( " + i.ToString() + " )", ex.Message);
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
                TaskDialog.Show("ERR Reconnecting", ex.Message);
                subD.RollBack();
            }
            fitting.Pinned = isPinned;
            return true;
            //flag++;
        }


    }
    
}
