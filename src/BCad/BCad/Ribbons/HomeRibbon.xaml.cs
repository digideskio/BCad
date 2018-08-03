using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IxMilia.BCad.Ribbons
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HomeRibbon : ContentView
    {
        public HomeRibbon()
        {
            InitializeComponent();
        }

        private void CadCommandButtonClicked(object sender, EventArgs e)
        {
            var button = (TaggedButton)sender;
            var command = button.Tag;
        }
    }
}
