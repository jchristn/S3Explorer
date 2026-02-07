using S3Explorer.Models;

namespace S3Explorer.Services;

public interface IDialogService
{
    Task<S3Account?> ShowAccountEditorAsync(S3Account? existing = null);
    Task<string?> ShowInputDialogAsync(string title, string prompt, string defaultValue = "");
    Task<bool> ShowConfirmAsync(string title, string message);
    Task ShowMessageAsync(string title, string message);
    Task<Dictionary<string, string>?> ShowMetadataDialogAsync(Dictionary<string, string> metadata);
    Task<List<KeyValuePair<string, string>>?> ShowTagsDialogAsync(List<KeyValuePair<string, string>> tags);
    Task<string?> ShowAclDialogAsync(string currentAcl, List<AclGrantEntry> grants);
    Task<string?> ShowSaveFileDialogAsync(string defaultFileName);
    Task<string[]?> ShowOpenFileDialogAsync(string title = "Select File");
    Task<string?> ShowOpenFolderDialogAsync(string title = "Select Folder");
    Task CopyToClipboardAsync(string text);
}

public class AclGrantEntry
{
    public string Grantee { get; set; } = "";
    public string GranteeType { get; set; } = "";
    public string Permission { get; set; } = "";
}
