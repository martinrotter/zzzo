using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ZZZO
{
  internal static class ColorImageSearcher
  {
    #region Metody

    public static PixelColor[,] CopyPixels(BitmapSource source)
    {
      if (source.Format != PixelFormats.Bgra32)
      {
        source = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
      }

      PixelColor[,] pixels = new PixelColor[source.PixelWidth, source.PixelHeight];
      int stride = source.PixelWidth * ((source.Format.BitsPerPixel + 7) / 8);
      GCHandle pinnedPixels = GCHandle.Alloc(pixels, GCHandleType.Pinned);

      source.CopyPixels(
        new Int32Rect(0, 0, source.PixelWidth, source.PixelHeight),
        pinnedPixels.AddrOfPinnedObject(),
        pixels.GetLength(0) * pixels.GetLength(1) * 4,
        stride);

      pinnedPixels.Free();
      return pixels;
    }

    public static IEnumerable<Color> Get2MostUsedColors(BitmapImage bitMap)
    {
      Dictionary<PixelColor, int> colorIncidence = new Dictionary<PixelColor, int>();
      PixelColor[,] pxls = CopyPixels(bitMap);

      const int groupization = 25;

      foreach (PixelColor pxl in pxls)
      {
        PixelColor pxla = new PixelColor
        {
          Alpha = pxl.Alpha,
          Blue = (byte)(Math.Floor(pxl.Blue / (double)groupization) * groupization),
          Green = (byte)(Math.Floor(pxl.Green / (double)groupization) * groupization),
          Red = (byte)(Math.Floor(pxl.Red / (double)groupization) * groupization),
        };

        if (colorIncidence.Keys.Contains(pxla))
        {
          colorIncidence[pxla]++;
        }
        else
        {
          colorIncidence.Add(pxla, 1);
        }
      }

      IEnumerable<Color> notWhiteish = colorIncidence
        .OrderByDescending(x => x.Value)
        .Where(x => x.Key.Blue < 200 || x.Key.Red < 200 || x.Key.Green < 200)
        .Take(2)
        .Select(x => Color.FromRgb(x.Key.Red, x.Key.Green, x.Key.Blue));

      return notWhiteish;
    }

    #endregion

    #region Vnořené typy

    [StructLayout(LayoutKind.Sequential)]
    public struct PixelColor : IEquatable<PixelColor>
    {
      public byte Blue;
      public byte Green;
      public byte Red;
      public byte Alpha;

      public bool Equals(PixelColor other)
      {
        return Blue == other.Blue && Green == other.Green && Red == other.Red && Alpha == other.Alpha;
      }

      public override bool Equals(object obj)
      {
        return obj is PixelColor other && Equals(other);
      }

      public override int GetHashCode()
      {
        return HashCode.Combine(Blue, Green, Red, Alpha);
      }

      public static bool operator ==(PixelColor left, PixelColor right)
      {
        return left.Equals(right);
      }

      public static bool operator !=(PixelColor left, PixelColor right)
      {
        return !left.Equals(right);
      }
    }

    #endregion
  }
}