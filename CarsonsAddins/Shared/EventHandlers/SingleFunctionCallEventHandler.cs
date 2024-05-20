using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarsonsAddins.Shared.EventHandlers
{
    class SingleCallFunctionEventHandler : IExternalEventHandler
    {
        public delegate void CallFunctionEvent(Document doc);
        public CallFunctionEvent Functions;

        public void Execute(UIApplication app)
        {
            Functions?.Invoke(app.ActiveUIDocument.Document);
            Functions = null;
        }

        public string GetName()
        {
            return "Generic Function Event Handler";
        }
    }
}
