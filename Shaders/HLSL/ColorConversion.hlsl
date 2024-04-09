#ifndef __COLOR_CONVERSIONS_NO201NDW1941W582__
#define __COLOR_CONVERSIONS_NO201NDW1941W582__
#include "Common.hlsl"

// http://www.brucelindbloom.com/
const float kE = 216.0 / 24389.0;
const float kK = 24389.0 / 27.0;
const float kKE = 8.0;

const float3 SRGB_RED_CHROMATICITY   = float3(0.64, 0.33, 0);
const float3 SRGB_GREEN_CHROMATICITY = float3(0.30, 0.60, 0);
const float3 SRGB_BLUE_CHROMATICITY  = float3(0.15, 0.06, 0);
const float3 D65_WHITE              = float3(0.95047, 1.0, 1.08883); 

///
// RGB to HSV/HSL/HCY/HCL in HLSL - https://www.chilliant.com/rgb2hsv.html
const float HCL_GAMMA = 3;
const float HCL_Y0 = 100;
const float HCL_MAX_L = 0.530454533953517; // == exp(HCL_GAMMA / HCL_Y0) - 0.5
// The weights of RGB contributions to luminance. Should sum to unity.
const float3 HCY_WTS = float3(0.299, 0.587, 0.114);
///

// Conversion Matricies - You'll need to fill this with values based on your RGB primaries
const float3x3 SRGB_TO_XYZ_MATRIX = float3x3(
    0.4124564, 0.3575761, 0.1804375,
    0.2126729, 0.7151522, 0.0721750,
    0.0193339, 0.1191920, 0.9503041
);

float3 sampleCubeLUT(sampler3D lut, float3 coord) {
  coord = saturate(coord); 
  return tex3D(lut, coord).xyz;

  // fixed4 c00 = tex3D(lut, coord);
  // fixed4 c01 = tex3D(lut, coord + fixed3(1.0 / _LUTSize, 0.0, 0.0));
  // fixed4 c10 = tex3D(lut, coord + fixed3(0.0, 1.0 / _LUTSize, 0.0));
  // fixed4 c11 = tex3D(lut, coord + fixed3(1.0 / _LUTSize, 1.0 / _LUTSize, 0.0));
  // fixed4 c02 = tex3D(lut, coord + fixed3(0.0, 0.0, 1.0 / _LUTSize));
  // fixed4 c03 = tex3D(lut, coord + fixed3(1.0 / _LUTSize, 0.0, 1.0 / _LUTSize));
  // fixed4 c12 = tex3D(lut, coord + fixed3(0.0, 1.0 / _LUTSize, 1.0 / _LUTSize));
  // fixed4 c13 = tex3D(lut, coord + fixed3(1.0 / _LUTSize, 1.0 / _LUTSize, 1.0 / _LUTSize));

  // fixed4 temp1 = lerp(c00, c01, frac(coord.x));
  // fixed4 temp2 = lerp(c10, c11, frac(coord.x));
  // fixed4 temp3 = lerp(c02, c03, frac(coord.x));
  // fixed4 temp4 = lerp(c12, c13, frac(coord.x));

  // fixed4 temp5 = lerp(temp1, temp2, frac(coord.y));
  // fixed4 temp6 = lerp(temp3, temp4, frac(coord.y));

  // fixed4 result = lerp(temp5, temp6, frac(coord.z));
  
  // return result; 
}

float3x3 RGBtoXYZMatrix(float3 redChrom, float3 greenChrom, float3 blueChrom, float3 refWhite) 
{
    // Calculate X, Y, Z for each primary
    float3 Xr = redChrom   / redChrom.y;
    float3 Xg = greenChrom / greenChrom.y;
    float3 Xb = blueChrom  / blueChrom.y;

    float3 Yr = float3(1.0, 1.0, 1.0); 

    float3 Zr = (1.0 - Xr - Yr) / Yr;
    float3 Zg = (1.0 - Xg - Yr) / Yr;
    float3 Zb = (1.0 - Xb - Yr) / Yr;

    // Calculate S for each primary
    float3 Sr = Xr * refWhite / Yr;
    float3 Sg = Xg * refWhite / Yr;
    float3 Sb = Xb * refWhite / Yr;

    // Construct the matrix
    float3x3 M;
    M[0] = Sr; 
    M[1] = Sg;
    M[2] = Sb; 

    return M;
}

