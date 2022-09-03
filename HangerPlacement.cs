using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Mechanical;
using System.IO;
using System.Reflection;

namespace DesignManagerAddins
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class HangerPlacement : IExternalCommand
    {
        //variables for the ducts data:
        int numberOfDucts = 0;
        int numberOfCurves = 0;
        List<Curve> curves = new List<Curve>();
        int numberOfHangers = 0; // The max value created from deviding the length of the duct, with the allowed minimum length between the hangers. 
        int numberOfSplines = 0;// the number of splines between the  hangers.
        double splineLength = 0; // the length of single spline
        double x_coor = 0;
        double y_coor = 0;
        double z_coor = 0;
        List<XYZ> hangerCoordinates = new List<XYZ>();
        int totCounter = 0;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {


            //Get UIDocument
            UIDocument uidoc = commandData.Application.ActiveUIDocument;

            //Get Document
            Document doc = uidoc.Document;
            // get user input for desired level
            //string inputLevelName = Microsoft.VisualBasic.Interaction.InputBox("Please insert the EXACT name of the desired level", "Insert Level", "");

            Form1 fm = new Form1(commandData, doc);
            fm.ShowDialog();


            if (Form1.stopScript == true)
            {
                return Result.Succeeded;
            }
            string inputLevelName = Form1.chosenLevel.ToString();
            double constantD = Form1.constantDistance;// the distance to devide the duct length in



            //Get Family Symbol
            FilteredElementCollector familyCollector = new FilteredElementCollector(doc);
            IList<Element> symbols = familyCollector.OfClass(typeof(FamilySymbol)).WhereElementIsElementType().ToElements();

            FamilySymbol hangerSymbol = null;
            foreach (Element ele in symbols)
            {
                if (ele.Name == "אביזר תמיכת צנרת פלח ומוטות הברגה")
                {
                    hangerSymbol = ele as FamilySymbol;
                    break;
                }
            }

            //Get Level(in order to create hanger element)
            Level level = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Levels)
                .WhereElementIsNotElementType()
                .Cast<Level>()
                .First(x => x.Name == inputLevelName);// use lambda expression to go over all of the level names and stop with the first name we want
            viewRange(doc, level);
            ////delete here---------------------
            //Form1.stopScript = true;

            if (Form1.stopScript == true)
            {
                return Result.Succeeded;
            }
            ////delete here---------------------

            Level levelAbove = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Levels)
                .WhereElementIsNotElementType()
                .Cast<Level>()
                .First(x => x.Elevation > level.Elevation);

            Categories categories = doc.Settings.Categories;// define a variable to store all the categorys.
            Category category = categories.get_Item("Floors"); // get the floors category
            ElementId categoryId = category.Id;// get an id element for the flooe category

            IEnumerable<Element> collector = new FilteredElementCollector(doc)//filter the floor category to get a floor list
                                .OfCategoryId(categoryId)
                                .WhereElementIsNotElementType()
                                .ToElements();

            Floor floor = null;
            foreach (Floor ele in collector)
            {
                if (ele.LevelId == level.Id)
                {
                    floor = ele as Floor;



                }

            }

            //Get collection of all the ducts in the project
            FilteredElementCollector ductCollection = new FilteredElementCollector(doc, doc.ActiveView.Id)
                .OfClass(typeof(Duct));

            //Add shared parameter for the duct coordinant:
            CreateHangerSharedParameter(doc, commandData);


            try
            {

                foreach (Duct duct in ductCollection)
                {
                    ++numberOfDucts;

                    ConnectorManager cm = duct.ConnectorManager;

                    foreach (Connector c in cm.Connectors)
                    {
                        //Change the witdh

                        if (c.Shape != ConnectorProfileType.Oval && c.Shape != ConnectorProfileType.Rectangular)
                        {
                            goto NextlDuct;
                        }

                    }
                    LocationCurve locationCurve = duct.Location as LocationCurve;// locationCurve == provides location functionallity for curve based elements

                    if (locationCurve != null)
                    {
                        double fitToUnits;
                        Form2 form2 = new Form2();
                        if (Form2.units != 0)
                        {
                            fitToUnits = Form2.units;

                        }
                        else
                        {
                            fitToUnits = 30.48; // defoult is cm
                        }
                        numberOfCurves++;
                        Curve curve = locationCurve.Curve;// ".Curve"==> property provides the abillity to get and set a curve of cuve based element
                        curves.Add(Line.CreateBound(curve.GetEndPoint(0), curve.GetEndPoint(1)));
                        double ductArea = duct.Width * duct.Height * 0.09290304;// area in square meters
                        //double ductLength = curves[numberOfCurves - 1].Length * 30.48;// length in cm
                        double ductLength = curves[numberOfCurves - 1].Length * fitToUnits;// length in cm

                        if (ductArea < 0.35)// area comparison square meters
                        {

                            if (ductLength > constantD)// length comperison in cm [defoult is 240
                            {
                                // fill locations list, and place family
                                fillLocationList(curve, ductLength, level, doc, hangerSymbol, floor, duct.Width, duct.Height, levelAbove, duct);

                            }
                        }
                    }
                NextlDuct:;
                }


                return Result.Succeeded;
            }
            catch (Exception e)
            {

                message = e.Message;
                return Result.Failed;
            }

        }






        // a method to fill in the location hanger list
        public void fillLocationList(Curve curve, double ductLength, Level level, Document doc, FamilySymbol hangerSymbol, Floor floor, double ductWidth, double ductHeight, Level levelAbove, Duct duct)
        {
            numberOfHangers = (int)Math.Floor(ductLength / Form1.constantDistance); // defoult distance is 240
            numberOfSplines = numberOfHangers + 1;
            splineLength = ductLength / numberOfSplines;

            for (int i = 1; i < numberOfSplines; i++)
            {

                if (i == 1)
                {
                    hangerCoordinates.Add(curve.GetEndPoint(0));
                    commitFamilyPlacement(doc, curve.GetEndPoint(0), hangerSymbol, floor, curve, ductWidth, ductHeight, level, levelAbove, duct);
                    hangerCoordinates.Add(curve.GetEndPoint(1));
                    commitFamilyPlacement(doc, curve.GetEndPoint(1), hangerSymbol, floor, curve, ductWidth, ductHeight, level, levelAbove, duct);


                }
                totCounter++;
                x_coor = curve.GetEndPoint(0).X + (curve.GetEndPoint(1).X - curve.GetEndPoint(0).X) * (splineLength * i / ductLength);//x=x0+(x1-x0)*alpha
                y_coor = curve.GetEndPoint(0).Y + (curve.GetEndPoint(1).Y - curve.GetEndPoint(0).Y) * (splineLength * i / ductLength);//y=y0+(y1+y0)*alpha
                z_coor = curve.GetEndPoint(0).Z + (curve.GetEndPoint(1).Z - curve.GetEndPoint(0).Z) * (splineLength * i / ductLength);//z=z0+(z1+y0)*alpha

                XYZ newDot = new XYZ(x_coor, y_coor, z_coor);

                if (!hangerCoordinates.Contains(newDot))
                {
                    hangerCoordinates.Add(newDot);
                    commitFamilyPlacement(doc, newDot, hangerSymbol, floor, curve, ductWidth, ductHeight, level, levelAbove, duct);

                }

            }
        }


        public void commitFamilyPlacement(Document doc, XYZ hanger_xyz, FamilySymbol hangerSymbol, Floor floor, Curve curve, double ductWidth, double ductHeight, Level level, Level levelAbove, Duct duct)
        {


            using (Transaction trans = new Transaction(doc, "Place Family"))
            {
                trans.Start();

                List<FamilyInstance> familyInstances = new List<FamilyInstance>();
                Line line1 = Line.CreateBound(hanger_xyz, new XYZ(hanger_xyz.X, hanger_xyz.Y, hanger_xyz.Z + 10));//line normal to the plane
                XYZ pntCenter = line1.Evaluate(0.5, true);
                XYZ normal = line1.Direction.Normalize();// make it ones and zeros
                XYZ ductDirection = (curve as Line).Direction;
                XYZ ductVector = new XYZ(ductDirection.X, ductDirection.Y, 0).Normalize();//direction vector for duct
                XYZ cross = ductVector.CrossProduct(normal);// perpendidular to the duct, in the plane x,y



                if (!hangerSymbol.IsActive)
                {
                    hangerSymbol.Activate();
                }
                Options options = new Options();// This class determines the output of the Element.Geometry property.
                options.ComputeReferences = true; // .ComputerReference is a property that Determines whether or not references to geometric objects are computed --> if true, refrences will be computed
                Face face = null; //GeometricElement contains a lot of geometric objects that build the element itself. hance it is iterable! 
                int index = 0;
                UV uv = new UV(hanger_xyz.X, hanger_xyz.Y);
                if (Form1.isLinked)//model is linked
                {

                    GeometryElement geoElement = duct.get_Geometry(options); // GeometriyElement --> This class contains geometric primitives that are generated from the parametric description of the element
                    face = LinkedModelCase(geoElement, index, face, uv);

                }
                else
                {
                    GeometryElement geoElement = floor.get_Geometry(options); // GeometriyElement --> This class contains geometric primitives that are generated from the parametric description of the element
                    face = nonLinkedModelCase(geoElement, index, face, uv);

                }


                try
                {
                    if (Math.Abs(hanger_xyz.Z - levelAbove.Elevation) < levelAbove.Elevation - level.Elevation && !Form1.isLinked)
                    {

                        

                        FamilyInstance familyInstance = doc.Create.NewFamilyInstance(face, hanger_xyz, cross, hangerSymbol);// create the hanger 
                        familyInstances.Add(familyInstance);// add it to hanger list
                        familyInstance.LookupParameter("אורך פלך תמיכה").Set(ductWidth + 0.3);// set the length of the hanger to be as  long as duckt +10 cn
                        familyInstance.LookupParameter("Hanger Coordinant").Set(hanger_xyz.ToString());//set the shared parameter for the location with the XYZ coordinant
                        if (!Form1.isLinked)
                        {
                            double floorThickness = floor.LookupParameter("Thickness").AsDouble();// take the thickness parameter of floor
                            familyInstance.LookupParameter("גובה_מתלה").Set(levelAbove.Elevation - floorThickness - hanger_xyz.Z + ductHeight);// set hanher height to be 
                                                                                                                                               // as far from the level above 
                        }

                    }else if(Math.Abs(hanger_xyz.Z - levelAbove.Elevation) < levelAbove.Elevation - level.Elevation && Form1.isLinked)
                    {
                        //Get Family Symbol
                        FilteredElementCollector familyCollector = new FilteredElementCollector(doc);
                        IList<Element> symbols = familyCollector.OfClass(typeof(FamilySymbol)).WhereElementIsElementType().ToElements();

                        foreach (Element ele in symbols)
                        {
                            if (ele.Name == "מתלה תעלות")
                            {
                                hangerSymbol = ele as FamilySymbol;
                                break;
                            }
                        }
                         
                        FamilyInstance familyInstance = doc.Create.NewFamilyInstance(face, hanger_xyz, cross, hangerSymbol);// create the hanger 
                        familyInstances.Add(familyInstance);// add it to hanger list
                        //---------04.06.22
                       
                        familyInstance.LookupParameter("אורך פלך תמיכה").Set(ductWidth + 0.3 );// set the length of the hanger to be as  long as duckt +10 cn

                        //---------04.06.22


                        //familyInstance.LookupParameter("אורך פלך תמיכה").Set(cabletrayWidth + 0.3);// set the length of the hanger to be as  long as duckt +10 cn
                        familyInstance.LookupParameter("Hanger Coordinant").Set(hanger_xyz.ToString());//set the shared parameter for the location with the XYZ coordinant
                        if (true)// was : "!Form1.isLinked" . FOR NOW!! we put true until form would be made for this feature
                        {
                            //double floorThickness = floor.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM).AsDouble();
                            //double cableTrayElevation = curve.Distance(new XYZ(hanger_xyz.X, hanger_xyz.Y, 0)); // distance between the (X,Y) projection on the lowest plane--> Z coordinant                                                                                                            // as far from the level above 
                            double angleOfTray = curve.GetEndPoint(1).Z - curve.GetEndPoint(0).Z;
                            if (Math.Abs(curve.GetEndPoint(1).Z - curve.GetEndPoint(0).Z) > 0.01 )
                            {
                                familyInstance.LookupParameter("גובה_מתלה").Set(0.16);// set hanher height to be 
                                familyInstance.LookupParameter("אורך מוט").Set(0.01);// set hanher height to be 

                            }
                            else
                            {
                                familyInstance.LookupParameter("גובה_מתלה").Set(0.16);// set hanher height to be 
                                familyInstance.LookupParameter("אורך מוט").Set(levelAbove.Elevation - hanger_xyz.Z + 0.2);// set hanher height to be 

                            }
                            //familyInstance.LookupParameter("גובה_מתלה").Set(levelAbove.Elevation - floorThickness - hanger_xyz.Z + cabletrayHeight);// set hanher height to be 


                        }



                    }

                }
                catch (Exception)
                {
                    TaskDialog.Show(" Error ocured", " one of the follwing is not as it should be: \n " +
                        "1. the hanger family: 'אביזר תמיכת צנרת פלח ומוטות הברגה' is not loaded to the project \n" +
                        "2. the floor family does not have the parameter 'Thickness' in it. \n" +
                        "3. you are trying to use the addin on the top floor ( is there another floor above?) \n" +
                        "4. no ducts has been added");


                }



                trans.Commit();
                //checkIfParallel(familyInstances, curve, doc);


            }


        }

        public Face LinkedModelCase(GeometryElement geoElement, int index, Face face, UV uv)
        {

            foreach (GeometryObject geomObj in geoElement)
            {

                Solid geomSolid = geomObj as Solid;
                if (geomSolid != null)
                {
                    foreach (Face geomFace in geomSolid.Faces)
                    {


                        if (index == 2)
                        {
                            if (geomFace.ComputeNormal(uv).Z != -1)
                            {

                            }
                            face = geomFace;
                            break;

                        }
                        index++;



                    }
                    break;
                }
            }


            return face;


        }

        public Face nonLinkedModelCase(GeometryElement geoElement, int index, Face face, UV uv)
        {

            foreach (GeometryObject geomObj in geoElement)
            {

                Solid geomSolid = geomObj as Solid;
                if (geomSolid != null)
                {
                    foreach (Face geomFace in geomSolid.Faces)
                    {

                        face = geomFace;

                        break;


                    }
                    break;
                }
            }



            return face;

        }
        FailureProcessingResult PreprocessFailures(FailuresAccessor a)
        {
            IList<FailureMessageAccessor> failures = a.GetFailureMessages();

            foreach (FailureMessageAccessor f in failures)
            {
                FailureSeverity fseverity = a.GetSeverity();

                if (fseverity == FailureSeverity.Warning)
                {
                    a.DeleteWarning(f);
                }
                else
                {
                    a.ResolveFailure(f);
                    return FailureProcessingResult.ProceedWithCommit;
                }
            }
            return FailureProcessingResult.Continue;


        }

        public void viewRange(Document doc, Level level)
        {
            //setting the view plan to be active view
            ViewPlan viewPlan = doc.ActiveView as ViewPlan;

            //setting plan- view planes to the top clip, cut plane,bottom plane and view depth
            PlanViewPlane planView_topClip = PlanViewPlane.TopClipPlane;
            PlanViewPlane planView_bottomClip = PlanViewPlane.BottomClipPlane;
            PlanViewPlane planView_cutClip = PlanViewPlane.CutPlane;
            PlanViewPlane planView_vDepth = PlanViewPlane.ViewDepthPlane;

            //setting view range variable
            PlanViewRange viewRange = viewPlan.GetViewRange();


            ElementId eleId_current = PlanViewRange.Current;
            ElementId eleId_inf = PlanViewRange.Unlimited;
            ElementId eleId_above = PlanViewRange.LevelAbove;
            ElementId eleId_below = PlanViewRange.LevelBelow;


            ElementId associatedTopPlane_Id = viewRange.GetLevelId(planView_topClip);
            ElementId associatedBottomPlane_Id = viewRange.GetLevelId(planView_bottomClip);
            ElementId associatedCutPlane_Id = viewRange.GetLevelId(planView_cutClip);
            ElementId associatedViewDepthPlane_Id = viewRange.GetLevelId(planView_vDepth);
            viewRange.SetOffset(planView_topClip, 400 * 0.03280839);
            if (associatedTopPlane_Id != level.Id || associatedViewDepthPlane_Id != level.Id)
            {

                //TaskDialog.Show("View range not valid", "Pleas adjust the view range to associate to the CURRENT level only.");
                TaskDialog.Show("View range not valid", "מתוקים, יש בעיה עם ה-view range. צריך לשנות אותו כך שה associated level מלמעלה ומלמטה יהיה עבור הקומה הנבחרת בלבד. (אם יש view template למבט הזה" +
                    "תהפוך אותו ל None ואז נסה לשנות view range. )קשה? ארוך? מעייף? תכניס מתלים  לבד נראה אותך..!");
                Form1.stopScript = true;
            }


        }




        /// Create shared parameter for XYZ location, for pipe accessory category
        private bool CreateHangerSharedParameter(Document doc, ExternalCommandData commandData)
        {
            // Create Hanger Shared Parameter Routine: -->
            // 1: Check whether the Hanger shared parameter("Hanger Coordinant") has been defined.
            // 2: Share parameter file locates under sample directory of this .dll module.
            // 3: Add a group named "HangerScheduleGroup".
            // 4: Add a shared parameter named "Hanger Coordinant" to "OST_PipeAccessory" category, which is visible.

            try
            {

                using (Transaction trans = new Transaction(doc, "New Param"))
                {
                    trans.Start();

                    // create shared parameter file
                    String modulePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    String paramFile = modulePath + "\\HangersSharedParameters.txt";
                    if (File.Exists(paramFile))
                    {
                        File.Delete(paramFile);
                    }
                    FileStream fs = File.Create(paramFile);
                    fs.Close();

                    // cache application handle
                    Autodesk.Revit.ApplicationServices.Application revitApp = commandData.Application.Application;

                    // prepare shared parameter file
                    commandData.Application.Application.SharedParametersFilename = paramFile;

                    // open shared parameter file
                    DefinitionFile parafile = revitApp.OpenSharedParameterFile();

                    // create a group
                    DefinitionGroup apiGroup = parafile.Groups.Create("HangerScheduleGroup");

                    // create a visible "Hanger Coordinant" of text type.
                    ExternalDefinitionCreationOptions ExternalDefinitionCreationOptions = new ExternalDefinitionCreationOptions("Hanger Coordinant", ParameterType.Text);
                    Definition hangersSharedParamDef = apiGroup.Definitions.Create(ExternalDefinitionCreationOptions);
                    // get "OST_PipeAccessory" category
                    Category hangerCat = commandData.Application.ActiveUIDocument.Document.Settings.Categories.get_Item(BuiltInCategory.OST_PipeAccessory);
                    CategorySet categories = revitApp.Create.NewCategorySet();
                    categories.Insert(hangerCat);

                    // insert the new parameter
                    InstanceBinding binding = revitApp.Create.NewInstanceBinding(categories);
                    commandData.Application.ActiveUIDocument.Document.ParameterBindings.Insert(hangersSharedParamDef, binding);

                    trans.Commit();
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to create shared parameter: " + ex.Message);
            }
        }

    }
}
