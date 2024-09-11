#version 460 core

const vec2 POSITIONS[6] = vec2[6](
    vec2(0.0, 0.0),
    vec2(1.0, 0.0),
    vec2(1.0, 1.0),
    vec2(1.0, 1.0),
    vec2(0.0, 1.0),
    vec2(0.0, 0.0)
);

struct GlyphData {
    vec2 min;
    vec2 max;
    vec2 minUV;
    vec2 maxUV;
};

layout(std430, binding = 0) buffer GlyphBuffer {
    GlyphData glyphs[];
};

layout(location = 0) uniform mat4 uMvp;

out vec2 vUv;

void main() {
    GlyphData glyph = glyphs[gl_InstanceID];
    vec3 pos = vec3(mix(glyph.min, glyph.max, POSITIONS[gl_VertexID]), 0.0);
    gl_Position = uMvp * vec4(pos, 1.0);
    
    vUv = mix(glyph.minUV, glyph.maxUV, POSITIONS[gl_VertexID]);
}