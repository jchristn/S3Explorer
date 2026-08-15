using System.Text.Json;
using S3Explorer.Models;

namespace S3Explorer.Services;

public class AccountStore
{
    private static readonly string DefaultConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "S3Explorer");

    private readonly string _configDir;
    private readonly string _configFile;

    /// <summary>
    /// Creates an account store persisting to <paramref name="configDir"/>. When
    /// <paramref name="configDir"/> is null or whitespace the default per-user
    /// application-data location is used. The directory is injectable primarily so
    /// the persistence behavior can be tested without touching the real user config.
    /// </summary>
    public AccountStore(string? configDir = null)
    {
        _configDir = string.IsNullOrWhiteSpace(configDir) ? DefaultConfigDir : configDir;
        _configFile = Path.Combine(_configDir, "accounts.json");
    }

    public List<S3Account> Accounts { get; private set; } = [];

    public void Load()
    {
        if (!File.Exists(_configFile))
        {
            Accounts = [];
            return;
        }

        try
        {
            var json = File.ReadAllText(_configFile);
            Accounts = JsonSerializer.Deserialize<List<S3Account>>(json) ?? [];
        }
        catch
        {
            Accounts = [];
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(_configDir);
        var json = JsonSerializer.Serialize(Accounts, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_configFile, json);
    }

    public void AddOrUpdate(S3Account account)
    {
        var existing = Accounts.FindIndex(a => a.Id == account.Id);
        if (existing >= 0)
            Accounts[existing] = account;
        else
            Accounts.Add(account);
        Save();
    }

    public void Remove(string id)
    {
        Accounts.RemoveAll(a => a.Id == id);
        Save();
    }
}
