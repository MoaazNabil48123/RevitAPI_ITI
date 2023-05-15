using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;


namespace finalProject.RevitMethods
{
    internal class RevitMethods
    {
        #region Column Creation From CAD
        public static void CreateColumnsFromCad(UIDocument uiDoc, string topLevelName, string BottomLevelName, double topOffset, double bottomOffset)
        {
            try
            {
                Autodesk.Revit.DB.Document doc = uiDoc.Document;
                // select cad link
                Reference cadRef = uiDoc.Selection.PickObject(ObjectType.Element);
                Element cadElement = doc.GetElement(cadRef);
                //Level level = doc.GetElement(cadElement.LevelId) as Level;
                Level bottomLevel = new FilteredElementCollector(doc).OfClass(typeof(Level)).FirstOrDefault(l => l.Name == BottomLevelName) as Level;
                Level topLevel = new FilteredElementCollector(doc).OfClass(typeof(Level)).FirstOrDefault(l => l.Name == topLevelName) as Level;

                #region Select Columns
                // get circular columns
                Options op = new Options();
                List<Arc> circularColList = (cadElement.get_Geometry(op).FirstOrDefault() as GeometryInstance).GetSymbolGeometry().OfType<Arc>()
                                            .Where(arc => (doc.GetElement(arc.GraphicsStyleId) as GraphicsStyle).GraphicsStyleCategory.Name == "Columns").ToList();
                //get rec columns and
                List<PolyLine> recColList = (cadElement.get_Geometry(op).FirstOrDefault() as GeometryInstance).GetSymbolGeometry().OfType<PolyLine>()
                                            .Where(pl => (doc.GetElement(pl.GraphicsStyleId) as GraphicsStyle).GraphicsStyleCategory.Name == "Columns").ToList();
                #endregion

                #region Circular Columns
                for (int i = 0; i < circularColList.Count; i++)
                {
                    Arc CurrentCol = circularColList[i];
                    double diameter_ft = CurrentCol.Radius * 2;
                    double diameter_mm = diameter_ft * 304.8;
                    XYZ center = CurrentCol.Center;
                    string familySymbolName = $"{Math.Round(diameter_mm,0)} mm";
                    //Check if the type is existed
                    FamilySymbol familysymbol = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralColumns)
                                                .WhereElementIsElementType().OfType<FamilySymbol>()
                                                .FirstOrDefault(ft => ft.Name == familySymbolName);
                    //create new type if not
                    if (familysymbol == null)
                    {
                        NewCircularColumnType(doc, familySymbolName, diameter_ft);
                        familysymbol = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralColumns)
                                                    .WhereElementIsElementType().OfType<FamilySymbol>()
                                                    .FirstOrDefault(ft => ft.Name == familySymbolName);
                    }
                    //create the column
                    using (Transaction transaction = new Transaction(doc, "create columns"))
                    {
                        transaction.Start();
                        if (!familysymbol.IsActive)
                        {
                            familysymbol.Activate();
                        }
                        FamilyInstance familyInstance = doc.Create.NewFamilyInstance(center, familysymbol, bottomLevel, StructuralType.NonStructural);
                        familyInstance.get_Parameter(BuiltInParameter.SCHEDULE_TOP_LEVEL_PARAM).Set(topLevel.Id);
                        familyInstance.get_Parameter(BuiltInParameter.SCHEDULE_TOP_LEVEL_OFFSET_PARAM).Set(topOffset / 304.8);
                        familyInstance.get_Parameter(BuiltInParameter.SCHEDULE_BASE_LEVEL_OFFSET_PARAM).Set(bottomOffset / 304.8);
                        transaction.Commit();
                    }
                }
                #endregion

