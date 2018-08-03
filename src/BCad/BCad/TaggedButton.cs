using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace IxMilia.BCad
{
    public class TaggedButton : Button
    {
        public static readonly BindableProperty TagProperty = BindableProperty.Create("Tag", typeof(string), typeof(TaggedButton), null);

        public string Tag
        {
            get => (string)GetValue(TagProperty);
            set => SetValue(TagProperty, value);
        }
    }
}
