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
}
