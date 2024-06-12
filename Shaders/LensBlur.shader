Shader "AM/Unlit/LensBlurShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Gamma ("Gamma", Float) = 1.8
        _BlurSize ("Blur Size", Float) = 1.0
        _ConvSide ("Convolution Side", Range(1, 20)) = 10
        _BlurRadius ("Blur Radius", Range(1, 20)) = 10
        _BrightSpotThreshold ("Bright Spot Threshold", Range(0, 10)) = 0.9
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Gamma;
            float _BlurSize;
            int _ConvSide;
            float _BlurRadius;
            float _BrightSpotThreshold;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float3 BriSp(float3 p)
            {
                if (p.r + p.g + p.b > _BrightSpotThreshold * 3.0)
                {
                    p = (1.0 / (1.0 - p) - 1.0) * (1.0 - _BrightSpotThreshold);
                }
                p = clamp(p, 0.0, 500.0);
                return p;
            }

            float3 color(float2 uv)
            {
                float3 p = tex2D(_MainTex, uv).rgb;
                return p;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 texelSize = _BlurSize / _ScreenParams.xy;
                
                float3 colorSum = float3(0.0, 0.0, 0.0);
                float count = 0.0;

                for (int x = -_ConvSide; x <= _ConvSide; x++)
                {
                    for (int y = -_ConvSide; y <= _ConvSide; y++)
                    {
                        float2 offset = float2(x, y) * texelSize;
                        // if (length(offset) <= _BlurRadius)
                        // {
                            float3 sampleColor = tex2D(_MainTex, uv + offset).rgb;
                            colorSum += pow(sampleColor, float3(_Gamma, _Gamma, _Gamma));
                            count += 1.0;
                        // }
                    }
                }

                colorSum /= count;
                float3 finalColor = pow(colorSum, float3(1.0 / _Gamma, 1.0 / _Gamma, 1.0 / _Gamma));
                return fixed4(finalColor, 1.0);
            }
            ENDCG
        }
    }
}