using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

[Serializable]
public struct float5x4 : IEquatable<float5x4>
{
    public float4 c0;
    public float4 c1;
    public float4 c2;
    public float4 c3;
    public float4 c4;
    public static readonly float5x4 identity = new float5x4(new float4(1f, 0f, 0f, 0f), new float4(0f, 1f, 0f, 0f), new float4(0f, 0f, 1f, 0f), new float4(0f, 0f, 0f, 1f), new float4(1.0));
    public static readonly float5x4 zero = new float5x4(new float4(0f), new float4(0f), new float4(0f), new float4(0f), new float4(0f));

    public float5x4(float4 c0, float4 c1, float4 c2, float4 c3, float4 c4)
    {
        this.c0 = c0;
        this.c1 = c1;
        this.c2 = c2;
        this.c3 = c3;
        this.c4 = c4;
    }

    public float5x4(float4x4 m, float4 c4)
    {
        this.c0 = m.c0;
        this.c1 = m.c1;
        this.c2 = m.c2;
        this.c3 = m.c3;
        this.c4 = c4;
    }

    public static explicit operator float4x4(float5x4 m) { return new float4x4(m.c0, m.c1, m.c2, m.c3); }


    /// <summary>Returns the result of a componentwise multiplication operation on two float4x4 matrices.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float5x4 operator *(float5x4 lhs, float5x4 rhs) { return new float5x4(lhs.c0 * rhs.c0, lhs.c1 * rhs.c1, lhs.c2 * rhs.c2, lhs.c3 * rhs.c3, lhs.c4 * rhs.c4); }

    /// <summary>Returns the result of a componentwise multiplication operation on a float5x4 matrix and a float value.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float5x4 operator *(float5x4 lhs, float rhs) { return new float5x4(lhs.c0 * rhs, lhs.c1 * rhs, lhs.c2 * rhs, lhs.c3 * rhs, lhs.c4 * rhs); }

    /// <summary>Returns the result of a componentwise multiplication operation on a float value and a float5x4 matrix.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float5x4 operator *(float lhs, float5x4 rhs) { return new float5x4(lhs * rhs.c0, lhs * rhs.c1, lhs * rhs.c2, lhs * rhs.c3, lhs * rhs.c4); }


    /// <summary>Returns the result of a componentwise addition operation on two float5x4 matrices.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float5x4 operator +(float5x4 lhs, float5x4 rhs) { return new float5x4(lhs.c0 + rhs.c0, lhs.c1 + rhs.c1, lhs.c2 + rhs.c2, lhs.c3 + rhs.c3, lhs.c4 + rhs.c4); }

    /// <summary>Returns the result of a componentwise addition operation on a float5x4 matrix and a float value.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float5x4 operator +(float5x4 lhs, float rhs) { return new float5x4(lhs.c0 + rhs, lhs.c1 + rhs, lhs.c2 + rhs, lhs.c3 + rhs, lhs.c4 + rhs); }

    /// <summary>Returns the result of a componentwise addition operation on a float value and a float5x4 matrix.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float5x4 operator +(float lhs, float5x4 rhs) { return new float5x4(lhs + rhs.c0, lhs + rhs.c1, lhs + rhs.c2, lhs + rhs.c3, lhs + rhs.c4); }


    /// <summary>Returns the result of a componentwise subtraction operation on two float5x4 matrices.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float5x4 operator -(float5x4 lhs, float5x4 rhs) { return new float5x4(lhs.c0 - rhs.c0, lhs.c1 - rhs.c1, lhs.c2 - rhs.c2, lhs.c3 - rhs.c3, lhs.c4 - rhs.c4); }

    /// <summary>Returns the result of a componentwise subtraction operation on a float5x4 matrix and a float value.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float5x4 operator -(float5x4 lhs, float rhs) { return new float5x4(lhs.c0 - rhs, lhs.c1 - rhs, lhs.c2 - rhs, lhs.c3 - rhs, lhs.c4 - rhs); }

    /// <summary>Returns the result of a componentwise subtraction operation on a float value and a float5x4 matrix.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float5x4 operator -(float lhs, float5x4 rhs) { return new float5x4(lhs - rhs.c0, lhs - rhs.c1, lhs - rhs.c2, lhs - rhs.c3, lhs - rhs.c4); }


    /// <summary>Returns the result of a componentwise division operation on two float5x4 matrices.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float5x4 operator /(float5x4 lhs, float5x4 rhs) { return new float5x4(lhs.c0 / rhs.c0, lhs.c1 / rhs.c1, lhs.c2 / rhs.c2, lhs.c3 / rhs.c3, lhs.c4 / rhs.c4); }

    /// <summary>Returns the result of a componentwise division operation on a float5x4 matrix and a float value.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float5x4 operator /(float5x4 lhs, float rhs) { return new float5x4(lhs.c0 / rhs, lhs.c1 / rhs, lhs.c2 / rhs, lhs.c3 / rhs, lhs.c4 / rhs); }

    /// <summary>Returns the result of a componentwise division operation on a float value and a float5x4 matrix.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float5x4 operator /(float lhs, float5x4 rhs) { return new float5x4(lhs / rhs.c0, lhs / rhs.c1, lhs / rhs.c2, lhs / rhs.c3, lhs / rhs.c4); }


