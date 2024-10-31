void SmoothSquareWave_float(float x, float smoothness, float frequency, float amplitude, out float Result)
{
    float wave = sin(x * 6.28318530718 * frequency);
    wave = smoothstep(-smoothness, smoothness, wave);
    Result = wave * amplitude;
}

void SmoothSquareWaveColor_float(float2 uv, float mask, float amplitude, float thickness, float smoothness, float frequency, float4 colorWave, float4 colorWave2, out float4 Result)
{
    float x = uv.x;
    float y;
    SmoothSquareWave_float(x, smoothness, frequency, amplitude, y);

    float eps = 0.001;
    float y1, y2;
    SmoothSquareWave_float(x + eps, smoothness, frequency, amplitude, y1);
    SmoothSquareWave_float(x - eps, smoothness, frequency, amplitude, y2);
    float dy = (y1 - y2) / (2 * eps);

    float2 delta = float2(uv.x - x, uv.y - y);
    float2 dir = normalize(float2(1, dy));
    float dist = abs(dot(delta, float2(-dir.y, dir.x)));

    float edgeSmoothness = thickness * 0.5;
    float waveAlpha = 1 - smoothstep(thickness - edgeSmoothness, thickness + edgeSmoothness, dist);

    float amplitudeCheck = 1 - smoothstep(amplitude - thickness, amplitude + thickness, uv.y);
    waveAlpha *= amplitudeCheck;

    float4 waveColor = lerp(colorWave2, colorWave, mask);
    waveColor.a *= waveAlpha;

    Result = waveColor;
}

void SmoothSquareWaveGlow_float(float2 uv, float mask, float amplitude, float thickness, float smoothness, float frequency, float4 colorWave, float4 colorWave2, float transition, float glowSamples, float glowRadius, out float4 Result)
{
    float4 glowColor = float4(0, 0, 0, 0);
    for (int i = 0; i < glowSamples; i++)
    {
        float angle = 2 * 3.14159265359 * i / glowSamples;
        float2 offset = float2(cos(angle), sin(angle)) * glowRadius;
        float4 sampleColor;
        SmoothSquareWaveColor_float(uv + offset, transition, amplitude, thickness, smoothness, frequency, colorWave, colorWave2, sampleColor);
        glowColor += sampleColor;
    }
    Result = glowColor / glowSamples;
}