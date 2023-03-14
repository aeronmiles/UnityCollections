using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DateTimeExt
{
    public static long ToUnixTimecode(this DateTime dateTime)
    {
        return ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
    }
}
