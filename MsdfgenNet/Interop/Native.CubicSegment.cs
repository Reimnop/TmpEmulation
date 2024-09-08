using OpenTK.Mathematics;
using System.Runtime.InteropServices;
using MsdfgenNet.Enum;

namespace MsdfgenNet.Interop;

internal static partial class Native
{
    [LibraryImport(LibraryName)]
    internal static partial IntPtr msdfgen_CubicSegment_create(Vector2d p0, Vector2d p1, Vector2d p2, Vector2d p3, EdgeColor edgeColor);
    
    [LibraryImport(LibraryName)]
    internal static partial IntPtr msdfgen_CubicSegment_toBase(IntPtr segment);
}