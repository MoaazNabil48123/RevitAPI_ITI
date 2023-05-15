using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace finalProject.TabCreator.ButtonClasses
{
    [Transaction(TransactionMode.Manual)]
    public class Adjust_foundation_optimized : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document Doc = uidoc.Document;
            using (Transaction trans = new Transaction(Doc, "Adjusting foudnation offset"))
            {
                trans.Start();
                var Foundation = new FilteredElementCollector(Doc).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_StructuralFoundation).OfType<FamilyInstance>().Cast<Element>();
                var slabs = new FilteredElementCollector(Doc).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_StructuralFoundation)
                 .Cast<Element>().Where(e => e is Floor);
                var structureslabs = slabs.Where(e => e.LookupParameter("Structural").AsInteger() == 1);
                var allcolumns = new FilteredElementCollector(Doc).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_StructuralColumns).OfType<FamilyInstance>().Cast<Element>().ToList();
                List<Element> Elements = new List<Element>();
                List<Element> slabselements = new List<Element>();

                List<double> thickkness = new List<double>();
                //getting thickness of all isolated footings
                foreach (var instance in Foundation)
                {
                    var type = instance.GetTypeId();
                    Element elementtype = Doc.GetElement(type);
                    Elements.Add(elementtype);
                    var thicknessforfoundation = double.Parse(elementtype.get_Parameter(BuiltInParameter.STRUCTURAL_FOUNDATION_THICKNESS).AsValueString());
                    thickkness.Add(thicknessforfoundation);


                }
                foreach (var instance in structureslabs)
                {
                    var type = instance.GetTypeId();
                    Element elementtype = Doc.GetElement(type);
                    slabselements.Add(elementtype);
                    var thicknessforslabs = double.Parse(elementtype.get_Parameter(BuiltInParameter.STRUCTURAL_FOUNDATION_THICKNESS).AsValueString());
                    thickkness.Add(thicknessforslabs);

                }
                double highestthickness = thickkness.Max(); ;

                //doingoffset
                foreach (var instance in structureslabs)
                {
                    var type = instance.GetTypeId();
                    Element elementtype = Doc.GetElement(type);
                    double elementthickness = double.Parse(elementtype.get_Parameter(BuiltInParameter.STRUCTURAL_FOUNDATION_THICKNESS).AsValueString());
                    double offset = elementthickness - highestthickness;
                    instance.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).Set(offset / 304.8);
                    var bbisomaxX = instance.get_BoundingBox(Doc.ActiveView).Max.X;
                    var bbisomaxY = instance.get_BoundingBox(Doc.ActiveView).Max.Y;
                    var bbisminX = instance.get_BoundingBox(Doc.ActiveView).Min.X;
                    var bbisminY = instance.get_BoundingBox(Doc.ActiveView).Min.Y;
                    foreach (var col in allcolumns)
                    {
                        LocationPoint pointofcolumn = col.Location as LocationPoint;
                        var columnpointX = pointofcolumn.Point.X;
                        var columnpointY = pointofcolumn.Point.Y;
                        if ((columnpointX < bbisomaxX) && (columnpointX > bbisminX) && (columnpointY < bbisomaxY) && (columnpointY > bbisminY))
                        {
                            var parameterbaseoffset =
                     col.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
                            parameterbaseoffset.Set(offset / 304.8);
                        }


                    }
                }
                //doing offset in foundation
                foreach (var instance in Foundation)
                {
                    var type = instance.GetTypeId();
                    Element elementtype = Doc.GetElement(type);
                    double elementthickness = double.Parse(elementtype.get_Parameter(BuiltInParameter.STRUCTURAL_FOUNDATION_THICKNESS).AsValueString());
                    double offset = elementthickness - highestthickness;
                    instance.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).Set(offset / 304.8);
                    var bbisomaxX = instance.get_BoundingBox(Doc.ActiveView).Max.X;
                    var bbisomaxY = instance.get_BoundingBox(Doc.ActiveView).Max.Y;
                    var bbisminX = instance.get_BoundingBox(Doc.ActiveView).Min.X;
                    var bbisminY = instance.get_BoundingBox(Doc.ActiveView).Min.Y;
                    foreach (var col in allcolumns)
                    {
                        LocationPoint pointofcolumn = col.Location as LocationPoint;
                        var columnpointX = pointofcolumn.Point.X;
                        var columnpointY = pointofcolumn.Point.Y;
                        if ((columnpointX < bbisomaxX) && (columnpointX > bbisminX) && (columnpointY < bbisomaxY) && (columnpointY > bbisminY))
                        {
                            var parameterbaseoffset =
                     col.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
                            parameterbaseoffset.Set(offset / 304.8);
                        }


                    }


                }
                trans.Commit();
            }
            return Result.Succeeded;
        }

    }
}
