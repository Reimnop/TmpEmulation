using System.Runtime.InteropServices;
using OpenTK.Mathematics;

namespace TmpRendering;

[StructLayout(LayoutKind.Sequential)]
public record struct RenderGlyph(Vector2 Min, Vector2 Max, Vector2 MinUV, Vector2 MaxUV);