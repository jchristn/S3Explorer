using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace S3Explorer.Views;

public partial class MetadataDialog : Window
{
    public MetadataDialog() { InitializeComponent(); }

    public MetadataDialog(Dictionary<string, string> metadata) : this()
    {
        var grid = this.FindControl<DataGrid>("MetadataGrid")!;
        grid.ItemsSource = metadata.Select(kv =>
            new KeyValuePair<string, string>(kv.Key, kv.Value)).ToList();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { Close(); e.Handled = true; return; }
        base.OnKeyDown(e);
    }

    private void OnClose(object? sender, RoutedEventArgs e) => Close();
}
