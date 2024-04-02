using System.Collections.Generic;

namespace JPEG.Codecs;

class BitsBuffer
{
    private List<byte> buffer = new();
    private BitsWithLength unfinishedBits = new();

    public void Add(BitsWithLength bitsWithLength)
    {
        var bitsCount = bitsWithLength.BitsCount;
        var bits = bitsWithLength.Bits;

        int neededBits = 8 - unfinishedBits.BitsCount;
        while (bitsCount >= neededBits)
        {
            bitsCount -= neededBits;
            buffer.Add((byte)((unfinishedBits.Bits << neededBits) + (bits >> bitsCount)));

            bits = bits & ((1 << bitsCount) - 1);

            unfinishedBits.Bits = 0;
            unfinishedBits.BitsCount = 0;

            neededBits = 8;
        }

        unfinishedBits.BitsCount += bitsCount;
        unfinishedBits.Bits = (unfinishedBits.Bits << bitsCount) + bits;
    }

    public byte[] ToArray(out long bitsCount)
    {
        bitsCount = buffer.Count * 8L + unfinishedBits.BitsCount;
        var result = new byte[bitsCount / 8 + (bitsCount % 8 > 0 ? 1 : 0)];
        buffer.CopyTo(result);
        if (unfinishedBits.BitsCount > 0)
            result[buffer.Count] = (byte)(unfinishedBits.Bits << (8 - unfinishedBits.BitsCount));
        return result;
    }
}