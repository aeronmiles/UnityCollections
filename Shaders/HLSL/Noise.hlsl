#ifndef __NOISE__385B023BFSLHF9520HFEF348__
#define __NOISE__385B023BFSLHF9520HFEF348__
#include "Common.hlsl"

float value_noise(float3 p)
{
    float3 pi = floor(p);
    float3 pf = p - pi;
    
    float3 w = pf * pf * (3.0 - 2.0 * pf);
    
    return 	lerp(
        		lerp(
        			lerp(hash31(pi + float3(0, 0, 0)), hash31(pi + float3(1, 0, 0)), w.x),
        			lerp(hash31(pi + float3(0, 0, 1)), hash31(pi + float3(1, 0, 1)), w.x), 
                    w.z),
        		lerp(
                    lerp(hash31(pi + float3(0, 1, 0)), hash31(pi + float3(1, 1, 0)), w.x),
        			lerp(hash31(pi + float3(0, 1, 1)), hash31(pi + float3(1, 1, 1)), w.x), 
                    w.z),
        		w.y);
}

float perlin_noise(float3 p)
{
    float3 pi = floor(p);
    float3 pf = p - pi;
    
    float3 w = pf * pf * (3.0 - 2.0 * pf);
    
    return 	lerp(
        		lerp(
                	lerp(dot(pf - float3(0, 0, 0), hash33(pi + float3(0, 0, 0))), 
                        dot(pf - float3(1, 0, 0), hash33(pi + float3(1, 0, 0))),
                       	w.x),
                	lerp(dot(pf - float3(0, 0, 1), hash33(pi + float3(0, 0, 1))), 
                        dot(pf - float3(1, 0, 1), hash33(pi + float3(1, 0, 1))),
                       	w.x),
                	w.z),
        		lerp(
                    lerp(dot(pf - float3(0, 1, 0), hash33(pi + float3(0, 1, 0))), 
                        dot(pf - float3(1, 1, 0), hash33(pi + float3(1, 1, 0))),
                       	w.x),
                   	lerp(dot(pf - float3(0, 1, 1), hash33(pi + float3(0, 1, 1))), 
                        dot(pf - float3(1, 1, 1), hash33(pi + float3(1, 1, 1))),
                       	w.x),
                	w.z),
    			w.y);
}

// Description : GLSL 2D simplex noise function
//      Author : Ian McEwan, Ashima Arts
//  Maintainer : ijm
//     Lastmod : 20110822 (ijm)
//     License :
//  Copyright (C) 2011 Ashima Arts. All rights reserved.
//  Distributed under the MIT License. See LICENSE file.
//  https://github.com/ashima/webgl-noise
//
float simplex(float2 st)
{
  // Precompute values for skewed triangular grid
  const float4 C = float4(0.211324865405187,
                      // (3.0-sqrt(3.0))/6.0
                      0.366025403784439,
                      // 0.5*(sqrt(3.0)-1.0)
                      -0.577350269189626,
                      // -1.0 + 2.0 * C.x
                      0.024390243902439);
                      // 1.0 / 41.0
  
  // First corner (x0)
  float2 i  = floor(st + dot(st, C.yy));
  float2 x0 = st - i + dot(i, C.xx);

  // Other two corners (x1, x2)
  float2 i1 = float2(0.0, 0.0);
  i1 = (x0.x > x0.y)? float2(1.0, 0.0):float2(0.0, 1.0);
  float2 x1 = x0.xy + C.xx - i1;
  float2 x2 = x0.xy + C.zz;

  // Do some permutations to avoid
  // truncation effects in permutation
  i = mod289(i);
  float3 p = permute(permute( i.y + float3(0.0, i1.y, 1.0))
              + i.x + float3(0.0, i1.x, 1.0 ));

  float3 m = max(0.5 - float3(
                      dot(x0,x0),
                      dot(x1,x1),
                      dot(x2,x2)
                      ), 0.0);

  m = m * m;
  m = m * m;

  // Gradients:
  //  41 pts uniformly over a line, mapped onto a diamond
  //  The ring size 17*17 = 289 is close to a multiple
  //      of 41 (41*7 = 287)

  float3 x = 2.0 * frac(p * C.www) - 1.0;
  float3 h = abs(x) - 0.5;
  float3 ox = floor(x + 0.5);
  float3 a0 = x - ox;

  // Normalise gradients implicitly by scaling m
  // Approximation of: m *= inversesqrt(a0*a0 + h*h);
  m *= 1.79284291400159 - 0.85373472095314 * (a0*a0+h*h);

  // Compute final noise value at P
  float3 g = float3(0.0,0.0,0.0);
  g.x  = a0.x  * x0.x  + h.x  * x0.y;
  g.yz = a0.yz * float2(x1.x,x2.x) + h.yz * float2(x1.y,x2.y);
  return 130.0 * dot(m, g);
}

float simplex_noise(float3 p)
{
  const float K1 = 0.333333333;
  const float K2 = 0.166666667;
  
  float3 i = floor(p + (p.x + p.y + p.z) * K1);
  float3 d0 = p - (i - (i.x + i.y + i.z) * K2);
  
  // thx nikita: https://www.shadertoy.com/view/XsX3zB
  float3 e = step(float3(0.0, 0.0, 0.0), d0 - d0.yzx);

  float3 i1 = e * (1.0 - e.zxy);
  float3 i2 = 1.0 - e.zxy * (1.0 - e);
  
  float3 d1 = d0 - (i1 - 1.0 * K2);
  float3 d2 = d0 - (i2 - 2.0 * K2);
  float3 d3 = d0 - (1.0 - 3.0 * K2);
  
  float4 h = max(0.6 - float4(dot(d0, d0), dot(d1, d1), dot(d2, d2), dot(d3, d3)), 0.0);
  float4 n = h * h * h * h * float4(dot(d0, hash33(i)), dot(d1, hash33(i + i1)), dot(d2, hash33(i + i2)), dot(d3, hash33(i + 1.0)));
  
  return dot(float4(31.316,31.316,31.316,31.316), n);
}

// Based on Morgan McGuire @morgan3d
// https://www.shadertoy.com/view/4dS3Wd
float noise (in float2 st) {
	float2 i = floor(st);
	float2 f = frac(st);

	// Four corners in 2D of a tile
	float a = rand(i);
	float b = rand(i + float2(1.0, 0.0));
	float c = rand(i + float2(0.0, 1.0));
	float d = rand(i + float2(1.0, 1.0));

	float2 u = f * f * (3.0 - 2.0 * f);

	return lerp(a, b, u.x) + (c - a)* u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
}

float fbm (in float2 uv, int octaves, float amplitude) {
	// Initial values
	float value = 0.0;
  // Loop of octaves
	for (int i = 0; i < octaves; i++) {
		value += amplitude * noise(uv);
		uv *= 2.;
		amplitude *= .5;
	}
	return value;
}

float fbmUnit(in float2 uv, int octaves, float amplitude)
{
  return fbm(uv, octaves, amplitude) * 2.0 / amplitude; 
}

void fbm_float(float2 uv, float octaves, float amplitude, float frequency, out float Out)
{
	uv *= frequency;
	Out = fbm(uv, (int)octaves, amplitude);
}


//
// END OF __NOISE__385B023BFSLHF9520HFEF348__
//
#endif