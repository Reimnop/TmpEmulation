using MsdfgenNet;
using MsdfgenNet.Data;
using OpenTK.Mathematics;
using SharpFont;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Range = MsdfgenNet.Data.Range;

namespace Debug;

public static class Program
{
    public static void Main(string[] args)
    {
        const int channels = 3;
        const int padding = 16;
        
        using var ftLibrary = new Library();
        using var face = ftLibrary.NewFace("Roboto-Regular.ttf", 0);
        face.SetPixelSizes(0, 256);
        
        // Load
        face.LoadChar('&', LoadFlags.Default, LoadTarget.Normal);
        var glyph = face.Glyph;
        var glyphMetrics = glyph.Metrics;
        
        var outline = glyph.Outline;
        var shape = ShapeBuilder.Build(outline);
        shape.InverseYAxis = true;
        shape.Normalize();
        
        // Fill edge colors
        EdgeColoring.ColorByDistance(shape, 3.0);
        
        // Create bitmap to fill MSDF in
        var width = (glyph.Metrics.Width.Value >> 6) + (padding << 1);
        var height = (glyph.Metrics.Height.Value >> 6) + (padding << 1);
        var msdf = new Bitmap(width, height, channels);
        
        // Create config
        var offsetX = -glyphMetrics.HorizontalBearingX.Value / 64.0 + padding;
        var offsetY = -(glyphMetrics.HorizontalBearingY.Value - glyphMetrics.Height.Value) / 64.0 + padding;
        
        var projection = new Projection(Vector2d.One, new Vector2d(offsetX, offsetY));
        var transformation = new SdfTransformation(projection, DistanceMapping.CreateRange(new Range(-2.0, 2.0)));
        var config = new MsdfGeneratorConfig(true, new ErrorCorrectionConfig());
        
        // Generate MSDF
        Msdfgen.GenerateMsdf(msdf, shape, transformation, config);
        
        // Save MSDF
        using var image = new Image<Rgb24>(width, height);

        var data = msdf.Data.ToArray();
        var min = data.Min();
        var max = data.Max();
        
        // Sample MSDF image to actual glyph image
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var index = (y * width + x) * channels;
                var r = data[index];
                var g = data[index + 1];
                var b = data[index + 2];
                var sdf = Median(r, g, b);
                image[x, y] = new Rgb24(
                    (byte) (MathHelper.MapRange(sdf, min, max, 0.0, 1.0) * 255.0),
                    (byte) (MathHelper.MapRange(sdf, min, max, 0.0, 1.0) * 255.0),
                    (byte) (MathHelper.MapRange(sdf, min, max, 0.0, 1.0) * 255.0)
                );
            }
        }
        
        image.Save("output.png");
    }
    
    private static float SmoothStep(float edge0, float edge1, float x)
    {
        var t = Math.Clamp((x - edge0) / (edge1 - edge0), 0.0f, 1.0f);
        return t * t * (3.0f - 2.0f * t);
    }
    
    private static float Median(float a, float b, float c) 
        => Math.Max(Math.Min(a, b), Math.Min(Math.Max(a, b), c));
}