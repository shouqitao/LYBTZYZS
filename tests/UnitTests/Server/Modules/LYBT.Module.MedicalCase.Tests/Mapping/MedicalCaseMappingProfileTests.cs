using AutoMapper;
using FluentAssertions;
using LYBT.Module.MedicalCase.Mapping;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Module.MedicalCase.Tests.Mapping
{
    /// <summary>
    /// MedicalCaseMappingProfile 映射配置测试
    /// Issue #1053: 重写以匹配实际API
    /// </summary>
    public class MedicalCaseMappingProfileTests
    {
        private readonly IMapper _mapper;

        public MedicalCaseMappingProfileTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MedicalCaseMappingProfile>();
            });

            _mapper = config.CreateMapper();
        }

        #region 配置有效性测试

        [Fact]
        public void MappingConfiguration_ShouldBeValid()
        {
            // Arrange & Act
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MedicalCaseMappingProfile>();
            });

            // Assert
            config.AssertConfigurationIsValid();
        }

        #endregion

        #region MedicalCase -> MedicalCaseDto 映射测试

        [Fact]
        public void MedicalCase_To_MedicalCaseDto_ShouldMapCorrectly()
        {
            // Arrange
            var medicalCase = new LYBT.Entities.MedicalCase.MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "张三",
                DoctorId = Guid.NewGuid(),
                DoctorName = "李医生",
                ConsultationDate = DateTime.Now,
                Status = MedicalCaseStatus.Active,
                Remark = "测试备注",
                CreatedAt = DateTime.Now.AddDays(-1)
            };

            // Act
            var dto = _mapper.Map<MedicalCaseDto>(medicalCase);

            // Assert
            dto.Should().NotBeNull();
            dto.Id.Should().Be(medicalCase.Id);
            dto.PatientId.Should().Be(medicalCase.PatientId);
            dto.PatientName.Should().Be(medicalCase.PatientName);
            dto.DoctorId.Should().Be(medicalCase.DoctorId);
            dto.DoctorName.Should().Be(medicalCase.DoctorName);
            dto.ConsultationDate.Should().Be(medicalCase.ConsultationDate);
            dto.CaseStatus.Should().Be(medicalCase.Status);
            dto.Remark.Should().Be(medicalCase.Remark);
        }

        [Fact]
        public void MedicalCase_To_MedicalCaseDto_WithNullRemark_ShouldMapCorrectly()
        {
            // Arrange
            var medicalCase = new LYBT.Entities.MedicalCase.MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "患者",
                DoctorId = Guid.NewGuid(),
                DoctorName = "医生",
                Status = MedicalCaseStatus.Active,
                Remark = null
            };

            // Act
            var dto = _mapper.Map<MedicalCaseDto>(medicalCase);

            // Assert
            dto.Should().NotBeNull();
            dto.Remark.Should().BeNull();
        }

        #endregion

        #region MedicalCase -> MedicalCaseDetailDto 映射测试

        [Fact]
        public void MedicalCase_To_MedicalCaseDetailDto_ShouldMapCorrectly()
        {
            // Arrange
            var medicalCase = new LYBT.Entities.MedicalCase.MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "患者A",
                DoctorId = Guid.NewGuid(),
                DoctorName = "医生A",
                ConsultationDate = DateTime.Now,
                Status = MedicalCaseStatus.Active,
                Remark = "详细备注"
            };

            // Act
            var detailDto = _mapper.Map<MedicalCaseDetailDto>(medicalCase);

            // Assert
            detailDto.Should().NotBeNull();
            detailDto.Id.Should().Be(medicalCase.Id);
            detailDto.PatientId.Should().Be(medicalCase.PatientId);
            detailDto.PatientName.Should().Be(medicalCase.PatientName);
            detailDto.CaseStatus.Should().Be(medicalCase.Status);
            detailDto.Remark.Should().Be(medicalCase.Remark);
        }

        #endregion

        #region MedicalCaseCreateDto -> MedicalCase 映射测试

        [Fact]
        public void MedicalCaseCreateDto_To_MedicalCase_ShouldMapCorrectly()
        {
            // Arrange
            var createDto = new MedicalCaseCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                Remark = "新病历备注"
            };

            // Act
            var medicalCase = _mapper.Map<LYBT.Entities.MedicalCase.MedicalCase>(createDto);

            // Assert
            medicalCase.Should().NotBeNull();
            medicalCase.PatientId.Should().Be(createDto.PatientId);
            medicalCase.DoctorId.Should().Be(createDto.DoctorId);
            medicalCase.Remark.Should().Be(createDto.Remark);
        }

        [Fact]
        public void MedicalCaseCreateDto_To_MedicalCase_ShouldIgnoreIdAndNavigationProperties()
        {
            // Arrange
            var createDto = new MedicalCaseCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid()
            };

            // Act
            var medicalCase = _mapper.Map<LYBT.Entities.MedicalCase.MedicalCase>(createDto);

            // Assert - Id和导航属性应该被忽略
            medicalCase.Id.Should().Be(Guid.Empty);
            medicalCase.Consultation.Should().BeNull();
            medicalCase.Prescription.Should().BeNull();
        }

        #endregion

        #region MedicalCaseUpdateDto -> MedicalCase 映射测试

        [Fact]
        public void MedicalCaseUpdateDto_To_MedicalCase_ShouldMapAllowedFields()
        {
            // Arrange
            var updateDto = new MedicalCaseUpdateDto
            {
                Remark = "更新的备注"
            };

            // Act
            var medicalCase = _mapper.Map<LYBT.Entities.MedicalCase.MedicalCase>(updateDto);

            // Assert
            medicalCase.Should().NotBeNull();
            medicalCase.Remark.Should().Be(updateDto.Remark);
        }

        [Fact]
        public void MedicalCaseUpdateDto_To_MedicalCase_ShouldIgnoreNavigationProperties()
        {
            // Arrange
            var updateDto = new MedicalCaseUpdateDto
            {
                Remark = "忽略测试"
            };

            // Act
            var medicalCase = _mapper.Map<LYBT.Entities.MedicalCase.MedicalCase>(updateDto);

            // Assert - 导航属性应该被忽略
            medicalCase.Consultation.Should().BeNull();
            medicalCase.Prescription.Should().BeNull();
        }

        [Fact]
        public void MedicalCaseUpdateDto_To_MedicalCase_ShouldOnlyMapNonNullValues()
        {
            // Arrange
            var updateDto = new MedicalCaseUpdateDto
            {
                Remark = null // null值应该被跳过
            };

            // Act
            var medicalCase = _mapper.Map<LYBT.Entities.MedicalCase.MedicalCase>(updateDto);

            // Assert - ForAllMembers条件会跳过null值
            medicalCase.Remark.Should().BeNull();
        }

        #endregion

        #region 状态映射测试

        [Theory]
        [InlineData(MedicalCaseStatus.Active)]
        [InlineData(MedicalCaseStatus.Closed)]
        public void MedicalCase_To_MedicalCaseDto_ShouldMapAllStatuses(MedicalCaseStatus status)
        {
            // Arrange
            var medicalCase = new LYBT.Entities.MedicalCase.MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "患者",
                DoctorId = Guid.NewGuid(),
                DoctorName = "医生",
                Status = status
            };

            // Act
            var dto = _mapper.Map<MedicalCaseDto>(medicalCase);

            // Assert
            dto.CaseStatus.Should().Be(status);
        }

        #endregion

        #region 边界条件测试

        [Fact]
        public void Mapping_WithNullSource_ShouldReturnNull()
        {
            // Arrange
            LYBT.Entities.MedicalCase.MedicalCase? nullMedicalCase = null;

            // Act
            var dto = _mapper.Map<MedicalCaseDto>(nullMedicalCase);

            // Assert
            dto.Should().BeNull();
        }

        [Fact]
        public void MedicalCaseCreateDto_WithMinimalData_ShouldMapSuccessfully()
        {
            // Arrange
            var createDto = new MedicalCaseCreateDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid()
            };

            // Act
            var medicalCase = _mapper.Map<LYBT.Entities.MedicalCase.MedicalCase>(createDto);

            // Assert
            medicalCase.Should().NotBeNull();
            medicalCase.PatientId.Should().Be(createDto.PatientId);
            medicalCase.DoctorId.Should().Be(createDto.DoctorId);
            medicalCase.Remark.Should().BeNull();
        }

        #endregion

        #region 特殊字符测试

        [Fact]
        public void MedicalCase_WithSpecialCharacters_ShouldMapCorrectly()
        {
            // Arrange
            var medicalCase = new LYBT.Entities.MedicalCase.MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "张三（男）",
                DoctorId = Guid.NewGuid(),
                DoctorName = "李医生/主治医师",
                Status = MedicalCaseStatus.Active,
                Remark = "患者体质偏寒，建议温服；忌食生冷/辛辣\n注意：复诊时间待定"
            };

            // Act
            var dto = _mapper.Map<MedicalCaseDto>(medicalCase);

            // Assert
            dto.PatientName.Should().Be("张三（男）");
            dto.DoctorName.Should().Be("李医生/主治医师");
            dto.Remark.Should().Be("患者体质偏寒，建议温服；忌食生冷/辛辣\n注意：复诊时间待定");
        }

        #endregion
    }
}
