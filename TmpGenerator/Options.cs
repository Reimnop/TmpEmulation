using CommandLine;

namespace TmpGenerator;

public class Options
{
    [Option('i', "input", Required = true, HelpText = "Input font file path.")]
    public required string Input { get; set; }
    
    [Option('o', "output", Required = true, HelpText = "Output atlas file path.")]
    public required string Output { get; set; }
    
    [Option('s', "size", Required = false, Default = 24, HelpText = "Font size.")]
    public required int Size { get; set; }
    
    [Option('p', "padding", Required = false, Default = 4, HelpText = "Padding in glyph texture.")]
    public required int Padding { get; set; }
    
    [Option('g', "gap", Required = false, Default = 2, HelpText = "Gap between glyphs in atlas.")]
    public required int Gap { get; set; }
    
    [Option('r', "range", Required = false, Default = 2.0, HelpText = "SDF range.")]
    public required double Range { get; set; }
}