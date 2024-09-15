using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TmpParser;

public static partial class TagParser
{
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
    
    [GeneratedRegex(@"<([^<>\n]+?)>")]
    private static partial Regex GetTagRegex();

    public static IEnumerable<IToken> EnumerateTokens(string text)
    {
        var regex = GetTagRegex();
        var matches = regex.Matches(text);
        var lastIndex = 0;
        foreach (Match match in matches)
        {
            if (match.Index > lastIndex)
                yield return new TextToken(text.Substring(lastIndex, match.Index - lastIndex));

            yield return new TagToken(match.Groups[1].Value, match.Value);
            lastIndex = match.Index + match.Length;
        }
        
        if (lastIndex < text.Length)
            yield return new TextToken(text.Substring(lastIndex));
    }

    public static IEnumerable<IElement> EnumerateElements(IEnumerable<IToken> tokens)
    {
        var state = new TagParsingState();

        foreach (var token in tokens)
        {
            var element = token switch
            {
                TextToken textToken => new TextElement { Value = textToken.Value },
                TagToken tagToken => GetTagElement(tagToken, state),
                _ => throw new InvalidOperationException($"Unknown token type: {token.Type}")
            };

            if (element is TextElement textElement)
            {
                foreach (var lineElement in SplitTextElementToLines(textElement))
                    yield return lineElement;
            }
            else
            {
                yield return element;
            }
        }
    }

    public static IEnumerable<IEnumerable<IElement>> EnumerateLines(IEnumerable<IElement> elements)
    {
        using var enumerator = elements.GetEnumerator();
        do
        {
            yield return EnumerateUntilLineBreak(enumerator);
        } while (enumerator.MoveNext());
    }

    private static IEnumerable<IElement> EnumerateUntilLineBreak(IEnumerator<IElement> enumerator)
    {
        do
        {
            if (enumerator.Current is LineBreakElement)
                break;
            if (enumerator.Current is not null)
                yield return enumerator.Current;
        } while (enumerator.MoveNext());
    }

