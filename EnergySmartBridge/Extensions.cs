using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EnergySmartBridge
{
    public static class Extensions
    {
        public static T ToObject<T>(this NameValueCollection collection) where T : new()
        {
            T obj = new T();

            foreach (KeyValuePair<string, string> kvp in collection.ToKVP())
            {
                PropertyInfo pi = obj.GetType().GetProperty(kvp.Key, BindingFlags.Public | BindingFlags.Instance);
                if (pi != null)
                {
                    if(pi.PropertyType == typeof(int))
                        pi.SetValue(obj, int.Parse(kvp.Value), null);
                    else if (pi.PropertyType == typeof(bool))
                        pi.SetValue(obj, bool.Parse(kvp.Value), null);
                    else
                        pi.SetValue(obj, kvp.Value, null);
                }
            }

            return obj;
        }

        public static IEnumerable<KeyValuePair<string, string>> ToKVP(this NameValueCollection source)
        {
            return source.AllKeys.SelectMany(
                source.GetValues,
                (k, v) => new KeyValuePair<string, string>(k, v));
        }
    }
}
