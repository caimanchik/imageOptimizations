using System;

namespace JPEG.Codecs;

public class HuffmanNode : IComparable<HuffmanNode>
{
    public byte? LeafLabel { get; set; }
    public int Frequency { get; set; }
    public HuffmanNode Left { get; set; }
    public HuffmanNode Right { get; set; }
    public int CompareTo(HuffmanNode other)
    {
        return Frequency.CompareTo(other.Frequency);
    }
}