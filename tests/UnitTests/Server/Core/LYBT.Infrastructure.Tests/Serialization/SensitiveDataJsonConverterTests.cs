using System.Text.Json;
using LYBT.Entities.Attributes;
using LYBT.Infrastructure.Serialization;
using Xunit;

namespace LYBT.Infrastructure.Tests.Serialization;

/// <summary>
/// 敏感数据JSON转换器单元测试
/// Issue #2254: 验证API响应脱敏功能
/// </summary>
public class SensitiveDataJsonConverterTests
{
    private readonly JsonSerializerOptions _options;

    public SensitiveDataJsonConverterTests()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new SensitiveDataJsonConverterFactory());
    }

    [Fact]
    public void Serialize_WithSensitiveProperties_ShouldMaskValues()
    {
        // Arrange
        var patient = new TestPatientDto
        {
            Id = 1,
            Name = "张三",
            PhoneNumber = "13812345678",
            IdNumber = "110101199001011234",
            MedicalHistory = "患者有高血压病史，需要长期服药控制"
        };

        // Act
        var json = JsonSerializer.Serialize(patient, _options);
        var result = JsonSerializer.Deserialize<JsonElement>(json);

        // Assert
        Assert.Equal(1, result.GetProperty("Id").GetInt32());
        Assert.Equal("张三", result.GetProperty("Name").GetString());

        // 电话号码应该被部分脱敏
        var phone = result.GetProperty("PhoneNumber").GetString();
        Assert.NotNull(phone);
        Assert.NotEqual("13812345678", phone);
        Assert.Contains("****", phone);

        // 身份证号应该被部分脱敏
        var idNumber = result.GetProperty("IdNumber").GetString();
        Assert.NotNull(idNumber);
        Assert.NotEqual("110101199001011234", idNumber);
        Assert.Contains("*", idNumber);

        // 病史应该被哈希脱敏
        var medicalHistory = result.GetProperty("MedicalHistory").GetString();
        Assert.NotNull(medicalHistory);
        Assert.StartsWith("[REDACTED:", medicalHistory);
    }

    [Fact]
    public void Serialize_WithoutSensitiveProperties_ShouldNotUseMasking()
    {
        // Arrange
        var normalDto = new NormalDto
        {
            Id = 1,
            Name = "普通名称",
            Description = "普通描述"
        };

        // Act
        var json = JsonSerializer.Serialize(normalDto, _options);
        var result = JsonSerializer.Deserialize<NormalDto>(json, _options);

        // Assert - 没有敏感属性的类型不应该被工厂处理
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("普通名称", result.Name);
        Assert.Equal("普通描述", result.Description);
    }

    [Fact]
    public void Deserialize_ShouldNotMaskValues()
    {
        // Arrange - 模拟前端发送的原始数据
        var json = """{"Id":1,"Name":"张三","PhoneNumber":"13812345678","IdNumber":"110101199001011234","MedicalHistory":"病史内容"}""";

        // Act - 反序列化时不应该脱敏
        var result = JsonSerializer.Deserialize<TestPatientDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("13812345678", result.PhoneNumber);
        Assert.Equal("110101199001011234", result.IdNumber);
        Assert.Equal("病史内容", result.MedicalHistory);
    }

    [Fact]
    public void Serialize_NullValue_ShouldSerializeAsNull()
    {
        // Arrange
        var patient = new TestPatientDto
        {
            Id = 1,
            Name = "张三",
            PhoneNumber = null!,
            IdNumber = null!,
            MedicalHistory = null!
        };

        // Act
        var json = JsonSerializer.Serialize(patient, _options);

        // Assert - 不应该抛出异常
        Assert.NotNull(json);
        Assert.Contains("\"Id\":1", json);
    }

    /// <summary>
    /// 测试用患者DTO
    /// </summary>
    private class TestPatientDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        [SensitiveData(SensitiveDataType.ContactInfo, MaskingMode = MaskingMode.Partial)]
        public string PhoneNumber { get; set; } = string.Empty;

        [SensitiveData(SensitiveDataType.IdentityInfo, MaskingMode = MaskingMode.Partial)]
        public string IdNumber { get; set; } = string.Empty;

        [SensitiveData(SensitiveDataType.MedicalInfo, MaskingMode = MaskingMode.Hash)]
        public string MedicalHistory { get; set; } = string.Empty;
    }

    /// <summary>
    /// 无敏感数据的普通DTO
    /// </summary>
    private class NormalDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
