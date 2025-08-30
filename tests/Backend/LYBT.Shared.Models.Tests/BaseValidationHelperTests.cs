using System;
using Xunit;
using LYBT.Shared.Models.Base;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Shared.Models.Tests
{
    /// <summary>
    /// BaseValidationHelper单元测试
    /// 测试基础验证功能，确保重构后的验证逻辑正确性
    /// </summary>
    public class BaseValidationHelperTests
    {
        private readonly TestableValidationHelper _validationHelper;

        public BaseValidationHelperTests()
        {
            _validationHelper = new TestableValidationHelper();
        }

        #region ValidateRequiredString Tests

        [Fact]
        public void ValidateRequiredString_WithValidString_ReturnsSuccess()
        {
            // Arrange
            var validString = "Valid String";
            var fieldName = "TestField";

            // Act
            var result = _validationHelper.ValidateRequiredStringPublic(validString, fieldName);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateRequiredString_WithInvalidString_ReturnsFailure(string invalidString)
        {
            // Arrange
            var fieldName = "TestField";

            // Act
            var result = _validationHelper.ValidateRequiredStringPublic(invalidString, fieldName);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("不能为空", result.ErrorMessage);
        }

        #endregion

        #region ValidateStringLength Tests

        [Fact]
        public void ValidateStringLength_WithValidLength_ReturnsSuccess()
        {
            // Arrange
            var validString = "Valid";
            var fieldName = "TestField";
            var maxLength = 10;
            var minLength = 2;

            // Act
            var result = _validationHelper.ValidateStringLengthPublic(validString, fieldName, maxLength, minLength);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void ValidateStringLength_ExceedsMaxLength_ReturnsFailure()
        {
            // Arrange
            var longString = "This is a very long string that exceeds the maximum length";
            var fieldName = "TestField";
            var maxLength = 10;

            // Act
            var result = _validationHelper.ValidateStringLengthPublic(longString, fieldName, maxLength);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains($"不能超过{maxLength}个字符", result.ErrorMessage);
        }

        [Fact]
        public void ValidateStringLength_BelowMinLength_ReturnsFailure()
        {
            // Arrange
            var shortString = "A";
            var fieldName = "TestField";
            var maxLength = 10;
            var minLength = 5;

            // Act
            var result = _validationHelper.ValidateStringLengthPublic(shortString, fieldName, maxLength, minLength);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains($"不能少于{minLength}个字符", result.ErrorMessage);
        }

        [Fact]
        public void ValidateStringLength_WithNullString_ReturnsSuccess()
        {
            // Arrange
            string nullString = null;
            var fieldName = "TestField";
            var maxLength = 10;

            // Act
            var result = _validationHelper.ValidateStringLengthPublic(nullString, fieldName, maxLength);

            // Assert
            Assert.True(result.IsSuccess);
        }

        #endregion

        #region ValidateGuid Tests

        [Fact]
        public void ValidateGuid_WithValidGuid_ReturnsSuccess()
        {
            // Arrange
            var validGuid = Guid.NewGuid();
            var fieldName = "TestId";

            // Act
            var result = _validationHelper.ValidateGuidPublic(validGuid, fieldName);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void ValidateGuid_WithEmptyGuid_ReturnsFailure()
        {
            // Arrange
            var emptyGuid = Guid.Empty;
            var fieldName = "TestId";

            // Act
            var result = _validationHelper.ValidateGuidPublic(emptyGuid, fieldName);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("不能为空", result.ErrorMessage);
        }

        #endregion

        #region ValidateNumericRange Tests

        [Theory]
        [InlineData(5, 1, 10)]
        [InlineData(1, 1, 10)]
        [InlineData(10, 1, 10)]
        public void ValidateNumericRange_WithinRange_ReturnsSuccess(decimal value, decimal min, decimal max)
        {
            // Arrange
            var fieldName = "TestNumber";

            // Act
            var result = _validationHelper.ValidateNumericRangePublic(value, fieldName, min, max);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData(0, 1, 10)]
        [InlineData(11, 1, 10)]
        [InlineData(-5, 0, 10)]
        public void ValidateNumericRange_OutsideRange_ReturnsFailure(decimal value, decimal min, decimal max)
        {
            // Arrange
            var fieldName = "TestNumber";

            // Act
            var result = _validationHelper.ValidateNumericRangePublic(value, fieldName, min, max);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains($"必须在{min}和{max}之间", result.ErrorMessage);
        }

        #endregion

        #region ValidatePositiveNumber Tests

        [Theory]
        [InlineData(10, false)]
        [InlineData(0.5, false)]
        public void ValidatePositiveNumber_WithPositiveValue_ReturnsSuccess(decimal value, bool allowZero)
        {
            // Arrange
            var fieldName = "TestNumber";

            // Act
            var result = _validationHelper.ValidatePositiveNumberPublic(value, fieldName, allowZero);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void ValidatePositiveNumber_WithZeroAndAllowZero_ReturnsSuccess()
        {
            // Arrange
            var value = 0m;
            var fieldName = "TestNumber";
            var allowZero = true;

            // Act
            var result = _validationHelper.ValidatePositiveNumberPublic(value, fieldName, allowZero);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void ValidatePositiveNumber_WithZeroAndNotAllowZero_ReturnsFailure()
        {
            // Arrange
            var value = 0m;
            var fieldName = "TestNumber";
            var allowZero = false;

            // Act
            var result = _validationHelper.ValidatePositiveNumberPublic(value, fieldName, allowZero);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("必须大于0", result.ErrorMessage);
        }

        [Theory]
        [InlineData(-1, true)]
        [InlineData(-10, false)]
        public void ValidatePositiveNumber_WithNegativeValue_ReturnsFailure(decimal value, bool allowZero)
        {
            // Arrange
            var fieldName = "TestNumber";

            // Act
            var result = _validationHelper.ValidatePositiveNumberPublic(value, fieldName, allowZero);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("不能为负数", result.ErrorMessage);
        }

        #endregion

        #region ValidatePhoneNumber Tests

        [Theory]
        [InlineData("13800138000")]
        [InlineData("15901234567")]
        [InlineData("18612345678")]
        public void ValidatePhoneNumber_WithValidChinesePhoneNumber_ReturnsSuccess(string phoneNumber)
        {
            // Arrange
            var fieldName = "手机号码";

            // Act
            var result = _validationHelper.ValidatePhoneNumberPublic(phoneNumber, fieldName);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("1234567890")]    // 不是1开头
        [InlineData("120012345678")]  // 超过11位
        [InlineData("1380013800")]    // 少于11位
        [InlineData("1AB0013800C")]   // 包含字母
        public void ValidatePhoneNumber_WithInvalidPhoneNumber_ReturnsFailure(string phoneNumber)
        {
            // Arrange
            var fieldName = "手机号码";

            // Act
            var result = _validationHelper.ValidatePhoneNumberPublic(phoneNumber, fieldName);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("格式不正确", result.ErrorMessage);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void ValidatePhoneNumber_WithEmptyValue_ReturnsSuccess(string phoneNumber)
        {
            // Arrange
            var fieldName = "手机号码";

            // Act
            var result = _validationHelper.ValidatePhoneNumberPublic(phoneNumber, fieldName);

            // Assert
            Assert.True(result.IsSuccess); // 电话号码是可选的
        }

        #endregion

        #region ValidateEmail Tests

        [Theory]
        [InlineData("test@example.com")]
        [InlineData("user.name@domain.co.uk")]
        [InlineData("admin@test123.org")]
        public void ValidateEmail_WithValidEmail_ReturnsSuccess(string email)
        {
            // Arrange
            var fieldName = "邮箱";

            // Act
            var result = _validationHelper.ValidateEmailPublic(email, fieldName);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("invalid-email")]
        [InlineData("@domain.com")]
        [InlineData("user@")]
        [InlineData("user@@domain.com")]
        public void ValidateEmail_WithInvalidEmail_ReturnsFailure(string email)
        {
            // Arrange
            var fieldName = "邮箱";

            // Act
            var result = _validationHelper.ValidateEmailPublic(email, fieldName);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("格式不正确", result.ErrorMessage);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void ValidateEmail_WithEmptyValue_ReturnsSuccess(string email)
        {
            // Arrange
            var fieldName = "邮箱";

            // Act
            var result = _validationHelper.ValidateEmailPublic(email, fieldName);

            // Assert
            Assert.True(result.IsSuccess); // 邮箱是可选的
        }

        #endregion

        #region ValidateIdCard Tests

        [Theory]
        [InlineData("11010519491231002X")]
        [InlineData("110105194912310021")]
        [InlineData("123456789012345")]  // 15位
        public void ValidateIdCard_WithValidIdCard_ReturnsSuccess(string idCard)
        {
            // Arrange
            var fieldName = "身份证号码";

            // Act
            var result = _validationHelper.ValidateIdCardPublic(idCard, fieldName);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("12345")]           // 太短
        [InlineData("1234567890123456789")] // 太长
        [InlineData("11010519491231002A")]  // 末位不是X或数字
        public void ValidateIdCard_WithInvalidIdCard_ReturnsFailure(string idCard)
        {
            // Arrange
            var fieldName = "身份证号码";

            // Act
            var result = _validationHelper.ValidateIdCardPublic(idCard, fieldName);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("格式不正确", result.ErrorMessage);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void ValidateIdCard_WithEmptyValue_ReturnsSuccess(string idCard)
        {
            // Arrange
            var fieldName = "身份证号码";

            // Act
            var result = _validationHelper.ValidateIdCardPublic(idCard, fieldName);

            // Assert
            Assert.True(result.IsSuccess); // 身份证是可选的
        }

        #endregion
    }

    /// <summary>
    /// 测试用的BaseValidationHelper实现类
    /// 暴露protected方法供测试使用
    /// </summary>
    public class TestableValidationHelper : BaseValidationHelper
    {
        public ServiceResult<bool> ValidateRequiredStringPublic(string value, string fieldName)
            => ValidateRequiredString(value, fieldName);

        public ServiceResult<bool> ValidateStringLengthPublic(string value, string fieldName, int maxLength, int minLength = 0)
            => ValidateStringLength(value, fieldName, maxLength, minLength);

        public ServiceResult<bool> ValidateGuidPublic(Guid id, string fieldName)
            => ValidateGuid(id, fieldName);

        public ServiceResult<bool> ValidateNumericRangePublic(decimal value, string fieldName, decimal min, decimal max)
            => ValidateNumericRange(value, fieldName, min, max);

        public ServiceResult<bool> ValidatePositiveNumberPublic(decimal value, string fieldName, bool allowZero = false)
            => ValidatePositiveNumber(value, fieldName, allowZero);

        public ServiceResult<bool> ValidatePhoneNumberPublic(string phone, string fieldName)
            => ValidatePhoneNumber(phone, fieldName);

        public ServiceResult<bool> ValidateEmailPublic(string email, string fieldName)
            => ValidateEmail(email, fieldName);

        public ServiceResult<bool> ValidateIdCardPublic(string idCard, string fieldName)
            => ValidateIdCard(idCard, fieldName);
    }
}