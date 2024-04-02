using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using JPEG.Codecs;
using JPEG.Compressors;
using JPEG.Images;
using static System.Drawing.Image;

namespace JPEG.Processor;

public class JpegProcessor : IJpegProcessor
{
	public static readonly JpegProcessor Init = new();
	public const int CompressionQuality = 70;
	public const int DctSize = 8;

	private static int[,] _quantizationMatrix8X8Q70;

	private JpegProcessor()
	{
		_quantizationMatrix8X8Q70 = GetQuantizationMatrix(70);
	}
	
	public async void Compress(string imagePath, string compressedImagePath)
	{
		await using var fileStream = File.OpenRead(imagePath);
#pragma warning disable CA1416
		using var bmp = (Bitmap)FromStream(fileStream, false, false);
#pragma warning restore CA1416
		var compressionResult = Compress(bmp, CompressionQuality);
		compressionResult.Save(compressedImagePath);
	}
	
	public void Uncompress(string compressedImagePath, string uncompressedImagePath)
	{
		var compressedImage = CompressedImage.Load(compressedImagePath);
		var result = Uncompress(compressedImage);
#pragma warning disable CA1416
		result.Save(uncompressedImagePath, ImageFormat.Bmp);
#pragma warning restore CA1416
	}
	
	private static CompressedImage Compress(Bitmap bitmap, int quality = 50)
	{
		var worker = new MatrixWorker(bitmap);
		var allQuantizedBytes = new byte[worker.Width * worker.Height * 3];
		
		Parallel.For(0, worker.Height / 8, y =>
		{
			var yBuffer1 = new double[DctSize, DctSize];
			var yBuffer2 = new double[DctSize, DctSize];
			var cbBuffer1 = new double[DctSize, DctSize];
			var cbBuffer2 = new double[DctSize, DctSize];
			var crBuffer1 = new double[DctSize, DctSize];
			var crBuffer2 = new double[DctSize, DctSize];
			
			for (var x = 0; x < worker.Width; x += DctSize)
			{
				worker.Read(y * DctSize, x, yBuffer1, cbBuffer1, crBuffer1, out var readCountX, out var readCountY);

				var yTask = Compress(quality, yBuffer1, yBuffer2, readCountY, readCountX);
				var cbTask = Compress(quality, cbBuffer1, cbBuffer2, readCountY, readCountX);
				var crTask = Compress(quality, crBuffer1, crBuffer2, readCountY, readCountX);

				var start = x * DctSize * 3 + worker.Width * DctSize * 3 * y;

				for (var i = 0; i < DctSize * DctSize; i++)
					allQuantizedBytes[start + i] = yTask[i];

				start += DctSize * DctSize;
				
				for (var i = 0; i < DctSize * DctSize; i++)
					allQuantizedBytes[start + i] = cbTask[i];
				
				start += DctSize * DctSize;
				
				for (var i = 0; i < DctSize * DctSize; i++)
					allQuantizedBytes[start + i] = crTask[i];
			}
		});
		
		worker.UnlockBits();

		var compressedBytes = HuffmanCodec.Encode(allQuantizedBytes, out var decodeTable, out var bitsCount);

		return new CompressedImage
		{
			Quality = quality, CompressedBytes = compressedBytes, BitsCount = bitsCount, DecodeTable = decodeTable,
			Height = worker.Height, Width = worker.Width
		};
	}
	
	private static byte[] Compress(int quality, double[,] buffer, double[,] buffer2, int height, int width)
	{
		ShiftMatrixValues(buffer, -128, height, width);
		DCT.DCT2D(buffer, buffer2, height, width);
		Quantize(buffer2, quality, height, width);
		return ZigZagScan(buffer2);
	}
	
