#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// SpriteBatch supplies its own transformed vertex shader.  Keep the texture
// parameter name used by MonoGame's SpriteEffect so the batcher binds the
// render target automatically for every presentation draw.
texture SpriteTexture;

sampler2D TextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = None;
    AddressU = Clamp;
    AddressV = Clamp;
};

float2 TextureSize;
float Time;
struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float3 SampleRgb(float2 uv)
{
    return tex2D(TextureSampler, saturate(uv)).rgb;
}

float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float2 uv = input.TextureCoordinates;
    float2 texel = 1.0 / max(TextureSize, float2(1.0, 1.0));
    float3 source = SampleRgb(uv);

    // A small four-tap bloom keeps bright node borders glowing without
    // blurring the pixel-art source texture.
    float3 glow = (
        SampleRgb(uv + float2(texel.x * 2.0, 0.0)) +
        SampleRgb(uv - float2(texel.x * 2.0, 0.0)) +
        SampleRgb(uv + float2(0.0, texel.y * 2.0)) +
        SampleRgb(uv - float2(0.0, texel.y * 2.0))) * 0.25;
    float3 phosphor = source + max(glow - source, 0.0) * 0.22;

    // Two subtly offset scanline frequencies avoid a harsh repeating band.
    float scanA = 0.985 + 0.015 * sin(uv.y * TextureSize.y * 3.14159265);
    float scanB = 0.993 + 0.007 * sin(uv.y * TextureSize.y * 1.57079632 + Time * 0.35);
    phosphor *= scanA * scanB;

    float2 centered = uv * 2.0 - 1.0;
    float edge = smoothstep(0.48, 1.08, dot(centered, centered));
    phosphor *= 1.0 - edge * 0.38;

    // The presentation quad is always opaque; preserving the source alpha
    // would let the transparent render-target clear show through the CRT.
    return float4(phosphor * input.Color.rgb, 1.0);
}

technique SpriteDrawing
{
    pass Pass1
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
