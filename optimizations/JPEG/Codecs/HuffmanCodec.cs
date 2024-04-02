using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JPEG.Codecs;

class HuffmanCodec
{
	public static byte[] Encode(IEnumerable<byte> data, out Dictionary<BitsWithLength, byte> decodeTable,
		out long bitsCount)
	{
		var frequences = CalcFrequences(data);

		var root = BuildHuffmanTree(frequences);

		var encodeTable = new BitsWithLength[byte.MaxValue + 1];
		FillEncodeTable(root, encodeTable);

		var bitsBuffer = new BitsBuffer();
		foreach (var b in data)
			bitsBuffer.Add(encodeTable[b]);

		decodeTable = CreateDecodeTable(encodeTable);

		return bitsBuffer.ToArray(out bitsCount);
	}

	public static byte[] Decode(byte[] encodedData, Dictionary<BitsWithLength, byte> decodeTable, long bitsCount)
	{
		var result = new List<byte>();

		byte decodedByte;
		var sample = new BitsWithLength { Bits = 0, BitsCount = 0 };
		for (var byteNum = 0; byteNum < encodedData.Length; byteNum++)
		{
			var b = encodedData[byteNum];
			for (var bitNum = 0; bitNum < 8 && byteNum * 8 + bitNum < bitsCount; bitNum++)
			{
				sample.Bits = (sample.Bits << 1) + ((b & (1 << (8 - bitNum - 1))) != 0 ? 1 : 0);
				sample.BitsCount++;

				if (decodeTable.TryGetValue(sample, out decodedByte))
				{
					result.Add(decodedByte);

					sample.BitsCount = 0;
					sample.Bits = 0;
				}
			}
		}

		return result.ToArray();
	}

	private static Dictionary<BitsWithLength, byte> CreateDecodeTable(BitsWithLength[] encodeTable)
	{
		var result = new Dictionary<BitsWithLength, byte>(new BitsWithLength.Comparer());
		for (int b = 0; b < encodeTable.Length; b++)
		{
			var bitsWithLength = encodeTable[b];
			if (bitsWithLength == null)
				continue;

			result[bitsWithLength] = (byte)b;
		}

		return result;
	}

	private static void FillEncodeTable(HuffmanNode node, BitsWithLength[] encodeSubstitutionTable,
		int bitvector = 0, int depth = 0)
	{
		if (node.LeafLabel != null)
			encodeSubstitutionTable[node.LeafLabel.Value] =
				new BitsWithLength { Bits = bitvector, BitsCount = depth };
		else
		{
			if (node.Left != null)
			{
				FillEncodeTable(node.Left, encodeSubstitutionTable, (bitvector << 1) + 1, depth + 1);
				FillEncodeTable(node.Right, encodeSubstitutionTable, (bitvector << 1) + 0, depth + 1);
			}
		}
	}

	private static HuffmanNode BuildHuffmanTree(int[] frequences)
	{
		var queue = GetPriorityQueue(frequences);

		while (queue.Count > 1)
		{
			var left = queue.Dequeue();
			var right = queue.Dequeue();

			var parent = new HuffmanNode
			{
				Frequency = left.Frequency + right.Frequency,
				Left = left,
				Right = right,
			};
			
			queue.Enqueue(parent, parent.Frequency);
		}

		return queue.Dequeue();
	}
	
	private static PriorityQueue<HuffmanNode, int> GetPriorityQueue(int[] frequences)
	{
		var queue = new PriorityQueue<HuffmanNode, int>();

		for (var i = 0; i < frequences.Length; i++)
		{
			var node = new HuffmanNode { Frequency = frequences[i], LeafLabel = (byte)i };
			queue.Enqueue(node, node.Frequency);
		}

		return queue;
	}

	private static HuffmanNode[] GetNodes(int[] frequences)
	{
		return Enumerable.Range(0, byte.MaxValue + 1)
			.Select(num => new HuffmanNode { Frequency = frequences[num], LeafLabel = (byte)num })
			.Where(node => node.Frequency > 0)
			.ToArray();
	}

	private static int[] CalcFrequences(IEnumerable<byte> data)
	{
		var result = new int[byte.MaxValue + 1];
		Parallel.ForEach(data, b => Interlocked.Increment(ref result[b]));
		return result;
	}
}