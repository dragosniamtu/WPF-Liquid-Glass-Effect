using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace DemoApp;

public static class ScreenCaptureHelper
{
    private const int SrcCopy = 0x00CC0020;
    private const int SmXVirtualScreen = 76;
    private const int SmYVirtualScreen = 77;
    private const int SmCxVirtualScreen = 78;
    private const int SmCyVirtualScreen = 79;
    private const int ColorOnColor = 3;

    public static BitmapSource? FullScreenSnapshot { get; private set; }
    public static int VirtualScreenX { get; private set; }
    public static int VirtualScreenY { get; private set; }
    public static int VirtualScreenWidth { get; private set; }
    public static int VirtualScreenHeight { get; private set; }

    public static void CaptureFullScreen()
    {
        VirtualScreenX = GetSystemMetrics(SmXVirtualScreen);
        VirtualScreenY = GetSystemMetrics(SmYVirtualScreen);
        VirtualScreenWidth = GetSystemMetrics(SmCxVirtualScreen);
        VirtualScreenHeight = GetSystemMetrics(SmCyVirtualScreen);

        var bitmap = CaptureRegion(VirtualScreenX, VirtualScreenY, VirtualScreenWidth, VirtualScreenHeight);
        if (bitmap != null)
        {
            FullScreenSnapshot = bitmap;
        }
    }

    public static BitmapSource? CaptureRegion(int x, int y, int width, int height, double scale = 1.0)
    {
        if (width <= 0 || height <= 0)
        {
            return null;
        }

        var scaledWidth = (int)(width * scale);
        var scaledHeight = (int)(height * scale);

        var screenDc = IntPtr.Zero;
        var memDc = IntPtr.Zero;
        var hBitmap = IntPtr.Zero;
        var oldBitmap = IntPtr.Zero;

        try
        {
            screenDc = GetDC(IntPtr.Zero);
            if (screenDc == IntPtr.Zero)
            {
                return null;
            }

            memDc = CreateCompatibleDC(screenDc);
            if (memDc == IntPtr.Zero)
            {
                return null;
            }

            hBitmap = CreateCompatibleBitmap(screenDc, scaledWidth, scaledHeight);
            if (hBitmap == IntPtr.Zero)
            {
                return null;
            }

            oldBitmap = SelectObject(memDc, hBitmap);

            if (Math.Abs(scale - 1.0) < 0.001)
            {
                _ = BitBlt(memDc, 0, 0, width, height, screenDc, x, y, SrcCopy);
            }
            else
            {
                _ = SetStretchBltMode(memDc, ColorOnColor);
                _ = StretchBlt(memDc, 0, 0, scaledWidth, scaledHeight, screenDc, x, y, width, height, SrcCopy);
            }

            var bitmap = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(scaledWidth, scaledHeight));

            bitmap.Freeze();
            return bitmap;
        }
        finally
        {
            if (oldBitmap != IntPtr.Zero && memDc != IntPtr.Zero)
            {
                _ = SelectObject(memDc, oldBitmap);
            }

            if (hBitmap != IntPtr.Zero)
            {
                _ = DeleteObject(hBitmap);
            }

            if (memDc != IntPtr.Zero)
            {
                _ = DeleteDC(memDc);
            }

            if (screenDc != IntPtr.Zero)
            {
                _ = ReleaseDC(IntPtr.Zero, screenDc);
            }
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int height);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int width, int height, IntPtr hdcSrc, int xSrc, int ySrc, int rop);

    [DllImport("gdi32.dll")]
    private static extern bool StretchBlt(IntPtr hdcDest, int xDest, int yDest, int widthDest, int heightDest, IntPtr hdcSrc, int xSrc, int ySrc, int widthSrc, int heightSrc, int rop);

    [DllImport("gdi32.dll")]
    private static extern int SetStretchBltMode(IntPtr hdc, int mode);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
}
