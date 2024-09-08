using MsdfgenNet.Enum;
using MsdfgenNet.Interop;
using OpenTK.Mathematics;

namespace MsdfgenNet;

public class CubicSegment : EdgeSegment
{
    internal CubicSegment(IntPtr handle) : base(handle)
    {
    }
    
    public CubicSegment(Vector2d p0, Vector2d p1, Vector2d p2, Vector2d p3, EdgeColor edgeColor = EdgeColor.White) 
        : base(Native.msdfgen_CubicSegment_create(p0, p1, p2, p3, edgeColor))
    {
    }
}