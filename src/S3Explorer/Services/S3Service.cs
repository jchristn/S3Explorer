using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using S3Explorer.Models;

namespace S3Explorer.Services;

public class S3Service : IDisposable
{
    private readonly AmazonS3Client _client;

    public S3Service(S3Account account)
    {
        var credentials = new BasicAWSCredentials(account.AccessKey, account.SecretKey);
        var config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(account.Region),
            ForcePathStyle = account.ForcePathStyle,
        };

        if (!string.IsNullOrWhiteSpace(account.ServiceUrl))
        {
            var scheme = account.UseSSL ? "https" : "http";
            var url = account.ServiceUrl;
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                url = $"{scheme}://{url}";
            config.ServiceURL = url;
        }

        _client = new AmazonS3Client(credentials, config);
    }

    public async Task<List<string>> ListBucketsAsync()
    {
        var response = await _client.ListBucketsAsync();
        return response.Buckets.Select(b => b.BucketName).OrderBy(n => n).ToList();
    }

    public async Task<List<S3ObjectItem>> ListObjectsAsync(string bucket, string prefix = "")
    {
        var items = new List<S3ObjectItem>();
        var seenPrefixes = new HashSet<string>();
        var request = new ListObjectsV2Request
        {
            BucketName = bucket,
            Prefix = prefix,
            Delimiter = "/"
        };

        ListObjectsV2Response response;
        do
        {
            response = await _client.ListObjectsV2Async(request);

            if (response.CommonPrefixes != null)
            foreach (var commonPrefix in response.CommonPrefixes)
            {
                if (commonPrefix == prefix) continue;

                // Normalize to one level deep from current prefix.
                // Some S3-compatible servers return deeply nested prefixes
                // (e.g. "test/test2/test3/") instead of just "test/".
                var relative = commonPrefix;
                if (!string.IsNullOrEmpty(prefix) && relative.StartsWith(prefix))
                    relative = relative[prefix.Length..];
                var slashIndex = relative.IndexOf('/');
                if (slashIndex >= 0)
                    relative = relative[..(slashIndex + 1)];
                var normalizedKey = prefix + relative;

                if (!seenPrefixes.Add(normalizedKey)) continue;

                var displayName = relative.TrimEnd('/');
                if (string.IsNullOrEmpty(displayName)) continue;

                items.Add(new S3ObjectItem
                {
                    Key = normalizedKey,
                    DisplayName = displayName + "/",
                    IsPrefix = true
                });
            }

            if (response.S3Objects != null)
            foreach (var obj in response.S3Objects)
            {
                if (obj.Key == prefix) continue;
                if (obj.Key.EndsWith("/")) continue;

                var displayName = obj.Key;
                if (displayName.Contains('/'))
                    displayName = displayName[(displayName.LastIndexOf('/') + 1)..];

                if (string.IsNullOrEmpty(displayName)) continue;

                items.Add(new S3ObjectItem
                {
                    Key = obj.Key,
                    DisplayName = displayName,
                    Size = obj.Size ?? 0,
                    LastModified = obj.LastModified,
                    StorageClass = obj.StorageClass?.Value ?? "",
                    ETag = obj.ETag ?? "",
                    IsPrefix = false
                });
            }

            request.ContinuationToken = response.NextContinuationToken;
        } while (response.IsTruncated == true);

        return items.OrderByDescending(i => i.IsPrefix).ThenBy(i => i.DisplayName).ToList();
    }

    public async Task DownloadObjectAsync(string bucket, string key, string localPath,
        Action<long, long>? onProgress = null, CancellationToken cancellationToken = default)
    {
        var metaResponse = await _client.GetObjectMetadataAsync(bucket, key, cancellationToken);
        var totalBytes = metaResponse.ContentLength;

        var request = new GetObjectRequest { BucketName = bucket, Key = key };
        using var response = await _client.GetObjectAsync(request, cancellationToken);
        using var responseStream = response.ResponseStream;
        using var fileStream = File.Create(localPath);

        var buffer = new byte[81920];
        long totalRead = 0;
        int bytesRead;
        while ((bytesRead = await responseStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalRead += bytesRead;
            onProgress?.Invoke(totalRead, totalBytes);
        }
    }

    public async Task UploadObjectAsync(string bucket, string key, string localPath,
        Action<long, long>? onProgress = null, CancellationToken cancellationToken = default)
    {
        var fileInfo = new FileInfo(localPath);
        var totalBytes = fileInfo.Length;

        using var fileStream = File.OpenRead(localPath);
        var trackingStream = new ProgressStream(fileStream, (read) => onProgress?.Invoke(read, totalBytes));

        var request = new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = trackingStream
        };

        await _client.PutObjectAsync(request, cancellationToken);
    }

    public async Task<Dictionary<string, string>> GetObjectMetadataAsync(string bucket, string key)
    {
        var response = await _client.GetObjectMetadataAsync(bucket, key);
        var metadata = new Dictionary<string, string>
        {
            ["Content-Type"] = response.Headers.ContentType ?? "",
            ["Content-Length"] = response.ContentLength.ToString(),
            ["ETag"] = response.ETag ?? "",
            ["Last-Modified"] = $"{response.LastModified}",
            ["Storage-Class"] = response.StorageClass?.Value ?? "STANDARD",
            ["Version-Id"] = response.VersionId ?? "(none)",
            ["Server-Side-Encryption"] = response.ServerSideEncryptionMethod?.Value ?? "(none)"
        };

        foreach (var key2 in response.Metadata.Keys)
        {
            metadata[$"x-amz-meta-{key2}"] = response.Metadata[key2];
        }

        return metadata;
    }

    public async Task<List<Tag>> GetObjectTagsAsync(string bucket, string key)
    {
        var response = await _client.GetObjectTaggingAsync(new GetObjectTaggingRequest
        {
            BucketName = bucket,
            Key = key
        });
        return response.Tagging?.ToList() ?? new List<Tag>();
    }

    public async Task SetObjectTagsAsync(string bucket, string key, List<Tag> tags)
    {
        await _client.PutObjectTaggingAsync(new PutObjectTaggingRequest
        {
            BucketName = bucket,
            Key = key,
            Tagging = new Tagging { TagSet = tags }
        });
    }

