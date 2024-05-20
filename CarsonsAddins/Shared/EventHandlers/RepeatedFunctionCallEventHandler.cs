using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CarsonsAddins.Utils
{
    class RepeatedFunctionCallEventHandler : IExternalEventHandler
    {
        public delegate void CallFunctionEvent(Document doc);
        public CallFunctionEvent Functions;

        public void Execute(UIApplication app)
        {
            Functions?.Invoke(app.ActiveUIDocument.Document);
        }

        public string GetName()
        {
            return "Repeated Function Call Event Handler";
        }
    }

}
