Shader "AM/Unlit/LUT3D"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _LutTex ("LUT", 3D) = "white" {} 
        _Lift ("Lift", Range(0, 2)) = 0
        _Gain ("Gain", Range(0, 2)) = 0
        _ShadowGamma ("Shadow Gamma", Range(0, 4)) = 1
        _HighlightGamma ("Highlight Gamma", Range(0, 4)) = 1
        _LUTSize ("LUT Size", Int) = 65
    }
    SubShader
    {
      Tags { "RenderType"="Transparent" "DisableBatching"="True" }
      Cull Back ZTest Always
      LOD 100
  
      Pass
      {
          CGPROGRAM
          #pragma vertex vert
          #pragma fragment frag
          
          #include "UnityCG.cginc"
          #include "HLSL/ColorConversion.hlsl"

        struct appdata {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f {
            float4 vertex : SV_POSITION;
            // float3 texcoord : TEXCOORD0;
            float2 uv : TEXCOORD0;
        };

        sampler2D _MainTex;
        float4 _MainTex_ST;

        float _LutAmount, _LUTSize, _Lift, _Gain, _ShadowGamma, _HighlightGamma;

        sampler3D _LutTex;

        v2f vert (appdata v) {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = TRANSFORM_TEX(v.uv, _MainTex);
            // o.texcoord = v.uv.xy; // Might need to flip the y-component
            return o;
        }

      float4 frag (v2f i) : SV_Target {
          float4 color = tex2D(_MainTex, i.uv);
          // color.rgb = RGBtoLIN(color.rgb);
          // color.rgb = levels(color.rgb, _Lift, _Gain, _ShadowGamma, _HighlightGamma);
          // color.rgb = LINtoRGB(color.rgb);
          float4 lutColor = color;
          lutColor = float4(sampleCubeLUT(_LutTex, color.rgb), 1.0);
          color = lerp(color, lutColor, _LutAmount);
          return color;
      }       
      ENDCG
    }
  }
}