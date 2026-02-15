using Avalonia.Controls;
using Avalonia.Media;
using System;
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
                }
            }
        }
        catch
        {
            // 인터넷 연결이 없거나 파일이 없으면 조용히 무시
        }
    }

    // 실수 변환 도우미 함수: 빈 값이면 기본값 사용
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

    // 입력값이 변경될 때마다 모든 계산 수행
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
