using System.Text.Json;
using S3Explorer.Models;

namespace S3Explorer.Services;

public class AccountStore
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "S3Explorer");

    private static readonly string ConfigFile = Path.Combine(ConfigDir, "accounts.json");

    public List<S3Account> Accounts { get; private set; } = [];

    public void Load()
    {
        if (!File.Exists(ConfigFile))
        {
            Accounts = [];
            return;
        }

        try
        {
            var json = File.ReadAllText(ConfigFile);
            Accounts = JsonSerializer.Deserialize<List<S3Account>>(json) ?? [];
        }
        catch
        {
            Accounts = [];
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(Accounts, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigFile, json);
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
