using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DateTimeExt
{
    public static long ToUnixTimecodeSeconds(this DateTime dateTime)
    {
        return ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
    }

    public static double ToUnixDecimalSeconds(this DateTime dateTime)
    {
        return (dateTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }
}
