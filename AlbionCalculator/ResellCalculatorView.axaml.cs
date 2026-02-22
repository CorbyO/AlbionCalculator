using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;

namespace AlbionCalculator;

public partial class ResellCalculatorView : UserControl
{
    public ResellCalculatorView()
    {
        InitializeComponent();
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

    public async void OnCopyText(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Content is TextBlock textBlock)
        {
            var text = textBlock.Text;
            if (!string.IsNullOrEmpty(text))
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel?.Clipboard != null)
                {
                    await topLevel.Clipboard.SetTextAsync(text);
                }
            }
        }
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