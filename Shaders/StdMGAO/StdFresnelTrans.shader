Shader "Custom/Standard Fresnel Trans"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Fresnel ("Fresnel", Range(0,4)) = 0.0
        _FresnelMin ("Fresnel Min", Range(0,1)) = 0.0
        //[KeywordEnum(OFF, ON)] _Normal("Normal Enabled", Int) = 0
        _BumpScale("Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}

        //[Space(15)][Enum(Off,2,On,0)] _Cull("Cull", Float) = 0 //"Back"
        [Enum(Off,0,On,1)] _ZWrite("ZWrite", Float) = 1.0 //"On"
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4 //"LessEqual"
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" "ForceNoShadowCasting"="True" }
        LOD 200
        
        ZWrite [_ZWrite]
        ZTest [_ZTest]
        Cull Back

        CGPROGRAM   
        //#pragma multi_compile _NORMAL_ON _NORMAL_OFF

        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows alpha:fade

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
            float3 worldNormal;
            INTERNAL_DATA
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        half _Fresnel;
        half _FresnelMin;
        half _BumpScale;
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
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            //#if defined(_NORMAL_ON)
            //o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex)) * _BumpScale;
            //#endif
            //o.Alpha = c.a;
            //o.Alpha = c.a * lerp(1.0, 0.0, pow(1.0 - saturate(dot(IN.viewDir, o.Normal)), _Fresnel));
            //get the dot product between the normal and the view direction
			float fresnel = dot(IN.worldNormal, IN.viewDir);
			//invert the fresnel so the big values are on the outside
			fresnel = saturate(1 - fresnel);
			//raise the fresnel value to the exponents power to be able to adjust it
			o.Alpha = lerp(_FresnelMin, 1.0, saturate(pow(fresnel, _Fresnel))) * _Color.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
