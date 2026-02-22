using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace AlbionCalculator;

public partial class MainWindow : Window
{
    private Dictionary<string, UserControl> _views = new Dictionary<string, UserControl>();

    public MainWindow()
    {
        InitializeComponent();
        SetWindowVersion();
        CheckForUpdatesAsync();

        // 초기 뷰 설정
        _views["Welcome"] = new WelcomeView();
        _views["Resell"] = new ResellCalculatorView();
        _views["FishSauce"] = new FishSauceCalculatorView();

        var functionList = this.FindControl<ListBox>("FunctionList");
        if (functionList != null)
        {
            functionList.SelectedIndex = 0; // Welcome 선택
        }

        // 애니메이션 설정 (커스텀 트랜지션 적용)
        var contentControl = this.FindControl<TransitioningContentControl>("MainContent");
        if (contentControl != null)
        {
            // 여기서 애니메이션 파라미터를 조절하세요.
            // 1. fadeOutDuration: 기존 화면이 사라지는 시간 (0.2초)
            // 2. slideInDuration: 새 화면이 나타나는 시간 (0.3초)
            // 3. offset: 새 화면이 올라오는 시작 위치 (30픽셀 아래)
            contentControl.PageTransition = new CustomPageTransition(
                fadeOutDuration: TimeSpan.FromSeconds(0.2), 
                slideInDuration: TimeSpan.FromSeconds(0.3), 
                offset: 30);
        }
    }

    private void FunctionList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is ListBoxItem selectedItem)
        {
            var tag = selectedItem.Tag as string;
            if (tag != null && _views.ContainsKey(tag))
            {
                var contentControl = this.FindControl<TransitioningContentControl>("MainContent");
                if (contentControl != null)
                {
                    contentControl.Content = _views[tag];
                }
            }
        }
    }

    private void SetWindowVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        if (version != null)
        {
            this.Title = $"Albion Calculator v{version.Major}.{version.Minor}.{version.Build}";
        }
    }

    private async void CheckForUpdatesAsync()
    {
        try
        {
            string updateUrl = "https://raw.githubusercontent.com/CorbyO/AlbionCalculator/main/version.txt";
            
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(2);
            
            var remoteVersionText = await client.GetStringAsync(updateUrl);
            
            if (Version.TryParse(remoteVersionText.Trim(), out var remoteVersion))
            {
                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                if (currentVersion != null && remoteVersion > currentVersion)
                {
                    this.Title += " (최신 버전 업데이트 가능!)";
                    Debug.WriteLine("Update available.");
                    await PerformUpdateAsync(remoteVersion);
                }
            }
        }
        catch
        {
            // Ignore errors
        }
    }
    
    private async Task PerformUpdateAsync(Version newVersion)
    {
        try
        {
            string downloadUrl = $"https://github.com/CorbyO/AlbionCalculator/releases/download/v{newVersion.Major}.{newVersion.Minor}.{newVersion.Build}/AlbionCalculator.zip";
            string currentExePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
            if (string.IsNullOrEmpty(currentExePath)) return;

            string updaterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AlbionCalculatorUpdater.exe");
            
            if (!File.Exists(updaterPath))
            {
                Debug.WriteLine("Updater not found.");
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = updaterPath,
                Arguments = $"\"{downloadUrl}\" \"{currentExePath}\"",
                UseShellExecute = true
            });

            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Update failed to start: {ex.Message}");
        }
    }
}

/// <summary>
/// 커스텀 페이지 전환 애니메이션
/// 1. 기존 화면 Fade Out (EaseInQuad)
/// 2. 새 화면 Slide Up + Fade In (EaseOutQuad)
/// </summary>
public class CustomPageTransition : IPageTransition
{
    private readonly TimeSpan _fadeOutDuration;
    private readonly TimeSpan _slideInDuration;
    private readonly double _offset;

    public CustomPageTransition(TimeSpan fadeOutDuration, TimeSpan slideInDuration, double offset)
    {
        _fadeOutDuration = fadeOutDuration;
        _slideInDuration = slideInDuration;
        _offset = offset;
    }

    public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        // [버그 수정] 애니메이션 시작 시점에 새 화면이 잠시 보이는 현상 방지
        // 새 화면을 미리 투명하게 설정하여 기존 화면이 사라지는 동안 보이지 않게 함
        if (to != null)
        {
            to.Opacity = 0;
        }

        // 1. 기존 화면 Fade Out
        if (from != null)
        {
            var fadeOutAnimation = new Animation
            {
                Duration = _fadeOutDuration,
                Easing = new QuadraticEaseIn(),
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0d),
                        Setters = { new Setter(Visual.OpacityProperty, 1d) }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1d),
                        Setters = { new Setter(Visual.OpacityProperty, 0d) }
                    }
                }
            };
            await fadeOutAnimation.RunAsync(from, cancellationToken);
            from.IsVisible = false;
            from.Opacity = 1; // 리셋
        }

        // 2. 새 화면 Slide Up + Fade In
        if (to != null)
        {
            to.IsVisible = true;
            // to.Opacity = 0; // 위에서 이미 설정함

            // RenderTransform이 TranslateTransform인지 확인하고 아니면 새로 할당
            if (!(to.RenderTransform is TranslateTransform))
            {
                to.RenderTransform = new TranslateTransform();
            }

            var slideInAnimation = new Animation
            {
                Duration = _slideInDuration,
                Easing = new QuadraticEaseOut(),
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0d),
                        Setters =
                        {
                            new Setter(Visual.OpacityProperty, 0d),
                            new Setter(TranslateTransform.YProperty, _offset)
                        }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1d),
                        Setters =
                        {
                            new Setter(Visual.OpacityProperty, 1d),
                            new Setter(TranslateTransform.YProperty, 0d)
                        }
                    }
                }
            };
            await slideInAnimation.RunAsync(to, cancellationToken);
        }
    }
}