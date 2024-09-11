using OpenTK.Mathematics;

namespace TmpRendering;

public static class MathUtil
{
    public static Matrix3 CreateTranslation(Vector2 translation)
        => new(
            1.0f,          0.0f,          0.0f,
            0.0f,          1.0f,          0.0f,
            translation.X, translation.Y, 1.0f);
    
    public static Matrix3 CreateScale(Vector2 scale)
        => new(
            scale.X, 0.0f,    0.0f,
            0.0f,    scale.Y, 0.0f,
            0.0f,    0.0f,    1.0f);
    
    public static Matrix3 CreateRotation(float angle)
    {
        var cos = MathF.Cos(angle);
        var sin = MathF.Sin(angle);
        
        return new Matrix3(
            cos, sin,  0.0f,
            -sin, cos,  0.0f,
            0.0f, 0.0f, 1.0f);
    }
}