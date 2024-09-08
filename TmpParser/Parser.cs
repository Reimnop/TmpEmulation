using System.Text.RegularExpressions;

namespace TmpParser;

internal static partial class Parser
{
    private static readonly Dictionary<string, TagProcessor> Processors = new()
    {
        ["b"] = TagProcessors.ProcessBold,
        ["i"] = TagProcessors.ProcessItalic,
        ["u"] = TagProcessors.ProcessUnderline,
        ["color"] = TagProcessors.ProcessColor,
        ["alpha"] = TagProcessors.ProcessAlpha,
    };
    
    [GeneratedRegex(@"<(\/?)([^<>\n]+?)>")]
    private static partial Regex GetTagRegex();
    
    public static IEnumerable<Run> Parse(string text)
    {
        var start = 0;
        var style = new Style();
        var state = new TagParsingState();
        var regex = GetTagRegex();
        foreach (Match match in regex.Matches(text))
        {
            var currentStyle = style;
            
            var close = match.Groups[1].Value == "/";
            var tag = match.Groups[2].Value;
            
            if (tag.StartsWith('#'))
                tag = $"color={tag}";
            
            var firstEqual = tag.IndexOf('=');
            var name = firstEqual == -1 ? tag : tag[..firstEqual];
            var value = firstEqual == -1 ? null : tag[(firstEqual + 1)..];
            
            // Skip closing tags with values, as they are invalid
            if (close && !string.IsNullOrEmpty(value))
                continue;
            
            // Remove quotes from values
            if (value is ['"', .., '"'])
                value = value[1..^1];
            
            // Trim and lowercase tag name and value
            name = name.Trim().ToLowerInvariant();
            value = value?.Trim().ToLowerInvariant();
            
            // Skip unknown tags
            if (Processors.TryGetValue(name, out var processor))
            {
                var newStyle = processor(close, value, style, state);
                if (!newStyle.HasValue)
                    continue;
                style = newStyle.Value;
            }
            else
            {
                continue;
            }
            
            // Return previous run
            if (start < match.Index)
                yield return new Run(text[start..match.Index], currentStyle);
            
            start = match.Index + match.Length;
        }
        
        if (start < text.Length)
            yield return new Run(text[start..], style);
    }
}