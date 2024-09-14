namespace TmpParser;

public class PosElement : IElement
{
    public float Value { get; set; }
    
    public override string ToString()
        => $"PosElement({Value})";
}