    private static IEnumerable<IElement> SplitTextElementToLines(TextElement element)
    {
        var lines = element.Value.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            yield return new TextElement { Value = lines[i] };
            if (i < lines.Length - 1)
                yield return new LineBreakElement();
        }
    }

    private static IElement GetTagElement(TagToken token, TagParsingState state)
    {
        var tagValue = token.Value
            .Trim()
            .ToLowerInvariant();
        
        if (string.IsNullOrWhiteSpace(tagValue))
            return new TextElement { Value = token.OriginalValue };
        
        // Special case for #<hex> tags
        if (tagValue.StartsWith("#"))
        {
            if (!TryParseColor(tagValue, out var rgb))
                return new TextElement { Value = token.OriginalValue };
            var currentColorAlpha = state.ColorStack.Count == 0
                ? default
                : state.ColorStack.Peek();
            var newColorAlpha = currentColorAlpha with { Rgb = rgb };
            state.ColorStack.Push(newColorAlpha);
            return new StyleElement { Color = newColorAlpha };
        }
        
        // Decompose tag
        if (!TryDecomposeTag(tagValue, out var name, out var value))
            return new TextElement { Value = token.OriginalValue };
        
        var (close, nameNormalized) = NormalizeName(name);
        
        // Process line break tags
        if (nameNormalized == "br" && !close)
            return new LineBreakElement();
        
        // Process bold, italic, underline tags
        if (nameNormalized == "b")
        {
            state.BoldNestLevel += close ? -1 : 1;
            return new StyleElement { Bold = state.BoldNestLevel > 0 };
        }
        
        if (nameNormalized == "i")
        {
            state.ItalicNestLevel += close ? -1 : 1;
            return new StyleElement { Italic = state.ItalicNestLevel > 0 };
        }
        
        if (nameNormalized == "u")
        {
            state.UnderlineNestLevel += close ? -1 : 1;
            return new StyleElement { Underline = state.UnderlineNestLevel > 0 };
        }

        // Process color tags
        if (nameNormalized == "color")
        {
            if (close)
            {
                if (state.ColorStack.Count == 0)
                    return new TextElement { Value = token.OriginalValue };
                state.ColorStack.Pop();
                return new StyleElement { Color = state.ColorStack.Count == 0 ? null : state.ColorStack.Peek() };
            }
            
            if (string.IsNullOrWhiteSpace(value))
                return new TextElement { Value = token.OriginalValue };
            
            if (!TryParseColor(value, out var rgb))
                return new TextElement { Value = token.OriginalValue };
            var currentColorAlpha = state.ColorStack.Count == 0
                ? default
                : state.ColorStack.Peek();
            var newColorAlpha = currentColorAlpha with { Rgb = rgb };
            state.ColorStack.Push(newColorAlpha);
            return new StyleElement { Color = newColorAlpha };
        }
        
        // Process alpha tags
        if (nameNormalized == "alpha")
        {
            // Alpha tags can't be closed
            if (close)
                return new TextElement { Value = token.OriginalValue };
            
            if (string.IsNullOrWhiteSpace(value))
                return new TextElement { Value = token.OriginalValue };
            
            if (!TryParseAlpha(value, out var alpha))
                return new TextElement { Value = token.OriginalValue };
            var currentColorAlpha = state.ColorStack.Count == 0
                ? default
                : state.ColorStack.Peek();
            var newColorAlpha = currentColorAlpha with { A = alpha };
            state.ColorStack.Push(newColorAlpha);
            return new StyleElement { Color = newColorAlpha };
        }
        
        // Process cspace tags
        if (nameNormalized == "cspace")
        {
            if (close)
            {
                if (state.CSpaceStack.Count == 0)
                    return new TextElement { Value = token.OriginalValue };
                state.CSpaceStack.Pop();
                return new CSpaceElement { Value = state.CSpaceStack.Count == 0 ? default : state.CSpaceStack.Peek() };
            }
            
            if (string.IsNullOrWhiteSpace(value))
                return new TextElement { Value = token.OriginalValue };
            
            if (!Measurement.TryParse(value, out var cspace))
                return new TextElement { Value = token.OriginalValue };
            
            state.CSpaceStack.Push(cspace);
            return new CSpaceElement { Value = cspace };
        }

        if (nameNormalized == "align")
        {
            if (close)
                return new AlignElement { Alignment = null };

            if (string.IsNullOrWhiteSpace(value))
                return new TextElement { Value = token.OriginalValue };
            
            if (!TryGetAlignment(value, out var alignment))
                return new TextElement { Value = token.OriginalValue };

            return new AlignElement { Alignment = alignment };
        }

        if (name == "pos")
        {
            // Pos tags can't be closed
            if (close)
                return new TextElement { Value = token.OriginalValue };
            
            if (string.IsNullOrWhiteSpace(value))
                return new TextElement { Value = token.OriginalValue };
            
            if (!Measurement.TryParse(value, out var pos))
                return new TextElement { Value = token.OriginalValue };
            
            return new PosElement { Value = pos };
        }
        
        if (name == "size")
        {
            // Size tags can't be closed
            if (close)
                return new TextElement { Value = token.OriginalValue };
            
            if (string.IsNullOrWhiteSpace(value))
                return new TextElement { Value = token.OriginalValue };
            
            if (!Measurement.TryParse(value, out var size))
                return new TextElement { Value = token.OriginalValue };
            
            return new SizeElement { Value = size };
        }

        if (name == "font")
        {
            if (close)
            {
                var oldFont = state.FontStack.Count == 0
                    ? null
                    : state.FontStack.Pop();
                return new FontElement { Value = oldFont };
            }
            
            if (string.IsNullOrWhiteSpace(value))
                return new TextElement { Value = token.OriginalValue };
            
            state.FontStack.Push(value);
            return new FontElement { Value = value };
        }
        
        return new TextElement { Value = token.OriginalValue };
    }

    private static bool TryGetAlignment(string value, out HorizontalAlignment horizontalAlignment)
    {
        switch (value)
        {
            case "left":
                horizontalAlignment = HorizontalAlignment.Left;
                return true;
            case "center":
                horizontalAlignment = HorizontalAlignment.Center;
                return true;
            case "right":
                horizontalAlignment = HorizontalAlignment.Right;
                return true;
            default:
                horizontalAlignment = default;
                return false;
        }
    }
    
    private static (bool Close, string Value) NormalizeName(string name)
    {
        name = name.Trim();
        return name.StartsWith('/') ? (true, name[1..].TrimStart()) : (false, name);
    }

    private static bool TryDecomposeTag(string tag, [MaybeNullWhen(false)] out string name, out string? value)
    {
        tag = tag.Trim();
        
        var equalsSignIndex = tag.IndexOf('=');
        if (equalsSignIndex == -1)
        {
            name = tag;
            value = null;
            return true;
        }
        
        name = tag[..equalsSignIndex].Trim();
        value = tag[(equalsSignIndex + 1)..].Trim();
        
        if (string.IsNullOrWhiteSpace(name))
            return false;
        
        if (value is ['"', .., '"'])
            value = value[1..^1];
        
        return true;
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