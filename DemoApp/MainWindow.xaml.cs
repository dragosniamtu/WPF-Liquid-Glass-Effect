using System.Windows;

namespace DemoApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // Window chrome only; capture and shader rendering remain inside the SDK.
    private void MinimizeWindow(object sender, RoutedEventArgs e) =>
        SystemCommands.MinimizeWindow(this);

    private void ToggleMaximizeWindow(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
            SystemCommands.RestoreWindow(this);
        else
            SystemCommands.MaximizeWindow(this);
    }

    private void CloseWindow(object sender, RoutedEventArgs e) =>
        SystemCommands.CloseWindow(this);
}
