using MsdfgenNet;

namespace TmpGenerator;

public class FontAtlas(int width, int height, int channels)
{
    public int Width { get; } = width;
    public int Height { get; } = height;
    public int Channels { get; } = channels;
    public Span<float> Data => data;
    public IReadOnlyList<AtlasElement> Elements => elements;
    
    private readonly float[] data = new float[width * height * channels];
    private readonly List<AtlasElement> elements = [];

    public void AddElement(Bitmap bitmap, int x, int y, int width, int height, int index = 0)
    {
        if (bitmap.Width != width || bitmap.Height != height)
            throw new ArgumentException("Bitmap dimensions do not match the specified dimensions");

        if (x < 0 || y < 0 || x + width > Width || y + height > Height)
            throw new ArgumentException("Element does not fit in the atlas");

        if (bitmap.Channels > Channels)
            throw new ArgumentException("Cannot fit all channels of the bitmap in the atlas");
        
        for (var i = 0; i < height; i++)
        {
            for (var j = 0; j < width; j++)
            {
                var bitmapIndex = (i * width + j) * bitmap.Channels;
                var atlasIndex = ((y + i) * Width + x + j) * Channels;
                for (var k = 0; k < bitmap.Channels; k++)
                    data[atlasIndex + k] = bitmap.Data[bitmapIndex + k];
            }
        }
        
        elements.Add(new AtlasElement(x, y, width, height, index));
    }
}