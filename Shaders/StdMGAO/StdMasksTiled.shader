Shader "Custom/Standard Masks Tiled"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _TileTex ("Tile Color (RGB)", 2D) = "white" {}
        _TileBlendAmount ("Tile Blend Amount", Range(0,1)) = 1
        _TileColorGamma ("Tile Color Gamma", Range(0,10)) = 1
        _TileColorOffset ("Tile Color Offset", Range(-1,2)) = 0
        _Glossiness ("Smoothness", Range(0,1)) = 1
        _Metallic ("Metallic", Range(0,1)) = 1
        _OcclusionStrength ("Occlusion Strength", Range(0,1)) = 1
        _MetallicGlossMap ("Masks (R) AO (G) Smoothness (A)", 2D) = "white" {}
        _BumpScale ("Normal Scale", Float) = 1
        _BumpMap ("Normal Map", 2D) = "bump" {}
        [Space(15)][Enum(Off,2,On,0)] _Cull("Double Sided", Float) = 0 //"Back"
        [Enum(Off,0,On,1)] _ZWrite("ZWrite", Float) = 1.0 //"On"
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4 //"LessEqual"
    }
    
    SubShader
    {
        Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
        LOD 200
        Cull[_Cull]
        ZWrite[_ZWrite]
        ZTest [_ZTest]

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _TileTex;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_TileTex;
        };

        // half _Tile;
        half _TileColorOffset;
        half _TileBlendAmount;
        half _TileColorGamma;
        half _Glossiness;
        fixed4 _Color;
        half _Cutoff;
        half _Metallic;
        half _OcclusionStrength;
        half _BumpScale;
        sampler2D _MetallicGlossMap;
        sampler2D _BumpMap;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
        // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            fixed4 overlay = pow(tex2D(_TileTex, IN.uv_TileTex), _TileColorGamma) + _TileColorOffset;
            // https://en.wikipedia.org/wiki/Blend_modes#Overlay
            // overlay = saturate(lerp(1 - 2 * (1 - c) * (1 - overlay),    2 * c * overlay, step(c, 0.5 )));
            o.Albedo = saturate(lerp(c.rgb, c.rgb * overlay.rgb, _TileBlendAmount));
            // Metallic and smoothness come from slider variables
            fixed4 m = tex2D(_MetallicGlossMap, IN.uv_MainTex);
            o.Metallic = m.r * _Metallic;
            o.Occlusion = lerp(1, m.g * m.g, _OcclusionStrength);
            o.Smoothness = m.a * _Glossiness;
            o.Normal = UnpackScaleNormal(tex2D(_BumpMap, IN.uv_MainTex), _BumpScale);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
