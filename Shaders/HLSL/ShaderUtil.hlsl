#include "Common.hlsl"
#include "Blending.hlsl"


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

