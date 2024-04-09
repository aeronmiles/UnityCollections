using System.Collections.Generic;
using System.Linq;

public static class IEnumerableExt
{
  public static bool AsArray<T>(this IEnumerable<T> array, out T[] outArray)
  {
    outArray = array as T[];
    if (outArray == null)
    {
      return false;
    }
    return false;
  }

  public static bool AsList<T>(this IEnumerable<T> array, out List<T> outList)
  {
    outList = array as List<T>;
    if (outList == null)
    {
      return false;
    }
    return true;
  }

  public static T TakeRandom<T>(this IEnumerable<T> array)
  {
    int l = array.Count();
    int x = UnityEngine.Random.Range(0, l);
    return array.ElementAt(x);
  }
}