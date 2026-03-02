using Avalonia;
using System;
using System.Reflection;

namespace AlbionCalculator;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--version")
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Console.WriteLine(version?.ToString(3) ?? "0.0.0");
            return;
        }

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}