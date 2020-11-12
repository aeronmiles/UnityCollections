[System.Serializable]
public struct XYZBool
{
    public bool X;
    public bool Y;
    public bool Z;
}

public static class XYZBoolExt
{
    public static bool Any(this XYZBool xyzbool) => xyzbool.X || xyzbool.Y || xyzbool.Z;
    public static bool None(this XYZBool xyzbool) => !xyzbool.X && !xyzbool.Y && !xyzbool.Z;
    public static bool All(this XYZBool xyzbool) => xyzbool.X && xyzbool.Y && xyzbool.Z;
}
