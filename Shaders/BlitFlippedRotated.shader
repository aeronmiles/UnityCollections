Shader "AM/Unlit/TileRotate"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TileXY ("Tile XY", Vector) = (1, 1, 0, 0)
        _RotationRadians ("Rotation Radians", Float) = 0
    }
    SubShader
    {
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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float2 _TileXY;
            float _RotationRadians;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                uv = tileRotate2D(uv, _TileXY.x, _TileXY.y, _RotationRadians);
                return tex2D(_MainTex, uv);
            }
            ENDCG
        }
    }
}
