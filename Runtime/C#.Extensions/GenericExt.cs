using System;
using System.Collections.Generic;
using System.Linq;

public static class GenericExt
{
    public static T Map<T, TU>(this T target, TU source)
    {
        // get property list of the target object.
        // this is a reflection extension which simply gets properties (CanWrite = true).
        var tprops = typeof(T).GetProperties();

        tprops.Where(x => x.CanWrite == true).ToList().ForEach(prop =>
          {
              // check whether source object has the the property
              var sp = source.GetType().GetProperty(prop.ToString());
              if (sp != null)
              {
                  // if yes, copy the value to the matching property
                  var value = sp.GetValue(source, null);
                  target.GetType().GetProperty(prop.ToString()).SetValue(target, value, null);
              }
          });

        return target;
    }

    public static void ForEach<T>(this IEnumerable<T> data, Action<T> action)
    {
        foreach (T element in data) action(element);
    }

    ///<summary>Finds the index of the first item matching an expression in an enumerable.</summary>
    ///<param name="items">The enumerable to search.</param>
    ///<param name="predicate">The expression to test the items against.</param>
    ///<returns>The index of the first matching item, or -1 if no items match.</returns>
    public static int FindIndex<T>(this IEnumerable<T> items, Func<T, bool> predicate)
    {
        if (items == null) throw new ArgumentNullException("items");
        if (predicate == null) throw new ArgumentNullException("predicate");

        int retVal = 0;
        foreach (var item in items)
        {
            if (predicate(item)) return retVal;
            retVal++;
        }
        return -1;
    }

    ///<summary>Finds the index of the first occurrence of an item in an enumerable.</summary>
    ///<param name="items">The enumerable to search.</param>
    ///<param name="item">The item to find.</param>
    ///<returns>The index of the first matching item, or -1 if the item was not found.</returns>
    public static int IndexOf<T>(this IEnumerable<T> items, T item) { return items.FindIndex(i => EqualityComparer<T>.Default.Equals(item, i)); }
}
