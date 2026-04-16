using System.Drawing;
using System.Runtime.InteropServices;
using WindowsInput;
using DrawingPoint = System.Drawing.Point;

namespace AutoTransfer;

internal static class AutomationTools
{
    private static readonly InputSimulator InputSimulator = new();

    public static void ClickAt(DrawingPoint point)
    {
        SetCursorPos(point.X, point.Y);
        Thread.Sleep(80);
        InputSimulator.Mouse.LeftButtonClick();
    }

    public static void TypeText(string text)
    {
        InputSimulator.Keyboard.TextEntry(text);
    }

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);
}
