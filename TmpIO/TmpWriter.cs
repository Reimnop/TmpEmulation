using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace TmpIO;

public static class TmpWriter
{
    public static void Write(Stream stream, TmpFile file)
    {
        var chunks = new List<RiffChunk>();
        chunks.Add(GetMetadataChunk(stream, file.Metadata));
        chunks.AddRange(file.Characters.Select(character => GetCharacterChunk(stream, character)));
        chunks.AddRange(file.Glyphs.Select(glyph => GetGlyphChunk(stream, glyph)));
        chunks.Add(GetAtlasChunk(stream, file.Atlas));
        
        WriteChunk(stream, new RiffChunk("RIFF", "TMPE"u8.ToArray(), chunks)); // RIFF header
    }

    private static RiffChunk GetMetadataChunk(Stream stream, TmpMetadata file)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream, Encoding.UTF8, true);
        writer.Write(file.Name.Length);
        writer.Write(Encoding.UTF8.GetBytes(file.Name));
        writer.Write(file.Size);
        writer.Write(file.SdfRange);
        
        return new RiffChunk("META", memoryStream.ToArray());
    }
    
    private static RiffChunk GetCharacterChunk(Stream stream, in TmpCharacter character)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream, Encoding.UTF8, true);
        writer.Write(character.Character);
        writer.Write(character.GlyphId);
        
        return new RiffChunk("CHAR", memoryStream.ToArray());
    }

    private static RiffChunk GetGlyphChunk(Stream stream, in TmpGlyph glyph)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream, Encoding.UTF8, true);
        writer.Write(glyph.Id);
        writer.Write(glyph.Advance);
        writer.Write(glyph.BearingX);
        writer.Write(glyph.BearingY);
        writer.Write(glyph.Width);
        writer.Write(glyph.Height);
        writer.Write(glyph.MinX);
        writer.Write(glyph.MinY);
        writer.Write(glyph.MaxX);
        writer.Write(glyph.MaxY);
        
        return new RiffChunk("GLPH", memoryStream.ToArray());
    }

    private static RiffChunk GetAtlasChunk(Stream stream, TmpAtlas atlas)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream, Encoding.UTF8, true);
        writer.Write(atlas.Width);
        writer.Write(atlas.Height);
        writer.Write(atlas.Data.Length * sizeof(float));

        using var dataStream = new GZipStream(stream, CompressionMode.Compress, true);
        writer.Write(MemoryMarshal.Cast<float, byte>(atlas.Data));
        
        return new RiffChunk("ATLS", memoryStream.ToArray());
    }
    
    private static void WriteChunk(Stream stream, RiffChunk chunk)
    {
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
        
        if (chunk.Id.Length != 4)
            throw new ArgumentException("Chunk ID must be 4 characters long", nameof(chunk));
        
        // Round data length to multiple of 2
        var dataLength = chunk.Data.Length;
        if (dataLength % 2 != 0)
            dataLength++;
        
        writer.Write(Encoding.ASCII.GetBytes(chunk.Id));
        writer.Write(dataLength + (chunk.SubChunks?.Sum(x => x.Data.Length + 8) ?? 0));
        writer.Write(chunk.Data);
        
        // Write padding
        if (dataLength != chunk.Data.Length)
            writer.Write((byte) 0);
        
        // Write sub-chunks
        if (chunk.SubChunks is not null)
            foreach (var subChunk in chunk.SubChunks)
                WriteChunk(stream, subChunk);
    }
}