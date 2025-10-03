using FluentAssertions;
using LYBT.Module.Consultation.Validators;
using LYBT.Shared.Models.Contracts.Consultation;
using Xunit;

namespace LYBT.Module.Consultation.Tests.Validators
{
    /// <summary>
    /// ConsultationCreateDtoValidator 单元测试
    /// </summary>
    public class ConsultationCreateDtoValidatorTests
    {
        private readonly ConsultationCreateDtoValidator _validator;

        public ConsultationCreateDtoValidatorTests()
        {
            _validator = new ConsultationCreateDtoValidator();
        }

        #region PatientId Validation Tests

        [Fact]
        public void Validate_WithEmptyPatientId_ShouldFail()
        {
            // Arrange
            var dto = new ConsultationCreateDto
            {
                PatientId = Guid.Empty,
                UserId = Guid.NewGuid(),
                ChiefComplaint = "主诉"
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.PatientId));
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains("患者ID不能为空"));
        }

        [Fact]
        public void Validate_WithValidPatientId_ShouldPass()
        {
            // Arrange
            var dto = new ConsultationCreateDto
            {
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                ChiefComplaint = "主诉"
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        #endregion

        #region UserId Validation Tests

        [Fact]
        public void Validate_WithEmptyUserId_ShouldFail()
        {
            // Arrange
            var dto = new ConsultationCreateDto
            {
                PatientId = Guid.NewGuid(),
                UserId = Guid.Empty,
                ChiefComplaint = "主诉"
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.UserId));
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains("医生ID不能为空"));
        }

        [Fact]
        public void Validate_WithValidUserId_ShouldPass()
        {
            // Arrange
            var dto = new ConsultationCreateDto
            {
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                ChiefComplaint = "主诉"
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        #endregion

        #region ChiefComplaint Validation Tests

        [Fact]
        public void Validate_WithNullChiefComplaint_ShouldPass()
        {
            // Arrange
            var dto = new ConsultationCreateDto
            {
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                ChiefComplaint = null
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WithEmptyChiefComplaint_ShouldPass()
        {
            // Arrange
            var dto = new ConsultationCreateDto
            {
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                ChiefComplaint = string.Empty
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WithChiefComplaintExceeding500Characters_ShouldFail()
        {
            // Arrange
            var dto = new ConsultationCreateDto
            {
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                ChiefComplaint = new string('测', 501) // 501个字符
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ChiefComplaint));
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains("主诉长度不能超过500个字符"));
        }

        [Fact]
        public void Validate_WithChiefComplaintAt500Characters_ShouldPass()
        {
            // Arrange
            var dto = new ConsultationCreateDto
            {
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                ChiefComplaint = new string('测', 500) // 正好500个字符
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        #endregion

        #region Diagnosis Validation Tests

        [Fact]
        public void Validate_WithNullDiagnosis_ShouldPass()
        {
            // Arrange
            var dto = new ConsultationCreateDto
            {
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Diagnosis = null
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WithDiagnosisExceeding1000Characters_ShouldFail()
        {
            // Arrange
            var dto = new ConsultationCreateDto
            {
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Diagnosis = new string('诊', 1001) // 1001个字符
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Diagnosis));
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains("诊断长度不能超过1000个字符"));
        }

        [Fact]
        public void Validate_WithDiagnosisAt1000Characters_ShouldPass()
        {
            // Arrange
            var dto = new ConsultationCreateDto
            {
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Diagnosis = new string('诊', 1000) // 正好1000个字符
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        #endregion

        #region Full DTO Validation Tests

        [Fact]
        public void Validate_WithAllValidFields_ShouldPass()
        {
            // Arrange
            var dto = new ConsultationCreateDto
            {
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                ChiefComplaint = "头痛发热",
                Diagnosis = "风寒感冒",
                StartTime = DateTime.Now
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void Validate_WithMultipleErrors_ShouldReturnAllErrors()
        {
            // Arrange
            var dto = new ConsultationCreateDto
            {
                PatientId = Guid.Empty, // 错误1
                UserId = Guid.Empty,    // 错误2
                ChiefComplaint = new string('测', 501), // 错误3
                Diagnosis = new string('诊', 1001)      // 错误4
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(4);
        }

        #endregion
    }
}
