using System.Drawing;
using System.Drawing.Imaging;
using UltimaAPI.Models;

namespace UltimaAPI.Services;

internal static class MediaEncoder
{
    public static MediaAsset ToPng(Bitmap source, string fileName)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1))
        {
            throw new PlatformNotSupportedException("Image encoding requires Windows 7 or later.");
        }

        using Bitmap compatible = new(source.Width, source.Height, PixelFormat.Format32bppArgb);
        using (Graphics graphics = Graphics.FromImage(compatible))
        {
            graphics.Clear(Color.Transparent);
            graphics.DrawImageUnscaled(source, 0, 0);
        }

        using MemoryStream stream = new();
        compatible.Save(stream, ImageFormat.Png);
        return new MediaAsset(stream.ToArray(), "image/png", fileName);
    }
}
