using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace JPEG.Images;

public class MatrixWorker
{
    public readonly int Width;
    public readonly int Height;
    public readonly WorkerMode WorkerMode;
    private readonly Bitmap Bitmap;
    private readonly BitmapData Data;
    
    public MatrixWorker(Bitmap bitmap)
    {
        WorkerMode = WorkerMode.Read;
        Bitmap = bitmap;

        
        // todo
        Width = bitmap.Width - bitmap.Width % 8;
        Height = bitmap.Height - bitmap.Height % 8;
        
        Data = bitmap.LockBits(new Rectangle(new Point(), new Size(bitmap.Width, bitmap.Height)),
            ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format24bppRgb);
    }
    
    public unsafe void Read(int j, int i, double[,] Y, double[,] Cb, double[,] Cr, out int readCountX, out int readCountY)
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

                Y[readCountY, readCountX] = 16.0 + (65.738 * r + 129.057 * g + 24.064 * b) / 256.0;
                Cb[readCountY, readCountX] = 128.0 + (-37.945 * r - 74.494 * g + 112.439 * b) / 256.0;
                Cr[readCountY, readCountX] = 128.0 + (112.439 * r - 94.154 * g - 18.285 * b) / 256.0;
                
                readCountX++;
            }
            
            readCountY++;
        }
    }

    public void UnlockBits()
    {
        Bitmap.UnlockBits(Data);
    }
}