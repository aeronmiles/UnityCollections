[System.Serializable]
public struct XYZBool
{
    public bool Any => X || Y || Z;
    public bool X;
    public bool Y;
    public bool Z;
}