// // Convert linear to companded space with gamma companding
// float3 Compand(float3 linear, float gamma = 2.2) 
// {
//     float3 companded;

//     for (int i = 0; i < 3; i++) 
//     {
//         float sign = linear[i] >= 0.0 ? 1.0 : -1.0; 
//         float absLinear = abs(linear[i]);

//         float gammaSelector = step(0.0, gamma);  

//         float gammaResult = pow(absLinear, 1.0 / gamma);
//         float srgbResult = (absLinear <= 0.0031308) ? (absLinear * 12.92) : (1.055 * pow(absLinear, 1.0 / 2.4) - 0.055);
//         float lstarResult = (absLinear <= (216.0 / 24389.0)) ? (absLinear * 24389.0 / 2700.0) : (1.16 * pow(absLinear, 1.0 / 3.0) - 0.16);

//         companded[i] = lerp(lerp(gammaResult, srgbResult, gammaSelector), lstarResult, 1.0 - gammaSelector);        
//         companded[i] *= sign;
//     }

//     return companded;
// }


float3 RGBtoXYZ(float3 rgb)
{
    return mul(rgb, RGBtoXYZMatrix(SRGB_RED_CHROMATICITY, SRGB_GREEN_CHROMATICITY, SRGB_BLUE_CHROMATICITY, D65_WHITE));
}

float3 RGBtoXYZ_GAMMA(float3 rgb, float gamma)
{
    return mul(pow(rgb, gamma), SRGB_TO_XYZ_MATRIX);
}


// // Convert Linear RGB to XYZ using L Companding: http://www.brucelindbloom.com/
// // Inspiration: L* companding is inspired by the CIE Lab color space, specifically the 'L*' component which represents perceptual lightness. It aims to encode color values in a way that's closer to how humans perceive brightness differences.
// // Non-Linearity: Like gamma companding, L* companding applies a non-linear transformation to better allocate the available bit depth in an image format. Darker tones are encoded with finer steps, as our eyes are more sensitive to differences in shadows.
// float3 RGBtoXYZ_LCOMP(float3 rgb)
// {
//         float kappa = 903.3; // Actual CIE standard (you may change if needed)
//         for (int i = 0; i < 3; i++) 
//         {
//             if (rgb[i] <= 0.08) 
//             {
//                 rgb[i] = 100 * rgb[i] / kappa;
//             } 
//             else 
//             {
//                 rgb[i] = pow((rgb[i] + 0.16) / 1.16, 3);
//             }
//         }
//     return mul(rgb, ?? SRGB_TO_XYZ_MATRIX);
// }// Precalculated constants for efficiency (assuming sRGB with D65 reference white)

float3 XYZtoLab(float3 xyz) 
{
    float3 Lab;
    // Assuming reference white is 1,1,1
    // float3 normXYZ = xyz / refWhite; // Normalize by reference white
    // float3 f = step(kE, normXYZ) * pow(normXYZ, 1.0 / 3.0) + 
    //            (1.0 - step(kE, normXYZ)) * ((kK * normXYZ + 16.0) / 116.0); 

    // float3 f = step(kE, xyz) * pow(xyz, 1.0 / 3.0) + 
    //            (1.0 - step(kE, xyz)) * ((kK * xyz + (16.0 / 255)) / (116.0 / 255)); 

    // Lab.x = (116.0 / 255) * f.y - (16.0 / 255);
    // Lab.y = (500.0 / 255) * (f.x - f.y);
    // Lab.z = (200.0 / 255) * (f.y - f.z);

    float3 f = step(kE, xyz) * pow(xyz, 1.0 / 3.0) + 
               (1.0 - step(kE, xyz)) * ((kK * xyz + 16.0 / 116.0)); 

    Lab.x = 116.0 * f.y - 16.0;
    Lab.y = 500.0 * (f.x - f.y);
    Lab.z = 200.0 * (f.y - f.z);


    return Lab;
}

