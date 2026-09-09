using System.Security.Cryptography;
using LYBT.Infrastructure.Serialization;

namespace LYBT.Tests.Server.Unit.Infrastructure;

/// <summary>
/// P1-9 患者敏感字段 AES-GCM 透明加解密验证
/// </summary>
public class PatientEncryptionTests
{
    [Fact]
    public void AesGcm_RoundTrip()
    {
        var converter = new AesGcmValueConverter();
        // ValueConverter holds lambdas; we test via direct encrypt/decrypt by invoking via EF? Simpler test via converter instance's underlying methods via reflection of static Encrypt/Decrypt is private, so test via ConvertToProvider/ConvertFromProvider
        var plain = "110101199001011234";
        var convertTo = converter.ConvertToProviderExpression.Compile();
        var convertFrom = converter.ConvertFromProviderExpression.Compile();
        var cipher = convertTo(plain) as string;
        Assert.NotNull(cipher);
        Assert.NotEqual(plain, cipher);
        var decrypted = convertFrom(cipher) as string;
        Assert.Equal(plain, decrypted);
    }

    [Fact]
    public void AesGcm_InvalidBase64_ThrowsFailClosed()
    {
        // T1.4（4d58c5464）：解密失败不再静默回退明文——密钥不匹配/密文损坏必须
        // 抛 CryptographicException（fail-closed），防止敏感数据以明文形式泄漏。
        var converter = new AesGcmValueConverter();
        var convertFrom = converter.ConvertFromProviderExpression.Compile();
        var invalidCipher = "not-base64-明文";
        var ex = Assert.Throws<CryptographicException>(() => convertFrom(invalidCipher));
        Assert.Contains("敏感数据解密失败", ex.Message);
    }

    [Fact]
    public void AesGcm_TooShortCipher_ThrowsFailClosed()
    {
        // Base64 解码成功但长度不足 nonce(12)+tag(16)+1 —— 非加密载荷，同样 fail-closed
        var converter = new AesGcmValueConverter();
        var convertFrom = converter.ConvertFromProviderExpression.Compile();
        var shortCipher = Convert.ToBase64String(new byte[] { 1, 2, 3 });
        var ex = Assert.Throws<CryptographicException>(() => convertFrom(shortCipher));
        Assert.Contains("敏感数据解密失败", ex.Message);
    }

    [Fact]
    public void AesGcm_NullHandling()
    {
        var converter = new AesGcmValueConverter();
        var to = converter.ConvertToProviderExpression.Compile();
        var from = converter.ConvertFromProviderExpression.Compile();
        Assert.Null(to(null));
        Assert.Null(from(null));
    }
}