                #region Rectangular Columns
                for (int i = 0; i < recColList.Count; i++)
                {
                    PolyLine currentCol = recColList[i];
                    XYZ p1 = currentCol.GetCoordinate(0);
                    XYZ p2 = currentCol.GetCoordinate(1);
                    XYZ p3 = currentCol.GetCoordinate(2);
                    XYZ center = new XYZ((p1.X + p3.X) / 2, (p1.Y + p3.Y) / 2, 0);
                    double b_ft = p2.DistanceTo(p1);
                    double h_ft = p2.DistanceTo(p3);
                    double radAngle;
                    //familysymbol name
                    string familySymbolName;
                    if (b_ft > h_ft)
                    {
                        double temp = b_ft;
                        b_ft = h_ft;
                        h_ft = temp;
                        double tan = (p2.Y - p1.Y) / (p3.X - p3.X);
                        radAngle = Math.Atan(tan);
                    }
                    else
                    {
                        double tan = (p2.Y - p1.Y) / (p2.X - p1.X);
                        radAngle = Math.Atan(tan);
                    }
                    familySymbolName = $"{Math.Round(b_ft * 304.8, 1)} x {Math.Round(h_ft * 304.8, 0)} mm";
                    //Check if the type is existed
                    FamilySymbol familysymbol = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralColumns)
                                                .WhereElementIsElementType().OfType<FamilySymbol>()
                                                .FirstOrDefault(ft => ft.Name == familySymbolName);
                    //create new type if not existed
                    if (familysymbol == null)
                    {
                        NewRecColumnType(doc, familySymbolName, b_ft, h_ft);
                        familysymbol = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralColumns)
                                                    .WhereElementIsElementType().OfType<FamilySymbol>()
                                                    .FirstOrDefault(ft => ft.Name == familySymbolName);
                    }

