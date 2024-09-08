using MsdfgenNet.Enum;
using MsdfgenNet.Interop;
using OpenTK.Mathematics;

namespace MsdfgenNet;

public class QuadraticSegment : EdgeSegment
{
    internal QuadraticSegment(IntPtr handle) : base(handle)
    {
    }
    
    public QuadraticSegment(Vector2d p0, Vector2d p1, Vector2d p2, EdgeColor edgeColor = EdgeColor.White) 
        : base(Native.msdfgen_QuadraticSegment_create(p0, p1, p2, edgeColor))
    {
    }
    
    public CubicSegment ConvertToCubic() 
        => new(Native.msdfgen_QuadraticSegment_convertToCubic(Handle));
}