Shader "AM/Image Processing/MultiplyTexturesToLab_SS"
{
    Properties
    {
        _MainTex ("Main Tex (Sreen UV Space)", 2D) = "white" {}
        _MultiplyTex ("Multiply Tex (Object UV Space)", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZTest Always
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Assets/UnityCollections/Shaders/HLSL/ColorConversion.hlsl"
            // #include "Assets/UnityCollections/Shaders/HLSL/CC.hlsl"
            #include "Assets/UnityCollections/Shaders/HLSL/Common.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uvScreen : TEXCOORD1;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 uvScreen : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            // float _GLOBAL_Gamma;

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _MultiplyTex; 
            float4 _MultiplyTex_ST;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uvScreen = uvScreen(o.vertex);

                return o;
            }
            
            half4 frag (v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uvScreen);
                col.rgb = XYZtoLab(RGBtoXYZ(col.rgb));
                clip(tex2D(_MultiplyTex, i.uv) - 0.01);
                return col;
            }        
            
            ENDCG
        }
    }
}
