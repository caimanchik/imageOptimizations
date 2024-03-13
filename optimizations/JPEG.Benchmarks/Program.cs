using BenchmarkDotNet.Running;

namespace JPEG.Benchmarks;

internal class Program
{
	public static void Main(string[] args)
	{
		BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
	}
}