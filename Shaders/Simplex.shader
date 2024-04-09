Shader "AM/Procedural/Simplex"
{
    Properties
    {
        _Scale ("Scale", Range(0.00001, 500)) = 10
        _ScaleMultiplier ("Scale Multiplier", Range(0.00001, 5)) = 1
        _Power ("Power", Range(0.00001, 10)) = 1
        _Offset ("Offset", Range(-2, 2)) = 0
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
            #include "Assets/UnityCollections/Shaders/HLSL/Noise.hlsl"

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
            float _Scale, _Power, _Offset, _ScaleMultiplier;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
						
            float4 frag (v2f i) : SV_Target
            {
              float3 snoise = float3(simplex_noise(float3(i.uv, 0.5) * _Scale), 
              simplex_noise(float3(i.uv, 0.5) * _Scale * _ScaleMultiplier),
              simplex_noise(float3(i.uv, 0.5) * _Scale * _ScaleMultiplier * _ScaleMultiplier));

              snoise = offset(snoise, _Power, _Offset);
              return float4(snoise.r, 0, 0, 1);
            }        
            ENDCG
        }
    }
}
