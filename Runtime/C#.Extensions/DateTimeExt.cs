using System;

// The following code caused a BUG, whenever UTC timecodes were passed in a non UTC locale, the timecodes have a the UTC offset x2, causing a future timecode. In UTC+ timecodes, this results in a negative timecode, when converting back to unix, which throws in the API. In UTC- timecodes, this results in offset into the future.
// public static long ToUnixTimeMilliseconds(this DateTime dateTime) => ((DateTimeOffset)dateTime).ToUnixTimeMilliseconds();

public static class DateTimeExt
{
  // Works on .NET 6+.  If you’re on an earlier runtime,
  // replace with:  private static readonly DateTime UnixEpoch = 
  //     new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
  private static readonly DateTime UnixEpoch = DateTime.UnixEpoch;

  /// <summary>
  /// Seconds since 1970‑01‑01T00:00:00Z.
  /// </summary>
  public static long ToUnixTimeSeconds(this DateTime dateTime) =>
      (long)(dateTime.ToUniversalTime() - UnixEpoch).TotalSeconds;

  /// <summary>
  /// Fractional‑second precision (e.g. 1714900932.582).
  /// </summary>
  public static double ToUnixDecimalSeconds(this DateTime dateTime) =>
      (dateTime.ToUniversalTime() - UnixEpoch).TotalSeconds;

  /// <summary>
  /// Milliseconds since 1970‑01‑01T00:00:00Z.
  /// </summary>
  public static long ToUnixTimeMilliseconds(this DateTime dateTime) =>
      (long)(dateTime.ToUniversalTime() - UnixEpoch).TotalMilliseconds;
}
