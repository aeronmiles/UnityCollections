Shader "AM/Unlit/Normals"
{
    Properties
    {
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

            struct appdata
            {
                float4 vertex : POSITION;
                fixed3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
            };

            float4 _UVTransform;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = v.normal; 
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(i.normal, 1.0);
            }
            ENDCG
        }
    }
}
