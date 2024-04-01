using System.Drawing;
using System.Drawing.Imaging;

namespace JPEG.Images;

class Matrix
{
  public readonly Pixel[,] Pixels;
  public readonly int Height;
  public readonly int Width;

  public Matrix(int height, int width)
  {
    Height = height;
    Width = width;

    Pixels = new Pixel[height, width];
  }

    public unsafe static Matrix FromBitmap(Bitmap bmp)
    {
        var height = bmp.Height - bmp.Height % 8;
        var width = bmp.Width - bmp.Width % 8;
        var matrix = new Matrix(height, width);

        var data = bmp.LockBits(new Rectangle(new Point(), new Size(bmp.Width, bmp.Height)),
                                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                                System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        var ptr = (byte*)data.Scan0.ToPointer();

        fixed (Pixel* pixelMatrixPtr = matrix.Pixels)
        {
            for (var j = 0; j < height; j++)
                for (var i = 0; i < width; i++, ptr += 3)
                {
                    var index = j * width + i;
                    
                    *(pixelMatrixPtr + index) = new Pixel(*(ptr + 2), *(ptr + 1), *ptr, PixelFormat.RGB);
                }
        }
        return matrix;
    }

    public unsafe Bitmap ToBitmap()
    {
        var bitmap = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

        var data = bitmap.LockBits(new Rectangle(new Point(), new Size(Width, Height)),
                                   System.Drawing.Imaging.ImageLockMode.WriteOnly,
                                   System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        var ptr = (byte*)data.Scan0.ToPointer();

        for (var j = 0; j < Height; j++)
            for (var i = 0; i < Width; i++, ptr += 3)
            {
                var pixel = Pixels[j, i];
                *ptr = ToByte(pixel.B);
                *(ptr + 1) = ToByte(pixel.G);
                *(ptr + 2) = ToByte(pixel.R);
            }

        bitmap.UnlockBits(data);
        return bitmap;
    }

  public static byte ToByte(double d)
  {
    var val = (int)d;
    if (val > byte.MaxValue)
      return byte.MaxValue;
    if (val < byte.MinValue)
      return byte.MinValue;
    return (byte)val;
  }
}