using Avalonia.Controls;
using Avalonia.Platform.Storage;
using S3Explorer.Models;
using S3Explorer.Views;

namespace S3Explorer.Services;

public class DialogService : IDialogService
{
    private readonly Window _owner;

    public DialogService(Window owner)
    {
        _owner = owner;
    }

    public async Task<S3Account?> ShowAccountEditorAsync(S3Account? existing = null)
    {
        var dialog = new AccountEditorDialog(existing);
        var result = await dialog.ShowDialog<S3Account?>(_owner);
        return result;
    }

    public async Task<string?> ShowInputDialogAsync(string title, string prompt, string defaultValue = "")
    {
        var dialog = new InputDialog(title, prompt, defaultValue);
        var result = await dialog.ShowDialog<string?>(_owner);
        return result;
    }

    public async Task<bool> ShowConfirmAsync(string title, string message)
    {
        var dialog = new ConfirmDialog(title, message);
        var result = await dialog.ShowDialog<object?>(_owner);
        return result is true;
    }

    public async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ConfirmDialog(title, message);
        await dialog.ShowDialog<object?>(_owner);
    }

    public async Task<Dictionary<string, string>?> ShowMetadataDialogAsync(Dictionary<string, string> metadata)
    {
        var dialog = new MetadataDialog(metadata);
        await dialog.ShowDialog<object?>(_owner);
        return null;
    }

    public async Task<List<KeyValuePair<string, string>>?> ShowTagsDialogAsync(List<KeyValuePair<string, string>> tags)
    {
        var dialog = new TagsDialog(tags);
        var result = await dialog.ShowDialog<List<KeyValuePair<string, string>>?>(_owner);
        return result;
    }

    public async Task<string?> ShowAclDialogAsync(string currentAcl, List<AclGrantEntry> grants)
    {
        var dialog = new AclDialog(currentAcl, grants);
        var result = await dialog.ShowDialog<string?>(_owner);
        return result;
    }

    public async Task<string?> ShowSaveFileDialogAsync(string defaultFileName)
    {
        var storageProvider = _owner.StorageProvider;
        var result = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = defaultFileName,
            Title = "Save File"
        });

        return result?.Path.LocalPath;
    }

    public async Task<string[]?> ShowOpenFileDialogAsync(string title = "Select File")
    {
        var storageProvider = _owner.StorageProvider;
        var results = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = true
        });

        if (results.Count == 0) return null;
        return results.Select(r => r.Path.LocalPath).ToArray();
    }

    public async Task<string?> ShowOpenFolderDialogAsync(string title = "Select Folder")
    {
        var storageProvider = _owner.StorageProvider;
        var results = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });

        if (results.Count == 0) return null;
        return results[0].Path.LocalPath;
    }

    public async Task CopyToClipboardAsync(string text)
    {
        var clipboard = TopLevel.GetTopLevel(_owner)?.Clipboard;
        if (clipboard != null)
            await clipboard.SetTextAsync(text);
    }
}
