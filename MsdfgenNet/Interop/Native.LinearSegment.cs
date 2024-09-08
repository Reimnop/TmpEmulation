using OpenTK.Mathematics;
using System.Runtime.InteropServices;
using MsdfgenNet.Enum;

namespace MsdfgenNet.Interop;

internal static partial class Native
{
    [LibraryImport(LibraryName)]
    internal static partial IntPtr msdfgen_LinearSegment_create(Vector2d p0, Vector2d p1, EdgeColor edgeColor);
    
    [LibraryImport(LibraryName)]
    internal static partial IntPtr msdfgen_LinearSegment_toBase(IntPtr segment);
}