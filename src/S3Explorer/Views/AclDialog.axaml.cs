using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using S3Explorer.Services;

namespace S3Explorer.Views;

public partial class AclDialog : Window
{
    private string? _selectedCannedAcl;

    public AclDialog() { InitializeComponent(); }

    public AclDialog(string owner, List<AclGrantEntry> grants) : this()
    {
        this.FindControl<TextBlock>("OwnerText")!.Text = owner;
        this.FindControl<DataGrid>("GrantsGrid")!.ItemsSource = grants;
        this.FindControl<ComboBox>("CannedAclCombo")!.SelectedIndex = 0;
    }

    public string? SelectedCannedAcl => _selectedCannedAcl;

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { Close(null); e.Handled = true; return; }
        base.OnKeyDown(e);
    }

    private void OnApplyCannedAcl(object? sender, RoutedEventArgs e)
    {
        var combo = this.FindControl<ComboBox>("CannedAclCombo")!;
        if (combo.SelectedItem is ComboBoxItem item)
        {
            _selectedCannedAcl = item.Tag?.ToString();
            Close(_selectedCannedAcl);
        }
    }

    private void OnClose(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
