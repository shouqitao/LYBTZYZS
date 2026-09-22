using System.Security.Cryptography;
using System.Text;

namespace LYBT.Infrastructure.Services.Backup;

/// <summary>
/// 备份文件加密/解密（B-06：备份文件加密）。
/// </summary>
/// <remarks>
/// <para><b>容器格式（v1）</b>：<c>magic(8) | iterations(4, LE) | salt(16) | iv(16) | hmac(32) | length(8, LE) | ciphertext</c>，
/// 共 84 字节头。加密算法 AES-256-CBC（PKCS7）+ HMAC-SHA256（encrypt-then-MAC，独立密钥），
/// 密钥由 PBKDF2-SHA256（默认 210000 次迭代）从口令与随机盐派生（64 字节 → 前 32 字节加密密钥、后 32 字节 MAC 密钥）。</para>
/// <para><b>流式处理</b>：全量数据库备份可达 GB 级，故读写均为 128KB 分块流式，不整文件驻留内存。</para>
/// <para><b>HMAC 覆盖范围</b>：头部前缀（magic/iterations/salt/iv）+ 全部密文——篡改头部或密文都会导致校验失败。</para>
/// </remarks>
public static class BackupFileEncryption
{
    /// <summary>加密文件扩展名（追加在 <c>.bak</c> 之后，如 <c>LYBTDB_20260922120000.bak.enc</c>）</summary>
    public const string EncryptedExtension = ".enc";

    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("LYBTBKP1");
    private const int DefaultIterations = 210_000;
    private const int MinIterations = 1_000;
    private const int MaxIterations = 2_000_000;
    private const int SaltSize = 16;
    private const int IvSize = 16;
    private const int MacSize = 32;
    private const int KeyMaterialSize = 64;
    private const int HeaderSize = 8 + 4 + SaltSize + IvSize + MacSize + 8;
    private const int BufferSize = 128 * 1024;

    /// <summary>判断文件是否为本服务的加密备份容器（读取魔数，不解析全文）</summary>
    public static bool IsEncryptedFile(string path)
    {
        if (!File.Exists(path))
            return false;

        try
        {
            using var stream = File.OpenRead(path);
            var header = new byte[Magic.Length];
            var read = stream.Read(header, 0, header.Length);
            return read == header.Length && header.AsSpan().SequenceEqual(Magic);
        }
        catch (IOException)
        {
            return false;
        }
    }

    /// <summary>加密文件：<paramref name="sourcePath"/> → <paramref name="destinationPath"/></summary>
    public static void Encrypt(string sourcePath, string destinationPath, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        if (string.Equals(sourcePath, destinationPath, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("加密输出路径不能与源文件相同", nameof(destinationPath));

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var iv = RandomNumberGenerator.GetBytes(IvSize);
        var keyMaterial = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, DefaultIterations, HashAlgorithmName.SHA256, KeyMaterialSize);

        var headerPrefix = BuildHeaderPrefix(DefaultIterations, salt, iv);

        using var source = File.OpenRead(sourcePath);
        using var destination = File.Create(destinationPath);
        try
        {
            // 头部占位（HMAC/长度待回填）
            destination.Write(new byte[HeaderSize]);

            using var hmac = new HMACSHA256(keyMaterial[32..]);
            hmac.TransformBlock(headerPrefix, 0, headerPrefix.Length, null, 0);

            using (var aes = Aes.Create())
            {
                aes.Key = keyMaterial[..32];
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var encryptor = aes.CreateEncryptor();
                using var hashing = new HashingWriteStream(destination, hmac);
                using var crypto = new CryptoStream(hashing, encryptor, CryptoStreamMode.Write);

                var buffer = new byte[BufferSize];
                int read;
                while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
                    crypto.Write(buffer, 0, read);

                crypto.FlushFinalBlock();
            }

            hmac.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            var cipherLength = destination.Position - HeaderSize;

            destination.Position = 0;
            destination.Write(headerPrefix);
            destination.Write(hmac.Hash!);
            destination.Write(BitConverter.GetBytes(cipherLength));
        }
        catch
        {
            destination.Dispose();
            TryDelete(destinationPath);
            throw;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyMaterial);
        }
    }

