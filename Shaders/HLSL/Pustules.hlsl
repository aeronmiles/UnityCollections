#include "Common.hlsl"
#include "Blending.hlsl"

float regionValue(float2 pos, SamplerState ss, Texture2D reg1, Texture2D reg2, float3 r1Mul, float3 r2Mul, float regPow)
{
    float3 r1 = reg1.Sample(ss, pos) * regPow;
    r1 = (r1.r * r1Mul.r) + (r1.g * r1Mul.g) + (r1.b * r1Mul.b);
    float3 r2 = reg2.Sample(ss, pos) * regPow;
    r2 = (r2.r * r2Mul.r) + (r2.g * r2Mul.g) + (r2.b * r2Mul.b);
    return r1 + r2;
}

void pustules_float(float2 uv, float3 rad, float3 power, float rVar, float powVar, float k, int n, int seed, SamplerState ss, Texture2D reg1, Texture2D reg2, float3 reg1Mul, float3 reg2Mul, float regPow, out float3 Out)
{ 
    // seed = sin(fmod(seed, 99999)) * 99999;
    float3 c = 1.;
    for (int i = 0; i < n; i++)
    {
        float rX = rand(float2(i, i-seed));
        float rY = rand(float2(i, i+seed));

        float2 pos = float2(0.5, 0.5) + (float2(rX, rY) * 0.66 - 0.33);
        if (regionValue(pos, ss, reg1, reg2, reg1Mul, reg2Mul, regPow) < 0.7f)
        {
            n++;
            continue;
        }

        float r0 = rand(float2(i+seed, i));
        float r = rad.x + (rad.x * r0 * rVar) - (rad.x *r0 * rVar * 0.5);

        float rInner = rad.x * rad.y;
        rInner += (rInner * r0 * rVar) - (rInner * r0 * rVar * 0.5);

        // float o = rand(float2(i-seed, i+seed)) * oVar;

        float l = length(uv - pos);
        float rP = rand(float2(i+seed, i-seed));
        rP = 1 + (rP * powVar) - (rP * powVar * 0.5);
        c.r = expsmin(c.r, pow(smoothstep(0., r, l), 1. / (power.x * rP)), k);
        c.g = expsmin(c.g, smoothstep(0., rInner, pow(l, 1. / (power.y * rP))), k);
        // c.r = lerp(c.r, expsmin(c.r, smoothstep(0., r * rad.z, pow(l, 1. / (power.z * rP))), k), 0.2);
    }
    
    Out = 1. - float3(c.r, c.g, c.b);
}