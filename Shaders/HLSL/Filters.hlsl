
#ifndef __FILTERS__903NF19NC1035NF9F1__
#define __FILTERS__903NF19NC1035NF9F1__
#include "Common.hlsl"

float4 lowPass(sampler2D tex, float2 uv, float2 texelSize, float kernelSize) : SV_Target
{
    // Calculate half kernel size, ensuring integer
    int halfKernel = (kernelSize - 1) / 2;  

    float4 color = float4(0.0, 0.0, 0.0, 0.0);
    float totalWeight = 0.0; // For normalization

    // Convolution loop 
    for (int y = -halfKernel; y <= halfKernel; y++)
    {
        for (int x = -halfKernel; x <= halfKernel; x++)
        {
            float2 offsetUV = uv + float2(x, y) * texelSize;
            color += tex2D(tex, offsetUV);
            totalWeight += 1.0; 
        }
    }

    return color / (totalWeight + EPSILON); 
}

// based on: http://www.ozone3d.net/smf/index.php?topic=68.0
half3 crok_highpass(sampler2D _source, float2 uv, half contrast, half widthHeight)
{
	half step_wh = 1.0 / widthHeight;

	float2 offset[9];
	half kernel[ 9 ];

	offset[ 0 ] = float2(-step_wh, -step_wh);
	offset[ 1 ] = float2(0.0, -step_wh);
	offset[ 2 ] = float2(step_wh, -step_wh);
	offset[ 3 ] = float2(-step_wh, 0.0);
	offset[ 4 ] = float2(0.0, 0.0);
	offset[ 5 ] = float2(step_wh, 0.0);
	offset[ 6 ] = float2(-step_wh, step_wh);
	offset[ 7 ] = float2(0.0, step_wh);
	offset[ 8 ] = float2(step_wh, step_wh);
	kernel[ 0 ] = -1.;
	kernel[ 1 ] = -1.;
	kernel[ 2 ] = -1.;
	kernel[ 3 ] = -1.;
	kernel[ 4 ] = 8.;
	kernel[ 5 ] = -1.;
	kernel[ 6 ] = -1.;
	kernel[ 7 ] = -1.;
	kernel[ 8 ] = -1.;

	half3 col = half3(0, 0, 0);
	for(int i=0; i<9; i++ )
	{
		half4 tmp = tex2D(_source, uv + offset[i]);
		col += tmp.rgb * kernel[i];
	}
	return col * contrast + 0.5;
}

float dilate(sampler2D tex, float2 uv, float pixelSize)
{
    float col;
    for (float x = -pixelSize; x <= pixelSize; x += pixelSize)
    {
        for (float y = -pixelSize; y <= pixelSize; y += pixelSize)
        {
            float4 sample = tex2D(tex, uv + float2(x, y));
            col = max(col, sample); // Keep the maximum value
        }
    }
    return col;
}

float sobel(sampler2D tex, float2 uv)
{
    float2 size = float2(0.15, 0.15) / _ScreenParams.xy;
    
    // Sobel operator kernel weights for edge detection
    float gx[9] = {-1, 0, 1, -2, 0, 2, -1, 0, 1};
    float gy[9] = {-1, -2, -1, 0, 0, 0, 1, 2, 1};

    float edgeDetect = 0.0;
    int index = 0;
    for(int y = -1; y <= 1; y++)
    {
        for(int x = -1; x <= 1; x++)
        {
            float4 texColor = tex2D(tex, uv + size * float2(x, y));
            float brightness = dot(texColor.rgb, float3(0.3, 0.59, 0.11)); // Convert to grayscale
            
            edgeDetect += gx[index] * brightness;
            edgeDetect += gy[index] * brightness;
            index++;
        }
    }

    // Normalize and enhance edge visibility
    edgeDetect = abs(edgeDetect) * 5;

    return edgeDetect;
}

