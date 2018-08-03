using System;
using System.Collections.Generic;
using System.Text;

namespace IxMilia.BCad
{
    public class CadCommandButton : TaggedButton
    {
        public CadCommandButton()
        {
            Clicked += CadCommandButton_Clicked;
        }

        private void CadCommandButton_Clicked(object sender, EventArgs e)
        {
            var command = Tag;
            // TODO: execute command
        }
    }
}
