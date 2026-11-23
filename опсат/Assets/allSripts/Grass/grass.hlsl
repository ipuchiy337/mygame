// Vertex shader (можно вставить как Custom Function Node в Shader Graph)
void GrassVertex_float(
    float3 positionOS, float3 positionWS, float vertexHeight, // 0 у корня, 1 у кончика
    float time,
    float windStrength, float windSpeed, float2 windDir, float windScale,
    float bendStrength,
    out float3 outPositionOS)
{
    // --- ВЕТЕР ---
    float2 windUV = positionWS.xz * windScale + windDir * time * windSpeed;
    float wind = (noise2D(windUV) * 2 - 1) * windStrength;
    float3 windOffset = float3(windDir.x, 0, windDir.y) * wind * vertexHeight;

    // --- ПРОГИБ ОТ ОБЪЕКТОВ ---
    float2 uv = (positionWS.xz - _TrampleAreaCenter.xz) / _TrampleAreaSize + 0.5;
    float trample = SAMPLE_TEXTURE2D_LOD(_TrampleMap, sampler_TrampleMap, uv, 0).r;

    float2 bendDir = normalize(positionWS.xz - _TrampleAreaCenter.xz + 0.001);
    float3 bendOffset = float3(bendDir.x, -0.6, bendDir.y) * trample * bendStrength * vertexHeight;

    outPositionOS = positionOS + windOffset + bendOffset;
}