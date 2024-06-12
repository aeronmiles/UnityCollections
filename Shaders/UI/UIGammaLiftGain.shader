Shader "AM/Unlit/UILevels"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Lift ("Lift", Range(-1.0, 1.0)) = 0.0
        _Gain ("Gain", Range(0.0, 3.0)) = 0.0
        _Gamma ("Gamma", Range(0.1, 3.0)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Assets/UnityCollections/Shaders/HLSL/ColorConversion.hlsl"

            struct appdata_t
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
            float _Gain, _Lift, _Gamma;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                col.rgb = levels(col.rgb, _Lift, _Gain, _Gamma);
                return col;
            }
            ENDCG
        }
    }
    FallBack "Unlit/Transparent"
}
