using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
                //ViewPlan.Create(doc, ViewFamily.FloorPlan, doc.lev)
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

        }
    }
}
