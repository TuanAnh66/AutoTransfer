using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Cv2 = OpenCvSharp.Cv2;
using DrawingBitmap = System.Drawing.Bitmap;
using DrawingPoint = System.Drawing.Point;
using ImreadModes = OpenCvSharp.ImreadModes;
using InterpolationFlags = OpenCvSharp.InterpolationFlags;
using Mat = OpenCvSharp.Mat;
using TemplateMatchModes = OpenCvSharp.TemplateMatchModes;

namespace AutoTransfer;

public partial class MainWindow : System.Windows.Window
{
    private const double MatchThreshold = 0.84;
    private DrawingBitmap? _lastCapture;
    private DrawingPoint? _lastFoundPoint;
    private string? _templatePath;
    private ScreenScale _lastCaptureScale = ScreenScale.Identity;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void CaptureScreen_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SetStatus("Đang chụp màn hình...");
            CaptureAndShowLatestScreen();
            SetStatus("Đã chụp màn hình");
            AppendLog($"Capture OK: {_lastCapture!.Width}x{_lastCapture!.Height} | scale={_lastCaptureScale.ScaleX:0.00}x{_lastCaptureScale.ScaleY:0.00}");
        }
        catch (Exception ex)
        {
            SetStatus("Lỗi khi chụp màn hình");
            AppendLog($"Capture FAILED: {ex.Message}");
        }
    }

    private void SelectTemplate_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        _templatePath = dialog.FileName;
        TemplatePathText.Text = _templatePath;
        AppendLog($"Template selected: {_templatePath}");
        SetStatus("Đã chọn ảnh mẫu");
    }

    private void CropTemplate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!EnsureCaptureAvailable("Không có ảnh để crop", "Crop FAILED: không có ảnh chụp"))
            {
                return;
            }

            var capture = _lastCapture!;
            var cropWindow = new TemplateCropWindow(capture);
            cropWindow.Owner = this;
            if (cropWindow.ShowDialog() != true || cropWindow.CroppedBitmap is null)
            {
                SetStatus("Đã hủy chọn vùng template");
                AppendLog("Crop cancelled");
                return;
            }

            var templateFile = GetTempTemplateFile();
            Directory.CreateDirectory(Path.GetDirectoryName(templateFile)!);
            cropWindow.CroppedBitmap.Save(templateFile, ImageFormat.Png);

            _templatePath = templateFile;
            TemplatePathText.Text = _templatePath;
            SetStatus("Đã lưu vùng template");
            AppendLog($"Crop OK: saved {templateFile}");
        }
        catch (Exception ex)
        {
            SetStatus("Lỗi khi crop template");
            AppendLog($"Crop FAILED: {ex.Message}");
        }
    }

    private void FindTemplate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_templatePath) || !File.Exists(_templatePath))
            {
                SetStatus("Chưa chọn ảnh mẫu");
                AppendLog("Find FAILED: chưa có ảnh mẫu");
                return;
            }

            if (!EnsureCaptureAvailable("Lỗi khi tìm ảnh mẫu", "Find FAILED: không có ảnh chụp"))
            {
                return;
            }

            var result = TemplateMatcher.FindTemplate(_lastCapture!, _templatePath, MatchThreshold);
            if (result.HasValue)
            {
                _lastFoundPoint = new DrawingPoint(result.Value.Point.X, result.Value.Point.Y);
                PointText.Text = $"Điểm tìm được: {_lastFoundPoint.Value.X}, {_lastFoundPoint.Value.Y}";
                SetStatus("Đã tìm thấy ảnh mẫu");
                AppendLog($"Find OK: {_lastFoundPoint.Value.X}, {_lastFoundPoint.Value.Y} | score={result.Value.Score:0.000} | scale={result.Value.Scale:0.00}");
                return;
            }

            _lastFoundPoint = null;
            PointText.Text = "Điểm tìm được: không thấy";
            SetStatus("Không tìm thấy ảnh mẫu");
            AppendLog("Find FAILED: không khớp ảnh mẫu");
        }
        catch (Exception ex)
        {
            SetStatus("Lỗi khi tìm ảnh mẫu");
            AppendLog($"Find FAILED: {ex.Message}");
        }
    }

    private void ClickFoundPoint_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!_lastFoundPoint.HasValue)
            {
                SetStatus("Chưa có điểm để click");
                AppendLog("Click skipped: chưa có điểm tìm được");
                return;
            }

            var physicalPoint = _lastCaptureScale.LogicalToPhysical(_lastFoundPoint.Value);
            AutomationTools.ClickAt(physicalPoint);
            SetStatus("Đã click điểm tìm được");
            AppendLog($"Click OK: logical={_lastFoundPoint.Value.X}, {_lastFoundPoint.Value.Y} | physical={physicalPoint.X}, {physicalPoint.Y}");
        }
        catch (Exception ex)
        {
            SetStatus("Lỗi khi click");
            AppendLog($"Click FAILED: {ex.Message}");
        }
    }

    private void SendTestText_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            AutomationTools.TypeText("AutoTransfer test text");
            SetStatus("Đã gửi text thử");
            AppendLog("Keyboard OK: sent test text");
        }
        catch (Exception ex)
        {
            SetStatus("Lỗi khi gửi text");
            AppendLog($"Keyboard FAILED: {ex.Message}");
        }
    }

    private void ClearLog_Click(object sender, RoutedEventArgs e)
    {
        LogBox.Clear();
        SetStatus("Đã xóa log");
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SetStatus(string message)
    {
        StatusText.Text = message;
    }

    private void AppendLog(string message)
    {
        LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        LogBox.ScrollToEnd();
    }

    private void ReplaceCapture(DrawingBitmap bitmap)
    {
        _lastCapture?.Dispose();
        _lastCapture = bitmap;
    }

    private void CaptureLatestScreen()
    {
        var capture = ScreenCaptureService.CaptureLogical(this);
        _lastCaptureScale = capture.Scale;
        ReplaceCapture(capture.Bitmap);
    }

    private void CaptureAndShowLatestScreen()
    {
        CaptureLatestScreen();
        PreviewImage.Source = ToBitmapSource(_lastCapture!);
    }

    private bool EnsureCaptureAvailable(string statusMessage, string logMessage)
    {
        if (_lastCapture is not null)
        {
            return true;
        }

        CaptureAndShowLatestScreen();
        if (_lastCapture is not null)
        {
            return true;
        }

        SetStatus(statusMessage);
        AppendLog(logMessage);
        return false;
    }

    private static string GetTempTemplateFile()
    {
        return Path.Combine(Path.GetTempPath(), "AutoTransfer", "template_crop.png");
    }

    private static BitmapSource ToBitmapSource(DrawingBitmap bitmap)
    {
        using var memory = new MemoryStream();
        bitmap.Save(memory, ImageFormat.Png);
        memory.Position = 0;

        var source = new BitmapImage();
        source.BeginInit();
        source.CacheOption = BitmapCacheOption.OnLoad;
        source.StreamSource = memory;
        source.EndInit();
        source.Freeze();
        return source;
    }

    protected override void OnClosed(EventArgs e)
    {
        _lastCapture?.Dispose();
        base.OnClosed(e);
    }
}

