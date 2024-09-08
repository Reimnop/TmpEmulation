using System.Runtime.InteropServices;
using MsdfgenNet;
using OpenTK.Mathematics;
using SharpFont;

namespace Debug;

public static class ShapeBuilder
{
    private class BuildState(Shape shape)
    {
        public Shape Shape { get; } = shape;
        public Contour? CurrentContour { get; set; }
        public Vector2d LastPoint { get; set; }
    }
    
    public static Shape Build(Outline outline)
    {
        var state = new BuildState(new Shape());
        var gcHandle = GCHandle.Alloc(state);

        try
        {
            var funcs = new OutlineFuncs();
            funcs.MoveFunction = MoveTo;
            funcs.LineFunction = LineTo;
            funcs.ConicFunction = ConicTo;
            funcs.CubicFunction = CubicTo;
            funcs.Shift = 0;
            
            outline.Decompose(funcs, (IntPtr) gcHandle);

            return state.Shape;
        }
        finally
        {
            gcHandle.Free();
        }
    }

    private static int MoveTo(ref FTVector to, IntPtr context)
    {
        var state = context.ToBuildState();
        var contour = state.Shape.AddContour();
        state.CurrentContour = contour;
        state.LastPoint = to.ToVector2d();
        return 0;
    }

    private static int LineTo(ref FTVector to, IntPtr context)
    {
        var state = context.ToBuildState();
        state.ThrowIfCurrentContourIsNull(out var contour);
        var segment = new LinearSegment(state.LastPoint, to.ToVector2d());
        var holder = contour.AddEdge();
        holder.Segment = segment;
        state.LastPoint = to.ToVector2d();
        return 0;
    }

    private static int ConicTo(ref FTVector control, ref FTVector to, IntPtr context)
    {
        var state = context.ToBuildState();
        state.ThrowIfCurrentContourIsNull(out var contour);
        var segment = new QuadraticSegment(state.LastPoint, control.ToVector2d(), to.ToVector2d());
        var holder = contour.AddEdge();
        holder.Segment = segment;
        state.LastPoint = to.ToVector2d();
        return 0;
    }
    
    private static int CubicTo(ref FTVector control1, ref FTVector control2, ref FTVector to, IntPtr context)
    {
        var state = context.ToBuildState();
        state.ThrowIfCurrentContourIsNull(out var contour);
        var segment = new CubicSegment(state.LastPoint, control1.ToVector2d(), control2.ToVector2d(), to.ToVector2d());
        var holder = contour.AddEdge();
        holder.Segment = segment;
        state.LastPoint = to.ToVector2d();
        return 0;
    }
    
    private static Vector2d ToVector2d(this FTVector vector)
    {
        return new Vector2d(vector.X.Value / 64.0, vector.Y.Value / 64.0);
    }
    
    private static BuildState ToBuildState(this IntPtr pointer)
    {
        var gcHandle = (GCHandle) pointer;
        var target = gcHandle.Target;
        if (target is not BuildState state)
            throw new InvalidOperationException("Invalid pointer when trying to convert to state");
        return state;
    }
    
    private static void ThrowIfCurrentContourIsNull(this BuildState state, out Contour contour)
    {
        contour = state.CurrentContour ?? throw new InvalidOperationException("No current contour");
    }
}