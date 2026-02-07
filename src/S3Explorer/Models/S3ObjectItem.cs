namespace S3Explorer.Models;

public class S3ObjectItem
{
    public string Key { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public long Size { get; set; }
    public DateTime? LastModified { get; set; }
    public string StorageClass { get; set; } = "";
    public bool IsPrefix { get; set; }
    public string ETag { get; set; } = "";

    public string FormattedSize => IsPrefix ? "--" : FormatSize(Size);

    public string FormattedDate => IsPrefix ? "--" : LastModified?.ToString("yyyy-MM-dd HH:mm:ss") ?? "--";

    private static string FormatSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}
