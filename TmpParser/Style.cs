namespace TmpParser;

public readonly record struct Style(
    bool Bold, 
    bool Italic, 
    bool Underline,
    float CSpace,
    ColorAlpha Color);