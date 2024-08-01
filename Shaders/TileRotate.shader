Shader "AM/Unlit/TileRotate"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TileXY ("Tile X Y", Vector) = (1,1,0,0)
        _RotationRadians ("Rotation Radians", Float) = 0
        _AspectRatio ("Aspect Ratio", Float) = 1
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
            float4 _TileXY;
            float _RotationRadians;
            float _AspectRatio;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv - 0.5;
                float s = sin(_RotationRadians);
                float c = cos(_RotationRadians);
                float2x2 rotationMatrix = float2x2(c, -s, s, c);
                uv = mul(rotationMatrix, uv);
                
                // Adjust for aspect ratio change
                uv.y *= _AspectRatio;
                
                uv = uv * _TileXY.xy + 0.5;
                
                // Clamp UV coordinates to ensure we don't sample outside the texture
                uv = clamp(uv, 0, 1);
                
                fixed4 col = tex2D(_MainTex, uv);
                return col;
            }
            ENDCG
        }
    }
}