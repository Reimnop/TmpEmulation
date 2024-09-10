using MsdfgenNet;
using MsdfgenNet.Data;
using OpenTK.Mathematics;
using SharpFont;
using TmpIO;

namespace TmpGenerator;

public static class App
{
    public static void Run(Options options)
    {
        using var freetype = new Library();
        using var face = freetype.NewFace(options.Input, 0);
        face.SetPixelSizes(0, (uint)options.Size);

        const string characters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        
        var characterToGlyphIndex = characters
            .Select(x => (x, (int)face.GetCharIndex(x)))
            .ToDictionary();
        var glyphs = characterToGlyphIndex.Values
            .Select(x => GetGlyph(x, face, options.Padding, options.Range))
            .Append(GetGlyph(0, face, options.Padding, options.Range)) // Tofu
            .ToList();
        var atlas = AtlasBuilder.Build(glyphs.Select(x => x.Bitmap));
        
        var tmpMetadata = new TmpMetadata(face.FamilyName, options.Size, (float) options.Range);
        var tmpCharacters = characterToGlyphIndex
            .Select(kvp => new TmpCharacter(kvp.Key, kvp.Value))
            .ToArray();
        var tmpGlyphs = atlas.Elements
            .Select(element =>
            {
                var minX = element.X / (float) atlas.Width;
                var minY = element.Y / (float) atlas.Height;
                var maxX = (element.X + element.Width) / (float) atlas.Width;
                var maxY = (element.Y + element.Height) / (float) atlas.Height;
                var glyph = glyphs[element.Index];
                return new TmpGlyph(
                    element.Index,
                    glyph.Advance,
                    glyph.BearingX,
                    glyph.BearingY,
                    glyph.Width,
                    glyph.Height,
                    minX,
                    minY,
                    maxX,
                    maxY);
            })
            .ToArray();
        var tmpAtlas = new TmpAtlas(atlas.Width, atlas.Height, atlas.Data.ToArray());
        var tmpFile = new TmpFile(tmpMetadata, tmpCharacters, tmpGlyphs, tmpAtlas);
        
        using var stream = File.Open(options.Output, FileMode.Create);
        TmpWriter.Write(stream, tmpFile);
    }

    private static Glyph GetGlyph(int glyphId, Face face, int padding, double range)
    {
        // Load glyph
        face.LoadGlyph((uint) glyphId, LoadFlags.Default, LoadTarget.Normal);
        var glyph = face.Glyph;
        var metrics = glyph.Metrics;
        
        // Build character shape from outline
        var outline = glyph.Outline;
        using var shape = ShapeBuilder.Build(outline);
        shape.InverseYAxis = true;
        shape.Normalize();
        
        // Fill in edge colors
        EdgeColoring.ColorByDistance(shape, range);
        
        // Initialize bitmap
        var width = (metrics.Width.Value >> 6) + (padding << 1);
        var height = (metrics.Height.Value >> 6) + (padding << 1);
        var msdf = new Bitmap(width, height, 3);
        
        // Calculate glyph offset
        var offsetX = -metrics.HorizontalBearingX.Value / 64.0 + padding;
        var offsetY = -(metrics.HorizontalBearingY.Value - metrics.Height.Value) / 64.0 + padding;
        
        // Generate MSDF
        using var projection = new Projection(Vector2d.One, new Vector2d(offsetX, offsetY));
        using var transformation = new SdfTransformation(projection, DistanceMapping.CreateRange(new MsdfgenNet.Data.Range(-range, range)));
        using var config = new MsdfGeneratorConfig(true, new ErrorCorrectionConfig());
        Msdfgen.GenerateMsdf(msdf, shape, transformation, config);

        return new Glyph(
            msdf,
            metrics.HorizontalAdvance.Value >> 6,
            metrics.HorizontalBearingX.Value / 64.0f - padding,
            metrics.HorizontalBearingY.Value / 64.0f + padding,
            metrics.Width.Value / 64.0f + padding * 2.0f,
            metrics.Height.Value / 64.0f + padding * 2.0f);
    }
}