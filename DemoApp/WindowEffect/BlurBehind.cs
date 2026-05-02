using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace DemoApp.WindowEffect;

public enum AccentState
{
    Disabled = 0,
    EnableGradient = 1,
    EnableTransparentGradient = 2,
    EnableBlurBehind = 3,
    EnableAcrylicBlurBehind = 4,
    InvalidState = 5
}

[StructLayout(LayoutKind.Sequential)]
public struct AccentPolicy
{
    public AccentState AccentState;
    public int AccentFlags;
    public int GradientColor;
    public int AnimationId;
}

[StructLayout(LayoutKind.Sequential)]
public struct WindowCompositionAttributeData
{
    public WindowCompositionAttribute Attribute;
    public IntPtr Data;
    public int SizeOfData;
}

public enum WindowCompositionAttribute
{
    AccentPolicy = 19
}

public static class BlurBehind
{
    [DllImport("user32.dll")]
    private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(BlurBehind),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    public static bool IsTransparencyEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("EnableTransparency") is int value ? value == 1 : true;
        }
        catch
        {
            return true;
        }
    }

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Window window)
        {
            return;
        }

        if (new WindowInteropHelper(window).Handle == IntPtr.Zero)
        {
            window.Loaded += (_, _) => ApplyBlur(window, (bool)e.NewValue);
            return;
        }

        ApplyBlur(window, (bool)e.NewValue);
    }

    private static void ApplyBlur(Window window, bool enable)
    {
        if (!IsTransparencyEnabled())
        {
            window.Background = new SolidColorBrush(Color.FromArgb(0x59, 0, 0, 0));
        }

        SetAccent(window, enable ? AccentState.EnableBlurBehind : AccentState.Disabled);
    }

    public static void SetAccent(Window window, AccentState accentState, int gradientColor = 0)
    {
        var accent = new AccentPolicy
        {
            AccentState = accentState,
            GradientColor = gradientColor
        };

        var accentStructSize = Marshal.SizeOf(accent);
        var accentPtr = Marshal.AllocHGlobal(accentStructSize);

        try
        {
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.AccentPolicy,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            _ = SetWindowCompositionAttribute(new WindowInteropHelper(window).Handle, ref data);
        }
        finally
        {
            Marshal.FreeHGlobal(accentPtr);
        }
    }
}
