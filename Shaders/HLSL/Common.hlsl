#ifndef __COMMON__DO234FDSA3523FEFW52355G__
#define __COMMON__DO234FDSA3523FEFW52355G__

// ========= Const ===========
#ifndef EPSILON
#define EPSILON 1e-10
#endif
#ifndef PI
#define PI 3.1415926536
#endif

// ========= Func ===========
float3 mod289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
float2 mod289(float2 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
float3 permute(float3 x) { return mod289(((x*34.0)+1.0)*x); }
float rand(float2 st) { return frac(sin(dot(st.xy, float2(12.9898,78.233))) * 43758.5453123); }
float randUnit(float2 st) { return frac(sin(dot(st.xy, float2(12.9898,78.233))) * 43758.5453123) * 2.0 - 1.0; }
float offset(float x, float power, float offset) { return pow(x, power) + offset; }
float3 offset(float3 xyz, float power, float offset) { return pow(xyz, power) + offset; }

float2 rotate2D(float2 vec, float angle) 
{ 
  return float2(cos(angle) * vec.x - sin(angle) * vec.y, sin(angle) * vec.x + cos(angle) * vec.y);
}

// Approximate screen space UVs (note: not 100% accurate with perspective)
float2 uvScreen(float4 vert)
{
    // Approximate screen space UVs (note: not 100% accurate with perspective)
    float2 uvScreen;
    uvScreen.x = (vert.x / vert.w + 1.0) * 0.5; 
    uvScreen.y = (vert.y / vert.w + 1.0) * 0.5;
    uvScreen.y = 1.0 - uvScreen.y; // Flip Y
    return uvScreen;
}

// Approximate screen space UVs (note: not 100% accurate with perspective)
float2 uvScreen(float4 vert, float rotationRadians)
{
    // Approximate screen space UVs (note: not 100% accurate with perspective)
    float2 uvScreen;
    uvScreen.x = (vert.x / vert.w + 1.0) * 0.5; 
    uvScreen.y = (vert.y / vert.w + 1.0) * 0.5;
    uvScreen.y = 1.0 - uvScreen.y; // Flip Y
    
    // Apply inverse rotation to the UV coordinates
    float2x2 rotationMatrix = float2x2(cos(rotationRadians), sin(rotationRadians),
                                       -sin(rotationRadians), cos(rotationRadians));
    
    // Translate UVs to center, apply rotation, then translate back
    uvScreen = mul(rotationMatrix, (uvScreen - 0.5)) + 0.5;

    return uvScreen;
}

float fresnel(float3 normal, float3 viewDir)
{   
    float dotNV = dot(normalize(viewDir), normalize(normal));
    return saturate(pow(1.0 - max(0, dotNV), 5)); 
}

float fresnel(float3 normal, float3 viewDir, float bias, float power)
{   
    float dotNV = dot(normalize(viewDir), normalize(normal));
    return saturate(pow(pow(1.0 - max(0, dotNV), 5), power) + bias); 
}

float expFresnel(float3 worldPos, float3 worldNorm)
{
    float3 normal = normalize(mul(unity_ObjectToWorld, float4(worldNorm, 0.0)).xyz); 
    float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);
    return 1.0 - fresnel(normal, viewDir);
}

float lerpWithMidpointOffset(float lerpFactor, float midpointOffset) {
  float adjustedLerpFactor = lerpFactor * 2.0 - 1.0; // Map lerpFactor from [0, 1] to [-1, 1]
  float offsetLerpFactor = adjustedLerpFactor * (1.0 - midpointOffset) + midpointOffset; // Apply midpoint offset
  float result = smoothstep(-1.0, 1.0, offsetLerpFactor); // Smoothly interpolate between 0 and 1
  return result;
}

// ========= Hash ===========
float3 hashOld33(float3 p)
{   
	p = float3( dot(p,float3(127.1,311.7, 74.7)),
			  dot(p,float3(269.5,183.3,246.1)),
			  dot(p,float3(113.5,271.9,124.6)));
    
  return -1.0 + 2.0 * frac(sin(p)*43758.5453123);
}

float hashOld31(float3 p)
{
    float h = dot(p,float3(127.1,311.7, 74.7));
    return -1.0 + 2.0 * frac(sin(h)*43758.5453123);
}

// Grab from https://www.shadertoy.com/view/4djSRW
#define MOD3 float3(.1031,.11369,.13787)
//#define MOD3 float3(443.8975,397.2973, 491.1871)
float hash31(float3 p3)
{
	p3  = frac(p3 * MOD3);
  p3 += dot(p3, p3.yzx + 19.19);
  return -1.0 + 2.0 * frac((p3.x + p3.y) * p3.z);
}

float3 hash33(float3 p3)
{
	p3 = frac(p3 * MOD3);
  p3 += dot(p3, p3.yxz+19.19);
  return -1.0 + 2.0 * frac(float3((p3.x + p3.y)*p3.z, (p3.x+p3.z)*p3.y, (p3.y+p3.z)*p3.x));
}


//
// END OF __COMMON__DO234FDSA3523FEFW52355G__
//
#endif
