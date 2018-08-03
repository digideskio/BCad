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
    [ExportRibbonTab("home")]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HomeRibbon : RibbonTab
    {
        [ImportingConstructor]
        public HomeRibbon(IWorkspace workspace)
        {
            InitializeComponent();
            Name = "Home";
        }
    }
}
