#include "Common.hlsl"

// Ridged multifractal
// See "Texturing & Modeling, A Procedural Approach", Chapter 12
float ridge(float h, float offset)
{
    h = abs(h);     // create creases
    h = offset - h; // invert so creases are at top
    h = h * h;      // sharpen creases
    return h;
}

void ridgedMF_float(float2 uv, float scale, float octaves, float lacunarity, float gain, float offset, out float Out) {

    Out = 0.0;
    float freq = 1.0, amp = 0.5;
    float prev = 1.0;
    int n = (int)octaves;
    uv *= scale;
    for(int i=0; i < n; i++)
    {
        float n = ridge(snoise(uv*freq), offset);
        Out = n*amp;
        Out += n*amp*prev;  // scale by previous octave
        prev = n;
        freq *= lacunarity;
        amp *= gain;
    }
}