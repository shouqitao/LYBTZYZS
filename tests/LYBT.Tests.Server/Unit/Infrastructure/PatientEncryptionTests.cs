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
    public void AesGcm_PlaintextFallback_OnInvalidBase64()
    {
        var converter = new AesGcmValueConverter();
        var convertFrom = converter.ConvertFromProviderExpression.Compile();
        var plain = "not-base64-明文";
        var result = convertFrom(plain) as string;
        Assert.Equal(plain, result);
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