    /// <summary>Returns the result of a componentwise modulus operation on two float5x4 matrices.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float5x4 operator %(float5x4 lhs, float5x4 rhs) { return new float5x4(lhs.c0 % rhs.c0, lhs.c1 % rhs.c1, lhs.c2 % rhs.c2, lhs.c3 % rhs.c3, lhs.c4 % rhs.c4); }

    /// <summary>Returns the result of a componentwise modulus operation on a float5x4 matrix and a float value.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float5x4 operator %(float5x4 lhs, float rhs) { return new float5x4(lhs.c0 % rhs, lhs.c1 % rhs, lhs.c2 % rhs, lhs.c3 % rhs, lhs.c4 % rhs); }

    /// <summary>Returns the result of a componentwise modulus operation on a float value and a float5x4 matrix.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float5x4 operator %(float lhs, float5x4 rhs) { return new float5x4(lhs % rhs.c0, lhs % rhs.c1, lhs % rhs.c2, lhs % rhs.c3, lhs % rhs.c4); }


    /// <summary>Returns the result of a componentwise increment operation on a float5x4 matrix.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float5x4 operator ++(float5x4 val) { return new float5x4(++val.c0, ++val.c1, ++val.c2, ++val.c3, ++val.c4); }


    ///<summary>Returns the result of a componentwise decrement operation on a float5x4 matrix.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float5x4 operator --(float5x4 val) { return new float5x4(--val.c0, --val.c1, --val.c2, --val.c3, --val.c4); }


    /// <summary>Returns the result of a componentwise unary minus operation on a float5x4 matrix.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float5x4 operator -(float5x4 val) { return new float5x4(-val.c0, -val.c1, -val.c2, -val.c3, -val.c4); }

    /// the result of acomponentwise unary plus operation on a float5x4 matrix.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float5x4 operator +(float5x4 val) { return new float5x4(+val.c0, +val.c1, +val.c2, +val.c3, +val.c4); }


    public bool Equals(float5x4 rhs) { return c0.Equals(rhs.c0) && c1.Equals(rhs.c1) && c2.Equals(rhs.c2) && c3.Equals(rhs.c3) && c4.Equals(rhs.c4); }

    /// <summary>Returns true if the float4x4 is equal to a given float4x4, false otherwise.</summary>
    public override bool Equals(object o) { return Equals((float5x4)o); }


    /// <summary>Returns a hash code for the float4x4.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() { return (int)this.hash(); }

    /// <summary>Returns a string representation of the float5x4.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString()
    {
        return string.Format("float5x4({0}f, {1}f, {2}f, {3}f, {4}f,  {5}f, {6}f, {7}f, {8}f, {9}f,  {10}f, {11}f, {12}f, {13}f, {14}f,  {15}f, {16}f, {17}f, {18}f, {19}f)", c0.x, c1.x, c2.x, c3.x, c4.x, c0.y, c1.y, c2.y, c3.y, c4.y, c0.z, c1.z, c2.z, c3.z, c4.z, c0.w, c1.w, c2.w, c3.w, c4.w);
    }

}

public static partial class float5x4Ext
{
    /// <summary>Returns a uint hash code of a float5x4 vector.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint hash(this float5x4 v)
    {
        return math.csum(math.asuint(v.c0) * math.uint4(0xC4B1493Fu, 0xBA0966D3u, 0xAFBEE253u, 0x5B419C01u) +
                    math.asuint(v.c1) * math.uint4(0x515D90F5u, 0xEC9F68F3u, 0xF9EA92D5u, 0xC2FAFCB9u) +
                    math.asuint(v.c2) * math.uint4(0x616E9CA1u, 0xC5C5394Bu, 0xCAE78587u, 0x7A1541C9u) +
                    math.asuint(v.c3) * math.uint4(0xF83BD927u, 0x6A243BCBu, 0x509B84C9u, 0x91D13847u) +
                    math.asuint(v.c4) * math.uint4(0x52F7230Fu, 0xCF286E83u, 0xE121E6ADu, 0xC9CA1249u)) + 0x69B60C81u;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 Position(this float5x4 m)
    {
        return new float3(m.c3[0], m.c3[1], m.c3[2]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static quaternion Rotation(this float5x4 m)
    {
        return quaternion.LookRotationSafe(m.Forward(), m.Up());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 Scale(this float5x4 m)
    {
        return new float3(math.length(m.c0), math.length(m.c1), math.length(m.c2));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 Forward(this float5x4 m)
    {
        return new float3(m.c2[0], m.c2[1], m.c2[2]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 Up(this float5x4 m)
    {
        return new float3(m.c1[0], m.c1[1], m.c1[2]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 Right(this float5x4 m)
    {
        return new float3(m.c0[0], m.c0[1], m.c0[2]);
    }

    public static float5x4 Scale4x4(this float5x4 a, float3 b)
    {
        return new float5x4(float4x4.TRS(a.Position(), a.Rotation(), a.Scale() * b), a.c4);
    }

}