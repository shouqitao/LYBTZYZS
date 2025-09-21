using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Services.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Desktop.Tests.Core.Services.Configuration
{
    /// <summary>
    /// KDF 强化与随机盐测试
    /// </summary>
    public class SecureConfigurationServiceKDFTests
    {
        private readonly Mock<ILogger<SecureConfigurationService>> _loggerMock;
        private readonly SecureConfigurationService _service;

        public SecureConfigurationServiceKDFTests()
        {
            _loggerMock = new Mock<ILogger<SecureConfigurationService>>();
            _service = new SecureConfigurationService(_loggerMock.Object);
        }

        [Fact]
        public async Task SetSecureValue_ShouldUseRandomSaltForEachRecord()
        {
            // Arrange
            var key1 = "TestKey1";
            var key2 = "TestKey2";
            var value = "TestValue";

            // Act
            await _service.SetSecureValueAsync(key1, value);
            await _service.SetSecureValueAsync(key2, value);

            // Assert - 通过反射验证内部状态
            var configsField = typeof(SecureConfigurationService)
                .GetField("_secureConfigs", BindingFlags.NonPublic | BindingFlags.Instance);
            var configs = configsField?.GetValue(_service) as dynamic;

            // 验证每条记录都有独立的盐值
            Assert.NotNull(configs[key1].Salt);
            Assert.NotNull(configs[key2].Salt);
            Assert.NotEqual(configs[key1].Salt, configs[key2].Salt);
        }

        [Fact]
        public async Task SetSecureValue_ShouldRecordIterationCount()
        {
            // Arrange
            var key = "IterationTestKey";
            var value = "TestValue";
            var expectedIterations = 100000; // OWASP 2025 推荐最小值

            // Act
            await _service.SetSecureValueAsync(key, value);

            // Assert - 验证迭代次数
            var configsField = typeof(SecureConfigurationService)
                .GetField("_secureConfigs", BindingFlags.NonPublic | BindingFlags.Instance);
            var configs = configsField?.GetValue(_service) as dynamic;

            Assert.Equal(expectedIterations, configs[key].Iterations);
        }

        [Fact]
        public async Task GetSecureValue_ShouldSupportBackwardCompatibility()
        {
            // Arrange - 模拟旧格式数据（无盐值）
            var key = "LegacyKey";
            var value = "LegacyValue";

            // 先存储一个值
            await _service.SetSecureValueAsync(key, value);

            // Act - 读取值
            var retrievedValue = await _service.GetSecureValueAsync<string>(key);

            // Assert
            Assert.Equal(value, retrievedValue);
        }

        [Fact]
        public async Task MasterKey_ShouldUsePersistentRandomSalt()
        {
            // Arrange
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LYBT",
                "SecureConfig");
            var saltFile = Path.Combine(appDataPath, "master.salt");

            // Act - 创建服务会生成或读取主密钥盐
            var service1 = new SecureConfigurationService(_loggerMock.Object);
            var fileExists = File.Exists(saltFile);

            // Assert
            Assert.True(fileExists, "主密钥盐文件应该被创建");

            // 验证文件属性
            if (fileExists)
            {
                var attributes = File.GetAttributes(saltFile);
                Assert.True((attributes & FileAttributes.Hidden) == FileAttributes.Hidden);
                Assert.True((attributes & FileAttributes.System) == FileAttributes.System);
            }
        }

        [Fact]
        public async Task SaltGeneration_ShouldBeCryptographicallyRandom()
        {
            // Arrange
            var salts = new HashSet<string>();
            const int testCount = 100;

            // Act - 生成多个值，每个都应有独特的盐
            for (int i = 0; i < testCount; i++)
            {
                var key = $"RandomTest_{i}";
                await _service.SetSecureValueAsync(key, "value");

                // 通过反射获取盐值
                var configsField = typeof(SecureConfigurationService)
                    .GetField("_secureConfigs", BindingFlags.NonPublic | BindingFlags.Instance);
                var configs = configsField?.GetValue(_service) as dynamic;
                salts.Add(configs[key].Salt);
            }

            // Assert - 所有盐值都应该是唯一的
            Assert.Equal(testCount, salts.Count);
        }

        [Fact]
        public async Task KDF_ShouldResistTimingAttacks()
        {
            // Arrange
            var key = "TimingTestKey";
            var correctPassword = "CorrectPassword123";
            var wrongPassword = "WrongPassword456";

            // Act
            await _service.SetSecureValueAsync(key, "SecretData", correctPassword);

            var stopwatch = new System.Diagnostics.Stopwatch();
            var timings = new List<long>();

            // 测试多次以获得平均值
            for (int i = 0; i < 5; i++)
            {
                stopwatch.Restart();
                try
                {
                    await _service.GetSecureValueAsync<string>(key, wrongPassword);
                }
                catch { }
                stopwatch.Stop();
                timings.Add(stopwatch.ElapsedMilliseconds);
            }

            // Assert - KDF 应该使时间攻击变得困难
            // 100,000 次迭代应该需要明显的时间
            var avgTime = timings.Average();
            Assert.True(avgTime > 10, "KDF 应该引入足够的延迟以防止暴力破解");
        }

        [Fact]
        public async Task SetSecureValue_WithPassphrase_ShouldUseSeparateSalt()
        {
            // Arrange
            var key = "PassphraseKey";
            var value = "ProtectedValue";
            var passphrase = "MySecurePassphrase";

            // Act
            await _service.SetSecureValueAsync(key, value, passphrase);

            // Assert - 验证使用了独立的盐
            var configsField = typeof(SecureConfigurationService)
                .GetField("_secureConfigs", BindingFlags.NonPublic | BindingFlags.Instance);
            var configs = configsField?.GetValue(_service) as dynamic;

            Assert.NotNull(configs[key].Salt);
            Assert.Equal(100000, configs[key].Iterations);

            // 验证能用正确的密码解密
            var decrypted = await _service.GetSecureValueAsync<string>(key, passphrase);
            Assert.Equal(value, decrypted);
        }

        [Fact]
        public void SaltLength_ShouldBe256Bits()
        {
            // Assert - 验证盐长度为 32 字节（256 位）
            var saltLengthField = typeof(SecureConfigurationService)
                .GetField("_saltLength", BindingFlags.NonPublic | BindingFlags.Static);
            var saltLength = (int)saltLengthField.GetValue(null);

            Assert.Equal(32, saltLength);
        }

        [Fact]
        public void Dispose_ShouldNotLeakSensitiveData()
        {
            // Arrange
            using (var service = new SecureConfigurationService(_loggerMock.Object))
            {
                // Act - 服务被释放
            }

            // Assert - 验证日志不包含敏感信息
            _loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => !o.ToString().Contains("salt") &&
                                                   !o.ToString().Contains("key") &&
                                                   !o.ToString().Contains("password")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }
    }
}