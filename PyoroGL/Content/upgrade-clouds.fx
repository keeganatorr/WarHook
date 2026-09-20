#if OPENGL
#define SV_POSITION POSITION
#define PS_SHADERMODEL ps_3_0
#else
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// SpriteBatch supplies the transformed vertex shader and vertex color.
float2 TextureSize;
float Time;
float CloudOpacity = 0.38;

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float Hash12(float2 p)
{
    float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float Noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = Hash12(i);
    float b = Hash12(i + float2(1.0, 0.0));
    float c = Hash12(i + float2(0.0, 1.0));
    float d = Hash12(i + float2(1.0, 1.0));
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

float Fbm(float2 p)
{
    float value = 0.0;
    float amplitude = 0.5;
    for (int i = 0; i < 6; ++i)
    {
        value += amplitude * Noise(p);
        p = p * 2.03 + float2(17.17, 9.23);
        amplitude *= 0.5;
    }
    return value;
}

float3 Nebula(float2 p)
{
    float2 axis = normalize(float2(1.0, -0.57));
    float2 normal = float2(-axis.y, axis.x);
    float2 origin = float2(-1.18, 0.72);
    float2 rel = p - origin;
    float along = dot(rel, axis);
    float across = dot(rel, normal);

    float envelope = exp(-pow(abs(across) / 0.30, 1.34));
    envelope *= smoothstep(-0.42, 0.04, along);
    envelope *= 1.0 - smoothstep(1.72, 2.55, along);

    float2 drift = float2(Time * 0.004, -Time * 0.002);
    float broad = Fbm(p * 2.15 + drift);
    float detail = Fbm(p * 6.4 - drift * 2.0 + float2(4.0, -2.0));
    float knots = smoothstep(0.38, 0.89, broad * 0.86 + detail * 0.43);
    float filamentCenter = 0.055 * sin(along * 4.7 + broad * 5.2);
    float filament = exp(-pow(abs(across - filamentCenter) / 0.045, 1.45));

    float cyan = envelope * (0.18 + 0.88 * knots) * (0.60 + 0.40 * filament);
    float blue = envelope * (0.16 + 0.64 * detail) * 0.72;

    float3 result = float3(0.015, 0.13, 0.30) * blue;
    result += float3(0.00, 0.40, 0.70) * cyan;
    result += float3(0.00, 0.72, 0.74) * cyan * cyan * 0.52;

    // A second, softer cloud sits behind the main diagonal band.
    float2 side = (p - float2(-0.78, 0.35)) * float2(0.72, 1.0);
    float sideCloud = exp(-dot(side, side) / 0.52);
    sideCloud *= smoothstep(0.30, 0.78, Fbm(p * 3.0 + float2(5.0, 1.0)));
    result += float3(0.00, 0.16, 0.31) * sideCloud;
    return result;
}

float4 CloudPS(VertexShaderOutput input) : COLOR0
{
    float2 uv = input.TextureCoordinates;
    float2 p = (uv * TextureSize - 0.5 * TextureSize) / max(TextureSize.y, 1.0);
    float3 color = Nebula(p) * CloudOpacity * input.Color.rgb;
    return float4(color, 1.0);
}

technique UpgradeClouds
{
    pass Pass1
    {
        PixelShader = compile PS_SHADERMODEL CloudPS();
    }
}
