using System.Runtime.CompilerServices;
using Unity.Mathematics;

public static class float4x4Ext
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 Position(this float4x4 m)
    {
        return new float3(m.c3[0], m.c3[1], m.c3[2]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static quaternion Rotation(this float4x4 m)
    {
        return quaternion.LookRotationSafe(m.Forward(), m.Up());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 Scale(this float4x4 m)
    {
        return new float3(math.length(m.c0), math.length(m.c1), math.length(m.c2));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 Forward(this float4x4 m)
    {
        return new float3(m.c2[0], m.c2[1], m.c2[2]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 Up(this float4x4 m)
    {
        return new float3(m.c1[0], m.c1[1], m.c1[2]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 Right(this float4x4 m)
    {
        return new float3(m.c0[0], m.c0[1], m.c0[2]);
    }
}
