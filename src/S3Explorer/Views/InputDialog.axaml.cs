using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace S3Explorer.Views;

public partial class InputDialog : Window
{
    public InputDialog() { InitializeComponent(); }

    public InputDialog(string title, string prompt, string defaultValue = "") : this()
    {
        Title = title;
        PromptText.Text = prompt;
        InputBox.Text = defaultValue;
    }

    public string? Result { get; private set; }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { Close(null); e.Handled = true; return; }
        base.OnKeyDown(e);
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        Result = InputBox.Text;
        Close(Result);
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
