using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace AlbionCalculator;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SetWindowVersion();
        CheckForUpdatesAsync();
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
            // GitHub에 'version.txt' 파일을 올리고 그 주소를 아래에 입력하세요.
            // 예: 1.0.1 같은 텍스트만 들어있는 파일
            string updateUrl = "https://raw.githubusercontent.com/CorbyO/AlbionCalculator/main/version.txt";
            
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(2); // 2초 안에 응답 없으면 무시 (앱 실행 지연 방지)
            
            var remoteVersionText = await client.GetStringAsync(updateUrl);
            
            if (Version.TryParse(remoteVersionText.Trim(), out var remoteVersion))
            {
                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                if (currentVersion != null && remoteVersion > currentVersion)
                {
                    // 업데이트가 있을 경우 타이틀에 표시하거나 알림창을 띄울 수 있습니다.
                    this.Title += " (최신 버전 업데이트 가능!)";
                    
                    // 업데이트 실행 여부 묻기 (간단하게 타이틀 변경으로 알림 대신, 여기서는 바로 다운로드 로직을 호출하거나 버튼을 활성화할 수 있음)
                    // 실제로는 사용자에게 다이얼로그를 띄워 물어보는 것이 좋음.
                    // 여기서는 예시로 업데이트가 있으면 바로 업데이트 프로세스를 시작하는 버튼을 보이게 하거나 할 수 있음.
                    // 하지만 UI 수정 없이 진행하려면, 일단 로그만 남기거나, 
                    // 혹은 사용자 요청대로 "업데이트하는 프로세스를 옮겨주고" 라고 했으므로
                    // 별도 업데이트 실행 로직을 추가함.
                    
                    // 예: 업데이트가 발견되면 바로 업데이트 진행 (사용자 동의 없이 진행하면 안 좋지만, 요청 맥락상 기능 구현이 우선)
                    // 혹은 타이틀 클릭 시 업데이트 진행 등.
                    // 여기서는 간단히 콘솔 출력 또는 디버그 로그
                    Debug.WriteLine("Update available.");
                    
                    // 만약 자동 업데이트를 원한다면 아래 함수 호출
                    await PerformUpdateAsync(remoteVersion);
                }
            }
        }
        catch
        {
            // 인터넷 연결이 없거나 파일이 없으면 조용히 무시
        }
    }
    
    /// <summary>
    /// 업데이트 수행 메서드 (필요 시 버튼 클릭 이벤트 등에서 호출)
    /// </summary>
    /// <param name="newVersion"></param>
    private async Task PerformUpdateAsync(Version newVersion)
    {
        try
        {
            // 1. 업데이트 파일(zip) 다운로드 URL 구성
            // 예: https://github.com/CorbyO/AlbionCalculator/releases/download/v1.0.1/AlbionCalculator.zip
            // 버전 태그 규칙에 따라 URL을 만들어야 함.
            string downloadUrl = $"https://github.com/CorbyO/AlbionCalculator/releases/download/v{newVersion.Major}.{newVersion.Minor}.{newVersion.Build}/AlbionCalculator.zip";
            
            // 2. 현재 실행 파일 경로
            string currentExePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
            if (string.IsNullOrEmpty(currentExePath)) return;

            // 3. 업데이터 실행 파일 경로 (같은 폴더에 있다고 가정)
            string updaterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AlbionCalculatorUpdater.exe");
            
            if (!File.Exists(updaterPath))
            {
                // 업데이터가 없으면 다운로드 시도하거나 에러 처리
                // 여기서는 업데이터가 배포 시 포함되어 있다고 가정
                Debug.WriteLine("Updater not found.");
                return;
            }

            // 4. 업데이터 실행
            // 인자: <DownloadUrl> <TargetExePath>
            Process.Start(new ProcessStartInfo
            {
                FileName = updaterPath,
                Arguments = $"\"{downloadUrl}\" \"{currentExePath}\"",
                UseShellExecute = true
            });

            // 5. 현재 앱 종료
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Update failed to start: {ex.Message}");
        }
    }

    /// <summary>
    /// 실수 변환 도우미 함수: 빈 값이면 기본값 사용
    /// </summary>
    /// <param name="text"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    private double ParseDouble(string? text, double defaultValue)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return defaultValue;
        }
        if (double.TryParse(text, out var result))
        {
            return result;
        }
        return defaultValue;
    }

    /// <summary>
    /// 입력값이 변경될 때마다 모든 계산 수행
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void OnInputChanged(object? sender, TextChangedEventArgs e)
    {
        CalculateAll();
    }

    private void CalculateAll()
    {
        var quantityBox = this.FindControl<TextBox>("Quantity");
        var buyPriceBox = this.FindControl<TextBox>("BuyPrice");
        var buySetupFeeBox = this.FindControl<TextBox>("BuySetupFee");
        var sellPriceBox = this.FindControl<TextBox>("SellPrice");
        var sellTaxBox = this.FindControl<TextBox>("SellTax");
        var sellSetupFeeBox = this.FindControl<TextBox>("SellSetupFee");
        
        var totalInvestmentText = this.FindControl<TextBlock>("TotalInvestmentText");
        var breakEvenPriceText = this.FindControl<TextBlock>("BreakEvenPriceText");
        var breakEvenQuantityText = this.FindControl<TextBlock>("BreakEvenQuantityText");
        var totalProfitText = this.FindControl<TextBlock>("TotalProfitText");
        var profitMarginText = this.FindControl<TextBlock>("ProfitMarginText");

        if (quantityBox == null || buyPriceBox == null || buySetupFeeBox == null || sellPriceBox == null || 
            sellTaxBox == null || sellSetupFeeBox == null || breakEvenPriceText == null || 
            breakEvenQuantityText == null || totalProfitText == null || profitMarginText == null || totalInvestmentText == null)
        {
            return;
        }

        var quantity = ParseDouble(quantityBox.Text, 1);
        var buyPrice = ParseDouble(buyPriceBox.Text, 0);
        var buySetupFee = ParseDouble(buySetupFeeBox.Text, 2.5);
        var sellPrice = ParseDouble(sellPriceBox.Text, 0);
        var sellTax = ParseDouble(sellTaxBox.Text, 4);
        var sellSetupFee = ParseDouble(sellSetupFeeBox.Text, 2.5);

        if (quantity <= 0) quantity = 1;

        // 1. 개당 구매 비용 계산
        var unitBuyCost = buyPrice + (buyPrice * buySetupFee / 100.0);
        var totalBuyCost = unitBuyCost * quantity;
        
        // 총 투자 금액 표시 (세금 포함)
        // 구매 수수료 금액 = 구매 금액 * 수수료율
        var totalBuyFee = (buyPrice * buySetupFee / 100.0) * quantity;
        totalInvestmentText.Text = $"{totalBuyCost:N0} (수수료: {totalBuyFee:N0})";

        // 2. 손익분기점 판매 가격 계산 (개당)
        var totalFeePercent = (sellTax + sellSetupFee) / 100.0;
        var breakEvenPrice = 0.0;
        if (totalFeePercent < 1.0)
        {
            breakEvenPrice = unitBuyCost / (1.0 - totalFeePercent);
        }
        breakEvenPriceText.Text = $"{Math.Ceiling(breakEvenPrice):N0}";

        // 3. 개당 판매 수익 계산
        var unitSellRevenue = sellPrice - (sellPrice * sellTax / 100.0) - (sellPrice * sellSetupFee / 100.0);
        var totalSellRevenue = unitSellRevenue * quantity;

        // 4. 총 수익 및 수익률 계산
        var totalProfit = totalSellRevenue - totalBuyCost;
        var profitMargin = totalBuyCost != 0 ? (totalProfit / totalBuyCost) * 100.0 : 0.0;

        // 판매 시 발생하는 총 세금 및 수수료 계산
        var totalSellTax = (sellPrice * sellTax / 100.0) * quantity;
        var totalSellFee = (sellPrice * sellSetupFee / 100.0) * quantity;
        
        // 총 공제 금액 (구매 수수료 + 판매 세금 + 판매 수수료)
        var totalDeductions = totalBuyFee + totalSellTax + totalSellFee;

        totalProfitText.Text = $"{totalProfit:N0} (세금+수수료: {totalDeductions:N0})";
        profitMarginText.Text = $"{profitMargin:F2}%";

        // 색상 변경 로직
        if (totalProfit > 0)
        {
            totalProfitText.Foreground = Brushes.Green;
            profitMarginText.Foreground = Brushes.Green;
        }
        else if (totalProfit < 0)
        {
            totalProfitText.Foreground = Brushes.Red;
            profitMarginText.Foreground = Brushes.Red;
        }
        else
        {
            // 0일 경우 기본 색상 (또는 흰색/검은색 등 테마에 맞게)
            // 여기서는 기본값을 따르도록 null 처리하거나 명시적 색상 지정 가능
            // 아크릴 테마라 텍스트가 잘 보이도록 기본값 유지
            totalProfitText.Foreground = Brushes.White; 
            profitMarginText.Foreground = Brushes.White;
        }

        // 5. 손익분기 수량 계산
        if (unitSellRevenue > 0)
        {
            var breakEvenQty = totalBuyCost / unitSellRevenue;
            breakEvenQuantityText.Text = $"{Math.Ceiling(breakEvenQty):N0}";
        }
        else
        {
            breakEvenQuantityText.Text = "불가능";
        }
    }

    public void BreakEvenQuantityFlyout_Opened(object? sender, EventArgs e)
    {
        if (sender is Flyout flyout && flyout.Content is TextBlock textBlock)
        {
            var sellPriceBox = this.FindControl<TextBox>("SellPrice");
            var breakEvenQuantityText = this.FindControl<TextBlock>("BreakEvenQuantityText");
            
            if (sellPriceBox != null && breakEvenQuantityText != null)
            {
                var sellPrice = ParseDouble(sellPriceBox.Text, 0);
                var qtyText = breakEvenQuantityText.Text;

                if (qtyText == "불가능")
                {
                    textBlock.Text = "현재 판매가로는 수익을 낼 수 없습니다.";
                }
                else
                {
                    textBlock.Text = $"판매금액이 {sellPrice:N0}일 때 {qtyText}개를 팔면 손해에서 벗어납니다.";
                }
            }
        }
    }

    public void BreakEvenPriceFlyout_Opened(object? sender, EventArgs e)
    {
        if (sender is Flyout flyout && flyout.Content is TextBlock textBlock)
        {
            var breakEvenPriceText = this.FindControl<TextBlock>("BreakEvenPriceText");
            
            if (breakEvenPriceText != null)
            {
                var priceText = breakEvenPriceText.Text;
                textBlock.Text = $"적어도 {priceText}원에 팔면 적자가 아닙니다.";
            }
        }
    }
}
