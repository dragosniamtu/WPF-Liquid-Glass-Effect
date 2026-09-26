#version 300 es
// Tutorial-inspired external GLSL sample, adapted for XamlTemplates.FX.Wpf.
// Original tutorial: AmirHossein Aghajari, Liquid Glass: iOS Effect Explanation.
// https://medium.com/@aghajari/liquid-glass-ios-effect-explanation-dabadd6414ae
// Adapted 2026-09-13: full-window lens, host-provided capture and @fx defaults.
// This is NOT the package's built-in Liquid Glass implementation.
// See ../../THIRD-PARTY-NOTICES.md for attribution and rights limitations.
precision highp float;
precision highp sampler2D;

in vec2 uv;
out vec4 out_color;

uniform vec2 u_resolution;
uniform sampler2D u_textures[16];

uniform float cornerRadius;         // @fx default=16
uniform float refractionStrength;   // @fx default=1
uniform float lensDisplacement;     // @fx default=60
uniform float refractionWidth;      // @fx default=0.30
uniform float blurRadius;           // @fx default=0.2
uniform float chromaticAberration;  // @fx default=1
uniform float brightness;           // @fx default=0.90
uniform float edgeHighlight;        // @fx default=0.15
uniform float saturation;           // @fx default=1
uniform vec4 tintColor;             // @fx type=Color default=#FFFFFFFF
uniform float tintStrength;         // @fx default=0

const float originalChromaticShiftPx = 3.0;

float roundedBoxDistance(vec2 samplePoint, vec2 halfSize, float radius)
{
    vec2 q = abs(samplePoint) - max(halfSize - vec2(radius), vec2(0.0));
    return length(max(q, vec2(0.0))) + min(max(q.x, q.y), 0.0) - radius;
}

vec2 safeDirection(vec2 value)
{
    float magnitudeSquared = dot(value, value);
    return magnitudeSquared > 0.000001
        ? value * inversesqrt(magnitudeSquared)
        : vec2(0.0);
}

vec2 toSourceUv(vec2 outputPixel, vec2 outputSize)
{
    return clamp(outputPixel / max(outputSize, vec2(1.0)), vec2(0.0), vec2(1.0));
}

float gaussianWeight(int x, int y)
{
    return exp(-0.25 * float(x * x + y * y));
}

float sampleBlurredChannel(vec2 centerPixel, float sampleRadius, vec2 outputSize, int channel)
{
    float weightedColor = 0.0;
    float totalWeight = 0.0;

    for (int x = -2; x <= 2; x++)
    {
        for (int y = -2; y <= 2; y++)
        {
            float weight = gaussianWeight(x, y);
            vec2 samplePixel = centerPixel + vec2(float(x), float(y)) * sampleRadius;
            vec4 sampleColor = texture(u_textures[0], toSourceUv(samplePixel, outputSize));
            weightedColor += sampleColor[channel] * weight;
            totalWeight += weight;
        }
    }

    return weightedColor / max(totalWeight, 0.000001);
}

void main()
{
    vec2 outputSize = max(u_resolution, vec2(1.0));
    vec2 halfSize = outputSize * 0.5;
    float radius = clamp(cornerRadius, 0.0, min(halfSize.x, halfSize.y));
    vec2 fragmentPixel = uv * outputSize;
    vec2 glassCoordinate = fragmentPixel - halfSize;
    float distancePx = roundedBoxDistance(glassCoordinate, halfSize, radius);
    float coverage = 1.0 - smoothstep(-1.0, 1.0, distancePx);

    if (coverage <= 0.0)
    {
        out_color = vec4(0.0);
        return;
    }

    float minimumGlassSize = max(min(outputSize.x, outputSize.y), 1.0);
    float inverseSdf = -distancePx / minimumGlassSize;
    float distanceFromCenter = 1.0 - clamp(
        inverseSdf / max(refractionWidth, 0.0001), 0.0, 1.0);
    float curvatureTerm = max(
        1.0 - distanceFromCenter * distanceFromCenter, 0.0);
    float distortion = 1.0 - sqrt(curvatureTerm);
    vec2 glassDirection = safeDirection(glassCoordinate);

    vec2 lensOffset = glassDirection
        * distortion
        * max(lensDisplacement, 0.0)
        * max(refractionStrength, 0.0);
    vec2 refractedPixel = fragmentPixel - lensOffset;

    float chromaticProfile = smoothstep(0.0, 0.02, inverseSdf);
    vec2 chromaticShift = glassDirection
        * chromaticProfile
        * originalChromaticShiftPx
        * max(chromaticAberration, 0.0);
    float dynamicBlurRadius = max(blurRadius, 0.0)
        * (1.0 - distanceFromCenter * 0.5);

    vec3 glassColor;
    glassColor.r = sampleBlurredChannel(
        refractedPixel - chromaticShift, dynamicBlurRadius, outputSize, 0);
    glassColor.g = sampleBlurredChannel(
        refractedPixel, dynamicBlurRadius, outputSize, 1);
    glassColor.b = sampleBlurredChannel(
        refractedPixel + chromaticShift, dynamicBlurRadius, outputSize, 2);

    vec3 color = glassColor * brightness;
    float luminance = dot(color, vec3(0.2126, 0.7152, 0.0722));
    color = mix(vec3(luminance), color, saturation);
    color = mix(color, tintColor.rgb, clamp(tintStrength, 0.0, 1.0));

    float edgeBand = 1.0 - smoothstep(0.0, 4.0, max(-distancePx, 0.0));
    color += edgeBand * clamp(edgeHighlight, 0.0, 1.0) * 0.18;

    out_color = vec4(color, coverage);
    out_color.rgb *= out_color.a;
    out_color = clamp(out_color, 0.0, 1.0);
}
