using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CarsonsAddins.Utils;
using CarsonsAddins.Properties;
using static CarsonsAddins.Utils.DimensioningUtils;

namespace CarsonsAddins
{








    /// <summary>
    /// I am defining a Pipe Line as all of the connected piping elements ( i.e. Pipes, Pipe Fittings, and Pipe Accessories ) between either a Pipe Junction ( i.e. a Bend, Tee, Wye, Cross, etc. ) or end of pipe ( an empty connection ).
    /// This is meant to be a utility class that will be called for actions such as movement and dimensioning of piping elements.
    /// Future revisions of this will include UI to customize settings.
    /// Note: Currently dimensioning a any junction that is not a bend in Revit does not work. Lateral Tees and Crosses seem to work for dimensioning but not for Routing Preferences. 
    /// Getting the programmatic dimensioning to work is put on delay until either the problem is solved within my current Revit setup or by Revit themselves.
    /// </summary>
    public class PipeLine
    {
        readonly private SelectionFilters.SelectionFilter_PipingElements filter = null;
        readonly private Element[] elements;
        readonly private Element selectedElement;
        public PipeLine(View view, Pipe pipe)
        {
            List<Element> elementList = new List<Element>();
            filter = new SelectionFilters.SelectionFilter_PipingElements(false, true, true, false, true, true, true);
            Connector[] connectors = ConnectionUtils.GetConnectors(pipe);
            AddNextElement_Left(view, connectors[0], ref elementList);
            elementList.Add(pipe);
            selectedElement = pipe;
            AddNextElement_Right(view, connectors[1], ref elementList);
            elements = elementList.ToArray();
        }

        public Element[] GetElements() => elements;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="uidoc">The active UIDocument</param>
        /// <param name="pipe">The selected Pipe</param>
        /// <param name="filter">The SelectionFilter</param>
        /// <returns>An array of elements containing all of the elements in the Pipeline.</returns>
        public PipeLine(View view, Pipe pipe, SelectionFilters.SelectionFilter_PipingElements filter)
        {
            this.filter = filter;
            List<Element> elementList = new List<Element>();
            Connector[] connectors = ConnectionUtils.GetConnectors(pipe);
            AddNextElement_Left(view, connectors[0], ref elementList);
            elementList.Add(pipe);
            AddNextElement_Right(view, connectors[1], ref elementList);

            elements = elementList.ToArray();
        }

        /// <summary>
        /// Recursively adds all the connected piping elements on one side (arbitrarly called the "left" side) of an element
        /// </summary>
        /// <param name="uidoc">The active UIDocument</param>
        /// <param name="current">The current connector/ </param>
        private void AddNextElement_Left(View view, Connector current, ref List<Element> elementList)
        {
            Connector adjacent = ConnectionUtils.GetAdjacentConnector(current);
            if (adjacent == null) return;
            Connector next = ConnectionUtils.TryGetConnected(adjacent);
            if (CanContinue(view, next)) AddNextElement_Left(view, next, ref elementList);
            if (adjacent.IsConnected && !next.Owner.IsHidden(view)) elementList.Add(next.Owner);
        }

        /// <summary>
        /// Recursively adds all the connected piping elements on one side (arbitrarly called the "right" side) of an element
        /// </summary>
        /// <param name="uidoc">The active UIDocument</param>
        /// <param name="current">The current connector/ </param>
        private void AddNextElement_Right(View view, Connector current, ref List<Element> elementList)
        {
            Connector adjacent = ConnectionUtils.GetAdjacentConnector(current);
            if (adjacent == null) return;
            Connector next = ConnectionUtils.TryGetConnected(adjacent);
            if (adjacent.IsConnected && !next.Owner.IsHidden(view)) elementList.Add(next.Owner);

            if (CanContinue(view, next)) AddNextElement_Right(view, next, ref elementList);
            
        }

        /// <summary>
        /// Checks if the Pipe Line extends to the next element, or if the current element is the last one on this end of the Pipeline.
        /// </summary>
        /// <param name="next">The next connector to be checked.</param>
        /// <returns>a boolean value determining if the next connector is still apart of the Pipeline.</returns>
        private bool CanContinue(View view, Connector next)
        {
            if (view == null) return false;
            if (next == null) return false;
            if (next.Owner == null) return false;
            if (next.Id.Equals(ElementId.InvalidElementId)) return false;
            if (!next.IsValidObject) return false;
            return (filter.AllowElement(next.Owner, next));
            
        }



    }
}