float3 LabToXYZ(float3 lab) 
{
    float3 XYZ;
    float fy = (lab.x + 16.0) / 116.0;

    // Calculation of fx, optimized
    float fx = fy + 0.002 * lab.y; 
    float fx3 = fx * fx * fx;
    float xr = step(kE, fx3) * fx3 + (1.0 - step(kE, fx3)) * ((116.0 * fx - 16.0) / kK); 

    // Calculation of yr, optimized
    float yr = step(kKE, lab.x) * pow(fy, 3.0) + (1.0 - step(kKE, lab.x)) * (lab.x / kK);

    // Calculation of fz, optimized 
    float fz = fy - 0.005 * lab.z;
    float fz3 = fz * fz * fz;
    float zr = step(kE, fz3) * fz3 + (1.0 - step(kE, fz3)) * ((116.0 * fz - 16.0) / kK); 

    // Assumong reference white is 1,1,1
    // Denormalization
    // XYZ = RefWhite * float3(xr, yr, zr); 
    XYZ = float3(xr, yr, zr);

    return XYZ;
}

float3 HUEtoRGB(in float H)
{
    float R = abs(H * 6 - 3) - 1;
    float G = 2 - abs(H * 6 - 2);
    float B = 2 - abs(H * 6 - 4);
    return saturate(float3(R,G,B));
}

float3 RGBtoHCV(in float3 RGB)
{
    // Based on work by Sam Hocevar and Emil Persson
    float4 P = (RGB.g < RGB.b) ? float4(RGB.bg, -1.0, 2.0/3.0) : float4(RGB.gb, 0.0, -1.0/3.0);
    float4 Q = (RGB.r < P.x) ? float4(P.xyw, RGB.r) : float4(RGB.r, P.yzx);
    float C = Q.x - min(Q.w, Q.y);
    float H = abs((Q.w - Q.y) / (6 * C + EPSILON) + Q.z);
    return float3(H, C, Q.x);
}

float3 HSLtoRGB(in float3 hsl)
{
    float3 RGB = HUEtoRGB(hsl.x);
    float C = (1 - abs(2 * hsl.z - 1)) * hsl.y;
    return (RGB - 0.5) * C + hsl.z;
}

float3 HCYtoRGB(in float3 hcy)
{
    float3 RGB = HUEtoRGB(hcy.x);
    float Z = dot(RGB, HCY_WTS);
    if (hcy.z < Z)
    {
        hcy.y *= hcy.z / Z;
    }
    else if (Z < 1)
    {
        hcy.y *= (1 - hcy.z) / (1 - Z);
    }
    return (RGB - Z) * hcy.y + hcy.z;
}

// @TODO: better implementation
float3 HCLtoRGB(in float3 HCL)
{
    float3 RGB = 0;
    if (HCL.z != 0)
    {
        float H = HCL.x;
        float C = HCL.y;
        float L = HCL.z * HCL_MAX_L;
        float Q = exp((1 - C / (2 * L)) * (HCL_GAMMA / HCL_Y0));
        float U = (2 * L - C) / (2 * Q - 1);
        float V = C / Q;
        float A = (H + min(frac(2 * H) / 4, frac(-2 * H) / 8)) * PI * 2;
        float T;
        H *= 6;
        if (H <= 0.999)
        {
            T = tan(A);
            RGB.r = 1;
            RGB.g = T / (1 + T);
        }
        else if (H <= 1.001)
        {
            RGB.r = 1;
            RGB.g = 1;
        }
        else if (H <= 2)
        {
            T = tan(A);
            RGB.r = (1 + T) / T;
            RGB.g = 1;
        }
        else if (H <= 3)
        {
            T = tan(A);
            RGB.g = 1;
            RGB.b = 1 + T;
        }
        else if (H <= 3.999)
        {
            T = tan(A);
            RGB.g = 1 / (1 + T);
            RGB.b = 1;
        }
        else if (H <= 4.001)
        {
            RGB.g = 0;
            RGB.b = 1;
        }
        else if (H <= 5)
        {
            T = tan(A);
            RGB.r = -1 / T;
            RGB.b = 1;
        }
        else
        {
            T = tan(A);
            RGB.r = 1;
            RGB.b = -T;
        }
        RGB = RGB * V + U;
    }
    return RGB;
}

