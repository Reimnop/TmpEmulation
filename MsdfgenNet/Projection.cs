using MsdfgenNet.Interop;
using OpenTK.Mathematics;

namespace MsdfgenNet;

public class Projection : NativeObject
{
    public unsafe Vector2d Scale 
    {
        get => Native.msdfgen_Projection_getScale(Handle);
        set => Native.msdfgen_Projection_setScale(Handle, new IntPtr(&value));
    }
    
    public unsafe Vector2d Translate 
    {
        get => Native.msdfgen_Projection_getTranslate(Handle);
        set => Native.msdfgen_Projection_setTranslate(Handle, new IntPtr(&value));
    }
    
    internal Projection(IntPtr handle) : base(handle)
    {
    }
    
    public Projection() 
        : this(Native.msdfgen_Projection_create())
    {
    }
    
    public unsafe Projection(Vector2d scale, Vector2d translate) 
        : this(Native.msdfgen_Projection_createRange(new IntPtr(&scale), new IntPtr(&translate)))
    {
    }
    
    public unsafe Vector2d Project(Vector2d coord)
        => Native.msdfgen_Projection_project(Handle, new IntPtr(&coord));
    
    public unsafe Vector2d Unproject(Vector2d coord)
        => Native.msdfgen_Projection_unproject(Handle, new IntPtr(&coord));
    
    public unsafe Vector2d ProjectVector(Vector2d vector)
        => Native.msdfgen_Projection_projectVector(Handle, new IntPtr(&vector));
    
    public unsafe Vector2d UnprojectVector(Vector2d vector)
        => Native.msdfgen_Projection_unprojectVector(Handle, new IntPtr(&vector));
    
    public double ProjectX(double x)
        => Native.msdfgen_Projection_projectX(Handle, x);
    
    public double ProjectY(double y)
        => Native.msdfgen_Projection_projectY(Handle, y);
    
    public double UnprojectX(double x)
        => Native.msdfgen_Projection_unprojectX(Handle, x);
    
    public double UnprojectY(double y)
        => Native.msdfgen_Projection_unprojectY(Handle, y);

    protected override void FreeHandle(IntPtr handle)
    {
        Native.msdfgen_Projection_destroy(handle);
    }
}