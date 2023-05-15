using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using finalProject.RevitMethods;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace finalProject.TabCreator.ButtonClasses
{
    [TransactionAttribute(TransactionMode.Manual)]
    internal class ColumnDimensionButton : IExternalCommand
    {

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uiDoc = commandData.Application.ActiveUIDocument;
                RevitMethods.RevitMethods.CreateColumnDimension(uiDoc);
                return Result.Succeeded;
            }
            catch (Exception)
            {

                return Result.Failed;
            }
        }
    }

}
