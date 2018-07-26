using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;

namespace FTCPathPlanning
{
    public static class NonDependencyBinding
    {
        private static Dictionary<INotifyPropertyChanged, Dictionary<string, Dictionary<INotifyPropertyChanged, List<string>>>> bindingSources
            = new Dictionary<INotifyPropertyChanged, Dictionary<string, Dictionary<INotifyPropertyChanged, List<string>>>>();

        public static void Create(INotifyPropertyChanged source, string sourceProperty, 
            INotifyPropertyChanged target, string targetProperty, BindingDirection direction = BindingDirection.OneWay)
        {
            if(!bindingSources.Keys.Contains(source))
            {
                bindingSources[source] = new Dictionary<string, Dictionary<INotifyPropertyChanged, List<string>>>();
            }
            if(!bindingSources[source].Keys.Contains(sourceProperty))
            {
                bindingSources[source][sourceProperty] = new Dictionary<INotifyPropertyChanged, List<string>>();
            }
            if(!bindingSources[source][sourceProperty].Keys.Contains(target))
            {
                bindingSources[source][sourceProperty][target] = new List<string>();
            }
            bindingSources[source][sourceProperty][target].Add(targetProperty);
            source.PropertyChanged -= Source_PropertyChanged;
            source.PropertyChanged += Source_PropertyChanged;
            if(direction == BindingDirection.TwoWay)
            {
                Create(target, targetProperty, source, sourceProperty);
            }
        }

        public static void CleanupBindingSource(INotifyPropertyChanged source)
        {
            bindingSources.Remove(source);
        }

        private static void Source_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            INotifyPropertyChanged source = sender as INotifyPropertyChanged;
            Type sourceType = source.GetType();
            PropertyInfo sourceProperty = sourceType.GetProperty(e.PropertyName, BindingFlags.Public | BindingFlags.Instance);
            object sourceValue = sourceProperty.GetValue(source);
            if (bindingSources[source].Keys.Contains(e.PropertyName))
            {
                Dictionary<INotifyPropertyChanged, List<string>> targets = bindingSources[source][e.PropertyName];
                foreach(KeyValuePair<INotifyPropertyChanged, List<string>> kp in targets)
                {
                    INotifyPropertyChanged target = kp.Key;
                    List<string> props = kp.Value;
                    Type targetType = target.GetType();
                    foreach(string prop in props)
                    {
                        PropertyInfo targetProperty = targetType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance);
                        try
                        {
                            if (sourceProperty.PropertyType == targetProperty.PropertyType)
                            {
                                //get a value
                                targetProperty.SetValue(target, sourceValue);
                            }
                            else
                            {
                                //try a conversion
                                targetProperty.SetValue(target, Convert.ChangeType(sourceValue, targetProperty.PropertyType));
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                    }
                }
            }
        }
    }
}
