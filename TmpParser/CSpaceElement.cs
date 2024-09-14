namespace TmpParser;

public class CSpaceElement : IElement
{
    public float CSpace { get; set; }
    
    public override string ToString()
        => $"CSpaceElement({CSpace})";
}