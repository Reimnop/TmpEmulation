using MsdfgenNet;
using RectpackSharp;

namespace TmpGenerator;

public static class AtlasBuilder
{
    public static FontAtlas Build(IEnumerable<Bitmap> bitmaps)
    {
        var bitmapList = bitmaps.ToList();
        var channels = bitmapList.EnsureAllEqual(x => x.Channels);
        var packingRectangles = bitmapList
            .Select((x, i) => new PackingRectangle(0u, 0u, (uint)x.Width, (uint)x.Height, i))
            .ToArray();
        RectanglePacker.Pack(packingRectangles, out var bounds);
        
        var atlas = new FontAtlas((int)bounds.Width, (int)bounds.Height, channels);
        foreach (var rect in packingRectangles)
        {
            var bitmap = bitmapList[rect.Id];
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