#pragma warning disable CS0618
    public async Task<GetACLResponse> GetObjectAclAsync(string bucket, string key)
    {
        return await _client.GetACLAsync(new GetACLRequest
        {
            BucketName = bucket,
            Key = key
        });
    }

    public async Task SetObjectAclAsync(string bucket, string key, S3CannedACL cannedAcl)
    {
        await _client.PutACLAsync(new PutACLRequest
        {
            BucketName = bucket,
            Key = key,
            CannedACL = cannedAcl
        });
    }
#pragma warning restore CS0618

    public async Task CreateBucketAsync(string bucketName)
    {
        await _client.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });
    }

    public async Task DeleteBucketAsync(string bucketName)
    {
        await _client.DeleteBucketAsync(bucketName);
    }

    public async Task DeleteObjectAsync(string bucket, string key)
    {
        await _client.DeleteObjectAsync(bucket, key);
    }

    public async Task CreateDirectoryAsync(string bucket, string key)
    {
        if (!key.EndsWith("/"))
            key += "/";

        var request = new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = new MemoryStream()
        };

        await _client.PutObjectAsync(request);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}

internal class ProgressStream : Stream
{
    private readonly Stream _inner;
    private readonly Action<long> _onProgress;
    private long _totalRead;

    public ProgressStream(Stream inner, Action<long> onProgress)
    {
        _inner = inner;
        _onProgress = onProgress;
    }

    public override bool CanRead => _inner.CanRead;
    public override bool CanSeek => _inner.CanSeek;
    public override bool CanWrite => _inner.CanWrite;
    public override long Length => _inner.Length;
    public override long Position
    {
        get => _inner.Position;
        set => _inner.Position = value;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = _inner.Read(buffer, offset, count);
        _totalRead += bytesRead;
        _onProgress(_totalRead);
        return bytesRead;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var bytesRead = await _inner.ReadAsync(buffer, offset, count, cancellationToken);
        _totalRead += bytesRead;
        _onProgress(_totalRead);
        return bytesRead;
    }

    public override void Flush() => _inner.Flush();
    public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
    public override void SetLength(long value) => _inner.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);
}
