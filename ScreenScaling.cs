using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows;
using System.Windows.Media;
using DrawingBitmap = System.Drawing.Bitmap;
using DrawingPoint = System.Drawing.Point;

namespace AutoTransfer;

internal readonly record struct ScreenScale(double ScaleX, double ScaleY)
{
    public static ScreenScale Identity => new(1.0, 1.0);

    public DrawingPoint LogicalToPhysical(DrawingPoint logicalPoint)
    {
        var x = (int)Math.Round(logicalPoint.X * ScaleX);
        var y = (int)Math.Round(logicalPoint.Y * ScaleY);
        return new DrawingPoint(x, y);
    }
}

internal static class ScreenCaptureService
{
    public static CaptureResult CaptureLogical(Window referenceWindow)
    {
        using var physicalCapture = KAutoHelper.CaptureHelper.CaptureScreen();
        var scale = GetCurrentScale(referenceWindow);
        var logicalCapture = NormalizeToLogical(new DrawingBitmap(physicalCapture), scale);
        return new CaptureResult(logicalCapture, scale);
    }

    private static ScreenScale GetCurrentScale(Window referenceWindow)
    {
        try
        {
            var dpi = VisualTreeHelper.GetDpi(referenceWindow);
            var scaleX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
            var scaleY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;
            return new ScreenScale(scaleX, scaleY);
        }
        catch
        {
            return ScreenScale.Identity;
        }
    }

    private static DrawingBitmap NormalizeToLogical(DrawingBitmap physicalBitmap, ScreenScale scale)
    {
        if (Math.Abs(scale.ScaleX - 1.0) < 0.0001 && Math.Abs(scale.ScaleY - 1.0) < 0.0001)
        {
            physicalBitmap.SetResolution(96, 96);
            return physicalBitmap;
        }

        var logicalWidth = Math.Max(1, (int)Math.Round(physicalBitmap.Width / scale.ScaleX));
        var logicalHeight = Math.Max(1, (int)Math.Round(physicalBitmap.Height / scale.ScaleY));

        var logicalBitmap = new DrawingBitmap(logicalWidth, logicalHeight);
        logicalBitmap.SetResolution(96, 96);

        using var graphics = Graphics.FromImage(logicalBitmap);
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.DrawImage(
            physicalBitmap,
            new Rectangle(0, 0, logicalWidth, logicalHeight),
            new Rectangle(0, 0, physicalBitmap.Width, physicalBitmap.Height),
            GraphicsUnit.Pixel);

        physicalBitmap.Dispose();
        return logicalBitmap;
    }
}

internal readonly record struct CaptureResult(DrawingBitmap Bitmap, ScreenScale Scale);
