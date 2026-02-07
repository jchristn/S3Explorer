using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using S3Explorer.Models;

namespace S3Explorer.Views;

public partial class AccountEditorDialog : Window
{
    private S3Account? _result;
    private readonly string _accountId;

    public AccountEditorDialog() : this(null) { }

    public AccountEditorDialog(S3Account? existing)
    {
        InitializeComponent();

        if (existing != null)
        {
            _accountId = existing.Id;
            Title = "Edit Account";
            DisplayNameBox.Text = existing.DisplayName;
            ServiceUrlBox.Text = existing.ServiceUrl;
            AccessKeyBox.Text = existing.AccessKey;
            SecretKeyBox.Text = existing.SecretKey;
            RegionBox.Text = existing.Region;
            ForcePathStyleBox.IsChecked = existing.ForcePathStyle;
            UseSSLBox.IsChecked = existing.UseSSL;
        }
        else
        {
            _accountId = Guid.NewGuid().ToString();
            Title = "Add Account";
            RegionBox.Text = "us-east-1";
        }
    }

    public S3Account? Result => _result;

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { Close(null); e.Handled = true; return; }
        base.OnKeyDown(e);
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DisplayNameBox.Text))
        {
            DisplayNameBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(AccessKeyBox.Text))
        {
            AccessKeyBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(SecretKeyBox.Text))
        {
            SecretKeyBox.Focus();
            return;
        }

        _result = new S3Account
        {
            Id = _accountId,
            DisplayName = DisplayNameBox.Text!.Trim(),
            ServiceUrl = ServiceUrlBox.Text?.Trim() ?? "",
            AccessKey = AccessKeyBox.Text!.Trim(),
            SecretKey = SecretKeyBox.Text!.Trim(),
            Region = string.IsNullOrWhiteSpace(RegionBox.Text) ? "us-east-1" : RegionBox.Text.Trim(),
            ForcePathStyle = ForcePathStyleBox.IsChecked ?? true,
            UseSSL = UseSSLBox.IsChecked ?? true
        };

        Close(_result);
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
