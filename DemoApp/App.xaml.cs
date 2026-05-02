using System.Windows;

namespace DemoApp;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        ScreenCaptureHelper.CaptureFullScreen();
        base.OnStartup(e);
    }
}
