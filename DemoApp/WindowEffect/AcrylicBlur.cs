using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace DemoApp.WindowEffect;

public static class AcrylicBlur
{
    private const uint BlurBackgroundColor = 0x990000;
    private const int BlurOpacity = 0;
    private static readonly Brush FallbackBrush = new SolidColorBrush(Color.FromArgb(0x59, 0, 0, 0));

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(AcrylicBlur),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

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
        if (!enable)
        {
            BlurBehind.SetAccent(window, AccentState.Disabled);
            return;
        }

        if (!BlurBehind.IsTransparencyEnabled())
        {
            if (window.Background is null || IsTransparentBrush(window.Background))
            {
                window.Background = FallbackBrush;
            }

            BlurBehind.SetAccent(window, AccentState.Disabled);
            return;
        }

        var gradientColor = (int)((BlurOpacity << 24) | (BlurBackgroundColor & 0xFFFFFF));
        BlurBehind.SetAccent(window, AccentState.EnableAcrylicBlurBehind, gradientColor);
    }

    private static bool IsTransparentBrush(Brush brush) =>
        brush is SolidColorBrush solid && solid.Color.A == 0;
}