// float4 dilateDiff(sampler2D tex, float2 uv, int numSteps, float dilationRadius) : SV_Target
// {
//     float4 maxColor = tex2D(tex, uv); // Initialize with the original color

//     float stepSize = dilationRadius / numSteps;

//     for (int step = 0; step < numSteps; ++step)
//     {
//         // Calculate random offsets within the dilation radius
//         float2 randomOffset = float2(rand(uv + step) - 0.5, rand(uv.yx + step * 2.0) - 0.5) * stepSize;

//         float4 sampleColor = tex2D(tex, uv + randomOffset);
//         maxColor = max(maxColor, sampleColor);
//     }

//     return maxColor;
// }

// float highPassMask(sampler2D tex, float3 col, float2 uv)
// {   
//     // Step 1: Dilation
//     float diff = abs(tex2D(tex, uv) - tex2D(_LowPassTex, uv));

//     // pixel value uv offset
//     float2 offset = _Dilation * float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y); 
//     float angleStep = 3.14159 * 2.0 / 3;  // Divide circle into steps
//     for (int step = 0; step < 3; step++)    
//     {
//         // float angle = step * angleStep;
//         // float2 offset = _Dilation * float2(cos(angle), sin(angle)); 
        
//         float diff1 = col - lowPass(tex, uv + float2(0, offset.y), _MainTex_TexelSize, _LowPassKernelSize);
//         float diff2 = col - lowPass(tex, uv + float2(0, -offset.y), _MainTex_TexelSize, _LowPassKernelSize);
//         float diff3 = col - lowPass(tex, uv + float2(offset.x, 0), _MainTex_TexelSize, _LowPassKernelSize);
//         float diff4 = col - lowPass(tex, uv + float2(-offset.x, 0), _MainTex_TexelSize, _LowPassKernelSize);
//         float diff5 = col - lowPass(tex, uv + float2(offset.x, offset.y), _MainTex_TexelSize, _LowPassKernelSize);
//         float diff6 = col - lowPass(tex, uv + float2(-offset.x, -offset.y), _MainTex_TexelSize, _LowPassKernelSize);
//         float diff7 = col - lowPass(tex, uv + float2(offset.x, -offset.y), _MainTex_TexelSize, _LowPassKernelSize);
//         float diff8 = col - lowPass(tex, uv + float2(-offset.x, offset.y), _MainTex_TexelSize, _LowPassKernelSize);
//         diff = max(diff, diff1);
//         diff = max(diff, diff2);
//         diff = max(diff, diff3);
//         diff = max(diff, diff4);
//         diff = max(diff, diff5);
//         diff = max(diff, diff6);
//         diff = max(diff, diff7);
//         diff = max(diff, diff8);
//         offset *= 2;
//     }

    // float4 dilatedCol = col - lowPass(tex, uv, _MainTex_TexelSize, _LowPassKernelSize); 
    // for (float x = -_Dilation; x <= _Dilation; x += _Dilation)
    // {
    //     for (float y = -_Dilation; y <= _Dilation; y += _Dilation)
    //     {
    //         // dilatedCol = max(dilatedCol, col - lowPass(_MainTex, clamp(float2(0,0), float2(1,1), uv + float2(x, y)), _LowPassKernelSize));
    //     }
    // }

    // for (int x = -1; x <= 1; x++)
    // {
    //     for (int y = -1; y <= 1; y++)
    //     {
    //         float2 uvOffset = uv + (float2(x, y) * _MainTex_TexelSize.xy * _Dilation);
    //         float4 sample = abs(tex2D(_MainTex, uvOffset) - tex2D(_LowPassTex, uvOffset));

    //         // Replace maxSample if we find a brighter color
    //         diff = max(diff, sample);
    //     }
    // }

    // Step 2: Difference for highpass
    // float4 diff = col - dilatedCol; 
//     return diff;
    
//     return -1;
// }


// END OF __FILTERS__903NF19NC1035NF9F1__
#endif