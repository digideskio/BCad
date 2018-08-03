using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace IxMilia.BCad.Ribbons
{
    public class RibbonTab : ContentView
    {
        public static readonly BindableProperty NameProperty = BindableProperty.Create(nameof(Name), typeof(string), typeof(RibbonTab), null);

        public string Name
        {
            get => (string)GetValue(NameProperty);
            set => SetValue(NameProperty, value);
        }
    }
}
