using System;
using Avalonia;

namespace LottieDemo.Desktop;

class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .With(new Win32PlatformOptions
            {
                AllowEglInitialization = true,
                UseWindowsUIComposition = true,
            })
            .With(new X11PlatformOptions
            {
                UseGpu = true,
            })
            .With(new AvaloniaNativePlatformOptions
            {
                UseGpu = true,
            })
            .With(new MacOSPlatformOptions
            {
                ShowInDock = true
            })
            .LogToTrace();
}
