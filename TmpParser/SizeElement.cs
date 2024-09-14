namespace TmpParser;

public class SizeElement : IElement
{
    public float Value { get; set; }
    
    public override string ToString()
        => $"SizeElement({Value})";
}