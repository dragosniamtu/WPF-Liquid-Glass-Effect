using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;

namespace DemoApp;

/// <summary>Explicit, session-only source selection; opening the demo never starts window capture.</summary>
public sealed class RecordingCaptureModel : INotifyPropertyChanged
{
    private CaptureWindow? selectedWindow;
    private bool isRecording;
    private IntPtr activeSource;
    private IReadOnlyList<CaptureWindow> windows = Array.Empty<CaptureWindow>();

    public RecordingCaptureModel()
    {
        RefreshCommand = new RefreshWindowsCommand(Refresh);
        Refresh();
    }

    public IReadOnlyList<CaptureWindow> Windows => windows;
    public ICommand RefreshCommand { get; }

    public CaptureWindow? SelectedWindow
    {
        get => selectedWindow;
        set
        {
            if (selectedWindow == value) return;
            selectedWindow = value;
            // Only the checkbox can change capture mode. A refresh/selection loss
            // must never quietly restore desktop capture while recording.
            if (isRecording && value is not null) activeSource = value.Handle;
            OnPropertyChanged();
            NotifyMode();
        }
    }

    public bool IsRecording
    {
        get => isRecording;
        set
        {
            if (isRecording == value || (value && selectedWindow is null)) return;
            isRecording = value;
            activeSource = value ? selectedWindow!.Handle : IntPtr.Zero;
            OnPropertyChanged();
            NotifyMode();
        }
    }

    public bool CanToggleRecording => isRecording || selectedWindow is not null;
    public IntPtr CaptureSourceWindow => isRecording ? activeSource : IntPtr.Zero;
    public string StatusText => isRecording
        ? "Recording mode requested. Keep the source restored and behind this window. Check FX state below for errors."
        : "Desktop mode: this glass window is excluded from screen recordings. Select a source, then enable recording mode.";

    private void Refresh()
    {
        CaptureWindow? previous = selectedWindow;
        var available = new List<CaptureWindow>();
        EnumWindows((handle, _) =>
        {
            if (!IsWindowVisible(handle) || IsIconic(handle)) return true;
            uint threadId = GetWindowThreadProcessId(handle, out uint processId);
            if (threadId == 0 || processId == (uint)Environment.ProcessId) return true;
            if (DwmGetWindowAttribute(handle, 14, out int cloaked, sizeof(int)) != 0 || cloaked != 0) return true;
            int length = GetWindowTextLength(handle);
            if (length <= 0) return true;
            var title = new StringBuilder(Math.Min(length + 1, 4096));
            if (GetWindowText(handle, title, title.Capacity) <= 0) return true;
            available.Add(new CaptureWindow(handle, processId, title.ToString()));
            return true;
        }, IntPtr.Zero);
        available.Sort((left, right) => StringComparer.CurrentCultureIgnoreCase.Compare(left.Title, right.Title));

        CaptureWindow? retained = previous is null ? null : available.Find(window =>
            window.Handle == previous.Handle && window.ProcessId == previous.ProcessId);
        if (previous is not null && retained is null)
        {
            // Keep the exact selection if the source closes/minimizes. The SDK
            // reports source loss and never silently substitutes another HWND.
            retained = previous;
            available.Insert(0, retained);
        }
        windows = available;
        OnPropertyChanged(nameof(Windows));
        SelectedWindow = retained;
        OnPropertyChanged(nameof(SelectedWindow));
    }

    private void NotifyMode()
    {
        OnPropertyChanged(nameof(CanToggleRecording));
        OnPropertyChanged(nameof(CaptureSourceWindow));
        OnPropertyChanged(nameof(StatusText));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public sealed record CaptureWindow(IntPtr Handle, uint ProcessId, string Title);

    private sealed class RefreshWindowsCommand(Action refresh) : ICommand
    {
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => refresh();
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }

    private delegate bool EnumWindowsCallback(IntPtr hwnd, IntPtr parameter);
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);
    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hwnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int maxCount);
    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, uint attribute, out int value, int size);
}
