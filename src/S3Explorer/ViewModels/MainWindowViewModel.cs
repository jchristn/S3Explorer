using System.Collections.ObjectModel;
using Amazon.S3;
using Amazon.S3.Model;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using S3Explorer.Models;
using S3Explorer.Services;

namespace S3Explorer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly AccountStore _accountStore;
    private readonly IDialogService _dialogService;
    private S3Service? _s3Service;

    [ObservableProperty] private ObservableCollection<S3Account> _accounts = [];
    [ObservableProperty] private S3Account? _selectedAccount;
    [ObservableProperty] private ObservableCollection<string> _buckets = [];
    [ObservableProperty] private string? _selectedBucket;
    [ObservableProperty] private ObservableCollection<S3ObjectItem> _objects = [];
    [ObservableProperty] private S3ObjectItem? _selectedObject;
    [ObservableProperty] private string _currentPrefix = "";
    [ObservableProperty] private ObservableCollection<BreadcrumbSegment> _breadcrumbs = [];
    [ObservableProperty] private ObservableCollection<ActivityLogEntry> _activityLog = [];
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusText = "Ready";
    [ObservableProperty] private bool _hasBucketSelected;
    [ObservableProperty] private bool _hasObjectSelected;
    [ObservableProperty] private bool _canGoUp;
    [ObservableProperty] private bool _isDarkMode;
    [ObservableProperty] private string _themeLabel = "Dark";

    public MainWindowViewModel() { _accountStore = new AccountStore(); _dialogService = null!; }

    public MainWindowViewModel(AccountStore accountStore, IDialogService dialogService)
    {
        _accountStore = accountStore;
        _dialogService = dialogService;
        Accounts = new ObservableCollection<S3Account>(_accountStore.Accounts);

        if (Accounts.Count > 0)
            SelectedAccount = Accounts[0];
    }

    partial void OnSelectedAccountChanged(S3Account? value)
    {
        _s3Service?.Dispose();
        _s3Service = null;
        Buckets.Clear();
        Objects.Clear();
        CurrentPrefix = "";
        SelectedBucket = null;
        HasBucketSelected = false;
        UpdateBreadcrumbs();

        if (value != null)
        {
            try
            {
                _s3Service = new S3Service(value);
                RunAsync(LoadBucketsAsync());
            }
            catch (Exception ex)
            {
                _s3Service = null;
                Log($"Failed to connect to account '{value.DisplayName}': {ex.Message}", LogLevel.Error);
            }
        }
    }

    [RelayCommand]
    private async Task ReconnectAsync()
    {
        if (SelectedAccount == null) return;

        _s3Service?.Dispose();
        _s3Service = null;
        Buckets.Clear();
        Objects.Clear();
        CurrentPrefix = "";
        SelectedBucket = null;
        HasBucketSelected = false;
        UpdateBreadcrumbs();

        try
        {
            _s3Service = new S3Service(SelectedAccount);
            Log($"Reconnecting to '{SelectedAccount.DisplayName}'", LogLevel.Info);
            await LoadBucketsAsync();
        }
        catch (Exception ex)
        {
            _s3Service = null;
            Log($"Failed to connect to account '{SelectedAccount.DisplayName}': {ex.Message}", LogLevel.Error);
        }
    }

    partial void OnSelectedBucketChanged(string? value)
    {
        try
        {
            HasBucketSelected = value != null;
            CurrentPrefix = "";
            Objects.Clear();
            UpdateBreadcrumbs();

            if (value != null)
            {
                RunAsync(LoadObjectsAsync());
            }
        }
        catch (Exception ex)
        {
            Log($"Error selecting bucket: {ex}", LogLevel.Error);
        }
    }

    private async void RunAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            Log($"Unexpected error: {ex.Message}", LogLevel.Error);
        }
    }

    partial void OnSelectedObjectChanged(S3ObjectItem? value)
    {
        HasObjectSelected = value != null && !value.IsPrefix;
    }

    private async Task LoadBucketsAsync()
    {
        if (_s3Service == null) return;

        IsBusy = true;
        StatusText = "Loading buckets";
        try
        {
            var buckets = await _s3Service.ListBucketsAsync();
            Buckets.Clear();
            foreach (var b in buckets)
                Buckets.Add(b);
            Log($"Loaded {buckets.Count} bucket(s)", LogLevel.Success);
        }
        catch (Exception ex)
        {
            Log($"Failed to load buckets: {ex.Message}", LogLevel.Error);
        }
        finally
        {
            IsBusy = false;
            StatusText = "Ready";
        }
    }

    private async Task LoadObjectsAsync()
    {
        if (_s3Service == null || SelectedBucket == null) return;

        IsBusy = true;
        StatusText = $"Loading objects in {SelectedBucket}/{CurrentPrefix}";
        try
        {
            var objects = await _s3Service.ListObjectsAsync(SelectedBucket, CurrentPrefix);
            Objects.Clear();
            foreach (var obj in objects)
                Objects.Add(obj);
            CanGoUp = !string.IsNullOrEmpty(CurrentPrefix);
            UpdateBreadcrumbs();
            Log($"Loaded {objects.Count} item(s) in {SelectedBucket}/{CurrentPrefix}", LogLevel.Info);
        }
        catch (Exception ex)
        {
            Log($"Failed to load objects: {ex.Message}", LogLevel.Error);
        }
        finally
        {
            IsBusy = false;
            StatusText = "Ready";
        }
    }

    private void UpdateBreadcrumbs()
    {
        Breadcrumbs.Clear();
        if (SelectedBucket == null) return;

        Breadcrumbs.Add(new BreadcrumbSegment { Label = SelectedBucket, Prefix = "" });

        if (!string.IsNullOrEmpty(CurrentPrefix))
        {
            var parts = CurrentPrefix.TrimEnd('/').Split('/');
            var accumulated = "";
            foreach (var part in parts)
            {
                accumulated += part + "/";
                Breadcrumbs.Add(new BreadcrumbSegment { Label = part, Prefix = accumulated });
            }
        }
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
        ThemeLabel = IsDarkMode ? "Light" : "Dark";
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeVariant = IsDarkMode
                ? ThemeVariant.Dark
                : ThemeVariant.Light;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (SelectedBucket != null)
            await LoadObjectsAsync();
        else
            await LoadBucketsAsync();
    }

    [RelayCommand]
    private async Task NavigateToPrefixAsync(S3ObjectItem? item)
    {
        if (item is not { IsPrefix: true }) return;

        CurrentPrefix = item.Key;
        await LoadObjectsAsync();
    }

    [RelayCommand]
    private async Task GoUpAsync()
    {
        if (string.IsNullOrEmpty(CurrentPrefix)) return;

        var trimmed = CurrentPrefix.TrimEnd('/');
        var lastSlash = trimmed.LastIndexOf('/');
        CurrentPrefix = lastSlash >= 0 ? trimmed[..(lastSlash + 1)] : "";
        await LoadObjectsAsync();
    }

    [RelayCommand]
    private async Task NavigateBreadcrumbAsync(BreadcrumbSegment? segment)
    {
        if (segment == null) return;
        CurrentPrefix = segment.Prefix;
        await LoadObjectsAsync();
    }

    [RelayCommand]
    private async Task DoubleClickObjectAsync(S3ObjectItem? item)
    {
        if (item == null) return;

        if (item.IsPrefix)
        {
            await NavigateToPrefixAsync(item);
        }
        else
        {
            await DownloadObjectAsync(item);
        }
    }

    [RelayCommand]
    private async Task DownloadObjectAsync(S3ObjectItem? item)
    {
        if (item == null || item.IsPrefix || _s3Service == null || SelectedBucket == null) return;

        var savePath = await _dialogService.ShowSaveFileDialogAsync(item.DisplayName);
        if (savePath == null) return;

        var logEntry = new ActivityLogEntry { Message = $"Downloading {item.DisplayName}" };
        ActivityLog.Insert(0, logEntry);
        while (ActivityLog.Count > 1000) ActivityLog.RemoveAt(ActivityLog.Count - 1);

        try
        {
            await _s3Service.DownloadObjectAsync(SelectedBucket, item.Key, savePath,
                (read, total) =>
                {
                    var pct = total > 0 ? (double)read / total * 100 : 0;
                    logEntry.ProgressPercent = pct;
                    logEntry.Message = $"Downloading {item.DisplayName} {pct:0}%";
                    OnPropertyChanged(nameof(ActivityLog));
                });

            logEntry.Message = $"Downloaded {item.DisplayName} to {savePath}";
            logEntry.IsComplete = true;
            logEntry.Level = LogLevel.Success;
            Log($"Downloaded {item.DisplayName}", LogLevel.Success);
        }
        catch (Exception ex)
        {
            logEntry.Message = $"Failed to download {item.DisplayName}: {ex.Message}";
            logEntry.Level = LogLevel.Error;
            logEntry.IsComplete = true;
        }
    }

    [RelayCommand]
    private async Task UploadObjectAsync()
    {
        if (_s3Service == null || SelectedBucket == null) return;

        var files = await _dialogService.ShowOpenFileDialogAsync("Select file(s) to upload");
        if (files == null || files.Length == 0) return;

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            var key = CurrentPrefix + fileName;
            var logEntry = new ActivityLogEntry { Message = $"Uploading {fileName}" };
            ActivityLog.Insert(0, logEntry);
            while (ActivityLog.Count > 1000) ActivityLog.RemoveAt(ActivityLog.Count - 1);

            try
            {
                await _s3Service.UploadObjectAsync(SelectedBucket, key, file,
                    (read, total) =>
                    {
                        var pct = total > 0 ? (double)read / total * 100 : 0;
                        logEntry.ProgressPercent = pct;
                        logEntry.Message = $"Uploading {fileName} {pct:0}%";
                        OnPropertyChanged(nameof(ActivityLog));
                    });

                logEntry.Message = $"Uploaded {fileName}";
                logEntry.IsComplete = true;
                logEntry.Level = LogLevel.Success;
                Log($"Uploaded {fileName} to {SelectedBucket}/{key}", LogLevel.Success);
            }
            catch (Exception ex)
            {
                logEntry.Message = $"Failed to upload {fileName}: {ex.Message}";
                logEntry.Level = LogLevel.Error;
                logEntry.IsComplete = true;
            }
        }

        await LoadObjectsAsync();
    }

    [RelayCommand]
    private async Task UploadDirectoryAsync()
    {
        if (_s3Service == null || SelectedBucket == null) return;

        var folderPath = await _dialogService.ShowOpenFolderDialogAsync("Select folder to upload");
        if (string.IsNullOrEmpty(folderPath)) return;

        var folderName = Path.GetFileName(folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);

        if (files.Length == 0)
        {
            Log($"No files found in '{folderName}'", LogLevel.Info);
            return;
        }

        Log($"Uploading {files.Length} file(s) from '{folderName}'", LogLevel.Info);

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(folderPath, file).Replace('\\', '/');
            var key = CurrentPrefix + folderName + "/" + relativePath;
            var displayName = folderName + "/" + relativePath;
            var logEntry = new ActivityLogEntry { Message = $"Uploading {displayName}" };
            ActivityLog.Insert(0, logEntry);
            while (ActivityLog.Count > 1000) ActivityLog.RemoveAt(ActivityLog.Count - 1);

            try
            {
                await _s3Service.UploadObjectAsync(SelectedBucket, key, file,
                    (read, total) =>
                    {
                        var pct = total > 0 ? (double)read / total * 100 : 0;
                        logEntry.ProgressPercent = pct;
                        logEntry.Message = $"Uploading {displayName} {pct:0}%";
                        OnPropertyChanged(nameof(ActivityLog));
                    });

                logEntry.Message = $"Uploaded {displayName}";
                logEntry.IsComplete = true;
                logEntry.Level = LogLevel.Success;
            }
            catch (Exception ex)
            {
                logEntry.Message = $"Failed to upload {displayName}: {ex.Message}";
                logEntry.Level = LogLevel.Error;
                logEntry.IsComplete = true;
            }
        }

        Log($"Finished uploading folder '{folderName}'", LogLevel.Success);
        await LoadObjectsAsync();
    }

    [RelayCommand]
    private async Task ViewMetadataAsync(S3ObjectItem? item)
    {
        if (item == null || item.IsPrefix || _s3Service == null || SelectedBucket == null) return;

        try
        {
            IsBusy = true;
            var metadata = await _s3Service.GetObjectMetadataAsync(SelectedBucket, item.Key);
            await _dialogService.ShowMetadataDialogAsync(metadata);
        }
        catch (Exception ex)
        {
            Log($"Failed to get metadata: {ex.Message}", LogLevel.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ManageTagsAsync(S3ObjectItem? item)
    {
        if (item == null || item.IsPrefix || _s3Service == null || SelectedBucket == null) return;

        try
        {
            IsBusy = true;
            var tags = await _s3Service.GetObjectTagsAsync(SelectedBucket, item.Key);
            var tagPairs = tags.Select(t => new KeyValuePair<string, string>(t.Key, t.Value)).ToList();

            var result = await _dialogService.ShowTagsDialogAsync(tagPairs);
            if (result != null)
            {
                var newTags = result.Select(kv => new Tag { Key = kv.Key, Value = kv.Value }).ToList();
                await _s3Service.SetObjectTagsAsync(SelectedBucket, item.Key, newTags);
                Log($"Updated tags for {item.DisplayName}", LogLevel.Success);
            }
        }
        catch (Exception ex)
        {
            Log($"Failed to manage tags: {ex.Message}", LogLevel.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ManageAclAsync(S3ObjectItem? item)
    {
        if (item == null || item.IsPrefix || _s3Service == null || SelectedBucket == null) return;

        try
        {
            IsBusy = true;
            var aclResponse = await _s3Service.GetObjectAclAsync(SelectedBucket, item.Key);
            var grants = (aclResponse.AccessControlList?.Grants ?? []).Select(g => new AclGrantEntry
            {
                Grantee = g.Grantee?.DisplayName
                          ?? g.Grantee?.EmailAddress
                          ?? g.Grantee?.URI
                          ?? g.Grantee?.ToString()
                          ?? "Unknown",
                GranteeType = g.Grantee?.Type?.Value ?? "Unknown",
                Permission = g.Permission?.Value ?? ""
            }).ToList();

            var currentOwner = aclResponse.AccessControlList?.Owner?.DisplayName
                               ?? aclResponse.AccessControlList?.Owner?.Id
                               ?? "Unknown";

            var newCannedAcl = await _dialogService.ShowAclDialogAsync(currentOwner, grants);
            if (newCannedAcl != null)
            {
                var cannedAcl = S3CannedACL.FindValue(newCannedAcl);
                await _s3Service.SetObjectAclAsync(SelectedBucket, item.Key, cannedAcl);
                Log($"Updated ACL for {item.DisplayName}", LogLevel.Success);
            }
        }
        catch (Exception ex)
        {
            Log($"Failed to manage ACL: {ex.Message}", LogLevel.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CreateBucketAsync()
    {
        if (_s3Service == null) return;

        var name = await _dialogService.ShowInputDialogAsync("Create Bucket", "Enter bucket name:");
        if (string.IsNullOrWhiteSpace(name)) return;

        try
        {
            IsBusy = true;
            await _s3Service.CreateBucketAsync(name);
            Log($"Created bucket: {name}", LogLevel.Success);
            await LoadBucketsAsync();
        }
        catch (Exception ex)
        {
            Log($"Failed to create bucket: {ex.Message}", LogLevel.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteBucketAsync()
    {
        if (_s3Service == null || SelectedBucket == null) return;

        var confirm = await _dialogService.ShowConfirmAsync("Delete Bucket",
            $"Are you sure you want to delete bucket '{SelectedBucket}'? The bucket must be empty.");
        if (!confirm) return;

        try
        {
            IsBusy = true;
            var bucketName = SelectedBucket;
            await _s3Service.DeleteBucketAsync(bucketName);
            SelectedBucket = null;
            Log($"Deleted bucket: {bucketName}", LogLevel.Success);
            await LoadBucketsAsync();
        }
        catch (Exception ex)
        {
            Log($"Failed to delete bucket: {ex.Message}", LogLevel.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CreateDirectoryAsync()
    {
        if (_s3Service == null || SelectedBucket == null) return;

        var name = await _dialogService.ShowInputDialogAsync("Create Directory", "Enter directory name:");
        if (string.IsNullOrWhiteSpace(name)) return;

        try
        {
            IsBusy = true;
            var key = CurrentPrefix + name.Trim() + "/";
            await _s3Service.CreateDirectoryAsync(SelectedBucket, key);
            Log($"Created directory: {name}/", LogLevel.Success);
            await LoadObjectsAsync();
        }
        catch (Exception ex)
        {
            Log($"Failed to create directory: {ex.Message}", LogLevel.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteObjectAsync(S3ObjectItem? item)
    {
        if (item == null || item.IsPrefix || _s3Service == null || SelectedBucket == null) return;

        var confirm = await _dialogService.ShowConfirmAsync("Delete Object",
            $"Are you sure you want to delete '{item.DisplayName}'?");
        if (!confirm) return;

        try
        {
            await _s3Service.DeleteObjectAsync(SelectedBucket, item.Key);
            Log($"Deleted {item.DisplayName}", LogLevel.Success);
            await LoadObjectsAsync();
        }
        catch (Exception ex)
        {
            Log($"Failed to delete object: {ex.Message}", LogLevel.Error);
        }
    }

    [RelayCommand]
    private async Task AddAccountAsync()
    {
        var account = await _dialogService.ShowAccountEditorAsync();
        if (account == null) return;

        _accountStore.AddOrUpdate(account);
        Accounts.Add(account);
        SelectedAccount = account;
        Log($"Added account: {account.DisplayName}", LogLevel.Success);
    }

    [RelayCommand]
    private async Task EditAccountAsync()
    {
        if (SelectedAccount == null) return;

        var edited = await _dialogService.ShowAccountEditorAsync(SelectedAccount);
        if (edited == null) return;

        _accountStore.AddOrUpdate(edited);
        var index = Accounts.IndexOf(SelectedAccount);
        Accounts[index] = edited;
        SelectedAccount = edited;
        Log($"Updated account: {edited.DisplayName}", LogLevel.Success);
    }

    [RelayCommand]
    private async Task RemoveAccountAsync()
    {
        if (SelectedAccount == null) return;

        var confirm = await _dialogService.ShowConfirmAsync("Remove Account",
            $"Remove account '{SelectedAccount.DisplayName}'? This will not delete any S3 data.");
        if (!confirm) return;

        var account = SelectedAccount;
        _accountStore.Remove(account.Id);
        Accounts.Remove(account);
        SelectedAccount = Accounts.FirstOrDefault();
        Log($"Removed account: {account.DisplayName}", LogLevel.Success);
    }

    [RelayCommand]
    private void ClearActivityLog()
    {
        ActivityLog.Clear();
    }

    [RelayCommand]
    private async Task CopyActivityLogAsync()
    {
        if (ActivityLog.Count == 0) return;
        var text = string.Join(Environment.NewLine, ActivityLog.Select(e => e.Display));
        await _dialogService.CopyToClipboardAsync(text);
        Log("Activity log copied to clipboard", LogLevel.Info);
    }

    public async Task UploadDroppedItemsAsync(IEnumerable<string> paths)
    {
        if (_s3Service == null || SelectedBucket == null) return;

        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                var folderName = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);

                Log($"Uploading {files.Length} file(s) from '{folderName}'", LogLevel.Info);

                foreach (var file in files)
                {
                    var relativePath = Path.GetRelativePath(path, file).Replace('\\', '/');
                    var key = CurrentPrefix + folderName + "/" + relativePath;
                    var displayName = folderName + "/" + relativePath;
                    var logEntry = new ActivityLogEntry { Message = $"Uploading {displayName}" };
                    ActivityLog.Insert(0, logEntry);
                    while (ActivityLog.Count > 1000) ActivityLog.RemoveAt(ActivityLog.Count - 1);

                    try
                    {
                        await _s3Service.UploadObjectAsync(SelectedBucket, key, file,
                            (read, total) =>
                            {
                                var pct = total > 0 ? (double)read / total * 100 : 0;
                                logEntry.ProgressPercent = pct;
                                logEntry.Message = $"Uploading {displayName} {pct:0}%";
                                OnPropertyChanged(nameof(ActivityLog));
                            });
                        logEntry.Message = $"Uploaded {displayName}";
                        logEntry.IsComplete = true;
                        logEntry.Level = LogLevel.Success;
                    }
                    catch (Exception ex)
                    {
                        logEntry.Message = $"Failed to upload {displayName}: {ex.Message}";
                        logEntry.Level = LogLevel.Error;
                        logEntry.IsComplete = true;
                    }
                }
            }
            else if (File.Exists(path))
            {
                var fileName = Path.GetFileName(path);
                var key = CurrentPrefix + fileName;
                var logEntry = new ActivityLogEntry { Message = $"Uploading {fileName}" };
                ActivityLog.Insert(0, logEntry);
                while (ActivityLog.Count > 1000) ActivityLog.RemoveAt(ActivityLog.Count - 1);

                try
                {
                    await _s3Service.UploadObjectAsync(SelectedBucket, key, path,
                        (read, total) =>
                        {
                            var pct = total > 0 ? (double)read / total * 100 : 0;
                            logEntry.ProgressPercent = pct;
                            logEntry.Message = $"Uploading {fileName} {pct:0}%";
                            OnPropertyChanged(nameof(ActivityLog));
                        });
                    logEntry.Message = $"Uploaded {fileName}";
                    logEntry.IsComplete = true;
                    logEntry.Level = LogLevel.Success;
                }
                catch (Exception ex)
                {
                    logEntry.Message = $"Failed to upload {fileName}: {ex.Message}";
                    logEntry.Level = LogLevel.Error;
                    logEntry.IsComplete = true;
                }
            }
        }

        await LoadObjectsAsync();
    }

    public async Task<string?> DownloadToTempAsync(S3ObjectItem item)
    {
        if (_s3Service == null || SelectedBucket == null) return null;

        var tempDir = Path.Combine(Path.GetTempPath(), "S3Explorer");
        Directory.CreateDirectory(tempDir);
        var tempPath = Path.Combine(tempDir, item.DisplayName);

        try
        {
            Log($"Downloading {item.DisplayName} for drag", LogLevel.Info);
            await _s3Service.DownloadObjectAsync(SelectedBucket, item.Key, tempPath);
            return tempPath;
        }
        catch (Exception ex)
        {
            Log($"Failed to prepare drag: {ex.Message}", LogLevel.Error);
            return null;
        }
    }

    private void Log(string message, LogLevel level = LogLevel.Info)
    {
        ActivityLog.Insert(0, new ActivityLogEntry
        {
            Message = message,
            Level = level,
            IsComplete = true
        });
        while (ActivityLog.Count > 1000)
            ActivityLog.RemoveAt(ActivityLog.Count - 1);
    }
}

public class BreadcrumbSegment
{
    public string Label { get; set; } = "";
    public string Prefix { get; set; } = "";
}
