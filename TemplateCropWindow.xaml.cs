using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using DrawingBitmap = System.Drawing.Bitmap;
using WpfPoint = System.Windows.Point;
using WpfRect = System.Windows.Rect;

namespace AutoTransfer;

public partial class TemplateCropWindow : Window
{
    private static readonly string DebugLogPath = Path.Combine(Path.GetTempPath(), "AutoTransfer", "crop_debug.log");
    private readonly DrawingBitmap _sourceBitmap;
    private bool _isDragging;
    private WpfPoint _dragStart;
    private WpfRect _selection;

    public DrawingBitmap? CroppedBitmap { get; private set; }

    public TemplateCropWindow(DrawingBitmap sourceBitmap)
    {
        InitializeComponent();
        _sourceBitmap = sourceBitmap;
        PreviewImage.Source = ToBitmapSource(sourceBitmap);
        ImageCanvas.Width = _sourceBitmap.Width;
        ImageCanvas.Height = _sourceBitmap.Height;
        PreviewImage.Width = _sourceBitmap.Width;
        PreviewImage.Height = _sourceBitmap.Height;
        DebugLog($"[Constructor] SourceBitmap: {_sourceBitmap.Width}x{_sourceBitmap.Height} | Canvas: {ImageCanvas.Width}x{ImageCanvas.Height}");
        Loaded += (_, _) => {
            DebugLog($"[Loaded] Viewport: {ImageViewport.RenderSize.Width}x{ImageViewport.RenderSize.Height} | ImageArea: {ImageArea.RenderSize.Width}x{ImageArea.RenderSize.Height}");
            UpdateHint();
        };
    }

    private void ImageCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var imagePoint = GetImagePoint(e);
        DebugLog($"[MouseDown] Canvas point: ({imagePoint.X:0.0},{imagePoint.Y:0.0})");
        if (imagePoint.X < 0 || imagePoint.Y < 0 || imagePoint.X > ImageCanvas.Width || imagePoint.Y > ImageCanvas.Height)
        {
            DebugLog($"[MouseDown] Point out of bounds: ({imagePoint.X:0.0},{imagePoint.Y:0.0})");
            return;
        }

        _isDragging = true;
        _dragStart = imagePoint;
        _selection = new WpfRect(imagePoint, imagePoint);
        SelectionRect.Visibility = Visibility.Visible;
        ImageCanvas.CaptureMouse();
        UpdateOverlay();
        UpdateHint();
    }

    private void ImageCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }

        var imagePoint = GetImagePoint(e);
        var nextSelection = new WpfRect(_dragStart, imagePoint);
        if (AreClose(_selection, nextSelection))
        {
            return;
        }

        _selection = nextSelection;
        UpdateOverlay();
    }

    private void ImageCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }

        _isDragging = false;
        ImageCanvas.ReleaseMouseCapture();
        UpdateOverlay();
        UpdateHint();
    }

    private static void DebugLog(string message)
    {
        var logDir = Path.GetDirectoryName(DebugLogPath);
        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir!);
        }

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logLine = $"{timestamp} | {message}";

        File.AppendAllText(DebugLogPath, logLine + Environment.NewLine);
    }

    private WpfPoint GetImagePoint(MouseEventArgs e)
    {
        // WPF already translates the pointer through the Viewbox transform into the canvas coordinate space.
        // That space matches the screenshot pixel grid because the canvas is sized to the bitmap dimensions.
        var point = e.GetPosition(ImageCanvas);
        var clampedX = Math.Clamp(point.X, 0, ImageCanvas.Width);
        var clampedY = Math.Clamp(point.Y, 0, ImageCanvas.Height);
        DebugLog($"[GetImagePoint] Canvas raw:({point.X:F1},{point.Y:F1}) -> Clamped:({clampedX:F1},{clampedY:F1})");
        return new WpfPoint(clampedX, clampedY);
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetSelectionRect(out var cropRect))
        {
            DebugLog($"[Confirm_Click] Invalid selection rect");
            MessageBox.Show(this, "Hãy kéo chọn một vùng hợp lệ trước.", "Chọn vùng template");
            return;
        }

        DebugLog($"[Confirm_Click] Cropping rect: X={cropRect.X} Y={cropRect.Y} W={cropRect.Width} H={cropRect.Height} | BitmapSize: {_sourceBitmap.Width}x{_sourceBitmap.Height}");
        CroppedBitmap = CropBitmap(_sourceBitmap, cropRect);
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void UpdateOverlay()
    {
        var selection = Normalize(_selection);
        if (selection.Width <= 0 || selection.Height <= 0)
        {
            SelectionRect.Visibility = Visibility.Collapsed;
            return;
        }

        // SelectionRect is on the Canvas, which is in image coordinate space
        // The Viewbox automatically scales it for display, so we set it in canvas/image coordinates
        DebugLog($"[UpdateOverlay] Setting rect in canvas/image space: ({selection.X:0.0},{selection.Y:0.0})+{selection.Width:0.0}x{selection.Height:0.0}");

        SelectionRect.Visibility = Visibility.Visible;
        Canvas.SetLeft(SelectionRect, selection.X);
        Canvas.SetTop(SelectionRect, selection.Y);
        SelectionRect.Width = selection.Width;
        SelectionRect.Height = selection.Height;
    }

    private void UpdateHint()
    {
        var selection = Normalize(_selection);
        if (selection.Width <= 0 || selection.Height <= 0)
        {
            HintText.Text = "Chưa chọn vùng nào.";
            return;
        }

        HintText.Text = $"Vùng chọn: {selection.Width:0} x {selection.Height:0}px";
    }

    private bool TryGetSelectionRect(out System.Drawing.Rectangle rect)
    {
        var selection = Normalize(_selection);
        rect = default;

        if (selection.Width < 4 || selection.Height < 4)
        {
            return false;
        }

        var x = (int)Math.Round(selection.X);
        var y = (int)Math.Round(selection.Y);
        var width = (int)Math.Round(selection.Width);
        var height = (int)Math.Round(selection.Height);

        if (x < 0 || y < 0 || x + width > _sourceBitmap.Width || y + height > _sourceBitmap.Height)
        {
            return false;
        }

        rect = new System.Drawing.Rectangle(x, y, width, height);
        return true;
    }

    private static bool AreClose(WpfRect a, WpfRect b)
    {
        return Math.Abs(a.Left - b.Left) < 1 &&
               Math.Abs(a.Top - b.Top) < 1 &&
               Math.Abs(a.Width - b.Width) < 1 &&
               Math.Abs(a.Height - b.Height) < 1;
    }

    private static WpfRect Normalize(WpfRect rect)
    {
        var x = Math.Min(rect.Left, rect.Right);
        var y = Math.Min(rect.Top, rect.Bottom);
        var width = Math.Abs(rect.Width);
        var height = Math.Abs(rect.Height);
        return new WpfRect(x, y, width, height);
    }

    private static DrawingBitmap CropBitmap(DrawingBitmap source, System.Drawing.Rectangle rect)
    {
        var cropped = new DrawingBitmap(rect.Width, rect.Height);
        using var graphics = Graphics.FromImage(cropped);
        graphics.DrawImage(source, new System.Drawing.Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
        return cropped;
    }

    private static BitmapSource ToBitmapSource(DrawingBitmap bitmap)
    {
        using var memory = new MemoryStream();
        bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
        memory.Position = 0;

        var source = new BitmapImage();
        source.BeginInit();
        source.CacheOption = BitmapCacheOption.OnLoad;
        source.StreamSource = memory;
        source.EndInit();
        source.Freeze();
        return source;
    }
}