float3 RGBtoHSV(float3 c)
{
    c = pow(c, 2.2); // Approximate gamma correction

    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = EPSILON;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

float3 LINtoHSV(float3 c)
{
    // c = pow(c, 2.2); // Approximate gamma correction

    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = EPSILON;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

// @TODO: lienar implementation
float3 HSVtoRGB(float3 c)
{
    // c = pow(c, 2.2); // Approximate gamma correction
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

// Hue Rotated by +0.5
float3 RGBtoHSV_Offset(float3 c, float hueOffset = 0.5)
{
    c = pow(c, 2.2); // Approximate gamma correction

    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = EPSILON;

    // Calculate raw hue
    float rawHue = abs(q.z + (q.w - q.y) / (6.0 * d + e)); 

    // Rotate hue (subtract 0.5 and wrap)
    float rotatedHue = (rawHue - hueOffset) % 1.0; 

    // Correct negative hues (if necessary)
    if (rotatedHue < 0.0) {
        rotatedHue += 1.0;
    }

    return float3(rotatedHue, d / (q.x + e), q.x);
}

// Hue Rotated by +0.5
float3 LINtoHSV_Offset(float3 c, float hueOffset = 0.5)
{
    // c = pow(c, 2.2); // Approximate gamma correction

    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = EPSILON;

    // Calculate raw hue
    float rawHue = abs(q.z + (q.w - q.y) / (6.0 * d + e)); 

    // Rotate hue (subtract 0.5 and wrap)
    float rotatedHue = (rawHue - hueOffset) % 1.0; 

    // Correct negative hues (if necessary)
    if (rotatedHue < 0.0) {
        rotatedHue += 1.0;
    }

    return float3(rotatedHue, d / (q.x + e), q.x);
}

// @TODO: lienar implementation
// Hue Rotated by +0.5
float3 HSVtoRGB_Offset(float3 c, float hueOffset = 0.5)
{
    // c = pow(c, 2.2); // Approximate gamma correction

    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    
    // Rotate hue back (add 0.5 and wrap)
    float originalHue = (c.x + hueOffset) % 1.0; 

    float3 p = abs(frac(originalHue + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

float3 RGBtoHSL(in float3 rgb)
{
    float3 hcv = RGBtoHCV(rgb);
    float L = hcv.z - hcv.y * 0.5;
    float S = hcv.y / (1 - abs(L * 2 - 1) + EPSILON);
    return float3(hcv.x, S, L);
}

float3 RGBtoHCY(in float3 rgb)
{
  // Corrected by David Schaeffer
  float3 hcv = RGBtoHCV(rgb);
  float Y = dot(rgb, HCY_WTS);
  float Z = dot(HUEtoRGB(hcv.x), HCY_WTS);
  // Branchless Correction Factor 
  float factor = lerp((1 - Z) / (EPSILON + 1 - Y), Z / (EPSILON + Y), step(Y, Z)); 
  hcv.y *= factor; 

  return float3(hcv.x, hcv.y, Y);
}

float3 RGBtoLIN(float3 rgb)
{
    return pow(rgb, 2.2);
}

float3 LINtoRGB(float3 rgb)
{
    return pow(rgb, 0.4545454545454545);
}

float3 levels(float3 col, float lift, float gain, float shadowGamma, float highGamma)
{
  col += lift;
  float luminance = RGBtoHSV(col).z;
  col = lerp(pow(col, shadowGamma), pow(col, highGamma), luminance);
  float remappedLuminance = lerp(luminance, 1.0, gain * luminance / 1);
  col *= (remappedLuminance / luminance);
  return col;
}

float4 levels(float4 col, float lift, float gain, float shadowGamma, float highGamma)
{
  col.rgb += lift;
  float luminance = RGBtoHSV(col.rgb).z;
  col.rgb = lerp(pow(col.rgb, shadowGamma), pow(col.rgb, highGamma), luminance);
  float remappedLuminance = lerp(luminance, 1.0, gain * luminance / 1);
  col.rgb *= (remappedLuminance / luminance);
  return col;
}

//
// END OF __COLOR_CONVERSIONS_NO201NDW1941W582__
//
#endif