internal static class TemplateMatcher
{
    private static readonly double[] ScaleCandidates = [0.80, 0.85, 0.90, 0.95, 1.00, 1.05, 1.10, 1.15, 1.20];

    public static MatchResult? FindTemplate(DrawingBitmap screenshot, string templatePath, double threshold)
    {
        using var screenshotGray = ToGrayMat(screenshot);
        using var templateOriginal = Cv2.ImRead(templatePath, ImreadModes.Color);
        using var templateGrayOriginal = ToGrayMat(templateOriginal);

        if (screenshotGray.Empty() || templateGrayOriginal.Empty())
        {
            return null;
        }

        MatchResult? best = null;

        foreach (var scale in ScaleCandidates)
        {
            using var scaledTemplate = ScaleMat(templateGrayOriginal, scale);
            if (scaledTemplate.Empty())
            {
                continue;
            }

            if (scaledTemplate.Width > screenshotGray.Width || scaledTemplate.Height > screenshotGray.Height)
            {
                continue;
            }

            using var result = new Mat();
            Cv2.MatchTemplate(screenshotGray, scaledTemplate, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out var maxVal, out _, out var maxLoc);

            if (best is null || maxVal > best.Value.Score)
            {
                best = new MatchResult(new DrawingPoint(maxLoc.X, maxLoc.Y), maxVal, scale);
            }
        }

        return best is not null && best.Value.Score >= threshold ? best : null;
    }

    private static Mat ToGrayMat(DrawingBitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return ToGrayMat(Cv2.ImDecode(stream.ToArray(), ImreadModes.Color));
    }

    private static Mat ToGrayMat(Mat source)
    {
        if (source.Channels() == 1)
        {
            return source.Clone();
        }

        var gray = new Mat();
        Cv2.CvtColor(source, gray, OpenCvSharp.ColorConversionCodes.BGR2GRAY);
        return gray;
    }

    private static Mat ScaleMat(Mat source, double scale)
    {
        if (Math.Abs(scale - 1.0) < 0.0001)
        {
            return source.Clone();
        }

        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        var resized = new Mat();
        Cv2.Resize(source, resized, new OpenCvSharp.Size(width, height), 0, 0, InterpolationFlags.Area);
        return resized;
    }

    public readonly record struct MatchResult(DrawingPoint Point, double Score, double Scale);
}
