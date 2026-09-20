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
    for (int i = 0; i < 5; ++i)
    {
        value += amplitude * Noise(p);
        p = p * 2.03 + float2(17.17, 9.23);
        amplitude *= 0.5;
    }
    return value;
}

float WarpFbm(float2 p)
{
    float value = 0.0;
    float amplitude = 0.5;
    for (int i = 0; i < 4; ++i)
    {
        value += amplitude * Noise(p);
        p = p * 2.01 + float2(12.7, 8.3);
        amplitude *= 0.5;
    }
    return value;
}

float3 Nebula(float2 p)
{
    // Slowly moving, differently scaled noise fields bend the cloud bands and
    // make their edges billow instead of sliding as a flat, repeating ribbon.
    // The oscillating offsets evolve the noise seed smoothly over time, so the
    // clouds gradually change shape as well as drift across the screen.
    float2 noiseSeedA = float2(sin(Time * 0.30), cos(Time * 0.23)) * 0.24;
    float2 noiseSeedB = float2(cos(Time * 0.20 + 1.7), sin(Time * 0.27 + 2.3)) * 0.24;
    float2 warpA = float2(Time * 0.045, -Time * 0.030);
    float2 warpB = float2(-Time * 0.033, Time * 0.042);
    float2 warp = float2(
        WarpFbm(p * 2.15 + warpA + noiseSeedA + float2(3.1, 1.7)),
        WarpFbm(p * 2.15 + warpB + noiseSeedB + float2(19.4, 7.2))
    ) - 0.5;
    float2 cloudP = p + warp * 0.42;

    float2 axis = normalize(float2(1.0, -0.43));
    float2 normal = float2(-axis.y, axis.x);
    float2 origin = float2(-0.63, 0.37);
    float2 rel = cloudP - origin;
    float along = dot(rel, axis);
    float across = dot(rel, normal);

    // Two broad, offset ribbons leave large gaps while reaching across almost
    // the full playfield. Their masks are shaped by the animated noise below.
    float ribbonA = exp(-pow(abs(across) / 0.45, 1.38));
    ribbonA *= smoothstep(-0.42, -0.02, along);
    ribbonA *= 1.0 - smoothstep(1.35, 2.10, along);
    float ribbonB = exp(-pow(abs(across + 0.38) / 0.34, 1.45));
    ribbonB *= smoothstep(-0.78, -0.20, along);
    ribbonB *= 1.0 - smoothstep(0.95, 1.72, along);
    float envelope = saturate(ribbonA * 0.78 + ribbonB * 0.58);

    float2 drift = float2(Time * 0.040, -Time * 0.024);
    float broad = Fbm(cloudP * 2.45 + drift + noiseSeedB + warp * 0.7);
    float detail = Fbm(cloudP * 5.1 - drift * 1.7 + noiseSeedA + float2(4.0, -2.0) + warp * 1.25);
    float cloudNoise = broad * 0.72 + detail * 0.38;
    float wisps = smoothstep(0.30, 0.72, cloudNoise);
    float knots = smoothstep(0.43, 0.82, cloudNoise);
    float distantClouds = smoothstep(0.46, 0.72, cloudNoise);
    float cloudMass = envelope * (0.22 + 0.78 * wisps);
    float filamentCenter = 0.095 * sin(along * 4.0 + broad * 7.0);
    float filament = exp(-pow(abs(across - filamentCenter) / 0.085, 1.45));

    float cyan = cloudMass * (0.16 + 0.72 * knots) * (0.70 + 0.30 * filament);
    float blue = cloudMass * (0.18 + 0.56 * detail) * 0.78;

    float3 result = float3(0.015, 0.13, 0.30) * blue;
    result += float3(0.00, 0.34, 0.64) * cyan;
    result += float3(0.00, 0.62, 0.69) * cyan * cyan * 0.44;
    // Low-contrast noise clouds continue beyond the brighter ribbons, so the
    // nebula reaches across the map while retaining plenty of open space.
    result += float3(0.00, 0.095, 0.22) * distantClouds * 0.72;

    // A few diffuse wisps outside the ribbons keep the nebula spread through
    // the background without turning it into a solid wash of color.
    float2 veil = (cloudP - float2(0.46, -0.27)) * float2(0.86, 1.32);
    float veilShape = exp(-dot(veil, veil) / 0.70);
    veilShape *= 0.18 + 0.82 * smoothstep(0.36, 0.74, detail * 0.62 + broad * 0.52);
    result += float3(0.00, 0.095, 0.23) * veilShape;
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
