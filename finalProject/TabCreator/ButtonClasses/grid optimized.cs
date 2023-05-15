using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace finalProject.TabCreator.ButtonClasses
{
    [Transaction(TransactionMode.Manual)]
    public class grid_optimized : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                //get ui document
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                //get the document
                Document doc = uidoc.Document;
                //opening transaction
                using (Transaction transaction = new Transaction(doc, "Create Grids Dimensions"))
                {
                    transaction.Start();
                    //filter by category
                    ElementCategoryFilter filter = new ElementCategoryFilter(BuiltInCategory.OST_Grids);
                    //filter in this document
                    FilteredElementCollector collector = new FilteredElementCollector(doc);
                    //get allt he grids and put them in list 
                    IEnumerable<Grid> grids = collector.WherePasses(filter).WhereElementIsNotElementType().ToElements().Cast<Grid>();

                    //get the horizontal grids , vertical and diagonal grids and put them in the ienumerable 
                    List<Grid> horizontalgrids = new List<Grid>();
                    List<Grid> verticalgrids = new List<Grid>();
                    List<Grid> diagonalgrids = new List<Grid>();


                    foreach (var Grid in grids)
                    {
                        Line line = (Grid.Curve) as Line;
                        if ((line.Direction.X == 1) || (line.Direction.X == -1))//horizontal grids
                        {
                            horizontalgrids.Add(Grid);

                        }
                        else if ((line.Direction.Y == 1) || (line.Direction.Y == -1))//vertical grids
                        {
                            verticalgrids.Add(Grid);
                        }
                        else // diagonal grids
                        {
                            diagonalgrids.Add(Grid);
                        }
                    }
                    //sort grids 
                    IEnumerable<Grid> sortedverticalgrids = from grid in verticalgrids
                                                            orderby grid.Curve.Evaluate(0.5, true).X
                                                            select grid;
                    IEnumerable<Grid> sortedhorizontalgrids = from grid in horizontalgrids
                                                              orderby grid.Curve.Evaluate(0.5, true).Y
                                                              select grid;
                    IEnumerable<Grid> sorteddiagonalsgrids = from grid in diagonalgrids
                                                             orderby grid.Curve.Evaluate(0.5, true).Y
                                                             select grid;
                    //**************************************************************************
                    //getting the line for vertical grids
                    var lastverticalcurve = sortedverticalgrids.Last().Curve;
                    XYZ point = null;
                    //check if it is top or bot 
                    if (lastverticalcurve.GetEndPoint(0).Y < lastverticalcurve.GetEndPoint(1).Y)
                    {
                        point = lastverticalcurve.GetEndPoint(1);
                    }
                    else
                    {
                        point = lastverticalcurve.GetEndPoint(0);
                    }


                    XYZ newpoint = new XYZ(point.X, point.Y, point.Z);
                    //create the horizontal line for vertical 
                    Line horizontallineforverticalgrids = Line.CreateBound(newpoint, new XYZ(newpoint.X - 10, newpoint.Y, newpoint.Z));
                    //**************************************************************************
                    //getting the line for horizontal grids
                    var lasyhorizontalcurve = sortedhorizontalgrids.Last().Curve;
                    XYZ point2 = null;
                    //check if it is top or bot 
                    if (lasyhorizontalcurve.GetEndPoint(0).X < lasyhorizontalcurve.GetEndPoint(1).X)
                    {
                        point2 = lasyhorizontalcurve.GetEndPoint(1);
                    }
                    else
                    {
                        point2 = lasyhorizontalcurve.GetEndPoint(0);
                    }


                    XYZ newpoint2 = new XYZ(point2.X, point2.Y, point2.Z);
                    //create the horizontal line for vertical 
                    Line verticallineforhorizontalgrids = Line.CreateBound(newpoint2, new XYZ(newpoint2.X, newpoint2.Y - 10, newpoint2.Z));

                    ReferenceArray horizontalgridsref = new ReferenceArray();
                    ReferenceArray verticalgridsref = new ReferenceArray();

                    foreach (var grid in horizontalgrids)
                    {

                        //get curves from grids
                        Curve curve = grid.Curve;
                        //get the reference
                        Reference reference = new Reference(grid);
                        // add it to the array
                        horizontalgridsref.Append(reference);
                    }
                    foreach (var grid in verticalgrids)
                    {

                        //get curves from grids
                        Curve curve = grid.Curve;
                        //get the reference
                        Reference reference = new Reference(grid);
                        // add it to the array
                        verticalgridsref.Append(reference);
                    }
                    //the long dimension for horizontal grids

                    List<Grid> firstandlasthorizontalgrids = new List<Grid>();
                    List<Grid> firstandlastverticalgrids = new List<Grid>();
                    ReferenceArray referenceArray1 = new ReferenceArray();
                    ReferenceArray referenceArray2 = new ReferenceArray();
                    var fhzgrid = sortedhorizontalgrids.First();
                    var lhzgrid = sortedhorizontalgrids.Last();
                    var fvtgrid = sortedverticalgrids.First();
                    var lvtgrid = sortedverticalgrids.Last();
                    //for horizontal grids
                    firstandlasthorizontalgrids.Add(fhzgrid);
                    firstandlasthorizontalgrids.Add(lhzgrid);
                    firstandlastverticalgrids.Add(fvtgrid);
                    firstandlastverticalgrids.Add(lvtgrid);


                    foreach (var item in firstandlasthorizontalgrids)
                    {
                        Reference reference = new Reference(item);
                        referenceArray1.Append(reference);
                    }
                    foreach (var item in firstandlastverticalgrids)
                    {
                        Reference reference = new Reference(item);
                        referenceArray2.Append(reference);
                    }

                    //creating horziontal dimension

                    Dimension hordim = doc.Create.NewDimension(uidoc.ActiveView, horizontallineforverticalgrids, verticalgridsref);
                    Dimension hordimlong = doc.Create.NewDimension(uidoc.ActiveView, horizontallineforverticalgrids, referenceArray2);

                    //creating vertical dimension
                    Dimension vrdim = doc.Create.NewDimension(uidoc.ActiveView, verticallineforhorizontalgrids, horizontalgridsref);
                    Dimension vrdimlong = doc.Create.NewDimension(uidoc.ActiveView, verticallineforhorizontalgrids, referenceArray1);
                    ElementTransformUtils.MoveElement(doc, vrdim.Id, new XYZ(-7, 0, 0));
                    ElementTransformUtils.MoveElement(doc, hordim.Id, new XYZ(0, -7, 0));
                    ElementTransformUtils.MoveElement(doc, hordimlong.Id, new XYZ(0, -2, 0));
                    ElementTransformUtils.MoveElement(doc, vrdimlong.Id, new XYZ(-2, 0, 0));


                    //diagonal dimensions
                    //getting the direvtion of all curves
                    List<XYZ> points = new List<XYZ>();
                    foreach (var item in sorteddiagonalsgrids)
                    {
                        ;
                        XYZ trying = (item.Curve as Line).Direction;
                        double xtrying = Math.Abs(trying.X);
                        double ytrying = Math.Abs(trying.Y);
                        double Ztrying = Math.Abs(trying.Y);
                        XYZ newdirection = new XYZ(xtrying, ytrying, Ztrying);
                        points.Add(newdirection);
                      


                    }
                    var distinctdirections = new List<XYZ>();
                    foreach (var item in points)
                    {
                        bool isDistinct = true;
                        foreach (XYZ distinctDirection in distinctdirections)
                        {
                            // Compare the direction vectors to see if they are parallel
                            if (item.IsAlmostEqualTo(distinctDirection))
                            {
                                isDistinct = false;
                                break;
                            }
                        }
                        // Add the direction vector to the list if it is distinct
                        if (isDistinct)
                        {
                            distinctdirections.Add(item);
                        }
                    }
                    int counter = 0;

                    try
                    {
                        //get the distict directions
                        foreach (var distinctlist in distinctdirections)
                        {
                            ReferenceArray referenceArray = new ReferenceArray();
                            List<Grid> list = new List<Grid>();

                            foreach (var biglist in sorteddiagonalsgrids)
                            {
                                double X = Math.Abs(Math.Round((biglist.Curve as Line).Direction.X, 8));
                                double Y = Math.Abs(Math.Round((biglist.Curve as Line).Direction.Y, 8));
                                double Z = Math.Abs(Math.Round((biglist.Curve as Line).Direction.Z, 8));
                                XYZ griddirection = new XYZ(X, Y, Z);
                                Reference reference = new Reference(biglist);
                                //if the grid direction equals one of the direction   add its reference
                                if ((Math.Abs(Math.Round(distinctlist.X,8)) == griddirection.X) && (Math.Abs(Math.Round(distinctlist.Y,8)) == griddirection.Y))
                                {

                                    counter++;
                                    referenceArray.Append(reference);
                                    list.Add(biglist);
                                }

                            }
                            if (counter < 2)
                            {
                                referenceArray.Clear();
                            }
                            else if (counter == 2)
                            {
                                Grid firstgrid = list.FirstOrDefault();
                                Grid lastgrid = list.LastOrDefault();
                                //get the highest point 
                                XYZ highestpoint = null;
                                XYZ highestpoint2 = null;
                               


                                var po = list.Select(m => m.Curve.Evaluate(0.5, true).Y).ToList();
                                var max = po.Max();
                                var gr = list[po.IndexOf(max)];
                                if (gr.Curve.GetEndPoint(0).Y > gr.Curve.GetEndPoint(1).Y)
                                {
                                    highestpoint = gr.Curve.Evaluate(0.05, true);



                                }
                                else
                                {
                                    highestpoint = gr.Curve.Evaluate(0.95, true);

                                }
                                var gtb = list[po.IndexOf(po.Min())];
                                if (gtb.Curve.GetEndPoint(0).Y > gtb.Curve.GetEndPoint(1).Y)
                                {
                                    highestpoint2 = gtb.Curve.Evaluate(0.05, true);

                                }
                                else
                                {
                                    highestpoint2 = gtb.Curve.Evaluate(0.95, true);

                                }



                                Line l = Line.CreateBound(highestpoint, highestpoint2);
                                Dimension diadim = doc.Create.NewDimension(uidoc.ActiveView, l, referenceArray);
                                ElementTransformUtils.MoveElement(doc, diadim.Id, new XYZ(0, -1, 0));
                                highestpoint = null;
                                highestpoint2 = null;
                            }
                            else
                            {
                                Grid firstgrid = list.FirstOrDefault();
                                Grid lastgrid = list.LastOrDefault();
                                //get the highest point 
                                XYZ highestpoint8 = null;
                                XYZ highestpoint = null;
                                XYZ highlastgrid = null;
                                XYZ highlastgridlong = null;

                                var po = list.Select(m => m.Curve.Evaluate(0.5, true).Y).ToList();
                                var max = po.Max();
                                var grt = list[po.IndexOf(max)];
                                if (grt.Curve.GetEndPoint(0).Y > grt.Curve.GetEndPoint(1).Y)
                                {
                                    highestpoint8 = grt.Curve.Evaluate(0.05, true);
                                    highestpoint = grt.Curve.Evaluate(0.1, true);

                                }
                                else
                                {
                                    highestpoint8 = grt.Curve.Evaluate(0.95, true);
                                    highestpoint = grt.Curve.Evaluate(0.9, true);


                                }
                                var grb = list[po.IndexOf(po.Min())];
                                if (grb.Curve.GetEndPoint(0).Y > grb.Curve.GetEndPoint(1).Y)
                                {
                                    highlastgridlong = grb.Curve.Evaluate(0.05, true);
                                    highlastgrid = grb.Curve.Evaluate(0.1, true);

                                }
                                else
                                {
                                    highlastgridlong = grb.Curve.Evaluate(0.95, true);
                                    highlastgrid = grb.Curve.Evaluate(0.9, true);


                                }
                                //XYZ firstgridpoint = firstgrid.Curve.Evaluate(0.5, true);
                                //XYZ lastgridpoint = lastgrid.Curve.Evaluate(0.5, true);
                                Line l = Line.CreateBound(highestpoint8, highlastgridlong);
                                Line l2 = Line.CreateBound(highestpoint, highlastgrid);
                                ReferenceArray onedim = new ReferenceArray();
                                Reference ref1 = new Reference(grt);
                                Reference ref2 = new Reference(grb);
                                onedim.Append(ref1);
                                onedim.Append(ref2);

                                Dimension diadim = doc.Create.NewDimension(uidoc.ActiveView, l2, referenceArray);
                               
                                Dimension diadimlong = doc.Create.NewDimension(uidoc.ActiveView, l, onedim);
                                
                                onedim.Clear();
                                highestpoint = null;
                                highestpoint8 = null;


                            }
                            //create dimension by create line

                            referenceArray.Clear();
                            list.Clear();
                            counter = 0;


                        }

                    }
                    catch (Exception)
                    {

                        TaskDialog.Show("Error in  diagonal grids", "there is an error in diagonal curves");
                    }


                    transaction.Commit();


                }


                return Result.Succeeded;
            }
            catch (Exception)
            {

                return Result.Failed;
            }
        }



    }



}



