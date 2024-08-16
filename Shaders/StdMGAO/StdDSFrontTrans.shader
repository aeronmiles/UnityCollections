Shader "Standard (M G) (AO) Double Sided Front Trans" {
  Properties {
      _Color("Color", Color) = (1,1,1,1)
      _MainTex("Albedo", 2D) = "white" {}
      [HideInInspector] _BackfaceAlpha("Backface Alpha", Range(0.0, 1.0)) = 0.0
      [KeywordEnum(ALBEDO_ALPHA, METALLIC_ALPHA)] _GlossMode ("Gloss Mode", Float) = 0
      _Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
      _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
      _MetallicGlossMap ("Metallic (R) Smoothness (G)", 2D) = "white" {}
      _BumpScale("Scale", Float) = 1.0
      _BumpMap("Normal Map", 2D) = "bump" {}
      _OcclusionAlbedoContribution("Occlusion Albedo Contribution", Range(0.0, 1.0)) = 0.0
      _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0
      _OcclusionMap("Occlusion", 2D) = "white" {}
      _EmissionStrength("Emission Strength", Float) = 1.0
      _EmissionMap("Emission", 2D) = "black" {}
        [KeywordEnum(OFF, ON)] _Outline("Outline Enabled", Int) = 0
      _OutlineMin("Outline Min", Range(0.0, 1.0)) = 1.0
      _OutlineOffset("Outline Power", Range(0.0, 6.0)) = 1.0
      _OutlinePower("Outline Power", Range(0.0, 6.0)) = 1.0
      _OutlineColor("Outline Color", Color) = (0,0,0,1)
      [Enum(Off,0,On,1)] _ZWrite("ZWrite", Float) = 1.0
      [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4
  }

  SubShader {
      Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
      LOD 200

      ZWrite [_ZWrite]
      ZTest [_ZTest]

      Cull Front
      CGPROGRAM
      #include "StdMGAO.hlsl"
      #pragma multi_compile _OUTLINE_ON _OUTLINE_OFF
      #pragma multi_compile _GLOSSMODE_ALBEDO_ALPHA _GLOSSMODE_METALLIC_ALPHA

      #pragma surface surf Standard fullforwardshadows
      #pragma vertex vert
      #pragma target 3.0

      // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
      // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
      // #pragma instancing_options assumeuniformscaling
      UNITY_INSTANCING_BUFFER_START(Props)
      // put more per-instance properties here
      UNITY_INSTANCING_BUFFER_END(Props)

      void vert(inout appdata_full v, out Input o)
      {
          UNITY_INITIALIZE_OUTPUT(Input, o);
          v.normal = -v.normal; //flip
      }

      void surf(Input IN, inout SurfaceOutputStandard o)
      {
          surface(IN, _BackfaceAlpha, 1, o);
      }
      ENDCG
      
      Cull Back
      CGPROGRAM
      #include "StdMGAO.hlsl"
      #pragma multi_compile _OUTLINE_ON _OUTLINE_OFF
      #pragma multi_compile _GLOSSMODE_ALBEDO_ALPHA _GLOSSMODE_METALLIC_ALPHA

      #pragma surface surf Standard fullforwardshadows alpha
      #pragma target 3.0

      // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
      // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
      // #pragma instancing_options assumeuniformscaling
      UNITY_INSTANCING_BUFFER_START(Props)
      // put more per-instance properties here
      UNITY_INSTANCING_BUFFER_END(Props)

      void surf(Input IN, inout SurfaceOutputStandard o)
      {
          surface(IN, _Color.a, 1, o);
      }
      ENDCG
  }
  FallBack "VertexLit"
}