Shader "Custom/Standard Emissive Cutout"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
         _Cutoff  ("Alpha Cutoff", Range(0,1)) = 0.1
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        // _BumpScale ("Normal Scale", Float) = 1
        // _BumpMap ("Normal Map", 2D) = "bump" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _EmissionColorTex ("Emission Color (RGB)", 2D) = "white" {}
         _ColorUVMultiplier  ("Color UV Multiplier", Range(0,0.5)) = 0.5
        _EmissionColorLerp ("Emission Color Amount", Range(0,1)) = 0.3
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        [Space(15)][Enum(Off,2,On,0)] _Cull("Double Sided", Float) = 0 //"Back"
        [Enum(Off,0,On,1)] _ZWrite("ZWrite", Float) = 1.0 //"On"
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4 //"LessEqual"
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
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
        sampler2D _BumpMap;
        sampler2D _EmissionColorTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        half _Glossiness;
        half _BumpScale;
        half _Metallic;
        half _Cutoff;
        half _ColorUVMultiplier;
        half _EmissionColorLerp;
        fixed4 _Color;
        fixed4 _EmissionColor;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            fixed4 texColor = tex2D(_EmissionColorTex, float2(IN.worldPos.x + IN.worldPos.z, IN.worldPos.y) * _ColorUVMultiplier);
            fixed4 emission = lerp(c * _Color, texColor * _Color, _EmissionColorLerp);
            clip(c.a - _Cutoff);
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            // o.Normal = UnpackScaleNormal(tex2D(_BumpMap, IN.uv_MainTex), _BumpScale);
            o.Smoothness = _Glossiness;
            o.Emission = emission * _EmissionColor;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
