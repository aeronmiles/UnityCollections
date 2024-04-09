
void parallax_occlusion_float(float3 rgb, float Amplitude, float Steps float2 UVs, out Out)
{
    float3 ParallaxOcclusionMapping_ViewDir = IN.TangentSpaceViewDirection * GetDisplacementObjectScale().xzy;
    float ParallaxOcclusionMapping_NdotV = ParallaxOcclusionMapping_ViewDir.z;
    float ParallaxOcclusionMapping_MaxHeight = Amplitude * 0.01;

    // Transform the view vector into the UV space.
    float3 ParallaxOcclusionMapping_ViewDirUV    = normalize(float3(ParallaxOcclusionMapping_ViewDir.xy * ParallaxOcclusionMapping_MaxHeight, ParallaxOcclusionMapping_ViewDir.z)); // TODO: skip normalize

    PerPixelHeightDisplacementParam ParallaxOcclusionMapping_POM;
    ParallaxOcclusionMapping_POM.uv = UVs.xy;

    float ParallaxOcclusionMapping_OutHeight;
    float2 _ParallaxOcclusionMapping_ParallaxUVs = UVs.xy + ParallaxOcclusionMapping(Lod, Lod_Threshold, Steps, ParallaxOcclusionMapping_ViewDirUV, ParallaxOcclusionMapping_POM, ParallaxOcclusionMapping_OutHeight);

    float _ParallaxOcclusionMapping_PixelDepthOffset = (ParallaxOcclusionMapping_MaxHeight - ParallaxOcclusionMapping_OutHeight * ParallaxOcclusionMapping_MaxHeight) / max(ParallaxOcclusionMapping_NdotV, 0.0001);
}