	private static Bitmap Uncompress(CompressedImage image)
	{
		var worker = new MatrixWorker(image.Height, image.Width);

		var allQuantizedBytes = HuffmanCodec.Decode(image.CompressedBytes, image.DecodeTable, image.BitsCount);

		Parallel.For(0, image.Height / 8, y =>
		{
			var yBuffer2 = new double[DctSize, DctSize];
			var yBuffer3 = new double[DctSize, DctSize];
			var cbBuffer2 = new double[DctSize, DctSize];
			var cbBuffer3 = new double[DctSize, DctSize];
			var crBuffer2 = new double[DctSize, DctSize];
			var crBuffer3 = new double[DctSize, DctSize];
			
			for (var x = 0; x < image.Width; x += DctSize)
			{
				var start = x * DctSize * 3 + worker.Width * DctSize * 3 * y;
				Uncompress(allQuantizedBytes.AsSpan(start, DctSize * DctSize), yBuffer2, yBuffer3, image.Quality);
				
				start += DctSize * DctSize;
				Uncompress(allQuantizedBytes.AsSpan(start, DctSize * DctSize), cbBuffer2, cbBuffer3, image.Quality);
				
				start += DctSize * DctSize;
				Uncompress(allQuantizedBytes.AsSpan(start, DctSize * DctSize), crBuffer2, crBuffer3, image.Quality);
				
				worker.Write(yBuffer3, cbBuffer3, crBuffer3, y * DctSize, x, 8, 8);
			}
		});
		
		worker.UnlockBits();

		return worker.Bitmap;
	}

	private static void Uncompress(Span<byte> input, double[,] buffer2, double[,] buffer3, int quality)
	{
		var zagUnScan = ZigZagUnScan(input);
		DeQuantize(zagUnScan, quality, buffer2);
		DCT.IDCT2D(buffer2, buffer3);
		ShiftMatrixValues(buffer3, 128);
	}
	
	private static void ShiftMatrixValues(double[,] subMatrix, int shiftValue, int height, int width)
	{
		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
			subMatrix[y, x] += shiftValue;
	}
	
	private static void Quantize(double[,] channelFreqs, int quality, int height, int width)
	{
		var quantMatrix = quality == 70 ? _quantizationMatrix8X8Q70 : GetQuantizationMatrix(quality);
		
		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
			channelFreqs[y, x] /= quantMatrix[y, x];
	}
	
	private static void DeQuantize(byte[,] quantizedBytes, int quality, double[,] result)
	{
		var quantMatrix = quality == 70 ? _quantizationMatrix8X8Q70 : GetQuantizationMatrix(quality);
		
		for (var y = 0; y < quantizedBytes.GetLength(0); y++)
		for (var x = 0; x < quantizedBytes.GetLength(1); x++)
			result[y, x] =
				((sbyte)quantizedBytes[y, x]) *
				quantMatrix[y, x]; //NOTE cast to sbyte not to loose negative numbers
	}
	
	private static int[,] GetQuantizationMatrix(int quality)
	{
		if (quality is < 1 or > 99)
			throw new ArgumentException("quality must be in [1,99] interval");

		var multiplier = quality < 50 ? 5000 / quality : 200 - 2 * quality;

		var result = new[,]
		{
			{ 16, 11, 10, 16, 24, 40, 51, 61 },
			{ 12, 12, 14, 19, 26, 58, 60, 55 },
			{ 14, 13, 16, 24, 40, 57, 69, 56 },
			{ 14, 17, 22, 29, 51, 87, 80, 62 },
			{ 18, 22, 37, 56, 68, 109, 103, 77 },
			{ 24, 35, 55, 64, 81, 104, 113, 92 },
			{ 49, 64, 78, 87, 103, 121, 120, 101 },
			{ 72, 92, 95, 98, 112, 100, 103, 99 }
		};

		for (var y = 0; y < result.GetLength(0); y++)
		for (var x = 0; x < result.GetLength(1); x++)
			result[y, x] = (multiplier * result[y, x] + 50) / 100;

		return result;
	}

	private static void ShiftMatrixValues(double[,] subMatrix, int shiftValue)
	{
		var height = subMatrix.GetLength(0);
		var width = subMatrix.GetLength(1);

		for (var y = 0; y < height; y++)
		for (var x = 0; x < width; x++)
			subMatrix[y, x] += shiftValue;
	}

