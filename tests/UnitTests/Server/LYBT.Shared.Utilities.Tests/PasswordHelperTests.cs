using Microsoft.Extensions.Logging;
using Xunit;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Security;

namespace LYBT.Shared.Utilities.Tests
{
    /// <summary>
    /// 统一密码帮助类单元测试
    /// 验证密码哈希、验证和生成功能的正确性和一致性
    /// </summary>
    public class PasswordHelperTests
    {
        private readonly ILogger<PasswordHelperTests> _logger;

        public PasswordHelperTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<PasswordHelperTests>();
        }

        #region 密码哈希测试

        [Fact]
        public void HashPassword_WithValidPassword_ReturnsHashedPassword()
        {
            // Arrange
            string password = "TestPassword123!";
            UserRole userType = UserRole.Doctor;

            // Act
            string hashedPassword = PasswordHelper.HashPassword(password, userType, _logger);

            // Assert
            Assert.NotNull(hashedPassword);
            Assert.NotEqual(password, hashedPassword);
            Assert.StartsWith("$2a$", hashedPassword); // BCrypt哈希格式
        }

        [Fact]
        public void HashPassword_WithEmptyPassword_ThrowsArgumentException()
        {
            // Arrange
            string password = "";
            UserRole userType = UserRole.Doctor;

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                PasswordHelper.HashPassword(password, userType, _logger));
        }

        [Theory]
        [InlineData(UserRole.Doctor)]
        [InlineData(UserRole.Admin)]
        [InlineData(UserRole.SuperAdmin)]
        public void HashPassword_WithDifferentUserTypes_ReturnsValidHash(UserRole userType)
        {
            // Arrange
            string password = "TestPassword123!";

            // Act
            string hashedPassword = PasswordHelper.HashPassword(password, userType, _logger);

            // Assert
            Assert.NotNull(hashedPassword);
            Assert.StartsWith("$2a$", hashedPassword);
        }

        #endregion

        #region 密码验证测试

        [Fact]
        public void VerifyPassword_WithCorrectPassword_ReturnsSuccess()
        {
            // Arrange
            string password = "TestPassword123!";
            string hashedPassword = PasswordHelper.HashPassword(password, UserRole.Doctor, _logger);

            // Act
            var result = PasswordHelper.VerifyPassword(password, hashedPassword, UserRole.Doctor, _logger);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Timestamp);
            Assert.Null(result.ErrorMessage);
        }

        [Fact]
        public void VerifyPassword_WithIncorrectPassword_ReturnsFailure()
        {
            // Arrange
            string correctPassword = "TestPassword123!";
            string incorrectPassword = "WrongPassword123!";
            string hashedPassword = PasswordHelper.HashPassword(correctPassword, UserRole.Doctor, _logger);

            // Act
            var result = PasswordHelper.VerifyPassword(incorrectPassword, hashedPassword, UserRole.Doctor, _logger);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Timestamp);
        }

        [Fact]
        public void VerifyPassword_WithEmptyPassword_ReturnsFailure()
        {
            // Arrange
            string hashedPassword = PasswordHelper.HashPassword("TestPassword123!", UserRole.Doctor, _logger);

            // Act
            var result = PasswordHelper.VerifyPassword("", hashedPassword, UserRole.Doctor, _logger);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
        }

        #endregion

        #region 密码重新哈希测试

        [Fact]
        public void VerifyAndRehashIfNeeded_WithDifferentWorkFactor_ReturnsRehashNeeded()
        {
            // Arrange
            string password = "TestPassword123!";
            // 使用默认工作因子11创建哈希
            string originalHash = PasswordHelper.HashPassword(password, UserRole.Doctor, _logger);

            // 模拟不同的工作因子（修改哈希字符串中的工作因子）
            string differentWorkFactorHash = originalHash.Replace("$2a$11$", "$2a$10$");

            // Act
            var result = PasswordHelper.VerifyPassword(password, differentWorkFactorHash, UserRole.Doctor, _logger);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(result.NeedsRehash);
            Assert.NotNull(result.NewHashedPassword);
            Assert.NotEqual(differentWorkFactorHash, result.NewHashedPassword);
        }

        [Fact]
        public void VerifyAndRehashIfNeeded_WithCorrectWorkFactor_NoRehashNeeded()
        {
            // Arrange
            string password = "TestPassword123!";
            string hashedPassword = PasswordHelper.HashPassword(password, UserRole.Doctor, _logger);

            // Act
            var result = PasswordHelper.VerifyAndRehashIfNeeded(password, hashedPassword, UserRole.Doctor, _logger);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(result.NeedsRehash);
            Assert.Null(result.NewHashedPassword);
        }

        #endregion

        #region 临时密码生成测试

        [Fact]
        public void GenerateTemporaryPassword_ReturnsValidPassword()
        {
            // Act
            string tempPassword = PasswordHelper.GenerateTemporaryPassword();

            // Assert
            Assert.NotNull(tempPassword);
            Assert.Equal(8, tempPassword.Length);
            Assert.Matches(@"^[A-Z][a-z]{4}\d{3}$", tempPassword); // 格式验证：大写字母(1) + 小写字母(4) + 数字(3)
        }

        [Fact]
        public void GenerateTemporaryPassword_CalledMultipleTimes_ReturnsUniquePasswords()
        {
            // Act
            string tempPassword1 = PasswordHelper.GenerateTemporaryPassword();
            string tempPassword2 = PasswordHelper.GenerateTemporaryPassword();
            string tempPassword3 = PasswordHelper.GenerateTemporaryPassword();

            // Assert
            Assert.NotEqual(tempPassword1, tempPassword2);
            Assert.NotEqual(tempPassword2, tempPassword3);
            Assert.NotEqual(tempPassword1, tempPassword3);
        }

        #endregion

        #region 盐值生成测试

        [Fact]
        public void GenerateSalt_WithDefaultLength_ReturnsValidSalt()
        {
            // Act
            string salt = PasswordHelper.GenerateSalt();

            // Assert
            Assert.NotNull(salt);
            Assert.Equal(32, Convert.FromBase64String(salt).Length); // 默认32字节
        }

        [Fact]
        public void GenerateSalt_WithCustomLength_ReturnsValidSalt()
        {
            // Arrange
            int customLength = 16;

            // Act
            string salt = PasswordHelper.GenerateSalt(customLength);

            // Assert
            Assert.NotNull(salt);
            Assert.Equal(customLength, Convert.FromBase64String(salt).Length);
        }

        #endregion

        #region 配置管理测试

        [Fact]
        public void GetConfiguration_ReturnsValidConfiguration()
        {
            // Act
            var config = PasswordHelper.GetConfiguration();

            // Assert
            Assert.NotNull(config);
            Assert.Equal(11, config.WorkFactor);
            Assert.True(config.EnableRehashing);
            Assert.Equal(5, config.PasswordHistoryCount);
            Assert.Equal(11, config.DefaultWorkFactor);
            Assert.Equal(10, config.MinWorkFactor);
            Assert.Equal(15, config.MaxWorkFactor);
        }

        [Fact]
        public void UpdateWorkFactor_WithValidValue_ReturnsTrue()
        {
            // Arrange
            int originalWorkFactor = PasswordHelper.WorkFactor;

            // Act
            bool result = PasswordHelper.UpdateWorkFactor(12);

            // Assert
            Assert.True(result);
            Assert.Equal(12, PasswordHelper.WorkFactor);
            Assert.NotEqual(originalWorkFactor, PasswordHelper.WorkFactor);
        }

        [Fact]
        public void UpdateWorkFactor_WithInvalidValue_ReturnsFalse()
        {
            // Arrange
            int originalWorkFactor = PasswordHelper.WorkFactor;

            // Act
            bool result = PasswordHelper.UpdateWorkFactor(20); // 超出最大值15

            // Assert
            Assert.False(result);
            Assert.Equal(originalWorkFactor, PasswordHelper.WorkFactor); // 工作因子未改变
        }

        #endregion

        #region 密码哈希一致性测试

        [Fact]
        public void HashAndVerify_RoundTripWithSamePassword_ReturnsConsistentResults()
        {
            // Arrange
            string password = "TestPassword123!@#";
            var userType = UserRole.Doctor;

            // Act
            string hashedPassword = PasswordHelper.HashPassword(password, userType, _logger);
            var verificationResult = PasswordHelper.VerifyPassword(password, hashedPassword, userType, _logger);

            // Assert
            Assert.True(verificationResult.IsSuccess);
            Assert.Equal(password.Length, password.Length);
        }

        [Theory]
        [InlineData("Password123")]
        [InlineData("ComplexPassword!@#$%^&*()")]
        [InlineData("汉字密码123")]
        [InlineData("🌟Password123🌟")]
        public void HashAndVerify_WithVariousPasswordTypes_ReturnsConsistentResults(string password)
        {
            // Act
            string hashedPassword = PasswordHelper.HashPassword(password, UserRole.Doctor, _logger);
            var verificationResult = PasswordHelper.VerifyPassword(password, hashedPassword, UserRole.Doctor, _logger);

            // Assert
            Assert.True(verificationResult.IsSuccess);
            Assert.NotNull(hashedPassword);
        }

        #endregion

        #region 边界条件测试

        [Fact]
        public void VerifyPassword_WithNullHashedPassword_ReturnsFailure()
        {
            // Arrange
            string password = "TestPassword123!";

            // Act
            var result = PasswordHelper.VerifyPassword(password, null!, UserRole.Doctor, _logger);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public void VerifyPassword_WithNullParameters_ReturnsFailure()
        {
            // Act
            var result = PasswordHelper.VerifyPassword(null!, null!, UserRole.Doctor, _logger);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
        }

        #endregion

        #region 性能相关测试

        [Fact]
        public void HashPassword_PerformanceTest_CompletesWithinReasonableTime()
        {
            // Arrange
            string password = "TestPassword123!";
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            for (int i = 0; i < 100; i++)
            {
                string hashedPassword = PasswordHelper.HashPassword(password, UserRole.Doctor, _logger);
                Assert.NotNull(hashedPassword);
            }

            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, $"密码哈希100次耗时: {stopwatch.ElapsedMilliseconds}ms");
        }

        [Fact]
        public void VerifyPassword_PerformanceTest_CompletesWithinReasonableTime()
        {
            // Arrange
            string password = "TestPassword123!";
            string hashedPassword = PasswordHelper.HashPassword(password, UserRole.Doctor, _logger);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            for (int i = 0; i < 100; i++)
            {
                var result = PasswordHelper.VerifyPassword(password, hashedPassword, UserRole.Doctor, _logger);
                Assert.True(result.IsSuccess);
            }

            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 2000, $"密码验证100次耗时: {stopwatch.ElapsedMilliseconds}ms");
        }

        #endregion

        #region 并发安全测试

        [Fact]
        public async Task HashPassword_ConcurrentExecution_ReturnsUniqueHashes()
        {
            // Arrange
            const int taskCount = 10;
            string password = "TestPassword123!";
            var tasks = new List<Task<string>>();

            // Act
            for (int i = 0; i < taskCount; i++)
            {
                tasks.Add(Task.Run(() => PasswordHelper.HashPassword(password, UserRole.Doctor, _logger)));
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(taskCount, results.Count);

            // 验证所有哈希值都是唯一的（BCrypt会自动添加盐值）
            var uniqueHashes = results.Distinct().ToList();
            Assert.Equal(taskCount, uniqueHashes.Count);
        }

        [Fact]
        public async Task VerifyPassword_ConcurrentExecution_AllSucceed()
        {
            // Arrange
            const int taskCount = 10;
            string password = "TestPassword123!";
            string hashedPassword = PasswordHelper.HashPassword(password, UserRole.Doctor, _logger);
            var tasks = new List<Task<PasswordVerificationResult>>();

            // Act
            for (int i = 0; i < taskCount; i++)
            {
                tasks.Add(Task.Run(() => PasswordHelper.VerifyPassword(password, hashedPassword, UserRole.Doctor, _logger)));
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(taskCount, results.Count);
            Assert.All(results, r => r.IsSuccess);
        }

        #endregion
    }
}