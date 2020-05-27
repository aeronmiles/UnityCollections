using System;

public static class StringExt
{
    public static string UnixEpoch(this String str)
    {
        return (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
        .TotalSeconds.ToString();
    }
}