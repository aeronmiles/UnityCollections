Shader "AM/Image Processing/Adaptive Histogram Equalization"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlockSize ("Block Size", Float) = 16
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        // Required for post-processing effects
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half4 _MainTex_TexelSize; 
            float _BlockSize; // Size of the AHE region in pixels

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // Sample the original texture color
                float4 texColor = tex2D(_MainTex, i.uv);
                return texColor;
                // // Convert to grayscale (for simplicity)
                // float gray = dot(texColor.rgb, float3(0.299, 0.587, 0.114));

                // // Calculate region boundaries
                // float2 regionStart = floor(i.uv * _MainTex_TexelSize.xy) / _MainTex_TexelSize.xy * _BlockSize; 
                // float2 regionEnd = regionStart + float2(_BlockSize, _BlockSize);

                // // Sample pixels within the region (replace with more efficient sampling if needed)
                // float regionSum = 0;
                // int regionCount = 0;
                // for (float x = regionStart.x; x < regionEnd.x; x += 1)
                // {
                //     for (float y = regionStart.y; y < regionEnd.y; y += 1)
                //     {
                //         regionSum += dot(tex2D(_MainTex, float2(x, y)).rgb, float3(0.299, 0.587, 0.114));
                //         regionCount++;
                //     }
                // }

                // // Calculate average value in the region
                // float regionAvg = regionSum / regionCount;

                // // Simplified CDF calculation
                // float cdf = clamp(gray / regionAvg, 0.0, 1.0); 

                // // Apply equalization
                // float equalizedGray = cdf;

                // return float4(equalizedGray, equalizedGray, equalizedGray, 1.0);
            }
            ENDCG
        }
    }
}
