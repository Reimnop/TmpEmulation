#version 460 core

layout(location = 0) out vec4 oFragColor;

layout(location = 1) uniform sampler2D uFontAtlas;

in vec2 vUv;

float median(float r, float g, float b) {
    return max(min(r, g), min(max(r, g), b));
}

void main() {
    vec3 msdf = texture(uFontAtlas, vUv).rgb;
    float distance = median(msdf.r, msdf.g, msdf.b);
    float alpha = smoothstep(0.5, 0.7, distance);
    
    oFragColor = vec4(1.0, 1.0, 1.0, alpha);
}