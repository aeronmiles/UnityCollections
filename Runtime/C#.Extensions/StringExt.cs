using System;
using System.Text.RegularExpressions;

public static class StringExt
{
    public static string RemoveSpecialCharacters(this string str)
    {
        return Regex.Replace(str, "[^a-zA-Z0-9_]+", "", RegexOptions.Compiled);
    }
}