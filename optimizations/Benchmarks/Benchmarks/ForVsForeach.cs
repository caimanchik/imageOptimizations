using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace Benchmarks.Benchmarks;

[DisassemblyDiagnoser]
public class ForVsForeach
{
	private int[] numbersArr;
	private List<int> numbersList;

	[Params(10000000)] public int NumbersCount;

	[GlobalSetup]
	public void Setup()
	{
		numbersArr = new int[NumbersCount];
		numbersList = new List<int>(NumbersCount);
		for (int i = 0; i < NumbersCount; i++)
		{
			numbersArr[i] = 1;
			numbersList.Add(1);
		}
	}

	[Benchmark]
	public void ForList()
	{
		var sum = 0;
		var count = numbersList.Count;
		for (int i = 0; i < count; i++)
		{
			sum += numbersList[i];
		}
	}

	[Benchmark]
	public void ForeachList()
	{
		var sum = 0;
		foreach (var number in numbersList)
		{
			sum += number;
		}
	}

	[Benchmark]
	public void ForArray()
	{
		var sum = 0;
		var length = numbersArr.Length;
		for (int i = 0; i < length; i++)
		{
			sum += numbersList[i];
		}
	}

	[Benchmark]
	public void ForeachArray()
	{
		var sum = 0;
		foreach (var number in numbersArr)
		{
			sum += number;
		}
	}

	[Benchmark]
	public unsafe void UnsafeArraySum()
	{
		var sum = 0;
		var length = numbersArr.Length;
		fixed (int* arrayPtr = numbersArr)
		{
			int* j = arrayPtr;
			for (int i = 0; i < length; i++, j++)
				sum += *j;
		}
	}

	[Benchmark]
	public unsafe void UnsafeArraySum2()
	{
		var sum = 0;
		var length = numbersArr.Length;
		fixed (int* arrayPtr = numbersArr)
		{
			int* j = arrayPtr;
			int* endPrt = j + length;
			while (j != endPrt)
			{
				sum += *j;
				j++;
			}
		}
	}
}