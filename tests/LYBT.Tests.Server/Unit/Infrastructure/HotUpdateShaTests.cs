using System.Security.Cryptography;
using System.Text;

namespace LYBT.Tests.Server.Unit.Infrastructure;

/// <summary>
/// P0-2 热更新 SHA256 校验测试 — 验证篡改 zip 能被检测
/// </summary>
public class HotUpdateShaTests
{
    private static async Task<string> ComputeSha256Async(string path)
    {
        await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(fs);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    [Fact]
    public async Task SHA256_SameContent_SameHash()
    {
        var tmp = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tmp, "hello-lybt", Encoding.UTF8);
            var h1 = await ComputeSha256Async(tmp);
            var h2 = await ComputeSha256Async(tmp);
            Assert.Equal(h1, h2);
        }
        finally { if (File.Exists(tmp)) File.Delete(tmp); }
    }

    [Fact]
    public async Task SHA256_TamperedContent_DifferentHash()
    {
        var tmp = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tmp, "original", Encoding.UTF8);
            var original = await ComputeSha256Async(tmp);
            await File.WriteAllTextAsync(tmp, "tampered", Encoding.UTF8);
            var tampered = await ComputeSha256Async(tmp);
            Assert.NotEqual(original, tampered);
        }
        finally { if (File.Exists(tmp)) File.Delete(tmp); }
    }

    [Fact]
    public async Task SaveUpdatePackage_WritesShaSidecar()
    {
        // 验证 DeployService 保存后 .update-pending.sha256 存在且与文件一致
        var svc = new LYBT.Infrastructure.Services.DeployService(
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<LYBT.Infrastructure.Services.DeployService>());

        // 构造最小合法 zip（空 zip 结构）
        using var ms = new MemoryStream();
        using (var zip = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            var entry = zip.CreateEntry("test.txt");
            await using var es = entry.Open();
            await es.WriteAsync(Encoding.UTF8.GetBytes("hot-update-test"));
        }
        ms.Position = 0;

        var uploadsDir = Path.Combine(AppContext.BaseDirectory, "uploads");
        if (Directory.Exists(uploadsDir)) Directory.Delete(uploadsDir, true);

        var result = await svc.SaveUpdatePackageAsync(ms, "update.zip");
        Assert.True(result.IsSuccess);
        var flag = Path.Combine(AppContext.BaseDirectory, ".update-pending");
        var shaFlag = Path.Combine(AppContext.BaseDirectory, ".update-pending.sha256");
        Assert.True(File.Exists(flag));
        Assert.True(File.Exists(shaFlag));
        var zipPath = (await File.ReadAllTextAsync(flag)).Trim();
        Assert.True(File.Exists(zipPath));
        var expected = (await File.ReadAllTextAsync(shaFlag)).Trim().ToLowerInvariant();
        var actual = await ComputeSha256Async(zipPath);
        Assert.Equal(expected, actual);

        // 清理
        try { File.Delete(flag); } catch { }
        try { File.Delete(shaFlag); } catch { }
        try { File.Delete(zipPath); } catch { }
        try { if (Directory.Exists(uploadsDir) && !Directory.EnumerateFileSystemEntries(uploadsDir).Any()) Directory.Delete(uploadsDir); } catch { }
    }
}
