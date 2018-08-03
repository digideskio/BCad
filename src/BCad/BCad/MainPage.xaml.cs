using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace IxMilia.BCad
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            CompositionContainer.Container.SatisfyImports(this);
        }

        [Import]
        public IWorkspace Workspace { get; set; }

        [OnImportsSatisfied]
        public void OnImportsSatisfied()
        {

        }
    }
}
