namespace TmpParser;

public class TagParsingState
{
    public int BoldNestLevel { get; set; }
    public int ItalicNestLevel { get; set; }
    public int UnderlineNestLevel { get; set; }
    public Stack<ColorAlpha> ColorStack { get; } = [];
}