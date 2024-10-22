#ifndef __MASKS__DJ134FDN239DHF2__
#define __MASKS__DJ134FDN239DHF2__

float smoothstepDistance(float val1, float val2, float threshold)
{
    // Ensure hues are in the 0-1 range
    val1 = frac(val1);
    val2 = frac(val2);

    // Calculate the shortest distance between hues (accounting for wrapping)
    float dist = min(abs(val1 - val2), 1.0 - abs(val1 - val2)); 

    // Apply smooth falloff
    float falloff = smoothstep(threshold, 0.0, dist); 

    // Return clamped and inverted result for the effect you described
    return clamp(falloff, 0.0, 1.0);
}

float3 smoothstepDistance(float3 val1, float3 val2, float3 threshold)
{
    // Ensure hues are in the 0-1 range
    val1 = frac(val1);
    val2 = frac(val2);

    // Calculate the shortest distance between hues (accounting for wrapping)
    float3 dist = min(abs(val1 - val2), 1.0 - abs(val1 - val2)); 

    // Apply smooth falloff
    float3 falloff = smoothstep(threshold, float3(0.0, 0.0, 0.0), dist); 

    // Return clamped and inverted result for the effect you described
    return clamp(falloff, 0.0, 1.0);
}

void midtone_mask_float(float value, float hpower, float lpower, out float Out)
{
    float x = value * 2 - 1;
    float midtones = 0;
    if (x < 0) 
    { 
        midtones = pow(abs(x), lpower);
    }
    else
    {
        midtones = pow(abs(x), hpower);
    } 

    Out = 1 - midtones;
}

void MaskX_float(float2 uv, float width, float offset, float smooth, out float Result)
{
    float maskLeft = smoothstep(0, smooth, uv.x - (0.5 - width + offset));
    float maskRight = smoothstep(0, smooth, 1 - (uv.x - (0.5 + width + offset)));
    Result = maskLeft * maskRight;
}

void CRTPixelMask_float(float2 uv, float2 density, float contrast, out float Result)
{
    float2 pixelCoord = uv * density;
    float2 pixelCenter = floor(pixelCoord) + 0.5;
    float2 delta = (pixelCoord - pixelCenter) / 0.5;
    float distSq = dot(delta, delta);
    float pixelMask = 1 - smoothstep(0.5 - contrast, 0.5 + contrast, distSq);
    Result = lerp(1 - contrast, 1, pixelMask);
}

//
// END OF __MASKS__DJ134FDN239DHF2__
//
#endif
