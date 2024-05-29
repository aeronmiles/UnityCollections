Shader "AM/Sampling/HighlightCompression"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [Header(Color Grading)]
        _HighCompressionThreshold ("High Compression Threshold", Range(0, 1)) = 0.95
        _HighightCompression ("Highlight Compression", Range(0, 1)) = 0.5
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
            float4 _MainTex_TexelSize;

            float _HighCompressionThreshold;
            float _HighightCompression;

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
                float3 col = tex2D(_MainTex, i.uv);
                col = highlightCompression(col, _HighCompressionThreshold, _HighightCompression);
                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
}