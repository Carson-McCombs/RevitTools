using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarsonsAddins.Utils
{
    class GenericFunctionEventHandler : IExternalEventHandler
    {
        public delegate void CallFunctionEvent();
        public CallFunctionEvent FunctionQueue;

        public void Execute(UIApplication app)
        {
            FunctionQueue?.Invoke();
        }

        public string GetName()
        {
            return "Generic Function Event Handler";
        }
    }
}
