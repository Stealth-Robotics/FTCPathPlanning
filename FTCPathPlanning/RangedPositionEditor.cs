using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.PropertyGrid;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace FTCPathPlanning
{
    public class RangedPositionEditor : ITypeEditor
    {
        public static Style Style { get; set; } = null;

        public FrameworkElement ResolveEditor(PropertyItem propertyItem)
        {
            bool writable = !propertyItem.IsReadOnly;

            DoubleUpDown editor = new DoubleUpDown();
            editor.Style = Style;
            editor.IsEnabled = writable;
            editor.ShowButtonSpinner = writable;
            if(!writable)
            {
                editor.Minimum = null;
                editor.Maximum = null;
            }

            //create the binding from the bound property item to the editor
            var _binding = new Binding("Value"); //bind to the Value property of the PropertyItem
            _binding.Source = propertyItem;
            _binding.ValidatesOnExceptions = true;
            _binding.ValidatesOnDataErrors = true;
            _binding.Mode = writable ? BindingMode.TwoWay : BindingMode.OneWay;
            BindingOperations.SetBinding(editor, DoubleUpDown.ValueProperty, _binding);
            return editor;
        }
    }
}
