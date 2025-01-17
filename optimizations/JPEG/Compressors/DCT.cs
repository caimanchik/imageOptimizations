﻿using System;
using System.Runtime.CompilerServices;
using JPEG.Processor;

namespace JPEG.Compressors;

public class DCT
{
	private static readonly double[,,] BasisHash =
		new double[JpegProcessor.DctSize, JpegProcessor.DctSize, JpegProcessor.DctSize];

	private static readonly double[,] AlphaHash = new double[JpegProcessor.DctSize, JpegProcessor.DctSize];

	private static readonly double[,] BetaHash = new double[JpegProcessor.DctSize, JpegProcessor.DctSize];

	private static readonly double Beta8x8;

	static DCT()
	{
		for (var i = 0; i < JpegProcessor.DctSize; i++)
		for (var j = 0; j < JpegProcessor.DctSize; j++)
		for (var k = 1; k < JpegProcessor.DctSize + 1; k++)
			BasisHash[i,j,k-1] = Math.Cos(((2d * i + 1d) * j * Math.PI) / (2 * k));
		
		for (var i = 0; i < JpegProcessor.DctSize; i++)
		for (var j = 0; j < JpegProcessor.DctSize; j++)
			AlphaHash[i, j] = Alpha(i) * Alpha(j);
		
		for (var i = 1; i < JpegProcessor.DctSize + 1; i++)
		for (var j = 1; j < JpegProcessor.DctSize + 1; j++)
			BetaHash[i - 1, j - 1] = Beta(i, j);

		Beta8x8 = Beta(8, 8);
	}
	
	public static void DCT2D(double[,] input, double[,] coeffs, int height, int width)
	{
		for (var u = 0; u < width; u++)
		for (var v = 0; v < height; v++)
		{
			var sum = 0.0;
			
			for (var x = 0; x < width; x++)
			for (var y = 0; y < height; y++)
				sum += BasisHash[x, u, width - 1] * BasisHash[y, v, height - 1] * input[x, y];
		
			coeffs[u, v] = sum * Beta8x8 * AlphaHash[u, v];
		}
	}

	public static void IDCT2D(double[,] coeffs, double[,] output)
	{
		var height = coeffs.GetLength(0);
		var width = coeffs.GetLength(1);

		for (var x = 0; x < width; x++)
		for (var y = 0; y < height; y++)
		{
			var sum = 0.0;
				
			for (var u = 0; u < width; u++)
			for (var v = 0; v < height; v++)
				sum += BasisHash[x, u, width - 1] * BasisHash[y, v, height - 1] * coeffs[u, v] * AlphaHash[u, v];

			output[x, y] = sum * Beta8x8;
		}
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