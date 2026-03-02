using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Threading.Tasks;

namespace AlbionCalculator;

public partial class FishChoppingCalculatorView : UserControl
{
    public FishChoppingCalculatorView()
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
        var choppedFishBox = this.FindControl<TextBox>("ChoppedFishPrice");
        
        var price1 = this.FindControl<TextBlock>("Price1");
        var price2 = this.FindControl<TextBlock>("Price2");
        var price3 = this.FindControl<TextBlock>("Price3");
        var price4 = this.FindControl<TextBlock>("Price4");
        var price6 = this.FindControl<TextBlock>("Price6");
        var price8 = this.FindControl<TextBlock>("Price8");
        var price10 = this.FindControl<TextBlock>("Price10");
        var price14 = this.FindControl<TextBlock>("Price14");
        var price20 = this.FindControl<TextBlock>("Price20");
        var price30 = this.FindControl<TextBlock>("Price30");
        var price200 = this.FindControl<TextBlock>("Price200");

        if (choppedFishBox == null || price1 == null) return;

        double choppedPrice = ParseDouble(choppedFishBox.Text, 0);

        UpdatePrice(price1, choppedPrice, 1);
        UpdatePrice(price2, choppedPrice, 2);
        UpdatePrice(price3, choppedPrice, 3);
        UpdatePrice(price4, choppedPrice, 4);
        UpdatePrice(price6, choppedPrice, 6);
        UpdatePrice(price8, choppedPrice, 8);
        UpdatePrice(price10, choppedPrice, 10);
        UpdatePrice(price14, choppedPrice, 14);
        UpdatePrice(price20, choppedPrice, 20);
        UpdatePrice(price30, choppedPrice, 30);
        UpdatePrice(price200, choppedPrice, 200);
    }

    private void UpdatePrice(TextBlock textBlock, double unitPrice, int nutrition)
    {
        if (unitPrice == 0)
        {
            textBlock.Text = "0";
        }
        else
        {
            double totalPrice = unitPrice * nutrition;
            textBlock.Text = $"{totalPrice:N0}";
        }
    }
}