using System.Windows;

namespace DemoApp.WindowEffect;

public static class WindowsSystemInfo
{
    public static readonly DependencyProperty IsWindowsSystemTransparencyEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsWindowsSystemTransparencyEnabled",
            typeof(bool),
            typeof(WindowsSystemInfo),
            new FrameworkPropertyMetadata(
                true,
                FrameworkPropertyMetadataOptions.None,
                null,
                CoerceIsWindowsSystemTransparencyEnabled));

    public static bool GetIsWindowsSystemTransparencyEnabled(DependencyObject obj) =>
        (bool)obj.GetValue(IsWindowsSystemTransparencyEnabledProperty);

    public static void SetIsWindowsSystemTransparencyEnabled(DependencyObject obj, bool value) =>
        obj.SetValue(IsWindowsSystemTransparencyEnabledProperty, value);

    private static object CoerceIsWindowsSystemTransparencyEnabled(DependencyObject d, object baseValue) =>
        BlurBehind.IsTransparencyEnabled();
}
