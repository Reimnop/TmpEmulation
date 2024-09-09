using MsdfgenNet;
using MsdfgenNet.Data;
using OpenTK.Mathematics;
using SharpFont;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace TmpGenerator;

public static class App
{
    public static void Run(Options options)
    {
        using var freetype = new Library();
        using var face = freetype.NewFace(options.Input, 0);
        face.SetPixelSizes(0, (uint)options.Size);

        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()-_=+[]{};:'\",.<>/?\\|`~";

        // ReSharper disable once AccessToDisposedClosure
        var bitmaps = chars.Select(x => GenerateMsdf(x, face, options.Size, options.Padding, options.Range));
        var atlas = AtlasBuilder.Build(bitmaps);
        
        using var image = new Image<Rgba32>(atlas.Width, atlas.Height);
        for (var y = 0; y < atlas.Height; y++)
        {
            for (var x = 0; x < atlas.Width; x++)
            {
                var index = (y * atlas.Width + x) * atlas.Channels;
                var dist = Median(atlas.Data[index], atlas.Data[index + 1], atlas.Data[index + 2]);
                var value = SmoothStep(0.5f, 0.7f, dist);
                image[x, y] = new Rgba32(value, value, value);
            }
        }
        
        image.Save(options.Output);
    }
    
    private static float SmoothStep(float edge0, float edge1, float x)
    {
        var t = Math.Clamp((x - edge0) / (edge1 - edge0), 0.0f, 1.0f);
        return t * t * (3.0f - 2.0f * t);
    }
    
    private static float Median(float a, float b, float c) 
        => Math.Max(Math.Min(a, b), Math.Min(Math.Max(a, b), c));
    
    private static byte MapFloatToByte(float value)
    {
        return (byte)Math.Clamp(MathHelper.MapRange(value, 0.0, 1.0, 0.0, 255.0), 0.0, 255.0);
    }

    private static Bitmap GenerateMsdf(char c, Face face, int size, int padding, double range)
    {
        face.LoadChar(c, LoadFlags.Default, LoadTarget.Normal);
        var glyph = face.Glyph;
        var metrics = glyph.Metrics;
        
        var outline = glyph.Outline;
        using var shape = ShapeBuilder.Build(outline);
        shape.InverseYAxis = true;
        shape.Normalize();
        
        EdgeColoring.ColorByDistance(shape, range);
        
        var width = (metrics.Width.Value >> 6) + (padding << 1);
        var height = (metrics.Height.Value >> 6) + (padding << 1);
        var msdf = new Bitmap(width, height, 3);
        
        var offsetX = -metrics.HorizontalBearingX.Value / 64.0 + padding;
        var offsetY = -(metrics.HorizontalBearingY.Value - metrics.Height.Value) / 64.0 + padding;
        
        using var projection = new Projection(Vector2d.One, new Vector2d(offsetX, offsetY));
        using var transformation = new SdfTransformation(projection, DistanceMapping.CreateRange(new MsdfgenNet.Data.Range(-range, range)));
        using var config = new MsdfGeneratorConfig(true, new ErrorCorrectionConfig());
        
        Msdfgen.GenerateMsdf(msdf, shape, transformation, config);
        
        return msdf;
    }
}