using LYBT.Shared.Utilities.Configuration;
using Microsoft.Extensions.Configuration;

namespace LYBT.Tests.Unit.Utilities.Configuration
{
    /// <summary>
    /// ConfigurationHelper工具类单元测试
    /// </summary>
    public class ConfigurationHelperTests
    {
        private readonly IConfiguration _configuration;
        private readonly IConfigurationSection _section;

        public ConfigurationHelperTests()
        {
            _configuration = Substitute.For<IConfiguration>();
            _section = Substitute.For<IConfigurationSection>();
        }

        #region GetValue方法测试

        [Fact]
        public void GetValue_WithValidStringValue_ShouldReturnString()
        {
            // Arrange
            var key = "TestKey";
            var value = "TestValue";
            _configuration[key].Returns(value);

            // Act
            var result = ConfigurationHelper.GetValue<string>(_configuration, key);

            // Assert
            result.Should().Be(value);
        }

        [Fact]
        public void GetValue_WithValidIntValue_ShouldReturnInt()
        {
            // Arrange
            var key = "TestKey";
            var value = "123";
            var expected = 123;
            _configuration[key].Returns(value);

            // Act
            var result = ConfigurationHelper.GetValue<int>(_configuration, key);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void GetValue_WithValidBoolValue_ShouldReturnBool()
        {
            // Arrange
            var key = "TestKey";
            var value = "true";
            _configuration[key].Returns(value);

            // Act
            var result = ConfigurationHelper.GetValue<bool>(_configuration, key);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GetValue_WithValidDoubleValue_ShouldReturnDouble()
        {
            // Arrange
            var key = "TestKey";
            var value = "123.45";
            var expected = 123.45;
            _configuration[key].Returns(value);

            // Act
            var result = ConfigurationHelper.GetValue<double>(_configuration, key);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void GetValue_WithValidTimeSpanValue_ShouldReturnTimeSpan()
        {
            // Arrange
            var key = "TestKey";
            var value = "01:30:00";
            var expected = TimeSpan.FromMinutes(90);
            _configuration[key].Returns(value);

            // Act
            var result = ConfigurationHelper.GetValue<TimeSpan>(_configuration, key);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void GetValue_WithNullValue_ShouldReturnDefaultValue()
        {
            // Arrange
            var key = "TestKey";
            var defaultValue = "DefaultValue";
            _configuration[key].Returns((string?)null);

            // Act
            var result = ConfigurationHelper.GetValue(_configuration, key, defaultValue);

            // Assert
            result.Should().Be(defaultValue);
        }

        [Fact]
        public void GetValue_WithEmptyValue_ShouldReturnDefaultValue()
        {
            // Arrange
            var key = "TestKey";
            var defaultValue = "DefaultValue";
            _configuration[key].Returns("");

            // Act
            var result = ConfigurationHelper.GetValue(_configuration, key, defaultValue);

            // Assert
            result.Should().Be(defaultValue);
        }

        [Fact]
        public void GetValue_WithWhitespaceValue_ShouldReturnDefaultValue()
        {
            // Arrange
            var key = "TestKey";
            var defaultValue = "DefaultValue";
            _configuration[key].Returns("   ");

            // Act
            var result = ConfigurationHelper.GetValue(_configuration, key, defaultValue);

            // Assert
            result.Should().Be(defaultValue);
        }

        [Fact]
        public void GetValue_WithInvalidIntValue_ShouldReturnDefaultValue()
        {
            // Arrange
            var key = "TestKey";
            var value = "not_a_number";
            var defaultValue = 42;
            _configuration[key].Returns(value);

            // Act
            var result = ConfigurationHelper.GetValue(_configuration, key, defaultValue);

            // Assert
            result.Should().Be(defaultValue);
        }

        [Fact]
        public void GetValue_WithInvalidBoolValue_ShouldReturnDefaultValue()
        {
            // Arrange
            var key = "TestKey";
            var value = "not_a_bool";
            var defaultValue = true;
            _configuration[key].Returns(value);

            // Act
            var result = ConfigurationHelper.GetValue(_configuration, key, defaultValue);

            // Assert
            result.Should().Be(defaultValue);
        }

        #endregion

        #region GetRequiredValue方法测试

        [Fact]
        public void GetRequiredValue_WithValidValue_ShouldReturnValue()
        {
            // Arrange
            var key = "TestKey";
            var value = "TestValue";
            _configuration[key].Returns(value);

            // Act
            var result = ConfigurationHelper.GetRequiredValue(_configuration, key);

            // Assert
            result.Should().Be(value);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void GetRequiredValue_WithInvalidValue_ShouldThrowException(string? value)
        {
            // Arrange
            var key = "TestKey";
            _configuration[key].Returns(value);

            // Act & Assert
            var act = () => ConfigurationHelper.GetRequiredValue(_configuration, key);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"配置项 '{key}' 未设置或为空");
        }

        #endregion

        #region Exists方法测试

        [Fact]
        public void Exists_WithValidValue_ShouldReturnTrue()
        {
            // Arrange
            var key = "TestKey";
            var value = "TestValue";
            _configuration[key].Returns(value);

            // Act
            var result = ConfigurationHelper.Exists(_configuration, key);

            // Assert
            result.Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Exists_WithInvalidValue_ShouldReturnFalse(string? value)
        {
            // Arrange
            var key = "TestKey";
            _configuration[key].Returns(value);

            // Act
            var result = ConfigurationHelper.Exists(_configuration, key);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region MergeConfigurationSources方法测试

        [Fact]
        public void MergeConfigurationSources_WithMultipleSources_ShouldApplyAllSources()
        {
            // Arrange
            var builder = new ConfigurationBuilder();
            var source1Applied = false;
            var source2Applied = false;

            Action<IConfigurationBuilder> source1 = b => { source1Applied = true; };
            Action<IConfigurationBuilder> source2 = b => { source2Applied = true; };

            // Act
            ConfigurationHelper.MergeConfigurationSources(builder, source1, source2);

            // Assert
            source1Applied.Should().BeTrue();
            source2Applied.Should().BeTrue();
        }

        [Fact]
        public void MergeConfigurationSources_WithNoSources_ShouldReturnBuilder()
        {
            // Arrange
            var builder = new ConfigurationBuilder();

            // Act
            var result = ConfigurationHelper.MergeConfigurationSources(builder);

            // Assert
            result.Should().BeSameAs(builder);
        }

        #endregion

        #region ValidateRequiredKeys方法测试

        [Fact]
        public void ValidateRequiredKeys_WithAllKeysPresent_ShouldReturnValidResult()
        {
            // Arrange
            var keys = new[] { "Key1", "Key2", "Key3" };
            _configuration["Key1"].Returns("Value1");
            _configuration["Key2"].Returns("Value2");
            _configuration["Key3"].Returns("Value3");

            // Act
            var result = ConfigurationHelper.ValidateRequiredKeys(_configuration, keys);

            // Assert
            result.IsValid.Should().BeTrue();
            result.MissingKeys.Should().BeEmpty();
        }

        [Fact]
        public void ValidateRequiredKeys_WithMissingKeys_ShouldReturnInvalidResult()
        {
            // Arrange
            var keys = new[] { "Key1", "Key2", "Key3" };
            _configuration["Key1"].Returns("Value1");
            _configuration["Key2"].Returns((string?)null);
            _configuration["Key3"].Returns("   ");

            // Act
            var result = ConfigurationHelper.ValidateRequiredKeys(_configuration, keys);

            // Assert
            result.IsValid.Should().BeFalse();
            result.MissingKeys.Should().Contain("Key2", "Key3");
            result.MissingKeys.Should().NotContain("Key1");
        }

        [Fact]
        public void ValidateRequiredKeys_WithEmptyKeyList_ShouldReturnValidResult()
        {
            // Act
            var result = ConfigurationHelper.ValidateRequiredKeys(_configuration);

            // Assert
            result.IsValid.Should().BeTrue();
            result.MissingKeys.Should().BeEmpty();
        }

        #endregion

        #region ConfigurationValidationResult类测试

        [Fact]
        public void ConfigurationValidationResult_WithNoMissingKeys_ShouldBeValid()
        {
            // Arrange
            var result = new ConfigurationValidationResult();

            // Assert
            result.IsValid.Should().BeTrue();
            result.GetErrorMessage().Should().BeEmpty();
        }

        [Fact]
        public void ConfigurationValidationResult_WithMissingKeys_ShouldBeInvalid()
        {
            // Arrange
            var result = new ConfigurationValidationResult();
            result.MissingKeys.Add("Key1");
            result.MissingKeys.Add("Key2");

            // Assert
            result.IsValid.Should().BeFalse();
            result.GetErrorMessage().Should().Be("以下配置项缺失: Key1, Key2");
        }

        #endregion

        #region 辅助类

        public class TestConfigSection
        {
            public string Property1 { get; set; } = string.Empty;
            public int Property2 { get; set; }
            public bool Property3 { get; set; }
        }

        #endregion

        #region 边界条件和异常测试

        [Fact]
        public void GetValue_WithComplexType_ShouldUseTypeConverter()
        {
            // Arrange
            var key = "TestKey";
            var value = "2023-12-25";
            var expected = new DateTime(2023, 12, 25);
            _configuration[key].Returns(value);

            // Act
            var result = ConfigurationHelper.GetValue<DateTime>(_configuration, key);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void GetValue_WithInvalidComplexType_ShouldReturnDefaultValue()
        {
            // Arrange
            var key = "TestKey";
            var value = "invalid_date";
            var defaultValue = DateTime.Now;
            _configuration[key].Returns(value);

            // Act
            var result = ConfigurationHelper.GetValue(_configuration, key, defaultValue);

            // Assert
            result.Should().Be(defaultValue);
        }

        [Fact]
        public void GetValue_WithUnsupportedType_ShouldReturnDefaultValue()
        {
            // Arrange
            var key = "TestKey";
            var value = "some_value";
            var defaultValue = new object();
            _configuration[key].Returns(value);

            // Act
            var result = ConfigurationHelper.GetValue(_configuration, key, defaultValue);

            // Assert
            result.Should().Be(defaultValue);
        }

        #endregion
    }
}
