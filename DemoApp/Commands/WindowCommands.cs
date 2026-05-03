using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace DemoApp.Commands;

public static class WindowCommands
{
    private const int WmNcLeftButtonDown = 0x00A1;
    private const int WmSysCommand = 0x0112;
    private const int HtCaption = 2;
    private const int ScSize = 0xF000;
    private const int WmszLeft = 1;
    private const int WmszRight = 2;
    private const int WmszTop = 3;
    private const int WmszTopLeft = 4;
    private const int WmszTopRight = 5;
    private const int WmszBottom = 6;
    private const int WmszBottomLeft = 7;
    private const int WmszBottomRight = 8;

    public static readonly DependencyProperty DragMoveProperty =
        DependencyProperty.RegisterAttached(
            "DragMove",
            typeof(bool),
            typeof(WindowCommands),
            new PropertyMetadata(false, OnDragMoveChanged));

    public static readonly DependencyProperty MinimizeProperty =
        DependencyProperty.RegisterAttached(
            "Minimize",
            typeof(bool),
            typeof(WindowCommands),
            new PropertyMetadata(false, OnMinimizeChanged));

    public static readonly DependencyProperty MaximizeRestoreProperty =
        DependencyProperty.RegisterAttached(
            "MaximizeRestore",
            typeof(bool),
            typeof(WindowCommands),
            new PropertyMetadata(false, OnMaximizeRestoreChanged));

    public static readonly DependencyProperty CloseProperty =
        DependencyProperty.RegisterAttached(
            "Close",
            typeof(bool),
            typeof(WindowCommands),
            new PropertyMetadata(false, OnCloseChanged));

    public static readonly DependencyProperty ResizeProperty =
        DependencyProperty.RegisterAttached(
            "Resize",
            typeof(bool),
            typeof(WindowCommands),
            new PropertyMetadata(false, OnResizeChanged));

    public static bool GetDragMove(DependencyObject obj) => (bool)obj.GetValue(DragMoveProperty);

    public static void SetDragMove(DependencyObject obj, bool value) => obj.SetValue(DragMoveProperty, value);

    public static bool GetMinimize(DependencyObject obj) => (bool)obj.GetValue(MinimizeProperty);

    public static void SetMinimize(DependencyObject obj, bool value) => obj.SetValue(MinimizeProperty, value);

    public static bool GetMaximizeRestore(DependencyObject obj) => (bool)obj.GetValue(MaximizeRestoreProperty);

    public static void SetMaximizeRestore(DependencyObject obj, bool value) => obj.SetValue(MaximizeRestoreProperty, value);

    public static bool GetClose(DependencyObject obj) => (bool)obj.GetValue(CloseProperty);

    public static void SetClose(DependencyObject obj, bool value) => obj.SetValue(CloseProperty, value);

    public static bool GetResize(DependencyObject obj) => (bool)obj.GetValue(ResizeProperty);

    public static void SetResize(DependencyObject obj, bool value) => obj.SetValue(ResizeProperty, value);

    private static void OnDragMoveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Border border || e.NewValue is not true)
        {
            return;
        }

        border.MouseLeftButtonDown += (_, args) =>
        {
            if (args.ChangedButton != MouseButton.Left || Mouse.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            if (IsFromInteractiveControl(args.OriginalSource as DependencyObject))
            {
                return;
            }

            var window = Window.GetWindow(border);
            if (window == null)
            {
                return;
            }

            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            _ = ReleaseCapture();
            _ = SendMessage(hwnd, WmNcLeftButtonDown, (IntPtr)HtCaption, IntPtr.Zero);
            args.Handled = true;
        };
    }

    private static void OnMinimizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Button button && e.NewValue is true)
        {
            button.Click += (_, _) =>
            {
                var window = Window.GetWindow(button);
                if (window != null)
                {
                    window.WindowState = WindowState.Minimized;
                }
            };
        }
    }

    private static void OnMaximizeRestoreChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Button button && e.NewValue is true)
        {
            button.Click += (_, _) =>
            {
                var window = Window.GetWindow(button);
                if (window != null)
                {
                    window.WindowState = window.WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
                }
            };
        }
    }

    private static void OnCloseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Button button && e.NewValue is true)
        {
            button.Click += (_, _) => Window.GetWindow(button)?.Close();
        }
    }

    private static void OnResizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Border border || e.NewValue is not true)
        {
            return;
        }

        border.MouseLeftButtonDown += (_, args) =>
        {
            var window = Window.GetWindow(border);
            if (window == null || window.WindowState == WindowState.Maximized)
            {
                return;
            }

            var direction = GetResizeDirection(args.GetPosition(border), border.ActualWidth, border.ActualHeight, border.BorderThickness.Left);
            if (direction == 0)
            {
                return;
            }

            var hwnd = new WindowInteropHelper(window).Handle;
            _ = SendMessage(hwnd, WmSysCommand, (IntPtr)(ScSize | direction), IntPtr.Zero);
            args.Handled = true;
        };

        border.MouseMove += (_, args) =>
        {
            var window = Window.GetWindow(border);
            border.Cursor = window == null || window.WindowState == WindowState.Maximized
                ? Cursors.Arrow
                : GetResizeCursor(args.GetPosition(border), border.ActualWidth, border.ActualHeight, border.BorderThickness.Left);
        };
    }

    private static int GetResizeDirection(Point mousePos, double width, double height, double borderThickness)
    {
        var isLeft = mousePos.X < borderThickness;
        var isRight = mousePos.X > width - borderThickness;
        var isTop = mousePos.Y < borderThickness;
        var isBottom = mousePos.Y > height - borderThickness;

        if (isTop && isLeft) return WmszTopLeft;
        if (isTop && isRight) return WmszTopRight;
        if (isBottom && isLeft) return WmszBottomLeft;
        if (isBottom && isRight) return WmszBottomRight;
        if (isLeft) return WmszLeft;
        if (isRight) return WmszRight;
        if (isTop) return WmszTop;
        if (isBottom) return WmszBottom;

        return 0;
    }

    private static Cursor GetResizeCursor(Point mousePos, double width, double height, double borderThickness)
    {
        var isLeft = mousePos.X < borderThickness;
        var isRight = mousePos.X > width - borderThickness;
        var isTop = mousePos.Y < borderThickness;
        var isBottom = mousePos.Y > height - borderThickness;

        if ((isTop && isLeft) || (isBottom && isRight)) return Cursors.SizeNWSE;
        if ((isTop && isRight) || (isBottom && isLeft)) return Cursors.SizeNESW;
        if (isLeft || isRight) return Cursors.SizeWE;
        if (isTop || isBottom) return Cursors.SizeNS;

        return Cursors.Arrow;
    }

    private static bool IsFromInteractiveControl(DependencyObject? source)
    {
        while (source != null)
        {
            if (source is ButtonBase or TextBoxBase or Selector or Slider or CheckBox or RadioButton)
            {
                return true;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();
}
