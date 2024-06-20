using System;

public static class DateTimeExt
{
  public static long ToUnixTimeSeconds(this DateTime dateTime) => ((DateTimeOffset)dateTime).ToUnixTimeSeconds();

  public static double ToUnixDecimalSeconds(this DateTime dateTime) => ((DateTimeOffset)dateTime).ToUnixTimeSeconds() + ((dateTime - dateTime.Date).TotalSeconds % 1);

  public static long ToUnixTimeMilliseconds(this DateTime dateTime) => ((DateTimeOffset)dateTime).ToUnixTimeMilliseconds();
}
