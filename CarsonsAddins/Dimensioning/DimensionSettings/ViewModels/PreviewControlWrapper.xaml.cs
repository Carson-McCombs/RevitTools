using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;


namespace CarsonsAddins
{
    /// <summary>
    /// Interaction logic for PreviewControlWrapper.xaml
    /// </summary>
    public partial class PreviewControlWrapper : UserControl
    {
        public PreviewControl previewControl;
        /*
        private Document previewDocument;
        public Document PreviewDocument
        {
            get => previewDocument;
            set => previewDocument = value;
        }
        private ElementId viewId = ElementId.InvalidElementId;
        public ElementId ViewId
        {
            get => viewId;
            set => viewId = value;
        }*/

        public PreviewControlWrapper()
        {
            InitializeComponent();
            //AddPreviewControl(previewDocument, ViewId);
        }
        public void AddPreviewControlWithCustomView(Document doc)
        {
            if (doc == null) return;
            Transaction transaction = new Transaction(doc);
            transaction.Start("Create Dimension View");
            try
            {
                Level level = Level.Create(doc, 0.0);
                ElementId floorPlanId = doc.GetDefaultElementTypeId(ElementTypeGroup.ViewTypeFloorPlan);
                if (level == null || floorPlanId == ElementId.InvalidElementId) 
                {
                    transaction.RollBack();
                    return;
                }
                ViewPlan viewPlan = ViewPlan.Create(doc, floorPlanId, level.Id);

                previewControl = new PreviewControl(doc, viewPlan.Id)
                {
                    IsEnabled = false
                };

                PreviewControlGrid.Children.Add(previewControl);



                transaction.Commit();
            } 
            catch(Exception ex)
            {
                transaction.RollBack();
                TaskDialog.Show("Error Creating Dimension View", ex.Message);
            }
        }
        public void AddPreviewControl(Document doc, ElementId viewId)
        {
            if (doc == null || viewId == ElementId.InvalidElementId) return;
            previewControl = new PreviewControl(doc, viewId);
            PreviewControlGrid.Children.Add(previewControl);
            previewControl.IsHitTestVisible = false;

        }
    }
}
