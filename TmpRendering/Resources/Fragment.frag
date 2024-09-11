#version 460 core

layout(location = 0) out vec4 oFragColor;

layout(location = 1) uniform sampler2D uFontAtlas;
layout(location = 2) uniform float uFontSize;
layout(location = 3) uniform float uPxRange;
layout(location = 4) uniform vec4 uBaseColor;

in vec2 vUv;
in vec2 vUvNormalized;
in vec4 vColor;
flat in int vBold;

float median(float r, float g, float b) {
    return max(min(r, g), min(max(r, g), b));
}

float screenPxRange() {
    vec2 unitRange = vec2(uPxRange) / vec2(uFontSize);
    vec2 screenTexSize = vec2(1.0) / fwidth(vUvNormalized);
    return max(dot(unitRange, screenTexSize), 1.0);
}

vec4 getColor() {
    float r = isnan(vColor.r) ? uBaseColor.r : vColor.r;
    float g = isnan(vColor.g) ? uBaseColor.g : vColor.g;
    float b = isnan(vColor.b) ? uBaseColor.b : vColor.b;
    float a = isnan(vColor.a) ? uBaseColor.a : vColor.a;
    return vec4(r, g, b, a);
}

void main() {
    vec3 msdf = texture(uFontAtlas, vUv).rgb;
    float distance = median(msdf.r, msdf.g, msdf.b);
    float pxDistance = screenPxRange() * (distance - (vBold == 0 ? 0.5 : 0.3));
    float alpha = clamp(pxDistance + 0.5, 0.0, 1.0);
    vec4 color = getColor();
    oFragColor = vec4(color.rgb, color.a * alpha);
}