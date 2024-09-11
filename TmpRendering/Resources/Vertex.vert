#version 460 core

const vec2 VERTICES[6] = vec2[6](
    vec2(0.0, 0.0),
    vec2(1.0, 0.0),
    vec2(1.0, 1.0),
    vec2(1.0, 1.0),
    vec2(0.0, 1.0),
    vec2(0.0, 0.0)
);

struct GlyphData {
    vec4 minMax;
    vec4 uv;
    vec4 color; // NaN = inherit
    int boldItalic;
};

layout(std430, binding = 0) buffer GlyphBuffer {
    GlyphData glyphs[];
};

layout(location = 0) uniform mat4 uMvp;

out vec2 vUv;
out vec2 vUvNormalized;
out vec4 vColor;
flat out int vBold;

void main() {
    vec2 vertex = VERTICES[gl_VertexID];
    GlyphData glyph = glyphs[gl_InstanceID];

    vUv = mix(glyph.uv.xy, glyph.uv.zw, vertex);
    vUvNormalized = vertex;
    vColor = glyph.color;
    vBold = (glyph.boldItalic & 1) != 0 ? 1 : 0;
    
    bool italic = (glyph.boldItalic & 2) != 0;
    
    vec2 min = glyph.minMax.xy;
    vec2 max = glyph.minMax.zw;
    vec2 pos = mix(min, max, vertex);
    pos.x -= italic ? (pos.y - min.y) * 0.2 : 0.0;
    
    gl_Position = uMvp * vec4(pos, 0.0, 1.0);
}