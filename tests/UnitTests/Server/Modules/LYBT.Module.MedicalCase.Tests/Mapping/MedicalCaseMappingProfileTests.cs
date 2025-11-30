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
            var medicalCase = new LYBT.Entities.MedicalCases.MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "张三",
                DoctorId = Guid.NewGuid(),
                DoctorName = "李医生",
                ConsultationDate = DateTime.Now,
                CaseStatus = MedicalCaseStatus.Active,
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
            dto.CaseStatus.Should().Be(medicalCase.CaseStatus);
            dto.Remark.Should().Be(medicalCase.Remark);
        }

        [Fact]
        public void MedicalCase_To_MedicalCaseDto_WithNullRemark_ShouldMapCorrectly()
        {
            // Arrange
            var medicalCase = new LYBT.Entities.MedicalCases.MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "患者",
                DoctorId = Guid.NewGuid(),
                DoctorName = "医生",
                CaseStatus = MedicalCaseStatus.Active,
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
            var medicalCase = new LYBT.Entities.MedicalCases.MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "患者A",
                DoctorId = Guid.NewGuid(),
                DoctorName = "医生A",
                ConsultationDate = DateTime.Now,
                CaseStatus = MedicalCaseStatus.Active,
                Remark = "详细备注"
            };

            // Act
            var detailDto = _mapper.Map<MedicalCaseDetailDto>(medicalCase);

            // Assert
            detailDto.Should().NotBeNull();
            detailDto.Id.Should().Be(medicalCase.Id);
            detailDto.PatientId.Should().Be(medicalCase.PatientId);
            detailDto.PatientName.Should().Be(medicalCase.PatientName);
            detailDto.CaseStatus.Should().Be(medicalCase.CaseStatus);
            detailDto.Remark.Should().Be(medicalCase.Remark);
        }

        #endregion

        #region MedicalCaseInputDto -> MedicalCase 映射测试（Epic #1961）

        [Fact]
        public void MedicalCaseInputDto_To_MedicalCase_ShouldMapCorrectly()
        {
            // Arrange
            var inputDto = new MedicalCaseInputDto
            {
                Id = null, // 创建场景：Id为null
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                VisitDate = DateTime.Now,
                ChiefComplaint = "主诉内容",
                Remark = "备注信息"
            };

            // Act
            var medicalCase = _mapper.Map<LYBT.Entities.MedicalCases.MedicalCase>(inputDto);

            // Assert
            medicalCase.Should().NotBeNull();
            medicalCase.PatientId.Should().Be(inputDto.PatientId);
            medicalCase.DoctorId.Should().Be(inputDto.DoctorId);
            medicalCase.ConsultationDate.Should().Be(inputDto.VisitDate);
            medicalCase.Remark.Should().Be(inputDto.Remark);
        }

        [Fact]
        public void MedicalCaseInputDto_To_MedicalCase_ShouldIgnoreIdAndNavigationProperties()
        {
            // Arrange
            var inputDto = new MedicalCaseInputDto
            {
                Id = Guid.NewGuid(), // 更新场景：提供Id但应被忽略
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                VisitDate = DateTime.Now
            };

            // Act
            var medicalCase = _mapper.Map<LYBT.Entities.MedicalCases.MedicalCase>(inputDto);

            // Assert - Id和导航属性应该被忽略
            // 注意：AutoMapper Ignore 不会将 Id 设为 Empty，而是保持实体的默认值
            // Service 层会在实际使用时生成新的 ID
            medicalCase.Id.Should().NotBe(inputDto.Id ?? Guid.Empty); // 确认未从 DTO 映射
            medicalCase.Consultation.Should().BeNull();
            medicalCase.Prescription.Should().BeNull();
        }

        [Fact]
        public void MedicalCaseInputDto_WithMinimalData_ShouldMapSuccessfully()
        {
            // Arrange - 只提供必填字段
            var inputDto = new MedicalCaseInputDto
            {
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                VisitDate = DateTime.Now
            };

            // Act
            var medicalCase = _mapper.Map<LYBT.Entities.MedicalCases.MedicalCase>(inputDto);

            // Assert - 可选字段为null
            medicalCase.Should().NotBeNull();
            medicalCase.Remark.Should().BeNull();
        }

        #endregion

        #region 状态映射测试

        [Theory]
        [InlineData(MedicalCaseStatus.Active)]
        [InlineData(MedicalCaseStatus.Completed)]
        // Issue #2242: Cancelled状态已废弃，移除测试用例
        public void MedicalCase_To_MedicalCaseDto_ShouldMapAllStatuses(MedicalCaseStatus status)
        {
            // Arrange
            var medicalCase = new LYBT.Entities.MedicalCases.MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "患者",
                DoctorId = Guid.NewGuid(),
                DoctorName = "医生",
                CaseStatus = status
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
            LYBT.Entities.MedicalCases.MedicalCase? nullMedicalCase = null;

            // Act
            var dto = _mapper.Map<MedicalCaseDto>(nullMedicalCase);

            // Assert
            dto.Should().BeNull();
        }

        #endregion

        #region 特殊字符测试

        [Fact]
        public void MedicalCase_WithSpecialCharacters_ShouldMapCorrectly()
        {
            // Arrange
            var medicalCase = new LYBT.Entities.MedicalCases.MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "张三（男）",
                DoctorId = Guid.NewGuid(),
                DoctorName = "李医生/主治医师",
                CaseStatus = MedicalCaseStatus.Active,
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
