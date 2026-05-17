using DemoApp.Shaders;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DemoApp.Behaviors;

public static class GlassyWindowBehavior
{
    private const int SwHide = 0;
    private const int SwShowNoActivate = 4;

    public static readonly DependencyProperty EnableProperty =
        DependencyProperty.RegisterAttached(
            "Enable",
            typeof(bool),
            typeof(GlassyWindowBehavior),
            new PropertyMetadata(false, OnEnableChanged));

    private static readonly DependencyProperty StateProperty =
        DependencyProperty.RegisterAttached(
            "State",
            typeof(GlassyWindowState),
            typeof(GlassyWindowBehavior),
            new PropertyMetadata(null));

    public static bool GetEnable(DependencyObject obj) => (bool)obj.GetValue(EnableProperty);

    public static void SetEnable(DependencyObject obj, bool value) => obj.SetValue(EnableProperty, value);

    private static GlassyWindowState? GetState(DependencyObject obj) => (GlassyWindowState?)obj.GetValue(StateProperty);

    private static void SetState(DependencyObject obj, GlassyWindowState? value) => obj.SetValue(StateProperty, value);

    private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Window window)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            Attach(window);
        }
        else
        {
            Detach(window);
        }
    }

    private static void Attach(Window window)
    {
        if (GetState(window) != null)
        {
            return;
        }

        var state = new GlassyWindowState(window);
        SetState(window, state);

        window.SourceInitialized += state.OnSourceInitialized;
        window.Loaded += state.OnLoaded;
        window.SizeChanged += state.OnSizeChanged;
        window.LocationChanged += state.OnLocationChanged;
        window.Activated += state.OnActivated;
        window.Deactivated += state.OnDeactivated;
        window.Closed += state.OnClosed;
    }

    private static void Detach(Window window)
    {
        var state = GetState(window);
        if (state == null)
        {
            return;
        }

        window.SourceInitialized -= state.OnSourceInitialized;
        window.Loaded -= state.OnLoaded;
        window.SizeChanged -= state.OnSizeChanged;
        window.LocationChanged -= state.OnLocationChanged;
        window.Activated -= state.OnActivated;
        window.Deactivated -= state.OnDeactivated;
        window.Closed -= state.OnClosed;

        state.Dispose();
        SetState(window, null);
    }

    private sealed class GlassyWindowState : IDisposable
    {
        private const int CaptureIntervalMs = 16; // ~60 FPS
        private readonly Window _window;
        private DispatcherTimer? _backdropUpdateTimer;
        private ImageBrush? _backdropBrush;
        private DateTime _lastCapture = DateTime.MinValue;
        private bool _isCapturing;
        private bool _isAffinitySet;
        private Border? _windowFrame;
        private Border? _glassyLayer;
        private FrameworkElement? _windowClipRoot;
        private RectangleGeometry? _windowFrameClip;
        private RectangleGeometry? _windowClipRootClip;
        private GlassyEffect? _glassyEffect;

        public GlassyWindowState(Window window)
        {
            _window = window;
        }

        public void OnSourceInitialized(object? sender, EventArgs e)
        {
            _window.ApplyTemplate();
            CacheTemplateParts();
            TrySetExclusionAffinity();
            SetBlurBehind(true);
            SetupBackdropCapture();
            EnsureGlassyEffect();
            UpdateGlassyEffectParameters();
            UpdateWindowClip();
        }

        public void OnLoaded(object sender, RoutedEventArgs e)
        {
            CacheTemplateParts();
            TrySetExclusionAffinity();
            SetupBackdropCapture();
            EnsureGlassyEffect();
            UpdateGlassyEffectParameters();
            UpdateWindowClip();
        }

        public void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateGlassyEffectParameters();
            UpdateWindowClip();
            ScheduleDelayedBackdropUpdate();
        }

        public void OnLocationChanged(object? sender, EventArgs e) => ScheduleDelayedBackdropUpdate();

        public void OnActivated(object? sender, EventArgs e) => ScheduleDelayedBackdropUpdate();

        public void OnDeactivated(object? sender, EventArgs e)
        {
        }

        public void OnClosed(object? sender, EventArgs e) => Dispose();

        public void Dispose()
        {
            if (_backdropUpdateTimer == null)
            {
                return;
            }

            _backdropUpdateTimer.Stop();
            _backdropUpdateTimer.Tick -= OnBackdropUpdateTick;
            _backdropUpdateTimer = null;
        }

        private void CacheTemplateParts()
        {
            _windowFrame = _window.Template.FindName("WindowFrame", _window) as Border;
            _glassyLayer = _window.Template.FindName("GlassyLayer", _window) as Border;
            _windowClipRoot = _window.Template.FindName("WindowClipRoot", _window) as FrameworkElement;
        }

        private void SetupBackdropCapture()
        {
            if (_windowFrame == null)
            {
                CacheTemplateParts();
            }

            if (_windowFrame == null)
            {
                return;
            }

            _backdropBrush ??= new ImageBrush
            {
                Stretch = Stretch.Fill,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top
            };

            RenderOptions.SetBitmapScalingMode(_backdropBrush, BitmapScalingMode.Linear);

            _windowFrame.Background = _backdropBrush;
            CaptureBehindWindow();
        }

        private void EnsureGlassyEffect()
        {
            _glassyEffect ??= new GlassyEffect();
            if (_glassyLayer != null)
            {
                _glassyLayer.Effect = _glassyEffect;
            }
        }

        private void UpdateGlassyEffectParameters()
        {
            if (_glassyEffect == null)
            {
                return;
            }

            var width = Math.Max(1.0, _window.ActualWidth);
            var height = Math.Max(1.0, _window.ActualHeight);

            _glassyEffect.TextureSize = new Point(width, height);
            _glassyEffect.GlassCenter = new Point(width * 0.5, height * 0.5);
            _glassyEffect.GlassSize = new Point(width, height);
            _glassyEffect.BlurIntensity = 0.45f;
        }

        private void UpdateWindowClip()
        {
            if (_windowFrame == null && _windowClipRoot == null)
            {
                return;
            }

            ApplyRectangularClip(_windowFrame, ref _windowFrameClip);
            ApplyRectangularClip(_windowClipRoot, ref _windowClipRootClip);
        }

        private static void ApplyRectangularClip(FrameworkElement? element, ref RectangleGeometry? clip)
        {
            if (element == null)
            {
                return;
            }

            var width = Math.Max(0.0, element.ActualWidth);
            var height = Math.Max(0.0, element.ActualHeight);
            if (width <= 0.0 || height <= 0.0)
            {
                return;
            }

            var rect = new Rect(0, 0, width, height);
            if (clip == null)
            {
                clip = new RectangleGeometry(rect);
                element.Clip = clip;
                return;
            }

            clip.Rect = rect;
        }

        private void ScheduleDelayedBackdropUpdate()
        {
            if (_isAffinitySet)
            {
                // When affinity is set, we can capture smoothly without flickering.
                CaptureBehindWindow();
                return;
            }

            _backdropUpdateTimer ??= new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(CaptureIntervalMs)
            };
            _backdropUpdateTimer.Tick -= OnBackdropUpdateTick;
            _backdropUpdateTimer.Tick += OnBackdropUpdateTick;

            if (!_backdropUpdateTimer.IsEnabled)
            {
                _backdropUpdateTimer.Start();
            }
        }

        private void OnBackdropUpdateTick(object? sender, EventArgs e)
        {
            _backdropUpdateTimer?.Stop();
            CaptureBehindWindow();
        }

        private void CaptureBehindWindow()
        {
            if (_isCapturing || (DateTime.Now - _lastCapture).TotalMilliseconds < CaptureIntervalMs)
            {
                return;
            }

            _isCapturing = true;
            _lastCapture = DateTime.Now;

            try
            {
                var hwnd = new WindowInteropHelper(_window).Handle;
                if (hwnd == IntPtr.Zero)
                {
                    _isCapturing = false;
                    return;
                }

                var topLeft = _window.PointToScreen(new Point(0, 0));
                var bottomRight = _window.PointToScreen(new Point(_window.ActualWidth, _window.ActualHeight));

                bool needsHide = !_isAffinitySet;
                if (needsHide)
                {
                    _ = ShowWindow(hwnd, SwHide);
                    _window.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));
                }
                var width = (int)Math.Max(1, Math.Round(bottomRight.X - topLeft.X));
                var height = (int)Math.Max(1, Math.Round(bottomRight.Y - topLeft.Y));

                if (width <= 0 || height <= 0)
                {
                    _isCapturing = false;
                    return;
                }

                Task.Run(() => ScreenCaptureHelper.CaptureRegion((int)topLeft.X, (int)topLeft.Y, width, height, 0.75))
                    .ContinueWith(t =>
                    {
                        _window.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                if (needsHide)
                                {
                                    _ = ShowWindow(hwnd, SwShowNoActivate);
                                    _window.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));
                                }

                                if (t.Status == TaskStatus.RanToCompletion)
                                {
                                    var snapshot = t.Result;
                                    if (snapshot != null && _backdropBrush != null)
                                    {
                                        _backdropBrush.ImageSource = snapshot;
                                        _backdropBrush.Viewbox = new Rect(0, 0, snapshot.PixelWidth, snapshot.PixelHeight);
                                        _backdropBrush.ViewboxUnits = BrushMappingMode.Absolute;
                                    }
                                }
                            }
                            catch
                            {
                                // Ignore errors in continuation
                            }
                            finally
                            {
                                _isCapturing = false;
                            }
                        }));
                    });
            }
            catch
            {
                _isCapturing = false;
            }
        }

        private void TrySetExclusionAffinity()
        {
            try
            {
                var hwnd = new WindowInteropHelper(_window).Handle;
                if (hwnd == IntPtr.Zero)
                {
                    return;
                }

                // WDA_EXCLUDEFROMCAPTURE = 0x00000011 (available since Windows 10 2004)
                if (SetWindowDisplayAffinity(hwnd, 0x00000011))
                {
                    _isAffinitySet = true;
                }
            }
            catch
            {
                // API might not be available or fails
            }
        }

        private void SetBlurBehind(bool enabled)
        {
            try
            {
                var hwnd = new WindowInteropHelper(_window).Handle;
                if (hwnd == IntPtr.Zero)
                {
                    return;
                }

                var blurBehind = new DwmBlurBehind
                {
                    Flags = DwmBlurBehindFlags.Enable | DwmBlurBehindFlags.BlurRegion,
                    Enabled = enabled,
                    BlurRegion = CreateRectRgn(0, 0, -1, -1)
                };

                _ = DwmEnableBlurBehindWindow(hwnd, ref blurBehind);

                if (blurBehind.BlurRegion != IntPtr.Zero)
                {
                    _ = DeleteObject(blurBehind.BlurRegion);
                }
            }
            catch
            {
                // DWM can be unavailable in remote or restricted sessions.
            }
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmEnableBlurBehindWindow(IntPtr hwnd, ref DwmBlurBehind blurBehind);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRectRgn(int left, int top, int right, int bottom);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint affinity);

    [StructLayout(LayoutKind.Sequential)]
    private struct DwmBlurBehind
    {
        public DwmBlurBehindFlags Flags;
        public bool Enabled;
        public IntPtr BlurRegion;
        public bool TransitionOnMaximized;
    }

    [Flags]
    private enum DwmBlurBehindFlags
    {
        Enable = 0x00000001,
        BlurRegion = 0x00000002,
        TransitionOnMaximized = 0x00000004
    }
}
