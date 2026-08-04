using System.Security.Cryptography;

namespace TeamActivity.Infrastructure;

public sealed record StoredEncryptedFile(string RelativePath, string ContentHash, long PlaintextBytes);

public sealed class EncryptedFileStore
{
    private const byte FormatVersion = 1;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly string rootPath;
    private readonly byte[] key;

    public EncryptedFileStore(string rootPath, ReadOnlySpan<byte> key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        if (key.Length != 32) throw new ArgumentException("A 256-bit key is required.", nameof(key));
        this.rootPath = Path.GetFullPath(rootPath);
        this.key = key.ToArray();
        Directory.CreateDirectory(this.rootPath);
    }

    public async Task<StoredEncryptedFile> StoreAsync(
        ReadOnlyMemory<byte> plaintext,
        DateOnly partition,
        CancellationToken cancellationToken = default)
    {
        if (plaintext.IsEmpty) throw new ArgumentException("File must not be empty.", nameof(plaintext));

        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var tag = new byte[TagSize];
        var ciphertext = new byte[plaintext.Length];
        using (var aes = new AesGcm(key, TagSize))
        {
            aes.Encrypt(nonce, plaintext.Span, ciphertext, tag);
        }

        var relativeDirectory = Path.Combine(
            partition.Year.ToString("0000", System.Globalization.CultureInfo.InvariantCulture),
            partition.Month.ToString("00", System.Globalization.CultureInfo.InvariantCulture),
            partition.Day.ToString("00", System.Globalization.CultureInfo.InvariantCulture));
        var absoluteDirectory = SafeCombine(rootPath, relativeDirectory);
        Directory.CreateDirectory(absoluteDirectory);
        var fileName = $"{Guid.NewGuid():N}.tas";
        var relativePath = Path.Combine(relativeDirectory, fileName);
        var absolutePath = SafeCombine(rootPath, relativePath);

        await using var stream = new FileStream(
            absolutePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous | FileOptions.WriteThrough);
        await stream.WriteAsync(new[] { FormatVersion }, cancellationToken);
        await stream.WriteAsync(nonce, cancellationToken);
        await stream.WriteAsync(tag, cancellationToken);
        await stream.WriteAsync(ciphertext, cancellationToken);
        await stream.FlushAsync(cancellationToken);

        return new StoredEncryptedFile(
            relativePath,
            Convert.ToHexString(SHA256.HashData(plaintext.Span)),
            plaintext.Length);
    }

    public async Task<byte[]> ReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var absolutePath = SafeCombine(rootPath, relativePath);
        var payload = await File.ReadAllBytesAsync(absolutePath, cancellationToken);
        if (payload.Length <= 1 + NonceSize + TagSize || payload[0] != FormatVersion)
            throw new CryptographicException("Unsupported or invalid encrypted file.");

        var nonce = payload.AsSpan(1, NonceSize);
        var tag = payload.AsSpan(1 + NonceSize, TagSize);
        var ciphertext = payload.AsSpan(1 + NonceSize + TagSize);
        var plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }

    internal static string SafeCombine(string root, string relativePath)
    {
        if (Path.IsPathRooted(relativePath)) throw new ArgumentException("Path must be relative.", nameof(relativePath));
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var candidate = Path.GetFullPath(Path.Combine(fullRoot, relativePath));
        if (!candidate.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Path escapes the configured data root.");
        return candidate;
    }
}
