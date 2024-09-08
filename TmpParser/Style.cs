namespace TmpParser;

public readonly record struct Style(bool Bold, bool Italic, bool Underline, ColorAlpha Color)
{
    public override string ToString()
    {
        return $"Style(Bold: {Bold}, Italic: {Italic}, Underline: {Underline}, Color: {Color})";
    }
}