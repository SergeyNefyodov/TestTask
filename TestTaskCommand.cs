using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace TestTask
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class TestTaskCommand : IExternalCommand
    {
        static Guid addinId = new Guid("D00B41A1-70B2-4399-876E-D28F09BA64B7");
        UIDocument uidoc { get; set; }
        Document doc { get; set; }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            uidoc = commandData.Application.ActiveUIDocument;
            doc = uidoc.Document;
            View activeView = doc.ActiveView;
            if (activeView.ViewType != ViewType.FloorPlan 
                && activeView.ViewType != ViewType.CeilingPlan
                && activeView.ViewType != ViewType.AreaPlan
                && activeView.ViewType != ViewType.EngineeringPlan
                && activeView.ViewType != ViewType.Section)
            {
                TaskDialog.Show("Error", "Активный вид не является планом или разрезом");
                return Result.Failed;
            }
            List<ElementId> workList = uidoc.Selection.GetElementIds().ToList();
            if (!CheckForGrids(ref workList))
            {
                var refs = uidoc.Selection.PickObjects(ObjectType.Element,
                    new SelectionFilter(), "Выберите оси");
                foreach (Reference r in refs)
                {
                    workList.Add(doc.GetElement(r).Id);
                }
            }
            using (Transaction t = new Transaction(doc, "Изменение режима осей"))
            {
                t.Start();
                foreach (ElementId id in workList)
                {
                    Grid grid = doc.GetElement(id) as Grid;
                    DatumExtentType det0 = grid.GetDatumExtentTypeInView(DatumEnds.End0, activeView);
                    DatumExtentType det1 = grid.GetDatumExtentTypeInView(DatumEnds.End1, activeView);
                    grid.SetDatumExtentType(DatumEnds.End0, activeView, ChangeType(det0));
                    grid.SetDatumExtentType(DatumEnds.End1, activeView, ChangeType(det1));
                }
                t.Commit();
            }
            return Result.Succeeded;
        }
        private bool CheckForGrids(ref List<ElementId> workList)
        {
            bool b = false;
            for (int i = workList.Count - 1; i>-1; i--)
            {
                if (doc.GetElement(workList[i]).Category.Id.IntegerValue == (int)BuiltInCategory.OST_Grids)
                {
                    b = true;
                }
                else
                {
                    workList.RemoveAt(i);
                }
            }            
            return b;
        }
        private DatumExtentType ChangeType(DatumExtentType det)
        {
            if (det == DatumExtentType.Model)
                return DatumExtentType.ViewSpecific;
            return DatumExtentType.Model;
        }
    }
    public class SelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Grids)
            {
                return true;
            }
            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }

}
