using Avalonia.Controls;
using Avalonia.Styling;

namespace S3Explorer.Views;

public partial class SplashScreen : Window
{
    public SplashScreen()
    {
        InitializeComponent();
        UpdateLogoVisibility();
        ActualThemeVariantChanged += (_, _) => UpdateLogoVisibility();
    }

    private void UpdateLogoVisibility()
    {
        var isDark = ActualThemeVariant == ThemeVariant.Dark;
        LightThemeLogo.IsVisible = !isDark;
        DarkThemeLogo.IsVisible = isDark;
    }
}
