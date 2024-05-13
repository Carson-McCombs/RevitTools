using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarsonsAddins.Setup.Availablity
{
    /// <summary>
    /// Only allows command to be available when the UIDocument is not null ( i.e. a document is opened ) and the opened document is not a family document.
    /// </summary>
    public class Availability_ProjectDocumentsOnly : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
        {
            return !applicationData.ActiveUIDocument?.Document?.IsFamilyDocument ?? false;
        }
    }

    
}
