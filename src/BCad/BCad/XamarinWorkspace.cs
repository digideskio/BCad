using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Threading.Tasks;

namespace IxMilia.BCad
{
    public abstract class XamarinWorkspace : WorkspaceBase
    {
        public XamarinWorkspace()
        {
            Update(drawing: Drawing.Update(author: Environment.UserName));
        }

        [OnImportsSatisfied]
        public void OnImportsSatisfied()
        {
            LoadSettings();
        }

        protected abstract void LoadSettings();

        public abstract void SaveSettings();
    }
}
