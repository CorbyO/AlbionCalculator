using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Threading.Tasks;

namespace AlbionCalculator;

public partial class FishSauceCalculatorView : UserControl
{
    public FishSauceCalculatorView()
    {
        InitializeComponent();
    }

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
        // Inputs
        var choppedFishBox = this.FindControl<TextBox>("ChoppedFishPrice");
        var seaweedBox = this.FindControl<TextBox>("SeaweedPrice");
        
        var basicMarketBox = this.FindControl<TextBox>("BasicMarketPrice");
        var fancyMarketBox = this.FindControl<TextBox>("FancyMarketPrice");
        var specialMarketBox = this.FindControl<TextBox>("SpecialMarketPrice");

        // Outputs (TextBlock inside Button)
        var basicMaterialText = this.FindControl<TextBlock>("BasicMaterialPrice");
        var basicDiffText = this.FindControl<TextBlock>("BasicDiff");
        
        var fancyMaterialText = this.FindControl<TextBlock>("FancyMaterialPrice");
        var fancyDiffText = this.FindControl<TextBlock>("FancyDiff");
        
        var specialMaterialText = this.FindControl<TextBlock>("SpecialMaterialPrice");
        var specialDiffText = this.FindControl<TextBlock>("SpecialDiff");

        if (choppedFishBox == null || seaweedBox == null || 
            basicMarketBox == null || fancyMarketBox == null || specialMarketBox == null ||
            basicMaterialText == null || basicDiffText == null ||
            fancyMaterialText == null || fancyDiffText == null ||
            specialMaterialText == null || specialDiffText == null)
        {
            return;
        }

        // Check if inputs are empty
        bool isChoppedFishEmpty = string.IsNullOrWhiteSpace(choppedFishBox.Text);
        bool isSeaweedEmpty = string.IsNullOrWhiteSpace(seaweedBox.Text);

        // Values
        var choppedFishPrice = ParseDouble(choppedFishBox.Text, 0);
        var seaweedPrice = ParseDouble(seaweedBox.Text, 0);
        
        var basicMarketPrice = ParseDouble(basicMarketBox.Text, 0);
        var fancyMarketPrice = ParseDouble(fancyMarketBox.Text, 0);
        var specialMarketPrice = ParseDouble(specialMarketBox.Text, 0);

        // Calculation
        var basicMaterialCost = (choppedFishPrice * 15) + seaweedPrice;
        var fancyMaterialCost = (choppedFishPrice * 45) + (seaweedPrice * 3);
        var specialMaterialCost = (choppedFishPrice * 135) + (seaweedPrice * 9);

        // Update Displays
        if (!(isChoppedFishEmpty && isSeaweedEmpty))
        {
            UpdateMaterialPrice(basicMaterialText, basicMaterialCost);
            UpdateMaterialPrice(fancyMaterialText, fancyMaterialCost);
            UpdateMaterialPrice(specialMaterialText, specialMaterialCost);
            
            UpdateDiff(basicMarketPrice, basicMaterialCost, basicDiffText);
            UpdateDiff(fancyMarketPrice, fancyMaterialCost, fancyDiffText);
            UpdateDiff(specialMarketPrice, specialMaterialCost, specialDiffText);
        }
        else
        {
            basicMaterialText.Text = "0";
            fancyMaterialText.Text = "0";
            specialMaterialText.Text = "0";
            
            basicDiffText.Text = "0 이득 (0%)";
            fancyDiffText.Text = "0 이득 (0%)";
            specialDiffText.Text = "0 이득 (0%)";
        }
    }

    private void UpdateMaterialPrice(TextBlock textBlock, double cost)
    {
        textBlock.Text = $"{cost:N0}";
    }

    private void UpdateDiff(double marketPrice, double materialCost, TextBlock diffText)
    {
        if (materialCost == 0)
        {
            diffText.Text = "0 이득 (0%)";
            diffText.Foreground = Brushes.Gray;
            return;
        }

        var diff = marketPrice - materialCost;
        var diffPercent = (diff / materialCost) * 100.0;

        if (diff < 0)
        {
            // 음수일 때: "손해" 텍스트, 마이너스 기호 제거 (절댓값 사용)
            // 퍼센트는 마이너스 유지
            diffText.Text = $"{Math.Abs(diff):N0} 손해 ({diffPercent:F2}%)";
            diffText.Foreground = Brushes.Red;
        }
        else if (diff > 0)
        {
            diffText.Text = $"{diff:N0} 이득 ({diffPercent:F2}%)";
            diffText.Foreground = Brushes.Green;
        }
        else
        {
            diffText.Text = "0 이득 (0%)";
            diffText.Foreground = Brushes.White;
        }
    }
}