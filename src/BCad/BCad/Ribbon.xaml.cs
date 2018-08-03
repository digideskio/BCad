using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IxMilia.BCad.Ribbons;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IxMilia.BCad
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Ribbon : ContentView
    {
        private Dictionary<string, ContentView> _ribbons;

        public Ribbon()
        {
            InitializeComponent();

            _ribbons = new Dictionary<string, ContentView>()
            {
                {"Home", new HomeRibbon()},
                {"View", new ViewRibbon()}
            };
            foreach (var name in _ribbons.Keys)
            {
                var button = new TaggedButton();
                button.Tag = name;
                button.Text = name;
                button.Clicked += TabButtonClicked;
                buttonLayout.Children.Add(button);
            }

            SetRibbon("Home");
        }

        private void TabButtonClicked(object sender, EventArgs e)
        {
            var button = (TaggedButton)sender;
            SetRibbon(button.Tag);
        }

        private void SetRibbon(string ribbonName)
        {
            if (_ribbons.TryGetValue(ribbonName, out var ribbon))
            {
                tabContent.Content = ribbon;
            }
        }
    }
}
