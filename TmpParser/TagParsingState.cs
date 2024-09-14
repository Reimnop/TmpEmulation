namespace TmpParser;

public class TagParsingState
{
    public int BoldNestLevel { get; set; }
    public int ItalicNestLevel { get; set; }
    public int UnderlineNestLevel { get; set; }
    public Stack<float> CSpaceStack { get; } = [];
    public Stack<ColorAlpha> ColorStack { get; } = [];
    public Stack<string> FontStack { get; } = [];
}