    /// <summary>
    /// 解密文件：<paramref name="sourcePath"/> → <paramref name="destinationPath"/>。
    /// 口令错误或文件被篡改时抛 <see cref="InvalidDataException"/>。
    /// </summary>
    public static void Decrypt(string sourcePath, string destinationPath, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        using var source = File.OpenRead(sourcePath);
        var prefix = new byte[8 + 4 + SaltSize + IvSize];
        if (source.Read(prefix, 0, prefix.Length) != prefix.Length || !prefix.AsSpan(0, 8).SequenceEqual(Magic))
            throw new InvalidDataException("不是有效的 LYBT 加密备份文件");

        var iterations = BitConverter.ToInt32(prefix, 8);
        if (iterations < MinIterations || iterations > MaxIterations)
            throw new InvalidDataException($"备份文件迭代次数异常：{iterations}");

        var salt = prefix.AsSpan(12, SaltSize).ToArray();
        var iv = prefix.AsSpan(12 + SaltSize, IvSize).ToArray();

        var expectedMac = new byte[MacSize];
        if (source.Read(expectedMac, 0, MacSize) != MacSize)
            throw new InvalidDataException("加密备份文件头部不完整");

        var lengthBytes = new byte[8];
        if (source.Read(lengthBytes, 0, 8) != 8)
            throw new InvalidDataException("加密备份文件头部不完整");
        var cipherLength = BitConverter.ToInt64(lengthBytes, 0);
        if (cipherLength < 0 || HeaderSize + cipherLength > source.Length)
            throw new InvalidDataException("加密备份文件长度字段异常");

        var keyMaterial = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, iterations, HashAlgorithmName.SHA256, KeyMaterialSize);

        try
        {
            var headerPrefix = BuildHeaderPrefix(iterations, salt, iv);

            // 第一遍：校验 HMAC（覆盖头部前缀 + 密文）
            using (var hmac = new HMACSHA256(keyMaterial[32..]))
            {
                hmac.TransformBlock(headerPrefix, 0, headerPrefix.Length, null, 0);
                var buffer = new byte[BufferSize];
                var remaining = cipherLength;
                while (remaining > 0)
                {
                    var read = source.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                    if (read <= 0)
                        throw new InvalidDataException("加密备份文件被截断");
                    hmac.TransformBlock(buffer, 0, read, null, 0);
                    remaining -= read;
                }
                hmac.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                if (!CryptographicOperations.FixedTimeEquals(hmac.Hash!, expectedMac))
                    throw new InvalidDataException("备份口令错误或文件已损坏（HMAC 校验失败）");
            }

            // 第二遍：解密
            source.Position = HeaderSize;
            using var destination = File.Create(destinationPath);
            using var aes = Aes.Create();
            aes.Key = keyMaterial[..32];
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            using var crypto = new CryptoStream(new LimitedReadStream(source, cipherLength), decryptor, CryptoStreamMode.Read);
            crypto.CopyTo(destination);
        }
        catch
        {
            TryDelete(destinationPath);
            throw;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyMaterial);
        }
    }

    private static byte[] BuildHeaderPrefix(int iterations, byte[] salt, byte[] iv)
    {
        var prefix = new byte[8 + 4 + SaltSize + IvSize];
        Magic.CopyTo(prefix, 0);
        BitConverter.GetBytes(iterations).CopyTo(prefix, 8);
        salt.CopyTo(prefix, 12);
        iv.CopyTo(prefix, 12 + SaltSize);
        return prefix;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
            // 清理失败不掩盖原始异常
        }
    }

    /// <summary>写入时同步更新 HMAC 的透传流（HMAC 覆盖密文）</summary>
    private sealed class HashingWriteStream : Stream
    {
        private readonly Stream _inner;
        private readonly HMAC _hmac;

        public HashingWriteStream(Stream inner, HMAC hmac)
        {
            _inner = inner;
            _hmac = hmac;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _hmac.TransformBlock(buffer, offset, count, null, 0);
            _inner.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            var rented = System.Buffers.ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                buffer.CopyTo(rented);
                _hmac.TransformBlock(rented, 0, buffer.Length, null, 0);
                _inner.Write(rented, 0, buffer.Length);
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(rented);
            }
        }

        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }

    /// <summary>限制可读长度的只读流（解密时仅读取密文段）</summary>
    private sealed class LimitedReadStream : Stream
    {
        private readonly Stream _inner;
        private long _remaining;

        public LimitedReadStream(Stream inner, long length)
        {
            _inner = inner;
            _remaining = length;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _remaining;
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_remaining <= 0)
                return 0;

            var toRead = (int)Math.Min(count, _remaining);
            var read = _inner.Read(buffer, offset, toRead);
            _remaining -= read;
            return read;
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
