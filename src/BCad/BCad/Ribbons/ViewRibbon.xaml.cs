using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IxMilia.BCad.Ribbons
{
    [ExportRibbonTab("view")]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ViewRibbon : RibbonTab
    {
        [ImportingConstructor]
        public ViewRibbon(IWorkspace workspace)
        {
            InitializeComponent();
            Name = "View";
        }
    }
}
