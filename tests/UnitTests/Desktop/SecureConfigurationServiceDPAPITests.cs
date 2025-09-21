using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Services.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Desktop.Tests.Core.Services.Configuration
{
    /// <summary>
    /// 主密钥 DPAPI 保护测试
    /// </summary>
    public class SecureConfigurationServiceDPAPITests : IDisposable
    {
        private readonly Mock<ILogger<SecureConfigurationService>> _loggerMock;
        private readonly SecureConfigurationService _service;
        private readonly string _testPath;

        public SecureConfigurationServiceDPAPITests()
        {
            _loggerMock = new Mock<ILogger<SecureConfigurationService>>();
            _service = new SecureConfigurationService(_loggerMock.Object);

            // 获取测试文件路径
            _testPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LYBT",
                "SecureConfig");
        }

        [Fact]
        public async Task MasterKey_ShouldBeProtectedWithDPAPI()
        {
            // Arrange
            var masterKeyFile = Path.Combine(_testPath, "master.key");

            // Act - 创建服务会生成主密钥
            var service = new SecureConfigurationService(_loggerMock.Object);

            // Assert
            Assert.True(File.Exists(masterKeyFile), "主密钥文件应该被创建");

            // 验证文件是 DPAPI 加密的（不是明文）
            if (File.Exists(masterKeyFile))
            {
                var encryptedKey = File.ReadAllBytes(masterKeyFile);

                // DPAPI 加密的数据应该比原始 32 字节密钥更大
                Assert.True(encryptedKey.Length > 32, "DPAPI 加密应该增加数据大小");

                // 验证文件属性
                var attributes = File.GetAttributes(masterKeyFile);
                Assert.True((attributes & FileAttributes.Hidden) == FileAttributes.Hidden);
                Assert.True((attributes & FileAttributes.System) == FileAttributes.System);
            }
        }

        [SkipOnNonWindowsFact]
        public async Task DPAPI_ShouldBeUserSpecific()
        {
            // Arrange
            var key = "UserSpecificKey";
            var value = "UserSpecificData";

            // Act
            await _service.SetSecureValueAsync(key, value);

            // Assert - 数据应该只能被当前用户解密
            // 注意：在单元测试中无法真正测试不同用户，但可以验证使用了正确的范围
            var retrieved = await _service.GetSecureValueAsync<string>(key);
            Assert.Equal(value, retrieved);

            // 验证日志显示使用了 DPAPI
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("DPAPI")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task MasterKey_ShouldNotContainMachineName()
        {
            // Arrange - 新的实现不应该依赖机器名
            var masterKeyFile = Path.Combine(_testPath, "master.key");

            // Act
            var service = new SecureConfigurationService(_loggerMock.Object);

            // Assert - 主密钥应该是完全随机的，不包含机器特征
            if (File.Exists(masterKeyFile))
            {
                var encryptedKey = File.ReadAllBytes(masterKeyFile);
                var keyString = Convert.ToBase64String(encryptedKey);

                // 不应该包含机器名或用户名的明文
                Assert.DoesNotContain(Environment.MachineName, keyString);
                Assert.DoesNotContain(Environment.UserName, keyString);
            }
        }

        [Fact]
        public async Task MasterKey_Regeneration_ShouldInvalidateOldData()
        {
            // Arrange
            var key = "TestKey";
            var value = "TestValue";
            var masterKeyFile = Path.Combine(_testPath, "master.key");

            // 第一次创建并存储数据
            await _service.SetSecureValueAsync(key, value);
            var firstValue = await _service.GetSecureValueAsync<string>(key);
            Assert.Equal(value, firstValue);

            // Act - 删除主密钥文件以强制重新生成
            if (File.Exists(masterKeyFile))
            {
                File.Delete(masterKeyFile);
            }

            // 创建新服务实例（会重新生成主密钥）
            var newService = new SecureConfigurationService(_loggerMock.Object);

            // Assert - 旧数据应该无法解密（因为主密钥已更改）
            var result = await newService.GetSecureValueAsync<string>(key);
            Assert.Null(result); // 或者抛出异常，取决于实现
        }

        [SkipOnNonWindowsFact]
        public async Task DPAPI_ShouldHandleKeyRotation()
        {
            // Arrange
            var key = "RotationTestKey";
            var value = "RotationTestValue";

            // Act - 存储值
            await _service.SetSecureValueAsync(key, value);

            // 模拟密钥轮换
            await _service.RotateEncryptionKeyAsync("", "NewPassword123");

            // Assert - 轮换后仍能读取
            var retrieved = await _service.GetSecureValueAsync<string>(key, "NewPassword123");
            Assert.Equal(value, retrieved);
        }

        [Fact]
        public async Task FallbackProtection_ShouldWorkOnNonWindows()
        {
            // Arrange
            var key = "FallbackKey";
            var value = "FallbackValue";

            // Act
            await _service.SetSecureValueAsync(key, value);
            var retrieved = await _service.GetSecureValueAsync<string>(key);

            // Assert - 应该能在所有平台工作
            Assert.Equal(value, retrieved);

            // 如果不是 Windows，应该有警告日志
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _loggerMock.Verify(
                    x => x.Log(
                        LogLevel.Warning,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("不支持 DPAPI")),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                    Times.AtLeastOnce);
            }
        }

        [Fact]
        public async Task MasterKey_ShouldBeConsistentAcrossRestarts()
        {
            // Arrange
            var key = "ConsistencyKey";
            var value = "ConsistencyValue";

            // Act - 第一个服务实例
            var service1 = new SecureConfigurationService(_loggerMock.Object);
            await service1.SetSecureValueAsync(key, value);

            // 第二个服务实例（模拟重启）
            var service2 = new SecureConfigurationService(_loggerMock.Object);
            var retrieved = await service2.GetSecureValueAsync<string>(key);

            // Assert - 数据应该保持一致
            Assert.Equal(value, retrieved);
        }

        [Fact]
        public async Task DPAPI_Error_ShouldLogAndRegenerate()
        {
            // Arrange
            var masterKeyFile = Path.Combine(_testPath, "master.key");

            // 创建一个损坏的主密钥文件
            if (File.Exists(masterKeyFile))
            {
                File.WriteAllBytes(masterKeyFile, new byte[] { 0x00, 0x01, 0x02 });
            }

            // Act - 创建服务应该处理错误并重新生成
            var service = new SecureConfigurationService(_loggerMock.Object);

            // Assert - 应该记录错误并重新生成
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("DPAPI") &&
                                                   o.ToString().Contains("失败")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);

            // 应该生成新的主密钥
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("生成新的主密钥")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        public void Dispose()
        {
            _service?.Dispose();

            // 清理测试文件
            try
            {
                if (Directory.Exists(_testPath))
                {
                    var files = new[] { "master.key", "master.salt", "secure.dat" };
                    foreach (var file in files)
                    {
                        var path = Path.Combine(_testPath, file);
                        if (File.Exists(path))
                        {
                            File.SetAttributes(path, FileAttributes.Normal);
                            File.Delete(path);
                        }
                    }
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// 仅在 Windows 平台运行的测试
    /// </summary>
    public sealed class SkipOnNonWindowsFactAttribute : FactAttribute
    {
        public SkipOnNonWindowsFactAttribute()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Skip = "此测试仅在 Windows 平台运行";
            }
        }
    }
}