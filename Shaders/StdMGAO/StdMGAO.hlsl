#ifndef __COMMON__DO3NK3ALS14ID5FU2SAD555G__
#define __COMMON__DO3NK3ALS14ID5FU2SAD555G__
// Standard Shader with Metallic Gloss (M G) Occlusion (AO)
fixed4 _Color;
sampler2D _MainTex;
half _Glossiness;
half _Metallic;
half _BackfaceAlpha;
sampler2D _MetallicGlossMap; 
half _BumpScale;
sampler2D _BumpMap;
half _OcclusionAlbedoContribution;
half _OcclusionStrength;
sampler2D _OcclusionMap;
float _EmissionStrength;
sampler2D _EmissionMap;

half _OutlineMin;
half _OutlineOffset;
half _OutlinePower;
fixed4 _OutlineColor;
half _OutlineAnimationSpeed;

struct Input
{
    float2 uv_MainTex;
    float2 uv2_MainTex;
    //float2 uv_BumpMap;
    float3 viewDir;
    float3 worldNormal;
    //float3 worldRefl;
    INTERNAL_DATA
};

inline float fresnel(half3 Normal, half3 ViewDir, half offset, half Power)
{
    return pow(saturate(dot(normalize(Normal), normalize(ViewDir))), Power);
}

inline void surface(Input IN, half alpha, half normal, inout SurfaceOutputStandard o)
{
    o.Normal = lerp(half3(0.5,0.5,1.0), UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex)) * _BumpScale, normal);

    o.Emission = tex2D(_EmissionMap, IN.uv_MainTex).rgb * _EmissionStrength;

    // Outline effect
    #if defined(_OUTLINE_ON)
        half outlineIntensity = fresnel(IN.worldNormal, IN.viewDir, _OutlineOffset, _OutlinePower);
        

        #if defined(_ANIMATEOUTLINE_ON)
            half animationFactor = abs(frac(_Time.y * _OutlineAnimationSpeed) * 2 - 1);
            outlineIntensity *= animationFactor;
        #endif

        o.Emission = lerp(o.Emission, o.Emission + (_OutlineColor.rgb * _OutlineColor.a), outlineIntensity);
    #endif

    o.Metallic = tex2D(_MetallicGlossMap, IN.uv_MainTex).r * _Metallic;
    o.Occlusion = lerp(1.0, tex2D(_OcclusionMap, IN.uv2_MainTex).r, _OcclusionStrength);

    half4 c = tex2D(_MainTex, IN.uv_MainTex);
    o.Albedo.rgb = c.rgb * _Color.rgb * lerp(1.0, o.Occlusion, _OcclusionAlbedoContribution);

    #if defined(_GLOSSMODE_METALLIC_ALPHA)
        o.Smoothness = tex2D(_MetallicGlossMap, IN.uv_MainTex).a * _Glossiness;
        #else
        o.Smoothness = tex2D(_MainTex, IN.uv_MainTex).a * _Glossiness;
    #endif
    o.Alpha = alpha;
}
//
// END OF __COMMON__DO3NK3ALS14ID5FU2SAD555G__
//
#endif