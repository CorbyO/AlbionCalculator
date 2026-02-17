using Avalonia.Controls;
using Avalonia.Threading;
using System.Diagnostics;
using System.IO.Compression;

namespace AlbionCalculatorUpdater;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        await CheckAndRunUpdate();
    }

    private async Task CheckAndRunUpdate()
    {
        string targetExeName = "AlbionCalculator.exe";
        string currentDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtime");
        Log(currentDir);
        string targetExePath = Path.Combine(currentDir, targetExeName);
        string versionUrl = "https://raw.githubusercontent.com/CorbyO/AlbionCalculator/main/version.txt";

        try
        {
            UpdateStatus("업데이트 확인중...");
            await Task.Delay(500); // Small delay for UX

            string targetVersionText;
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                targetVersionText = (await client.GetStringAsync(versionUrl)).Trim();
            }
            Version targetVersion = ParseVersion(targetVersionText);

            Version currentVersion = new Version(0, 0, 0);
            bool acExists = File.Exists(targetExePath);
            if (acExists)
            {
                currentVersion = await GetCurrentVersion(targetExePath);
            }

            if (!acExists || currentVersion < targetVersion)
            {
                UpdateStatus("업데이트 중...");
                await DownloadAndExtract(targetVersionText, currentDir);
            }

            UpdateStatus("시작중...");
            await Task.Delay(500); // Give user a moment to see the status
            
            StartAc(targetExePath);
            Close();
        }
        catch (Exception ex)
        {
            Log(ex.ToString());
            
            if (File.Exists(targetExePath))
            {
                StartAc(targetExePath);
                Close();
            }
            else
            {
                UpdateStatus($"Error: {ex.Message}");
                await Task.Delay(3000);
                Close();
            }
        }
    }

    private void UpdateStatus(string status)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var statusTextBlock = this.FindControl<TextBlock>("StatusText");
            if (statusTextBlock != null)
            {
                statusTextBlock.Text = status;
            }
        });
    }

    private Version ParseVersion(string versionText)
    {
        string v = versionText.Trim();
        if (v.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            v = v.Substring(1);
        }
        if (Version.TryParse(v, out var result))
        {
            return result;
        }
        return new Version(0, 0, 0);
    }

    private async Task<Version> GetCurrentVersion(string exePath)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                return ParseVersion(output);
            }
        }
        catch
        {
            // Ignore errors
        }
        return new Version(0, 0, 0);
    }

    private async Task DownloadAndExtract(string versionText, string destinationDir)
    {
        string downloadUrl = $"https://github.com/CorbyO/AlbionCalculator/releases/download/{versionText}/AlbionCalculator.zip";
        string tempZipPath = Path.Combine(Path.GetTempPath(), "AlbionCalculatorUpdate.zip");

        using (var client = new HttpClient())
        {
            var bytes = await client.GetByteArrayAsync(downloadUrl);
            await File.WriteAllBytesAsync(tempZipPath, bytes);
        }

        // Kill ac.exe if running
        var processes = Process.GetProcessesByName("AlbionCalculator");
        foreach (var p in processes)
        {
            try { p.Kill(); await p.WaitForExitAsync(); } catch { }
        }
        
        using (ZipArchive archive = ZipFile.OpenRead(tempZipPath))
        {
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string destinationPath = Path.Combine(destinationDir, entry.FullName);
                string? destinationSubDir = Path.GetDirectoryName(destinationPath);
                Log(destinationSubDir);

                if (!string.IsNullOrEmpty(destinationSubDir) && !Directory.Exists(destinationSubDir))
                {
                    Directory.CreateDirectory(destinationSubDir);
                }

                if (!string.IsNullOrEmpty(entry.Name))
                {
                    entry.ExtractToFile(destinationPath, true);
                }
            }
        }

        if (File.Exists(tempZipPath))
        {
            try { File.Delete(tempZipPath); } catch { }
        }
    }

    private void StartAc(string exePath)
    {
        if (File.Exists(exePath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true
            });
        }
    }

    [Conditional("DEBUG")]
    private void Log(string? message)
    {
        Console.WriteLine(message);
    }
}