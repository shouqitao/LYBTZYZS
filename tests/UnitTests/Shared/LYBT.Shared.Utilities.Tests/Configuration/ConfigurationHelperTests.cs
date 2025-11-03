using FluentAssertions;
using LYBT.Shared.Utilities.Configuration;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace LYBT.Shared.Utilities.Tests.Configuration
{
    /// <summary>
    /// ConfigurationHelper工具类单元测试
    /// </summary>
    public class ConfigurationHelperTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IConfigurationSection> _mockSection;

        public ConfigurationHelperTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockSection = new Mock<IConfigurationSection>();
        }

        #region GetValue方法测试

        [Fact]
        public void GetValue_WithValidStringValue_ShouldReturnString()
        {
            // Arrange
            var key = "TestKey";
            var value = "TestValue";
            _mockConfiguration.Setup(x => x[key]).Returns(value);

            // Act
            var result = ConfigurationHelper.GetValue<string>(_mockConfiguration.Object, key);

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
            _mockConfiguration.Setup(x => x[key]).Returns(value);

            // Act
            var result = ConfigurationHelper.GetValue<int>(_mockConfiguration.Object, key);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void GetValue_WithValidBoolValue_ShouldReturnBool()
        {
            // Arrange
            var key = "TestKey";
            var value = "true";
            _mockConfiguration.Setup(x => x[key]).Returns(value);

            // Act
            var result = ConfigurationHelper.GetValue<bool>(_mockConfiguration.Object, key);

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
            _mockConfiguration.Setup(x => x[key]).Returns(value);

            // Act
            var result = ConfigurationHelper.GetValue<double>(_mockConfiguration.Object, key);

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
            _mockConfiguration.Setup(x => x[key]).Returns(value);

            // Act
            var result = ConfigurationHelper.GetValue<TimeSpan>(_mockConfiguration.Object, key);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void GetValue_WithNullValue_ShouldReturnDefaultValue()
        {
            // Arrange
            var key = "TestKey";
            var defaultValue = "DefaultValue";
            _mockConfiguration.Setup(x => x[key]).Returns((string?)null);

            // Act
            var result = ConfigurationHelper.GetValue(_mockConfiguration.Object, key, defaultValue);

            // Assert
            result.Should().Be(defaultValue);
        }

        [Fact]
        public void GetValue_WithEmptyValue_ShouldReturnDefaultValue()
        {
            // Arrange
            var key = "TestKey";
            var defaultValue = "DefaultValue";
            _mockConfiguration.Setup(x => x[key]).Returns("");

            // Act
            var result = ConfigurationHelper.GetValue(_mockConfiguration.Object, key, defaultValue);

            // Assert
            result.Should().Be(defaultValue);
        }

        [Fact]
        public void GetValue_WithWhitespaceValue_ShouldReturnDefaultValue()
        {
            // Arrange
            var key = "TestKey";
            var defaultValue = "DefaultValue";
            _mockConfiguration.Setup(x => x[key]).Returns("   ");

            // Act
            var result = ConfigurationHelper.GetValue(_mockConfiguration.Object, key, defaultValue);

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
            _mockConfiguration.Setup(x => x[key]).Returns(value);

            // Act
            var result = ConfigurationHelper.GetValue(_mockConfiguration.Object, key, defaultValue);

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
            _mockConfiguration.Setup(x => x[key]).Returns(value);

            // Act
            var result = ConfigurationHelper.GetValue(_mockConfiguration.Object, key, defaultValue);

            // Assert
            result.Should().Be(defaultValue);
        }

        #endregion

        #region GetConnectionString方法测试

        [Fact]
        public void GetConnectionString_WithEnvironmentVariable_ShouldReturnEnvironmentValue()
        {
            // Arrange
            var envVarName = "TEST_CONNECTION_STRING";
            var envValue = "Server=env;Database=test;";
            var configValue = "Server=config;Database=test;";

            Environment.SetEnvironmentVariable(envVarName, envValue);
            _mockConfiguration.Setup(x => x.GetConnectionString("DefaultConnection")).Returns(configValue);

            try
            {
                // Act
                var result = ConfigurationHelper.GetConnectionString(_mockConfiguration.Object, "DefaultConnection", envVarName);

                // Assert
                result.Should().Be(envValue);
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable(envVarName, null);
            }
        }

        [Fact]
        public void GetConnectionString_WithoutEnvironmentVariable_ShouldReturnConfigValue()
        {
            // Arrange
            var configValue = "Server=config;Database=test;";
            _mockConfiguration.Setup(x => x.GetConnectionString("DefaultConnection")).Returns(configValue);

            // Act
            var result = ConfigurationHelper.GetConnectionString(_mockConfiguration.Object, "DefaultConnection", "NONEXISTENT_ENV_VAR");

            // Assert
            result.Should().Be(configValue);
        }

        [Fact]
        public void GetConnectionString_WithNullEnvironmentVariable_ShouldReturnConfigValue()
        {
            // Arrange
            var configValue = "Server=config;Database=test;";
            _mockConfiguration.Setup(x => x.GetConnectionString("DefaultConnection")).Returns(configValue);

            // Act
            var result = ConfigurationHelper.GetConnectionString(_mockConfiguration.Object, "DefaultConnection", null);

            // Assert
            result.Should().Be(configValue);
        }

        [Fact]
        public void GetConnectionString_WithEmptyConfigValue_ShouldReturnEmptyString()
        {
            // Arrange
            _mockConfiguration.Setup(x => x.GetConnectionString("DefaultConnection")).Returns((string?)null);

            // Act
            var result = ConfigurationHelper.GetConnectionString(_mockConfiguration.Object, "DefaultConnection", "NONEXISTENT_ENV_VAR");

            // Assert
            result.Should().Be(string.Empty);
        }

        #endregion

        #region GetRequiredValue方法测试

        [Fact]
        public void GetRequiredValue_WithValidValue_ShouldReturnValue()
        {
            // Arrange
            var key = "TestKey";
            var value = "TestValue";
            _mockConfiguration.Setup(x => x[key]).Returns(value);

            // Act
            var result = ConfigurationHelper.GetRequiredValue(_mockConfiguration.Object, key);

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
            _mockConfiguration.Setup(x => x[key]).Returns(value);

            // Act & Assert
            var act = () => ConfigurationHelper.GetRequiredValue(_mockConfiguration.Object, key);
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
            _mockConfiguration.Setup(x => x[key]).Returns(value);

            // Act
            var result = ConfigurationHelper.Exists(_mockConfiguration.Object, key);

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
            _mockConfiguration.Setup(x => x[key]).Returns(value);

            // Act
            var result = ConfigurationHelper.Exists(_mockConfiguration.Object, key);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region GetSection方法测试

        [Fact]
        public void GetSection_WithExistingSection_ShouldReturnBoundObject()
        {
            // Arrange
            var sectionName = "TestSection";
            _mockSection.Setup(x => x.Exists()).Returns(true);
            _mockConfiguration.Setup(x => x.GetSection(sectionName)).Returns(_mockSection.Object);

            // Act
            var result = ConfigurationHelper.GetSection<TestConfigSection>(_mockConfiguration.Object, sectionName);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<TestConfigSection>();
        }

        [Fact]
        public void GetSection_WithNonExistingSection_ShouldReturnNull()
        {
            // Arrange
            var sectionName = "NonExistentSection";
            _mockSection.Setup(x => x.Exists()).Returns(false);
            _mockConfiguration.Setup(x => x.GetSection(sectionName)).Returns(_mockSection.Object);

            // Act
            var result = ConfigurationHelper.GetSection<TestConfigSection>(_mockConfiguration.Object, sectionName);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetRequiredSection方法测试

        [Fact]
        public void GetRequiredSection_WithExistingSection_ShouldReturnBoundObject()
        {
            // Arrange
            var sectionName = "TestSection";
            _mockSection.Setup(x => x.Exists()).Returns(true);
            _mockConfiguration.Setup(x => x.GetSection(sectionName)).Returns(_mockSection.Object);

            // Act
            var result = ConfigurationHelper.GetRequiredSection<TestConfigSection>(_mockConfiguration.Object, sectionName);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<TestConfigSection>();
        }

        [Fact]
        public void GetRequiredSection_WithNonExistingSection_ShouldThrowException()
        {
            // Arrange
            var sectionName = "NonExistentSection";
            _mockSection.Setup(x => x.Exists()).Returns(false);
            _mockConfiguration.Setup(x => x.GetSection(sectionName)).Returns(_mockSection.Object);

            // Act & Assert
            var act = () => ConfigurationHelper.GetRequiredSection<TestConfigSection>(_mockConfiguration.Object, sectionName);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"配置节 '{sectionName}' 未找到");
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
            _mockConfiguration.Setup(x => x["Key1"]).Returns("Value1");
            _mockConfiguration.Setup(x => x["Key2"]).Returns("Value2");
            _mockConfiguration.Setup(x => x["Key3"]).Returns("Value3");

            // Act
            var result = ConfigurationHelper.ValidateRequiredKeys(_mockConfiguration.Object, keys);

            // Assert
            result.IsValid.Should().BeTrue();
            result.MissingKeys.Should().BeEmpty();
        }

        [Fact]
        public void ValidateRequiredKeys_WithMissingKeys_ShouldReturnInvalidResult()
        {
            // Arrange
            var keys = new[] { "Key1", "Key2", "Key3" };
            _mockConfiguration.Setup(x => x["Key1"]).Returns("Value1");
            _mockConfiguration.Setup(x => x["Key2"]).Returns((string?)null);
            _mockConfiguration.Setup(x => x["Key3"]).Returns("   ");

            // Act
            var result = ConfigurationHelper.ValidateRequiredKeys(_mockConfiguration.Object, keys);

            // Assert
            result.IsValid.Should().BeFalse();
            result.MissingKeys.Should().Contain("Key2", "Key3");
            result.MissingKeys.Should().NotContain("Key1");
        }

        [Fact]
        public void ValidateRequiredKeys_WithEmptyKeyList_ShouldReturnValidResult()
        {
            // Act
            var result = ConfigurationHelper.ValidateRequiredKeys(_mockConfiguration.Object);

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
            _mockConfiguration.Setup(x => x[key]).Returns(value);

            // Act
            var result = ConfigurationHelper.GetValue<DateTime>(_mockConfiguration.Object, key);

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
            _mockConfiguration.Setup(x => x[key]).Returns(value);

            // Act
            var result = ConfigurationHelper.GetValue(_mockConfiguration.Object, key, defaultValue);

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
            _mockConfiguration.Setup(x => x[key]).Returns(value);

            // Act
            var result = ConfigurationHelper.GetValue(_mockConfiguration.Object, key, defaultValue);

            // Assert
            result.Should().Be(defaultValue);
        }

        #endregion
    }
}
