using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace JPEG.Images;

public class MatrixWorker
{
    public readonly int Width;
    public readonly int Height;
    public readonly WorkerMode WorkerMode;
    public readonly Bitmap Bitmap;
    private readonly BitmapData Data;
    
#pragma warning disable CA1416
    public MatrixWorker(Bitmap bitmap)
    {
        WorkerMode = WorkerMode.Read;
        Bitmap = bitmap;
        

        Width = bitmap.Width - bitmap.Width % 8;

        Height = bitmap.Height - bitmap.Height % 8;

        Data = bitmap.LockBits(new Rectangle(new Point(), new Size(bitmap.Width, bitmap.Height)),
            ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format24bppRgb);


    }

    public MatrixWorker(int height, int width)
    {
        WorkerMode = WorkerMode.Write;
        Bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

        Width = width;
        Height = height;

        Data = Bitmap.LockBits(new Rectangle(new Point(), new Size(Width, Height)),
            ImageLockMode.WriteOnly,
            System.Drawing.Imaging.PixelFormat.Format24bppRgb);
    }
    
    public unsafe void Read(int j, int i, double[,] y, double[,] cb, double[,] cr, out int readCountX, out int readCountY)
    {
        var ptr = (byte*)Data.Scan0;
        var endJ = Math.Min(j + 8, Height);
        var endI = Math.Min(i + 8, Width);

        readCountX = 0;
        readCountY = 0;

        double r;
        double g;
        double b;
        
        for (var h = j; h < endJ; h++)
        {
            var pos = ptr + h * Width * 3 + i * 3;
            readCountX = 0;

            for (var w = i; w < endI; w++)
            {
                b = *(pos++);
                g = *(pos++);
                r = *(pos++);

                y[readCountY, readCountX] = 16.0 + (65.738 * r + 129.057 * g + 24.064 * b) / 256.0;
                cb[readCountY, readCountX] = 128.0 + (-37.945 * r - 74.494 * g + 112.439 * b) / 256.0;
                cr[readCountY, readCountX] = 128.0 + (112.439 * r - 94.154 * g - 18.285 * b) / 256.0;
                
                readCountX++;
            }
            
            readCountY++;
        }
    }

    public unsafe void Write(double[,] Y, double[,] cb, double[,] cr, int j, int i, int countY, int countX)
    {
        var ptr = (byte*)Data.Scan0;

        for (var y = 0; y < countY; y++)
        {
            var pos = ptr + (j + y) * Width * 3 + i * 3;
            for (var x = 0; x < countX; x++, pos += 3)
            {
                *pos = ToByte((298.082 * Y[y, x] + 516.412 * cb[y, x]) / 256.0 - 276.836);
                *(pos + 1) = ToByte((298.082 * Y[y, x] - 100.291 * cb[y, x] - 208.120 * cr[y, x]) / 256.0 + 135.576);
                *(pos + 2) = ToByte((298.082 * Y[y, x] + 408.583 * cr[y, x]) / 256.0 - 222.921);
            }
        }
    }

    public void UnlockBits()
    {
        Bitmap.UnlockBits(Data);
    }

    private static byte ToByte(double d)
    {
        var val = (int)d;
        
        return val switch
        {
            > byte.MaxValue => byte.MaxValue,
            < byte.MinValue => byte.MinValue,
            _ => (byte)val
        };
    }
}