Shader "AM/Sampling/Lanczos2UpscalerCAS"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _CASLevel ("CAS Level", Range(0, 1)) = 0.5  
        _Lift ("Lift", Range(-1, 1)) = 0.0
        _GammaShadows ("Gamma Shadows", Range(0, 3)) = 1.0
        _GammaHighlights ("Gamma Highlights", Range(0, 3)) = 1.0
        _Gain ("Gain", Range(-2, 2)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZWrite Off
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "HLSL/ColorConversion.hlsl"

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
            sampler3D _LutTex;
            float4 _MainTex_TexelSize;
            float _CASLevel, _LutAmount, _Lift, _GammaShadows, _GammaHighlights, _Gain;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
				        o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Lanczos-2 image upscaler
                float4 scale = float4(
                    1.0 / float2(_MainTex_TexelSize.z, _MainTex_TexelSize.w),
                    float2(_MainTex_TexelSize.z, _MainTex_TexelSize.w) / _ScreenParams.xy
                );

                float2 src_pos = scale.zw * i.uv * _ScreenParams.xy;
                float2 src_centre = floor(src_pos - 0.5) + 0.5;
                float4 f; f.zw = 1.0 - (f.xy = src_pos - src_centre);

                float4 l2_w0_o3 = ((1.5672 * f - 2.6445) * f + 0.0837) * f + 0.9976;
                float4 l2_w1_o3 = ((-0.7389 * f + 1.3652) * f - 0.6295) * f - 0.0004;

                float4 w1_2 = l2_w0_o3;
                float2 w12 = w1_2.xy + w1_2.zw;
                float4 wedge = l2_w1_o3.xyzw * w12.yxyx;

                float2 tc12 = scale.xy * (src_centre + w1_2.zw / w12);
                float2 tc0 = scale.xy * (src_centre - 1.0);
                float2 tc3 = scale.xy * (src_centre + 2.0);

                float sum = wedge.x + wedge.y + wedge.z + wedge.w + w12.x * w12.y;
                wedge /= sum;

                float3 col = float3(
                    tex2D(_MainTex, float2(tc12.x, tc0.y)).rgb * wedge.y +
                    tex2D(_MainTex, float2(tc0.x, tc12.y)).rgb * wedge.x +
                    tex2D(_MainTex, tc12.xy).rgb * (w12.x * w12.y) +
                    tex2D(_MainTex, float2(tc3.x, tc12.y)).rgb * wedge.z +
                    tex2D(_MainTex, float2(tc12.x, tc3.y)).rgb * wedge.w
                );

                // AMD FidelityFX Contrast Adaptive Sharpening (CAS)
                float max_g = col.y;
                float min_g = col.y;
                float4 uvoff = float4(1,0,1,-1) * _MainTex_TexelSize.xyxy;

                float3 colw;
                float3 col1 = tex2D(_MainTex, i.uv + uvoff.yw).xyz;
                max_g = max(max_g, col1.y);
                min_g = min(min_g, col1.y);
                colw = col1;

                col1 = tex2D(_MainTex, i.uv + uvoff.xy).xyz;
                max_g = max(max_g, col1.y);
                min_g = min(min_g, col1.y);
                colw += col1;

                col1 = tex2D(_MainTex, i.uv + uvoff.yz).xyz;
                max_g = max(max_g, col1.y);
                min_g = min(min_g, col1.y);
                colw += col1;

                col1 = tex2D(_MainTex, i.uv - uvoff.xy).xyz;
                max_g = max(max_g, col1.y);
                min_g = min(min_g, col1.y);
                colw += col1;

                float d_min_g = min_g;
                float d_max_g = 1.0 - max_g;

                float A;
                max_g = max(0.0, max_g);
                if (d_max_g < d_min_g)
                {
                    A = d_max_g / max_g;
                }
                else
                {
                    A = d_min_g / max_g;
                }

                A = sqrt(max(0.0, A));
                A *= _CASLevel * -0.125;

                float3 col_out = (col + colw * A) / (1.0 + 4.0 * A);

                float3 lutColor = col_out;
                lutColor = float4(sampleCubeLUT(_LutTex, col_out.rgb), 1.0);
                col_out = lerp(col_out, lutColor, _LutAmount);
                
                // Levels
                // col_out = LINtoRGB(levels(RGBtoLIN(col_out), _Lift, _Gain, _GammaShadows, _GammaHighlights));
                // col_out = pow(col_out, 1.0 / _GLOBAL_Gamma);
                return fixed4(col_out, 1.0);
            }
            ENDCG
        }
    }
}