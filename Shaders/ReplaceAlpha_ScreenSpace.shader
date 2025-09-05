Shader "AM/Unlit/ReplaceAlpha_SS"
{
  Properties
  {
    _MainTex ("Footage", 2D) = "white" {}
    _AlphaTex ("Alpha Texture (R)", 2D) = "white" {}
    _Alpha ("Alpha", Range(0, 1)) = 1
    [Toggle(REPLACE_ALPHA_TEX_R)] _UseTexRedAsAlpha ("Alpha From Alpha Texture (R)", Float) = 0
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
      #pragma multi_compile __ REPLACE_ALPHA_TEX_R

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
      sampler2D _AlphaTex;
      
      float _Alpha;

      v2f vert(appdata v)
      {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = uvScreen(o.vertex);
        o.uv.y = 1.0 - o.uv.y; // Flip Y for screen space
        return o;
      }
      
      float4 frag (v2f i) : SV_Target
      {
        float4 col = tex2D(_MainTex, i.uv);
        float alpha = _Alpha;

        #ifdef REPLACE_ALPHA_TEX_R
          alpha *=tex2D(_AlphaTex, i.uv).r;
        #endif

        return float4(col.rgb, alpha);
      }
      ENDCG
    }
  }
}