	private static byte[] ZigZagScan(double[,] channelFreqs)
	{
		return new[]
		{
			(byte)channelFreqs[0, 0], (byte)channelFreqs[0, 1], (byte)channelFreqs[1, 0], (byte)channelFreqs[2, 0], (byte)channelFreqs[1, 1],
			(byte)channelFreqs[0, 2], (byte)channelFreqs[0, 3], (byte)channelFreqs[1, 2],
			(byte)channelFreqs[2, 1], (byte)channelFreqs[3, 0], (byte)channelFreqs[4, 0], (byte)channelFreqs[3, 1], (byte)channelFreqs[2, 2],
			(byte)channelFreqs[1, 3], (byte)channelFreqs[0, 4], (byte)channelFreqs[0, 5],
			(byte)channelFreqs[1, 4], (byte)channelFreqs[2, 3], (byte)channelFreqs[3, 2], (byte)channelFreqs[4, 1], (byte)channelFreqs[5, 0],
			(byte)channelFreqs[6, 0], (byte)channelFreqs[5, 1], (byte)channelFreqs[4, 2],
			(byte)channelFreqs[3, 3], (byte)channelFreqs[2, 4], (byte)channelFreqs[1, 5], (byte)channelFreqs[0, 6], (byte)channelFreqs[0, 7],
			(byte)channelFreqs[1, 6], (byte)channelFreqs[2, 5], (byte)channelFreqs[3, 4],
			(byte)channelFreqs[4, 3], (byte)channelFreqs[5, 2], (byte)channelFreqs[6, 1], (byte)channelFreqs[7, 0], (byte)channelFreqs[7, 1],
			(byte)channelFreqs[6, 2], (byte)channelFreqs[5, 3], (byte)channelFreqs[4, 4],
			(byte)channelFreqs[3, 5], (byte)channelFreqs[2, 6], (byte)channelFreqs[1, 7], (byte)channelFreqs[2, 7], (byte)channelFreqs[3, 6],
			(byte)channelFreqs[4, 5], (byte)channelFreqs[5, 4], (byte)channelFreqs[6, 3],
			(byte)channelFreqs[7, 2], (byte)channelFreqs[7, 3], (byte)channelFreqs[6, 4], (byte)channelFreqs[5, 5], (byte)channelFreqs[4, 6],
			(byte)channelFreqs[3, 7], (byte)channelFreqs[4, 7], (byte)channelFreqs[5, 6],
			(byte)channelFreqs[6, 5], (byte)channelFreqs[7, 4], (byte)channelFreqs[7, 5], (byte)channelFreqs[6, 6], (byte)channelFreqs[5, 7],
			(byte)channelFreqs[6, 7], (byte)channelFreqs[7, 6], (byte)channelFreqs[7, 7]
		};
	}

	private static byte[,] ZigZagUnScan(Span<byte> quantizedBytes)
	{
		return new[,]
		{
			{
				quantizedBytes[0], quantizedBytes[1], quantizedBytes[5], quantizedBytes[6], quantizedBytes[14],
				quantizedBytes[15], quantizedBytes[27], quantizedBytes[28]
			},
			{
				quantizedBytes[2], quantizedBytes[4], quantizedBytes[7], quantizedBytes[13], quantizedBytes[16],
				quantizedBytes[26], quantizedBytes[29], quantizedBytes[42]
			},
			{
				quantizedBytes[3], quantizedBytes[8], quantizedBytes[12], quantizedBytes[17], quantizedBytes[25],
				quantizedBytes[30], quantizedBytes[41], quantizedBytes[43]
			},
			{
				quantizedBytes[9], quantizedBytes[11], quantizedBytes[18], quantizedBytes[24], quantizedBytes[31],
				quantizedBytes[40], quantizedBytes[44], quantizedBytes[53]
			},
			{
				quantizedBytes[10], quantizedBytes[19], quantizedBytes[23], quantizedBytes[32], quantizedBytes[39],
				quantizedBytes[45], quantizedBytes[52], quantizedBytes[54]
			},
			{
				quantizedBytes[20], quantizedBytes[22], quantizedBytes[33], quantizedBytes[38], quantizedBytes[46],
				quantizedBytes[51], quantizedBytes[55], quantizedBytes[60]
			},
			{
				quantizedBytes[21], quantizedBytes[34], quantizedBytes[37], quantizedBytes[47], quantizedBytes[50],
				quantizedBytes[56], quantizedBytes[59], quantizedBytes[61]
			},
			{
				quantizedBytes[35], quantizedBytes[36], quantizedBytes[48], quantizedBytes[49], quantizedBytes[57],
				quantizedBytes[58], quantizedBytes[62], quantizedBytes[63]
			}
		};
	}
}