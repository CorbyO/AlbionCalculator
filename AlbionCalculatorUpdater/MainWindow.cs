using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace AlbionCalculatorUpdater;

public class MainWindow : Form
{
    private Label _statusLabel;
    private Panel _loadingSpinner;
    private Timer _animationTimer;
    
    private float _rotation = 0f;
    private float _arcStart = 0f;
    private float _arcLength = 10f;
    private bool _isExpanding = true;

    public MainWindow()
    {
        InitializeComponent();
        Load += OnLoad;
    }

    private void InitializeComponent()
    {
        Text = "AlbionCalculator Updater";
        Size = new(300, 150);
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(45, 45, 45); // Dark background

        var titleLabel = new Label
        {
            Text = "Albion Calculator Updater",
            ForeColor = Color.White,
            Font = new("Segoe UI", 12, FontStyle.Bold),
            AutoSize = true,
            Location = new(0, 20),
            TextAlign = ContentAlignment.MiddleCenter
        };
        titleLabel.Location = new((ClientSize.Width - titleLabel.PreferredWidth) / 2, 20);

        _statusLabel = new()
        {
            Text = "Checking for updates...",
            ForeColor = Color.FromArgb(204, 204, 204), // Light gray
            AutoSize = true,
            Location = new(0, 50),
            TextAlign = ContentAlignment.MiddleCenter
        };
        _statusLabel.Location = new((ClientSize.Width - _statusLabel.PreferredWidth) / 2, 50);

        _loadingSpinner = new()
        {
            Size = new(40, 40),
            Location = new((ClientSize.Width - 40) / 2, 80),
            BackColor = Color.Transparent
        };
        
        // Enable DoubleBuffered to prevent flickering
        typeof(Panel).InvokeMember("DoubleBuffered",
            BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
            null, _loadingSpinner, [true]);

        _loadingSpinner.Paint += LoadingSpinner_Paint;

        _animationTimer = new()
        {
            Interval = 15 // Faster updates for smoother animation (approx 60fps)
        };
        _animationTimer.Tick += AnimationTimer_Tick;
        _animationTimer.Start();

        Controls.Add(titleLabel);
        Controls.Add(_statusLabel);
        Controls.Add(_loadingSpinner);
    }

    private async void OnLoad(object? sender, EventArgs e)
    {
        await CheckAndRunUpdate();
    }

    private async Task CheckAndRunUpdate()
    {
        var targetExeName = "AlbionCalculator.exe";
        var currentDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtime");
        Log(currentDir);
        var targetExePath = Path.Combine(currentDir, targetExeName);
        var versionUrl = "https://raw.githubusercontent.com/CorbyO/AlbionCalculator/main/AlbionCalculator/AlbionCalculator.csproj";

        try
        {
            UpdateStatus("업데이트 확인중...");
            await Task.Delay(500);

            string targetVersionText;
            using (var client = new WebClient())
            {
                client.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                var xmlContent = await client.DownloadStringTaskAsync(versionUrl);
                var doc = XDocument.Parse(xmlContent);
                targetVersionText = doc.Descendants("Version").FirstOrDefault()?.Value;
                
                if (string.IsNullOrWhiteSpace(targetVersionText))
                {
                    throw new Exception("Version info not found in csproj");
                }
                targetVersionText = targetVersionText.Trim();
            }
            var targetVersion = ParseVersion(targetVersionText);

            var currentVersion = new Version(0, 0, 0);
            var acExists = File.Exists(targetExePath);
            if (acExists)
            {
                currentVersion = await GetCurrentVersion(targetExePath);
            }
            Log($"Current version: {currentVersion}");

            if (!acExists || currentVersion < targetVersion)
            {
                UpdateStatus("업데이트 중...");
                await DownloadAndExtract(targetVersionText, currentDir);
            }

            UpdateStatus("시작중...");
            await Task.Delay(500);
            
            StartAc(targetExePath);
            _animationTimer.Stop();
            Close();
        }
        catch (Exception ex)
        {
            Log(ex.ToString());
            
            if (File.Exists(targetExePath))
            {
                StartAc(targetExePath);
                _animationTimer.Stop();
                Close();
            }
            else
            {
                UpdateStatus($"Error: {ex.Message}");
                await Task.Delay(3000);
                _animationTimer.Stop();
                Close();
            }
        }
    }

    private void UpdateStatus(string status)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<string>(UpdateStatus), status);
            return;
        }
        
        _statusLabel.Text = status;
        _statusLabel.Location = new((ClientSize.Width - _statusLabel.PreferredWidth) / 2, 50);
    }

    private void LoadingSpinner_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        
        // Draw the arc
        using var pen = new Pen(Color.White, 4);
        pen.StartCap = LineCap.Round;
        pen.EndCap = LineCap.Round;

        // Define the circle area with some padding
        const float padding = 4;
        const float x = padding;
        const float y = padding;
        var size = Math.Min(_loadingSpinner.Width, _loadingSpinner.Height) - (padding * 2);

        // DrawArc arguments: pen, x, y, width, height, startAngle, sweepAngle
        // We add _rotation to _arcStart to combine the global rotation with the local expansion/contraction
        e.Graphics.DrawArc(pen, x, y, size, size, _rotation + _arcStart, _arcLength);
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        _rotation += 4;
        if (_rotation >= 360) _rotation -= 360;

        if (_isExpanding)
        {
            _arcLength += 6;
            if (_arcLength >= 260)
            {
                _isExpanding = false;
            }
        }
        else
        {
            _arcLength -= 6;
            _arcStart += 12;
            if (_arcStart >= 360) _arcStart -= 360;

            if (_arcLength <= 10)
            {
                _isExpanding = true;
            }
        }

        _loadingSpinner.Invalidate();
    }

    private Version ParseVersion(string versionText)
    {
        var v = versionText.Trim();
        if (v.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            v = v.Substring(1);
        }
        if (Version.TryParse(v, out var result))
        {
            return result;
        }
        return new(0, 0, 0);
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
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                return ParseVersion(output);
            }
        }
        catch
        {
            // Ignore errors
        }
        return new(0, 0, 0);
    }

    private async Task DownloadAndExtract(string versionText, string destinationDir)
    {
        var downloadUrl = $"https://github.com/CorbyO/AlbionCalculator/releases/download/{versionText}/AlbionCalculator.zip";
        var tempZipPath = Path.Combine(Path.GetTempPath(), "AlbionCalculatorUpdate.zip");

        using (var client = new WebClient())
        {
            client.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
            Log($"download: {downloadUrl}");
            await client.DownloadFileTaskAsync(downloadUrl, tempZipPath);
        }

        var processes = Process.GetProcessesByName("AlbionCalculator");
        foreach (var p in processes)
        {
            try { p.Kill(); await p.WaitForExitAsync(); } catch { }
        }
        
        using (var archive = ZipFile.OpenRead(tempZipPath))
        {
            foreach (var entry in archive.Entries)
            {
                var destinationPath = Path.Combine(destinationDir, entry.FullName);
                var destinationSubDir = Path.GetDirectoryName(destinationPath);
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