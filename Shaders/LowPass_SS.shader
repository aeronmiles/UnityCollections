Shader "AM/Unlit/LowPass_SS"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _KernelSize ("KernelSize", Int) = 5 // should be odd
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
            #include "Assets/UnityCollections/Shaders/HLSL/Common.hlsl"
            #include "Assets/UnityCollections/Shaders/HLSL/Filters.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float2 _MainTex_TexelSize;            
            int _KernelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = uvScreen(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // make kernel odd
                int kernel = _KernelSize & 1;
                return lowPass(_MainTex, i.uv, _MainTex_TexelSize, kernel);
            }
            ENDCG
        }
    }
}
