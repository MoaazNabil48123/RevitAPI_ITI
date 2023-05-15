using Autodesk.Revit.UI;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;

namespace finalProject.TabCreator
{
    internal class TabCreator : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            application.CreateRibbonTab("Geeks");
            string path = Assembly.GetExecutingAssembly().Location;

            #region CAD to Revit panel
            PushButtonData columnCreationButton = new PushButtonData("Column Creation", "Column Creation", path, "finalProject.TabCreator.ButtonClasses.ColumnCreationButton");
            PushButtonData slabCreationButton = new PushButtonData("slab Creation", "slab Creation", path, "finalProject.TabCreator.ButtonClasses.SlabCreationButton");
            RibbonPanel panel1 = application.CreateRibbonPanel("Geeks", "CAD to Revit");

            PushButton push = panel1.AddItem(columnCreationButton) as PushButton;
        Uri Imagepath = new Uri(@"E:\ITI\08- API\02- Revit\Lab\final Project\finalProject\finalProject\Images\columns.png");
            BitmapImage image = new BitmapImage(Imagepath);
            push.LargeImage = image;

            push = panel1.AddItem(slabCreationButton) as PushButton;
            Imagepath = new Uri(@"E:\ITI\08- API\02- Revit\Lab\final Project\finalProject\finalProject\Images\slab.png");
            image = new BitmapImage(Imagepath);
            push.LargeImage = image;

            #endregion

            #region Auto Dimension panel
            PushButtonData columnDimensionButton = new PushButtonData("button", "Column Dimension", path, "finalProject.TabCreator.ButtonClasses.ColumnDimensionButton");
            PushButtonData gridDimensionButton = new PushButtonData("Grids Dimesnion", "Grids Dimesnion", path, "finalProject.TabCreator.ButtonClasses.grid_optimized");
            RibbonPanel panel2 = application.CreateRibbonPanel("Geeks", "Auto Dimension");


            push = panel2.AddItem(columnDimensionButton) as PushButton;
            Imagepath = new Uri(@"E:\ITI\08- API\02- Revit\Lab\final Project\finalProject\finalProject\Images\size.png");
            image = new BitmapImage(Imagepath);
            push.LargeImage = image;

            //push = panel2.AddItem(gridDimensionButton) as PushButton;
            Imagepath = new Uri(@"E:\ITI\08- API\02- Revit\Lab\final Project\finalProject\finalProject\Images\grid.png");
            image = new BitmapImage(Imagepath);
            push.LargeImage = image;
            #endregion

            #region footing
            //PushButtonData foundatioButton = new PushButtonData("Adjust footing", "Adjust footing", path, "finalProject.TabCreator.ButtonClasses.Adjust_foundation_optimized");
            //RibbonPanel panel3 = application.CreateRibbonPanel("Geeks", "footing");
            //PushButton push2 = panel3.AddItem(foundatioButton) as PushButton;

            //Uri Imagepath2 = new Uri(@"E:\ITI\08- API\02- Revit\Lab\final Project\finalProject\finalProject\Images\foundation24.png");
            //BitmapImage image2 = new BitmapImage(Imagepath2);
            //push2.LargeImage = image2;
            #endregion

            return Result.Succeeded;
        }
    }

}
