using System.Globalization;

namespace TmpParser;

public delegate Style? TagProcessor(bool close, string? value, Style style, TagParsingState state);

public static class TagProcessors
{
    // black, blue, green, orange, purple, red, white, and yellow
    private static readonly Dictionary<string, Color3> KnownColors = new()
    {
        ["black"] = Color3.ParseHex("000000"),
        ["blue"] = Color3.ParseHex("0000FF"),
        ["green"] = Color3.ParseHex("00FF00"),
        ["orange"] = Color3.ParseHex("FFA500"),
        ["purple"] = Color3.ParseHex("800080"),
        ["red"] = Color3.ParseHex("FF0000"),
        ["white"] = Color3.ParseHex("FFFFFF"),
        ["yellow"] = Color3.ParseHex("FFFF00"),
    };
    
    public static Style? ProcessBold(bool close, string? value, Style style, TagParsingState state)
    {
        state.BoldNestLevel += close ? -1 : 1;
        return style with { Bold = state.BoldNestLevel > 0 };
    }
    
    public static Style? ProcessItalic(bool close, string? value, Style style, TagParsingState state)
    {
        state.ItalicNestLevel += close ? -1 : 1;
        return style with { Italic = state.ItalicNestLevel > 0 };
    }
    
    public static Style? ProcessUnderline(bool close, string? value, Style style, TagParsingState state)
    {
        state.UnderlineNestLevel += close ? -1 : 1;
        return style with { Underline = state.UnderlineNestLevel > 0 };
    }
    
    public static Style? ProcessAlpha(bool close, string? value, Style style, TagParsingState state)
    {
        if (close)
            return null; // Alpha tag can't be closed (thankfully)
        
        if (string.IsNullOrWhiteSpace(value))
            return null; // Alpha tag must have a value

        if (!TryParseAlpha(value, out var alpha))
            return null;
        
        var oldColor = state.ColorStack.Count == 0 ? default : state.ColorStack.Peek();
        state.ColorStack.Push(oldColor with { A = alpha });
        return style with { Color = state.ColorStack.Peek() };
    }
    
    public static Style? ProcessColor(bool close, string? value, Style style, TagParsingState state)
    {
        if (close)
        {
            if (state.ColorStack.Count == 0)
                return null; // No color to close
            
            state.ColorStack.Pop();
            return style with { Color = state.ColorStack.Count == 0 ? default : state.ColorStack.Peek() };
        }
        
        if (string.IsNullOrWhiteSpace(value))
            return null; // Color tag must have a value
        
        if (value.StartsWith('#'))
            value = value[1..];

        var colorHex = value.Length == 8 ? value[..6] : value;
        var alphaHex = value.Length == 8 ? value[6..] : null;
        
        if (!TryParseColor(colorHex, out var color))
            return null; // Invalid color value
        
        var oldColor = state.ColorStack.Count == 0 ? default : state.ColorStack.Peek();
        var newColor = oldColor with { Rgb = color };
        
        if (alphaHex is not null)
        {
            if (!TryParseAlpha(alphaHex, out var alpha))
                return null; // Invalid alpha value
            
            newColor = newColor with { A = alpha };
        }
        
        state.ColorStack.Push(newColor);
        return style with { Color = state.ColorStack.Peek() };
    }
    
    public static Style? ProcessPos(bool close, string? value, Style style, TagParsingState state)
    {
        // TODO: Implement pos tag
        return style;
    }

    public static Style? ProcessCSpace(bool close, string? value, Style style, TagParsingState state)
    {
        if (close)
        {
            var oldCSpace = state.CSpaceStack.Count == 0 ? default : state.CSpaceStack.Peek();
            state.CSpaceStack.Pop();
            return style with { CSpace = oldCSpace };
        }
        
        if (string.IsNullOrWhiteSpace(value))
            return null; // CSpace tag must have a value
        
        if (!float.TryParse(value, out var cSpace))
            return null; // Invalid CSpace value
        
        state.CSpaceStack.Push(cSpace);
        return style with { CSpace = cSpace };
    }

    private static bool TryParseAlpha(string value, out byte alpha)
    {
        alpha = default;
        
        if (string.IsNullOrWhiteSpace(value))
            return false;
        
        if (value.StartsWith('#'))
            value = value[1..];
        
        return byte.TryParse(value, NumberStyles.HexNumber, null, out alpha);
    }

    private static bool TryParseColor(string hex, out Color3 color)
    {
        color = default;
        
        if (string.IsNullOrWhiteSpace(hex))
            return false;
        
        if (KnownColors.TryGetValue(hex, out color))
            return true;
        
        if (hex.StartsWith('#'))
            hex = hex[1..];
        
        if (hex.Length != 3 && hex.Length != 6)
            return false;
        
        if (hex.Length == 3)
            hex = string.Concat(hex.Select(c => new string(c, 2)));
        
        return Color3.TryParseHex(hex, out color);
    }
}