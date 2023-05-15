using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using finalProject.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace finalProject.TabCreator.ButtonClasses
{
    [TransactionAttribute(TransactionMode.Manual)]
    internal class ColumnCreationButton : IExternalCommand
    {
        #region Properties
        public static UIDocument Uidoc { get; set; }
        #endregion

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Uidoc = commandData.Application.ActiveUIDocument;
            Document doc = Uidoc.Document;
            try
            {
                ColumnCreation m = new ColumnCreation();
                m.ShowDialog();
                return Result.Succeeded;
            }
            catch (Exception)
            {
                return Result.Failed;
            }
        }
    }
}

