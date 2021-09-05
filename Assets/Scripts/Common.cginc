float2 Random(float2 p)
{
    float3 a = frac(p.xyx * float3(123.34, 234.34, 345.65));
    a += dot(a, a + 34.45);
    return frac(float2(a.x * a.y, a.y * a.z));
}

float3 HsvToRgb(float3 c)
{
    float3 rgb = clamp(abs((c.x * 6.0 + float3(0.0, 4.0, 2.0)) % 6.0 - 3.0) - 1.0, 0.0, 1.0);
    rgb = rgb * rgb * (3.0 - 2.0 * rgb);
    float3 o = c.z * lerp(float3(1.0, 1.0, 1.0), rgb, c.y);
    return float3(o.r, o.g, o.b);
}

float3 RgbToHsv(float3 c)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

uint Hash(uint s)
{
    s ^= 2747636419u;
    s *= 2654435769u;
    s ^= s >> 16;
    s *= 2654435769u;
    s ^= s >> 16;
    s *= 2654435769u;
    return s;
}

float RandomHash(uint seed)
{
    return float(Hash(seed)) / 4294967295.0; // 2^32-1
}

float2 GetRandomPos(float x, float seed, int resolution)
{
    return Random(float2(seed * 0.001, x * 0.1 / seed)) * (resolution - 1);    
}

float2 GetRandomDir(float2 id, float seed)
{
    float2 rnd = Random(id.xx * 0.001 + sin(seed));
    return normalize(2 * rnd - 1);    
}