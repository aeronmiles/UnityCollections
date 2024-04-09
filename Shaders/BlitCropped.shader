Shader "Hidden/BlitCropped"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _CropRect ("Crop Rect", Vector) = (0,0,1,1) // x, y, width, height in normalized coordinates
    }
    SubShader
    {
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
            float4 _CropRect;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate UVs based on crop rect
                float2 croppedUV = float2(_CropRect.x + i.uv.x * _CropRect.z, _CropRect.y + i.uv.y * _CropRect.w);
                return tex2D(_MainTex, croppedUV);
            }
            ENDCG
        }
    }
}
