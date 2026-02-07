using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace S3Explorer.Views;

public partial class TagsDialog : Window
{
    private readonly ObservableCollection<KeyValuePair<string, string>> _tags;
    private bool _saved;

    public TagsDialog() : this([]) { }

    public TagsDialog(List<KeyValuePair<string, string>> tags)
    {
        InitializeComponent();
        _tags = new ObservableCollection<KeyValuePair<string, string>>(tags);
        this.FindControl<DataGrid>("TagsGrid")!.ItemsSource = _tags;
    }

    public List<KeyValuePair<string, string>>? Result => _saved ? _tags.ToList() : null;

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { Close(null); e.Handled = true; return; }
        base.OnKeyDown(e);
    }

    private void OnAddTag(object? sender, RoutedEventArgs e)
    {
        var key = this.FindControl<TextBox>("NewKeyBox")!.Text?.Trim();
        var value = this.FindControl<TextBox>("NewValueBox")!.Text?.Trim();
        if (string.IsNullOrWhiteSpace(key)) return;

        _tags.Add(new KeyValuePair<string, string>(key, value ?? ""));
        var keyBox = this.FindControl<TextBox>("NewKeyBox")!;
        var valueBox = this.FindControl<TextBox>("NewValueBox")!;
        keyBox.Text = "";
        valueBox.Text = "";
        keyBox.Focus();
    }

    private void OnRemoveTag(object? sender, RoutedEventArgs e)
    {
        var grid = this.FindControl<DataGrid>("TagsGrid")!;
        if (grid.SelectedItem is KeyValuePair<string, string> selected)
        {
            _tags.Remove(selected);
        }
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        _saved = true;
        Close(_tags.ToList());
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
