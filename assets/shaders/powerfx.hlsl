// powerfx.hlsl
// Windows Terminal experimental pixel shader
// 用于 experimental.pixelShaderPath 配置项
//
// 效果：CRT 扫描线 + 轻微色差（Chromatic Aberration）+ 暗角
// Phase 1 实现：静态后处理，不依赖时间动画
//
// Windows Terminal shader 签名：
//   float4 main(float4 pos : SV_POSITION, float2 tex : TEXCOORD) : SV_TARGET

Texture2D    shaderTexture : register(t0);
SamplerState samplerState  : register(s0);

cbuffer ConstantBuffer : register(b0)
{
    float  Time;       // 运行时间（秒）
    float4 Resolution; // xy = 宽高（像素）
};

// ── 参数 ─────────────────────────────────────────────────────────────────────
static const float SCANLINE_STRENGTH  = 0.10;
static const float CHROMA_ABERRATION  = 0.002;
static const float VIGNETTE_STRENGTH  = 0.30;
static const float VIGNETTE_RADIUS    = 0.75;

// ── 辅助函数 ──────────────────────────────────────────────────────────────────

float scanline(float2 uv)
{
    // 每隔一行降低亮度模拟 CRT 扫描线
    float line = fmod(floor(uv.y * Resolution.y), 2.0);
    return 1.0 - SCANLINE_STRENGTH * line;
}

float vignette(float2 uv)
{
    float2 center = uv - 0.5;
    float  dist   = length(center);
    return 1.0 - smoothstep(VIGNETTE_RADIUS, 1.0, dist) * VIGNETTE_STRENGTH;
}

float4 sampleWithChroma(float2 uv)
{
    float2 offset = float2(CHROMA_ABERRATION, 0.0);
    float r = shaderTexture.Sample(samplerState, uv + offset).r;
    float g = shaderTexture.Sample(samplerState, uv).g;
    float b = shaderTexture.Sample(samplerState, uv - offset).b;
    float a = shaderTexture.Sample(samplerState, uv).a;
    return float4(r, g, b, a);
}

// ── 主函数 ────────────────────────────────────────────────────────────────────

float4 main(float4 pos : SV_POSITION, float2 uv : TEXCOORD) : SV_TARGET
{
    float4 color = sampleWithChroma(uv);

    // 应用扫描线
    color.rgb *= scanline(uv);

    // 应用暗角
    color.rgb *= vignette(uv);

    return color;
}
