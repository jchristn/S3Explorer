using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace S3Explorer.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog() { InitializeComponent(); }

    public ConfirmDialog(string title, string message) : this()
    {
        Title = title;
        MessageText.Text = message;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { Close(false); e.Handled = true; return; }
        base.OnKeyDown(e);
    }

    private void OnYes(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnNo(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
