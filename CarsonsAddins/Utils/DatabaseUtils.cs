using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarsonsAddins.Utils
{
    /// <summary>
    /// Static class containing frequently referenced functions thaat retrieves data from Revit's Database.
    /// </summary>
    public static class DatabaseUtils
    {
        /// <summary>
        /// Retrieves all of the Elements with the ElementType of "PipeType" within the Revit DB
        /// </summary>
        /// <param name="doc">The active Revit Document</param>
        /// <returns>a List containing all of the PipeType Families (i.e. Generic, Flanged, Victaulic, etc. )</returns>
        public static List<Element> GetAllPipeTypeFamilies(Document doc)
        {
            return new FilteredElementCollector(doc).OfClass(typeof(PipeType)).ToElements() as List<Element>;
        }

        /// <summary>
        /// Calls the GetAllPipeTypeFamilies function and then returns a list of all the PipeType names
        /// </summary>
        /// <param name="doc">The active Revit Document</param>
        /// <returns>a List containing all of the PipeType Family Names</returns>
        public static List<string> GetAllPipeTypeNames(Document doc)
        {
            return (List<string>)GetAllPipeTypeFamilies(doc).Select(ps => ps.Name);
        }

        /// <summary>
        /// Retrieves all of the Elements within the PipingSystem category. This returns all of the instances of each Piping System 
        /// </summary>
        /// <param name="doc">The active Revit Document</param>
        /// <returns>a List containing all of the Pipe System instances ( i.e. for the BYPASS Piping System, it would return BYP1, BYP2, BYP3, etc. )</returns>
        public static List<Element> GetAllPipeSystemInstances(Document doc)
        {
            return new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_PipingSystem).ToElements() as List<Element>;
        }

        /// <summary>
        /// Retrieves all of the ElementTypes within the PipingSystem category. This returns all of the Piping System ( i.e. AIR INTAKE, BYPASS, etc. )
        /// </summary>
        /// <param name="doc">The active Revit Document</param>
        /// <returns>a List containing all of the Pipe System Families ( i.e. AIR INTAKE, BYPASS, etc. )</returns>
        public static List<Element> GetAllPipeSystemFamilies(Document doc)
        {
            return new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_PipingSystem).WhereElementIsElementType().ToElements() as List<Element>;
        }

        /// <summary>
        /// Calls the GetAllPipeSystemFamilies function and then returns a list of all the Pipe System names
        /// </summary>
        /// <param name="doc">The active Revit Document</param>
        /// <returns>a List containing all of the Pipe System Family Names ( i.e. "AIR INTAKE", "BYPASS", etc. )</returns>
        public static List<string> GetAllPipeSystemNames(Document doc) { return (List<string>)GetAllPipeSystemFamilies(doc).Select(ps => ps.Name); }

        /// <summary>
        /// Retrieves all list of all of the Pipe Fitting Families within the Revit DB
        /// </summary>
        /// <param name="doc">The active Revit Document</param>
        /// <returns>a List containing all of the Pipe Fitting Families ( i.e. Flange, TR-Flex Bell, MJ Bell, etc. )</returns>
        public static List<Element> GetAllPipeFittingFamilies(Document doc)
        {
            return new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_PipeFitting).WhereElementIsElementType().ToElements() as List<Element>;
        }

        ///<summary>
        ///Calls the GetAllPipeFittingFamilies function and then returns a list of all the Pipe Fitting Family Names
        ///</summary>
        public static List<string> GetAllPipeFittingFamilyNames(Document doc)
        {
            return (List<string>)GetAllPipeFittingFamilies(doc).Select(ps => ps.Name);
        }
    }
}
