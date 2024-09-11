using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using TmpIO;
using TmpParser;
using Color3 = TmpParser.Color3;

namespace TmpRendering;

public class App() : GameWindow(GameWindowSettings, NativeWindowSettings)
{
    private static readonly GameWindowSettings GameWindowSettings = new();
    
    private static readonly NativeWindowSettings NativeWindowSettings = new()
    {
        ClientSize = new Vector2i(800, 600),
        APIVersion = new Version(4, 6),
        Title = "TmpRendering"
    };

    private int vao;
    private int program;
    private int fontTexture;
    private int sampler;
    private int glyphBuffer;
    private int glyphCount;

    private float fontSize;
    private float sdfRange;

    protected override void OnLoad()
    {
        base.OnLoad();

        vao = GL.CreateVertexArray(); // This won't do anything, but it won't render without it
        
        // Load shader program
        var vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, GetResourceString("Resources.Vertex.vert"));
        GL.CompileShader(vertexShader);
        
        var vertexStatus = GL.GetShaderi(vertexShader, ShaderParameterName.CompileStatus);
        if (vertexStatus != 1)
        {
            GL.GetShaderInfoLog(vertexShader, out var infoLog);
            throw new InvalidOperationException($"Vertex shader compilation failed: {infoLog}");
        }
        
        var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, GetResourceString("Resources.Fragment.frag"));
        GL.CompileShader(fragmentShader);
        
        var fragmentStatus = GL.GetShaderi(fragmentShader, ShaderParameterName.CompileStatus);
        if (fragmentStatus != 1)
        {
            GL.GetShaderInfoLog(fragmentShader, out var infoLog);
            throw new InvalidOperationException($"Fragment shader compilation failed: {infoLog}");
        }
        
        program = GL.CreateProgram();
        GL.AttachShader(program, vertexShader);
        GL.AttachShader(program, fragmentShader);
        GL.LinkProgram(program);
        
        var linkStatus = GL.GetProgrami(program, ProgramProperty.LinkStatus);
        if (linkStatus != 1)
        {
            GL.GetProgramInfoLog(program, out var infoLog);
            throw new InvalidOperationException($"Program linking failed: {infoLog}");
        }
        
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
        
        // Load font
        var fontStream = GetResourceStream("Resources.Inconsolata-Regular.tmpe");
        var tmpFile = TmpRead.Read(fontStream);
        
        fontSize = tmpFile.Metadata.Size;
        sdfRange = tmpFile.Metadata.SdfRange;
        
        // Upload font atlas
        fontTexture = GL.CreateTexture(TextureTarget.Texture2d);
        GL.TextureStorage2D(fontTexture, 1, SizedInternalFormat.Rgb32f, tmpFile.Atlas.Width, tmpFile.Atlas.Height);
        GL.TextureSubImage2D(fontTexture, 0, 0, 0, tmpFile.Atlas.Width, tmpFile.Atlas.Height, PixelFormat.Rgb, PixelType.Float, tmpFile.Atlas.Data);
        
        // Create sampler
        sampler = GL.CreateSampler();
        GL.SamplerParameteri(sampler, SamplerParameterI.TextureMinFilter, (int) TextureMinFilter.Linear);
        GL.SamplerParameteri(sampler, SamplerParameterI.TextureMagFilter, (int) TextureMagFilter.Linear);
        
        // Upload glyphs
        const string text = "Whereas disregard and contempt for human rights have resulted.";
        var glyphs = GetGlyphs(tmpFile, text).ToArray();
        glyphCount = glyphs.Length;
        
        glyphBuffer = GL.CreateBuffer();
        GL.NamedBufferData(glyphBuffer, glyphs.Length * Unsafe.SizeOf<RenderGlyph>(), glyphs, VertexBufferObjectUsage.StaticDraw);
        
        // Enable blending
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Enable(EnableCap.Blend);
    }

    private static IEnumerable<RenderGlyph> GetGlyphs(TmpFile tmp, string str)
    {
        var glyphLookup = tmp.Glyphs.ToDictionary(x => x.Id);
        var characterLookup = tmp.Characters.ToDictionary(x => x.Character);
        
        TmpGlyph? GetGlyph(char c)
        {
            if (!characterLookup.TryGetValue(c, out var character))
                return null;
            if (!glyphLookup.TryGetValue(character.GlyphId, out var glyph))
                return null;
            return glyph;
        }
        
        var x = 0.0f;
        var y = 0.0f;
        foreach (var run in TagParser.Parse(str))
        {
            var colorAlpha = run.Style.Color;
            var color = new Vector4(
                colorAlpha.Rgb.HasValue 
                    ? new Vector3(
                        colorAlpha.Rgb.Value.R / 255.0f,
                        colorAlpha.Rgb.Value.G / 255.0f,
                        colorAlpha.Rgb.Value.B / 255.0f)
                    : new Vector3(float.NaN), 
                colorAlpha.A.HasValue
                    ? colorAlpha.A.Value / 255.0f
                    : float.NaN);
            var bold = run.Style.Bold;
            var italic = run.Style.Italic;
            var boldItalic = (bold ? BoldItalic.Bold : BoldItalic.None) | (italic ? BoldItalic.Italic : BoldItalic.None);
            
            foreach (var c in run.Text)
            {
                var glyphNullable = GetGlyph(c);
                if (!glyphNullable.HasValue)
                    continue;
                var glyph = glyphNullable.Value;
                if (glyph.Width != 0.0f && glyph.Height != 0.0f)
                {
                    var minX = x + glyph.BearingX;
                    var minY = y - glyph.BearingY;
                    var maxX = minX + glyph.Width;
                    var maxY = minY + glyph.Height;
                    var minUV = new Vector2(glyph.MinX, glyph.MinY);
                    var maxUV = new Vector2(glyph.MaxX, glyph.MaxY);
                    yield return new RenderGlyph(new Vector2(minX, minY), new Vector2(maxX, maxY), minUV, maxUV, color, boldItalic);
                }
                x += glyph.Advance;
            }
        }
    }
    
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        
        var translate = Matrix4.CreateScale(1.0f) * Matrix4.CreateTranslation(0.0f, 32.0f, 0.0f);
        var camera = CreateCameraMatrix(ClientSize);
        
        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        
        GL.UseProgram(program);
        GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, glyphBuffer);
        GL.UniformMatrix4f(0, 1, false, translate * camera);
        GL.BindTextureUnit(0, fontTexture);
        GL.BindSampler(0, sampler);
        GL.Uniform1f(2, fontSize);
        GL.Uniform1f(3, sdfRange);
        GL.Uniform4f(4, 1, (Vector4) Color4.White);
        GL.BindVertexArray(vao);
        GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, 6, glyphCount);
        
        SwapBuffers();
    }
    
    private static Matrix4 CreateCameraMatrix(Vector2i size)
    {
        // top left is (0, 0), bottom right is (width, height)
        return Matrix4.CreateOrthographicOffCenter(0.0f, size.X, size.Y, 0.0f, -1.0f, 1.0f);
    }

    private static string GetResourceString(string name)
    {
        var assembly = typeof(App).Assembly;
        var resourceName = $"{assembly.GetName().Name}.{name}";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            throw new InvalidOperationException($"Resource '{resourceName}' not found");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
    
    private static Stream GetResourceStream(string name)
    {
        var assembly = typeof(App).Assembly;
        var resourceName = $"{assembly.GetName().Name}.{name}";
        var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            throw new InvalidOperationException($"Resource '{resourceName}' not found");
        return stream;
    }
}