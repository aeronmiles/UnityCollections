using System;

public static class DateTimeExt
{
  public static long ToUnixTimeSeconds(this DateTime dateTime)
  {
    return ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
  }

  public static double ToUnixDecimalSeconds(this DateTime dateTime)
  {
    return ((DateTimeOffset)dateTime).ToUnixTimeSeconds() + (dateTime - dateTime.Date).TotalSeconds % 1;
  }

  public static long ToUnixTimeMilliseconds(this DateTime dateTime)
  {
    return ((DateTimeOffset)dateTime).ToUnixTimeMilliseconds();
  }
}
