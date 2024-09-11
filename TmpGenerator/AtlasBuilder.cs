using MsdfgenNet;
using RectpackSharp;

namespace TmpGenerator;

public static class AtlasBuilder
{
    public static FontAtlas Build(IEnumerable<(int Id, Bitmap Bitmap)> bitmaps)
    {
        var bitmapIdToBitmap = bitmaps.ToDictionary();
        var channels = bitmapIdToBitmap.Values
            .EnsureAllEqual(x => x.Channels);
        var packingRectangles = bitmapIdToBitmap
            .Select(kvp => new PackingRectangle(0u, 0u, (uint)kvp.Value.Width, (uint)kvp.Value.Height, kvp.Key))
            .ToArray();
        RectanglePacker.Pack(packingRectangles, out var bounds);
        
        var atlas = new FontAtlas((int)bounds.Width, (int)bounds.Height, channels);
        foreach (var rect in packingRectangles)
        {
            var bitmap = bitmapIdToBitmap[rect.Id];
            atlas.AddElement(bitmap, (int)rect.X, (int)rect.Y, bitmap.Width, bitmap.Height, rect.Id);
        }
        return atlas;
    }
    
    private static TOut EnsureAllEqual<T, TOut>(this IEnumerable<T> values, Func<T, TOut> selector)
    {
        using var enumerator = values.GetEnumerator();
        var result = enumerator.MoveNext() ? selector(enumerator.Current) : throw new ArgumentException("Sequence is empty");
        while (enumerator.MoveNext())
        {
            if (!Equals(selector(enumerator.Current), result))
                throw new ArgumentException("Values are not equal");
        }
        return result;
    }
}