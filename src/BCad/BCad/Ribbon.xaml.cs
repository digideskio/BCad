using System;
using System.Collections.Generic;
using System.Composition;
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
        private Dictionary<string, ContentView> _ribbons = new Dictionary<string, ContentView>();

        [Import]
        public IWorkspace Workspace { get; set; }

        [ImportMany]
        public IEnumerable<Lazy<RibbonTab, RibbonTabMetadata>> RibbonTabs { get; set; }

        public Ribbon()
        {
            InitializeComponent();

            CompositionContainer.Container.SatisfyImports(this);
        }

        [OnImportsSatisfied]
        public void OnImportsSatisfied()
        {
            foreach (var ribbonId in Workspace.SettingsService.GetValue<string[]>(XamarinSettingsProvider.RibbonOrder))
            {
                var ribbonMetadata = RibbonTabs.FirstOrDefault(t => t.Metadata.Id == ribbonId);
                if (ribbonMetadata != null)
                {
                    var id = ribbonMetadata.Metadata.Id;
                    var ribbon = ribbonMetadata.Value;
                    var ribbonButton = new TaggedButton();
                    ribbonButton.Tag = id;
                    ribbonButton.Text = ribbon.Name;
                    ribbonButton.Clicked += TabButtonClicked;
                    buttonLayout.Children.Add(ribbonButton);
                    _ribbons[id] = ribbon;
                }
            }

            SetRibbon("home");
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
