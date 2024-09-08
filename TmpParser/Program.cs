using System.Web;
using TmpParser;

Console.WriteLine("<p>");
foreach (var run in Parser.Parse("<b>bold <b><u>bold underline (inner)</b> bold underline (outer)</b> underline</u>"))
{
    var (bold, italic, underline, (rgb, a)) = run.Style;
    var styleString = $"font-weight: {(bold ? "bold" : "normal")}; " +
                      $"font-style: {(italic ? "italic" : "normal")}; " +
                      $"text-decoration: {(underline ? "underline" : "none")}; " +
                      $"color: {rgb?.ToString() ?? "inherit"}; " +
                      $"opacity: {a?.ToPercent() ?? "inherit"};";
    Console.WriteLine($"  <span style=\"{styleString}\">{HttpUtility.HtmlEncode(run.Text)}</span>");
}
Console.WriteLine("</p>");

public static class IntExtensions
{
    public static string ToPercent(this int value) => $"{value / 255.0}";
}