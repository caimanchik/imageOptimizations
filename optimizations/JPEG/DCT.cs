using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JPEG.Processor;

namespace JPEG;

public class DCT
{
	private static readonly double[,,] BasisHash =
		new double[JpegProcessor.DCTSize, JpegProcessor.DCTSize, JpegProcessor.DCTSize];

	private static readonly double[,] AlphaHash = new double[JpegProcessor.DCTSize, JpegProcessor.DCTSize];

	private static readonly double[,] BetaHash = new double[JpegProcessor.DCTSize, JpegProcessor.DCTSize];

	static DCT()
	{
		for (var i = 0; i < JpegProcessor.DCTSize; i++)
		for (var j = 0; j < JpegProcessor.DCTSize; j++)
		for (var k = 1; k < JpegProcessor.DCTSize + 1; k++)
			BasisHash[i,j,k-1] = Math.Cos(((2d * i + 1d) * j * Math.PI) / (2 * k));
		
		for (var i = 0; i < JpegProcessor.DCTSize; i++)
		for (var j = 0; j < JpegProcessor.DCTSize; j++)
			AlphaHash[i, j] = Alpha(i) * Alpha(j);
		
		for (var i = 1; i < JpegProcessor.DCTSize + 1; i++)
		for (var j = 1; j < JpegProcessor.DCTSize + 1; j++)
			BetaHash[i - 1, j - 1] = Beta(i, j);
	}
	
	public static void DCT2D(double[,] input, double[,] coeffs, int height, int width)
	{
		for (var u = 0; u < height; u++)
		for (var v = 0; v < height; v++)
		{
			var sum = 0.0;
			
			for (var x = 0; x < width; x++)
			for (var y = 0; y < height; y++)
				sum += BasisHash[x, u, width - 1] * BasisHash[y, v, height - 1] * input[x, y];
		
			coeffs[u, v] = sum * BetaHash[height - 1, width - 1] * AlphaHash[u, v];
		}
	}

	public static void IDCT2D(double[,] coeffs, double[,] output)
	{
		var height = coeffs.GetLength(0);
		var width = coeffs.GetLength(1);

		Parallel.For(0, width, x =>
		{
			for (var y = 0; y < height; y++)
			{
				var sum = 0.0;
				
				for (var u = 0; u < width; u++)
				for (var v = 0; v < height; v++)
					sum += BasisHash[x, u, width - 1] * BasisHash[y, v, height - 1] * coeffs[u, v] * AlphaHash[u, v];

				output[x, y] = sum * BetaHash[height - 1, width - 1];
			}
		});
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static double Alpha(int u)
	{
		if (u == 0)
			return 1 / Math.Sqrt(2);
		return 1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static double Beta(int height, int width)
	{
		return 1d / width + 1d / height;
	}
}