                    //create the column
                    using (Transaction transaction = new Transaction(doc, "create columns"))
                    {
                        transaction.Start();
                        if (!familysymbol.IsActive)
                        {
                            familysymbol.Activate();
                        }
                        FamilyInstance familyInstance = doc.Create.NewFamilyInstance(center, familysymbol, bottomLevel, StructuralType.NonStructural);
                        familyInstance.get_Parameter(BuiltInParameter.SCHEDULE_TOP_LEVEL_PARAM).Set(topLevel.Id);
                        familyInstance.get_Parameter(BuiltInParameter.SCHEDULE_TOP_LEVEL_OFFSET_PARAM).Set(topOffset / 304.8);
                        familyInstance.get_Parameter(BuiltInParameter.SCHEDULE_BASE_LEVEL_OFFSET_PARAM).Set(bottomOffset / 304.8);
                        #region rotation
                        Line line = Line.CreateBound(center, new XYZ(center.X, center.Y, center.Z + 1));
                        ElementTransformUtils.RotateElement(doc, familyInstance.Id, line, radAngle);
                        #endregion
                        transaction.Commit();
                    }
                }
                #endregion

                //return Result.Succeeded;
            }
            catch (Exception)
            {

                //return Result.Failed;
            }
        }

        public static void NewCircularColumnType(Document doc, string familySymbolName, double diameter_ft)
        {
            Family cirColFamily = new FilteredElementCollector(doc).OfClass(typeof(Family)).OfType<Family>().FirstOrDefault(f => f.Name == "M_Concrete-Round-Column");
            Document familyDocument = doc.EditFamily(cirColFamily);
            FamilyManager familyManager = familyDocument.FamilyManager;
            using (Transaction trans = new Transaction(familyDocument, "Add Type to Family"))
            {
                trans.Start();
                var types = familyManager.Types;
                foreach (FamilyType item in types)
                {
                    if (item.Name == familySymbolName)
                    {
                        familyManager.CurrentType = item;
                        familyManager.DeleteCurrentType();
                    }

                }
                FamilyType newFamilyType = familyManager.NewType(familySymbolName);
                FamilyParameter familyParam = familyManager.get_Parameter("b");
                familyManager.Set(familyParam, diameter_ft);
                trans.Commit();
                // if could not make the change or could not commit it, we return
                if (trans.GetStatus() != TransactionStatus.Committed)
                {
                    return;
                }
            }

            LoadOpts loadOptions = new LoadOpts();
            cirColFamily = familyDocument.LoadFamily(doc, loadOptions);
        }
        public static void NewRecColumnType(Document doc, string familySymbolName, double b, double h)
        {

            Family recColFamily = new FilteredElementCollector(doc).OfClass(typeof(Family)).OfType<Family>().FirstOrDefault(f => f.Name == "M_Concrete-Rectangular-Column");
            Document familyDoc = doc.EditFamily(recColFamily);
            FamilyManager familyManager = familyDoc.FamilyManager;
            using (Transaction trans = new Transaction(familyDoc, "Add Type to Family"))
            {
                trans.Start();
                var types = familyManager.Types;
                foreach (FamilyType item in types)
                {
                    if (item.Name == familySymbolName)
                    {
                        familyManager.CurrentType = item;
                        familyManager.DeleteCurrentType();
                    }
                }
                FamilyType newFamilyType = familyManager.NewType(familySymbolName);

                // look for 'b' and 'h' parameters and set them to 2 feet
                FamilyParameter familyParam = familyManager.get_Parameter("b");
                if (null != familyParam)
                {
                    familyManager.Set(familyParam, b);
                }

                familyParam = familyManager.get_Parameter("h");
                if (null != familyParam)
                {
                    familyManager.Set(familyParam, h);
                }

                trans.Commit();
                // if could not make the change or could not commit it, we return
                if (trans.GetStatus() != TransactionStatus.Committed)
                {
                    return;
                }
            }

            LoadOpts loadOptions = new LoadOpts();
            recColFamily = familyDoc.LoadFamily(doc, loadOptions);

        }
        class LoadOpts : IFamilyLoadOptions
        {
            public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
            {
                overwriteParameterValues = true;
                return true;
            }

            public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
            {
                source = FamilySource.Family;
                overwriteParameterValues = true;
                return true;
            }
        }
        #endregion

        #region Column Dimension
        public static void CreateColumnDimension(UIDocument uiDoc)
        {
            Autodesk.Revit.DB.Document doc = uiDoc.Document;
            using (Transaction trans = new Transaction(doc, "Columns Dimension"))
            {
                trans.Start();

                #region get the V & H axes
                List<Grid> gridList = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Grids).WhereElementIsNotElementType().Cast<Grid>().ToList();
                List<Grid> hGridList = new List<Grid>();
                List<Grid> vGridList = new List<Grid>();
                for (int i = 0; i < gridList.Count; i++)
                {
                    Line l = gridList[i].Curve as Line;
                    if (l.Direction.X == 1 || l.Direction.X == -1)
                    {
                        hGridList.Add(gridList[i]);
                    }
                    if (l.Direction.Y == 1 || l.Direction.Y == -1)
                    {
                        vGridList.Add(gridList[i]);
                    }
                }
                #endregion

                List<FamilyInstance> recColumnList = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralColumns)
                                                  .WhereElementIsNotElementType().Where(e => e.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM).AsValueString() == "M_Concrete-Rectangular-Column")
                                                  .Cast<FamilyInstance>().ToList();



                List<FamilyInstance> circularColumnList = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralColumns)
                                                  .WhereElementIsNotElementType().Where(e => e.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM)
                                                  .AsValueString() == "M_Concrete-Round-Column" || e.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM)
                                                  .AsValueString() == "UC-Universal Columns-Column" || e.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM)
                                                  .AsValueString() == "M_HSS-Hollow Structural Section-Column").Cast<FamilyInstance>().ToList();


                //List<FamilyInstance> iSectionColumnList = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralColumns)
                //                                  .WhereElementIsNotElementType().Where(e => e.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM)
                //                                  .AsValueString() == "UC-Universal Columns-Column").Cast<FamilyInstance>().ToList();
                #region Rectangular columns

                Options op = new Options();
                op.ComputeReferences = true;
                //op.IncludeNonVisibleObjects = true;
                op.View = doc.ActiveView;
                for (int i = 0; i < recColumnList.Count; i++)
                {
                    List<Edge> topEdges = recColumnList[i].get_Geometry(op).OfType<GeometryInstance>()
                        .FirstOrDefault().GetSymbolGeometry().OfType<Solid>().Where(s => s.Volume != 0)
                        .FirstOrDefault().Faces.OfType<PlanarFace>().Where(pf => pf.FaceNormal.Z == -1)
                        .FirstOrDefault().EdgeLoops.OfType<EdgeArray>().FirstOrDefault().OfType<Edge>().ToList();

                    #region get column top edges, x DIM and y DIM
                    double rotation = Math.Round((recColumnList[i].Location as LocationPoint).Rotation, 2);
                    List<Edge> hTopEdges = new List<Edge>();
                    List<Edge> vTopEdges = new List<Edge>();
                    double xDim, yDim;

                    if (rotation % 3.14 == 0)
                    {
                        hTopEdges = topEdges.Where(e => (e.AsCurve() as Line).Direction.X == 1 || (e.AsCurve() as Line).Direction.X == -1).ToList();
                        vTopEdges = topEdges.Where(e => (e.AsCurve() as Line).Direction.Y == 1 || (e.AsCurve() as Line).Direction.Y == -1).ToList();
                        ElementType ee = doc.GetElement(recColumnList[i].GetTypeId()) as ElementType;
                        xDim = ee.LookupParameter("b").AsDouble();
                        yDim = doc.GetElement(recColumnList[i].GetTypeId()).LookupParameter("h").AsDouble();
                    }
                    else if (rotation % 1.57 == 0 || rotation % 4.71 == 0)
                    {
                        vTopEdges = topEdges.Where(e => (e.AsCurve() as Line).Direction.X == 1 || (e.AsCurve() as Line).Direction.X == -1).ToList();
                        hTopEdges = topEdges.Where(e => (e.AsCurve() as Line).Direction.Y == 1 || (e.AsCurve() as Line).Direction.Y == -1).ToList();
                        xDim = doc.GetElement(recColumnList[i].GetTypeId()).LookupParameter("h").AsDouble();
                        yDim = doc.GetElement(recColumnList[i].GetTypeId()).LookupParameter("b").AsDouble();
                    }
                    else
                    {
                        break;
                    }
                    #endregion

                    #region V Dimension

                    #region get the nearest H Grid and the nearest Column top edge
                    XYZ colPoint = (recColumnList[i].Location as LocationPoint).Point;
                    List<double> distances = new List<double>();
                    for (int j = 0; j < hGridList.Count; j++)
                    {
                        distances.Add(hGridList[j].Curve.Distance(colPoint));
                    }
                    Grid nearestHGrid = hGridList.ElementAt(distances.IndexOf(distances.Min()));

                    distances.Clear();
                    //for (int j = 0; j < hTopEdges.Count; j++)
                    //{
                    //    distances.Add(hTopEdges[j].AsCurve().Distance((nearestHGrid.Curve as Line).Origin));
                    //}
                    //Edge nearestEdge = hTopEdges.ElementAt(distances.IndexOf(distances.Min()));
                    #endregion

                    #region draw the V Dimension

                    ReferenceArray refArray = new ReferenceArray();
                    refArray.Append(hTopEdges[0].Reference);
                    refArray.Append(new Reference(nearestHGrid));
                    XYZ p2 = new XYZ(colPoint.X, colPoint.Y + 0.5, colPoint.Z);
                    Line l = Line.CreateBound(colPoint, p2);
                    Dimension d1 = doc.Create.NewDimension(doc.ActiveView, l, refArray);
                    refArray.Clear();
                    refArray.Append(hTopEdges[1].Reference);
                    refArray.Append(new Reference(nearestHGrid));
                    Dimension d2 = doc.Create.NewDimension(doc.ActiveView, l, refArray);
                    List<Dimension> twoVDim = new List<Dimension>() { d1, d2 };
                    bool d1IsDeleted = true;
                    if (d1.Value > d2.Value)
                    {
                        doc.Delete(d1.Id);
                        twoVDim.RemoveAt(0);
                    }
                    else
                    {
                        doc.Delete(d2.Id);
                        twoVDim.RemoveAt(1);
                        d1IsDeleted = false;
                    }
                    if (d1IsDeleted)
                    {
                        if (d2.Value < .000001)
                        {
                            doc.Delete(d2.Id);
                            twoVDim.RemoveAt(0);
                        }
                    }
                    else
                    {
                        if (d1.Value < .000001)
                        {
                            doc.Delete(d1.Id);
                            twoVDim.RemoveAt(0);
                        }

                    }

                    #endregion

                    #endregion

                    #region H Dimension

                    #region get the nearest H Grid and the nearest Column top edge
                    distances.Clear();
                    for (int j = 0; j < vGridList.Count; j++)
                    {
                        distances.Add(vGridList[j].Curve.Distance(colPoint));
                    }
                    Grid nearestVGrid = vGridList.ElementAt(distances.IndexOf(distances.Min()));
                    #endregion

                    #region draw the H Dimension
                    refArray.Clear();
                    refArray.Append(vTopEdges[0].Reference);
                    refArray.Append(new Reference(nearestVGrid));
                    p2 = new XYZ(colPoint.X + 0.5, colPoint.Y, colPoint.Z);
                    l = Line.CreateBound(colPoint, p2);
                    Dimension d3 = doc.Create.NewDimension(doc.ActiveView, l, refArray);
                    refArray.Clear();
                    refArray.Append(vTopEdges[1].Reference);
                    refArray.Append(new Reference(nearestVGrid));
                    Dimension d4 = doc.Create.NewDimension(doc.ActiveView, l, refArray);
                    List<Dimension> twoHDim = new List<Dimension>() { d3, d4 };
                    bool d3IsDeleted = true;
                    if (d3.Value > d4.Value)
                    {
                        doc.Delete(d3.Id);
                        twoHDim.RemoveAt(0);
                    }
                    else
                    {
                        doc.Delete(d4.Id);
                        twoHDim.RemoveAt(1);
                        d3IsDeleted = false;
                    }
                    if (d3IsDeleted)
                    {
                        if (d4.Value < .000001)
                        {
                            doc.Delete(d4.Id);
                            twoHDim.RemoveAt(1);
                        }
                    }
                    else
                    {
                        if (d3.Value < .000001)
                        {
                            doc.Delete(d3.Id);
                            twoHDim.RemoveAt(0);
                        }

                    }

                    #endregion

                    #endregion

                    #region translation of Dim
                    double offset = 1.5;
                    #region V Dimension Translation
                    if (Math.Abs((nearestHGrid.Curve as Line).Origin.Y - colPoint.Y) < yDim / 2)
                    {

                        if ((nearestVGrid.Curve as Line).Origin.X < colPoint.X && twoVDim.Count == 1)
                        {
                            ElementTransformUtils.MoveElement(doc, twoVDim[0].Id, new XYZ(-xDim / 2 - offset, 0, 0));

                        }
                        else if (twoVDim.Count == 1)
                        {
                            ElementTransformUtils.MoveElement(doc, twoVDim[0].Id, new XYZ(xDim / 2 + offset * 2, 0, 0));
                        }
                    }

                    #endregion

                    #region H Dimension translation
                    if ((Math.Abs((nearestVGrid.Curve as Line).Origin.X - colPoint.X) < xDim / 2))
                    {

                        if ((nearestHGrid.Curve as Line).Origin.Y < colPoint.Y && twoHDim.Count == 1)
                        {
                            ElementTransformUtils.MoveElement(doc, twoHDim[0].Id, new XYZ(0, -yDim / 2 - 2 * offset, 0));

                        }
                        else if (twoHDim.Count == 1)
                        {
                            ElementTransformUtils.MoveElement(doc, twoHDim[0].Id, new XYZ(0, yDim / 2 + offset, 0));
                        }
                    }

                    #endregion

                    #endregion
                }
                #endregion

                #region circular columns
                for (int i = 0; i < circularColumnList.Count; i++)
                {
                    Options opt = new Options();
                    opt.ComputeReferences = true;
                    opt.IncludeNonVisibleObjects = true;
                    #region V Dimension

                    #region get the nearest H Grid 
                    XYZ colPoint = (circularColumnList[i].Location as LocationPoint).Point;
                    List<double> distances = new List<double>();
                    for (int j = 0; j < hGridList.Count; j++)
                    {
                        distances.Add(hGridList[j].Curve.Distance(colPoint));
                    }
                    Grid nearestHGrid = hGridList.ElementAt(distances.IndexOf(distances.Min()));
                    distances.Clear();
                    #endregion

                    #region draw the V Dimension
                    ReferenceArray refArray = new ReferenceArray();
                    refArray.Append(circularColumnList[i].get_Geometry(opt).OfType<Point>().FirstOrDefault().Reference);
                    refArray.Append(new Reference(nearestHGrid));
                    XYZ p2 = new XYZ(colPoint.X, colPoint.Y + 0.5, colPoint.Z);
                    Line l = Line.CreateBound(colPoint, p2);
                    Dimension d1 = doc.Create.NewDimension(doc.ActiveView, l, refArray);
                    if (d1.Value < .000001)
                    {
                        doc.Delete(d1.Id);
                    }
                    #endregion

                    #endregion

                    #region H Dimension

                    #region get the nearest H Grid and the nearest Column top edge
                    distances.Clear();
                    for (int j = 0; j < vGridList.Count; j++)
                    {
                        distances.Add(vGridList[j].Curve.Distance(colPoint));
                    }
                    Grid nearestVGrid = vGridList.ElementAt(distances.IndexOf(distances.Min()));
                    #endregion

                    #region draw the H Dimension
                    refArray.Clear();
                    refArray.Append(circularColumnList[i].get_Geometry(opt).OfType<Point>().FirstOrDefault().Reference);
                    refArray.Append(new Reference(nearestVGrid));
                    p2 = new XYZ(colPoint.X + 0.5, colPoint.Y, colPoint.Z);
                    l = Line.CreateBound(colPoint, p2);
                    Dimension d3 = doc.Create.NewDimension(doc.ActiveView, l, refArray);

                    if (d3.Value < .000001)
                    {
                        doc.Delete(d3.Id);
                    }
                    #endregion

                    #endregion

                    #region translation of Dim
                    double offset = 1.5;
                    #region V Dimension Translation
                    //if (Math.Abs((nearestHGrid.Curve as Line).Origin.Y - colPoint.Y) < yDim / 2)
                    //{

                    //    if ((nearestVGrid.Curve as Line).Origin.X < colPoint.X && twoVDim.Count == 1)
                    //    {
                    //        ElementTransformUtils.MoveElement(doc, twoVDim[0].Id, new XYZ(-xDim / 2 - offset, 0, 0));

                    //    }
                    //    else if (twoVDim.Count == 1)
                    //    {
                    //        ElementTransformUtils.MoveElement(doc, twoVDim[0].Id, new XYZ(xDim / 2 + offset * 2, 0, 0));
                    //    }
                    //}

                    #endregion

                    #region H Dimension translation
                    //if ((Math.Abs((nearestVGrid.Curve as Line).Origin.X - colPoint.X) < xDim / 2))
                    //{

                    //    if ((nearestHGrid.Curve as Line).Origin.Y < colPoint.Y && twoHDim.Count == 1)
                    //    {
                    //        ElementTransformUtils.MoveElement(doc, twoHDim[0].Id, new XYZ(0, -yDim / 2 - 2 * offset, 0));

                    //    }
                    //    else if (twoHDim.Count == 1)
                    //    {
                    //        ElementTransformUtils.MoveElement(doc, twoHDim[0].Id, new XYZ(0, yDim / 2 + offset, 0));
                    //    }
                    //}

                    #endregion

                    #endregion
                }
                #endregion


                trans.Commit();
            }

        }
        #endregion

        #region Slab Creation From CAD
        public static void CreateFloorFromCad(UIDocument uiDoc, string typeName, string levelName, double offset)
        {
            Document doc = uiDoc.Document;
            //select cad
            Reference cadReference = uiDoc.Selection.PickObject(ObjectType.Element);
            Element cadElement = doc.GetElement(cadReference);
            //get the level
            Level level = new FilteredElementCollector(doc).OfClass(typeof(Level)).FirstOrDefault(l => l.Name == levelName) as Level;
            //get floor type
            FloorType floorType = new FilteredElementCollector(doc).OfClass(typeof(FloorType)).FirstOrDefault(e => e.Name == typeName) as FloorType;
            //get floor profile
            List<CurveLoop> profile = new List<CurveLoop>();
            Options op = new Options();
            GeometryElement polylinesAndArcs = (cadElement.get_Geometry(op).FirstOrDefault() as GeometryInstance).GetSymbolGeometry();
            List<Arc> arcList = polylinesAndArcs.OfType<Arc>().Where(arc => (doc.GetElement(arc.GraphicsStyleId) as GraphicsStyle).GraphicsStyleCategory.Name == "Slabs").ToList();
            List<PolyLine> polylineList = polylinesAndArcs.OfType<PolyLine>().Where(arc => (doc.GetElement(arc.GraphicsStyleId) as GraphicsStyle).GraphicsStyleCategory.Name == "Slabs").ToList();
            List<Line> lineList = polylinesAndArcs.OfType<Line>().Where(arc => (doc.GetElement(arc.GraphicsStyleId) as GraphicsStyle).GraphicsStyleCategory.Name == "Slabs").ToList();
            foreach (Arc arc in arcList)
            {
                CurveLoop curveLoop = new CurveLoop();
                #region 1
                //int m = item.Tessellate().Count / 2;
                //Arc arc = Arc.Create(item.GetEndPoint(0), item.GetEndPoint(1), item.Tessellate()[m]);
                //curveLoop.Append(arc);
                #endregion

                #region 2
                //List<XYZ> pointsList = arc.Tessellate().ToList();
                //for (int i = 0; i < pointsList.Count-1; i++)
                //{
                //    curveLoop.Append(Line.CreateBound(pointsList[i], pointsList[i+1]));
                //}
                #endregion

                #region 3
                curveLoop.Append(arc);
                #endregion

                profile.Add(curveLoop);
            }
            foreach (PolyLine polyLine in polylineList)
            {
                List<XYZ> pointsList = polyLine.GetCoordinates().ToList();
                CurveLoop curveLoop = new CurveLoop();
                for (int i = 0; i < pointsList.Count - 1; i++)
                {
                    Line line = Line.CreateBound(pointsList[i], pointsList[i + 1]);
                    curveLoop.Append(line);
                }
                profile.Add(curveLoop);
            }
            foreach (Line line in lineList)
            {
                CurveLoop curveLoop = new CurveLoop();
                curveLoop.Append(line);
                profile.Add(curveLoop);
            }

            using (Transaction trans = new Transaction(doc, "Floor creation"))
            {
                trans.Start();
                XYZ delete = XYZ.BasisX;
                Line line = Line.CreateBound(new XYZ(0, 0, 0), new XYZ(1, 0, 0));
                Floor floor = Floor.Create(doc, profile, floorType.Id, level.Id, true, line, 0);
                floor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).Set(offset / 304.8);
                trans.Commit();
            }
        }
        #endregion
    }
}
