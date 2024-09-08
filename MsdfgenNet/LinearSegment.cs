using MsdfgenNet.Enum;
using MsdfgenNet.Interop;
using OpenTK.Mathematics;

namespace MsdfgenNet;

public class LinearSegment : EdgeSegment
{
    internal LinearSegment(IntPtr handle) : base(handle)
    {
    }
    
    public LinearSegment(Vector2d p0, Vector2d p1, EdgeColor edgeColor = EdgeColor.White) 
        : base(Native.msdfgen_LinearSegment_create(p0, p1, edgeColor))
    {
    }
}