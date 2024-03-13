using System.IO;
using ImageProcessing;

namespace DotTraceExamples.Programs;

public class MeanShiftProgram : IProgram
{
	public void Run()
	{
		var fileName = @"TestImages\TestImage.jpg";
		using var fileStream = File.OpenRead(fileName);
		var image = RGBImage.FromStream(fileStream);
		image.MeanShift().SaveToFile(Path.Combine(Directory.GetCurrentDirectory(), "TestImages",
			$"{Path.GetFileNameWithoutExtension(fileName)}_Processed.jpg"));
	}
}