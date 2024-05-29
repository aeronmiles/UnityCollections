Shader "AM/Unlit/ReplaceAlpha_SS"
{
    Properties
    {
        _MainTex ("Footage", 2D) = "white" {}
        _Alpha ("Alpha", Range(0, 1)) = 1
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "DisableBatching"="True" }
				ZTest Always
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Assets/UnityCollections/Shaders/HLSL/Common.hlsl"

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
            float _Alpha;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = uvScreen(o.vertex);
                return o;
            }
						
            float4 frag (v2f i) : SV_Target
            {
                return float4(tex2D(_MainTex, i.uv).rgb, _Alpha);
            }        
            ENDCG
        }
    }
}
