using AutoMapper;
using FluentAssertions;
using LYBT.Entities.Patients;
using LYBT.Module.Patients.Mapping;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Module.Patients.Tests.Mapping
{
    /// <summary>
    /// Patients模块AutoMapper映射配置单元测试
    /// 测试所有映射配置的有效性和正确�?
    /// </summary>
    public class PatientMappingProfileTests
    {
        private readonly IMapper _mapper;

        public PatientMappingProfileTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new PatientMappingProfile());
            });

            _mapper = config.CreateMapper();
        }

        [Fact]
        public void MappingConfiguration_Should_BeValid()
        {
            // Arrange & Act
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new PatientMappingProfile());
            });

            // Assert
            config.AssertConfigurationIsValid();
        }

        [Fact]
        public void Map_Patient_To_PatientDto_Should_Success()
        {
            // Arrange
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                Name = "张三",
                Gender = Gender.Male,
                // Age������ֻ����������
                PhoneNumber = "13812345678",
                IdNumber = "110101198801011234",
                Address = "北京市朝阳区",
                PinYinCode = "ZS",
                LastVisitTime = DateTime.Now,
                VisitCount = 3,
                DisableReason = null
            };

            // Act
            var patientDto = _mapper.Map<PatientDto>(patient);

            // Assert
            patientDto.Should().NotBeNull();
            patientDto.Id.Should().Be(patient.Id);
            patientDto.Name.Should().Be(patient.Name);
            patientDto.Gender.Should().Be(patient.Gender);
            // Age: PatientDto.Age 是 int（非空），Patient.Age 是 int?（可空）
            patientDto.Age.Should().Be(patient.Age ?? 0);
            patientDto.PhoneNumber.Should().Be(patient.PhoneNumber);
            patientDto.IdNumber.Should().Be(patient.IdNumber);
            patientDto.Address.Should().Be(patient.Address);
            patientDto.PinYinCode.Should().Be(patient.PinYinCode);
        }

        [Fact]
        public void Map_PatientCreateDto_To_Patient_Should_Success()
        {
            // Arrange
            var createDto = new PatientCreateDto
            {
                Name = "李四",
                Gender = Gender.Female,
                // Age������ֻ����������
                PhoneNumber = "13987654321",
                IdNumber = "110101199501011234",
                Address = "上海市浦东新区",
                // Occupation和MedicalHistory不存在于实体中
            };

            // Act
            var patient = _mapper.Map<Patient>(createDto);

            // Assert
            patient.Should().NotBeNull();
            patient.Name.Should().Be(createDto.Name);
            patient.Gender.Should().Be(createDto.Gender);
            // Age: Patient.Age 是 int?（计算属性），CreateDto.Age 是 int（计算属性）
            // 由于 BirthDate 为 null，Patient.Age 返回 null
            patient.Age.Should().BeNull();
            patient.PhoneNumber.Should().Be(createDto.PhoneNumber);
            patient.IdNumber.Should().Be(createDto.IdNumber);
            patient.Address.Should().Be(createDto.Address);
            // Occupation和MedicalHistory不存在于实体中

            // 验证忽略字段（Id 由 BaseEntity 自动生成新 Guid）
            patient.Id.Should().NotBe(Guid.Empty); // BaseEntity 默认生成新 Guid
            patient.LastVisitTime.Should().BeNull();
            patient.VisitCount.Should().Be(0);
            patient.DisableReason.Should().BeNull();
        }

        [Fact]
        public void Map_PatientUpdateDto_To_Patient_Should_Success()
        {
            // Arrange
            var updateDto = new PatientUpdateDto
            {
                Id = Guid.NewGuid(),
                Name = "王五",
                Gender = Gender.Male,
                // Age������ֻ����������
                PhoneNumber = "13911111111",
                Address = "广州市天河区",
                // Occupation和MedicalHistory不存在于UpdateDto中
            };

            // Act
            var patient = _mapper.Map<Patient>(updateDto);

            // Assert
            patient.Should().NotBeNull();
            patient.Name.Should().Be(updateDto.Name);
            patient.Gender.Should().Be(updateDto.Gender);
            // Age: Patient.Age 是 int?（计算属性），UpdateDto.Age 是 int（计算属性）
            // 由于 BirthDate 为 null，Patient.Age 返回 null
            patient.Age.Should().BeNull();
            patient.PhoneNumber.Should().Be(updateDto.PhoneNumber);
            patient.Address.Should().Be(updateDto.Address);
            // Occupation和MedicalHistory不存在于实体中

            // 验证忽略字段
            patient.LastVisitTime.Should().BeNull();
            patient.VisitCount.Should().Be(0);
            patient.DisableReason.Should().BeNull();
        }

        [Fact]
        public void Map_PatientDto_To_Patient_Should_Success()
        {
            // Arrange
            var patientDto = new PatientDto
            {
                Id = Guid.NewGuid(),
                Name = "赵六",
                Gender = Gender.Female,
                // Age������ֻ����������
                PhoneNumber = "13822222222",
                IdNumber = "110101199001011234",
                Address = "深圳市南山区",
                PinYinCode = "ZL"
            };

            // Act
            var patient = _mapper.Map<Patient>(patientDto);

            // Assert
            patient.Should().NotBeNull();
            patient.Name.Should().Be(patientDto.Name);
            patient.Gender.Should().Be(patientDto.Gender);
            // Age: Patient.Age 是 int?（计算属性），PatientDto.Age 是 int（计算属性）
            // 由于 BirthDate 为 null，Patient.Age 返回 null
            patient.Age.Should().BeNull();
            patient.PhoneNumber.Should().Be(patientDto.PhoneNumber);
            patient.IdNumber.Should().Be(patientDto.IdNumber);
            patient.Address.Should().Be(patientDto.Address);
            patient.PinYinCode.Should().Be(patientDto.PinYinCode);

            // 验证忽略字段（Id 被忽略，但 BaseEntity 生成了新 Guid）
            patient.Id.Should().NotBe(Guid.Empty).And.NotBe(patientDto.Id); // 忽略源 Id，使用 BaseEntity 生成的新 Guid
            patient.LastVisitTime.Should().BeNull();
            patient.VisitCount.Should().Be(0);
            patient.DisableReason.Should().BeNull();
        }

        [Fact]
        public void Map_PatientCreateDto_To_PatientDto_Should_Success()
        {
            // Arrange
            var createDto = new PatientCreateDto
            {
                Name = "孙七",
                Gender = Gender.Male,
                // Age������ֻ����������
                PhoneNumber = "13933333333",
                IdNumber = "110101199401011234",
                Address = "杭州市西湖区"
            };

            // Act
            var patientDto = _mapper.Map<PatientDto>(createDto);

            // Assert
            patientDto.Should().NotBeNull();
            patientDto.Name.Should().Be(createDto.Name);
            patientDto.Gender.Should().Be(createDto.Gender);
            patientDto.Age.Should().Be(createDto.Age);
            patientDto.PhoneNumber.Should().Be(createDto.PhoneNumber);
            patientDto.IdNumber.Should().Be(createDto.IdNumber);
            patientDto.Address.Should().Be(createDto.Address);

            // 验证忽略字段
            patientDto.Id.Should().Be(Guid.Empty);
            patientDto.PinYinCode.Should().BeNull();
        }

        [Fact]
        public void Map_PatientUpdateDto_To_PatientDto_Should_Success()
        {
            // Arrange
            var updateDto = new PatientUpdateDto
            {
                Id = Guid.NewGuid(),
                Name = "周八",
                Gender = Gender.Female,
                // Age������ֻ����������
                PhoneNumber = "13944444444",
                Address = "成都市锦江区"
            };

            // Act
            var patientDto = _mapper.Map<PatientDto>(updateDto);

            // Assert
            patientDto.Should().NotBeNull();
            patientDto.Id.Should().Be(updateDto.Id);
            patientDto.Name.Should().Be(updateDto.Name);
            patientDto.Gender.Should().Be(updateDto.Gender);
            patientDto.Age.Should().Be(updateDto.Age);
            patientDto.PhoneNumber.Should().Be(updateDto.PhoneNumber);
            patientDto.Address.Should().Be(updateDto.Address);

            // 验证忽略字段
            patientDto.PinYinCode.Should().BeNull();
        }

        [Fact]
        public void Map_Patient_With_FemaleGender_Should_Success()
        {
            // Arrange
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                Name = "女患者",
                Gender = Gender.Female,
                // Age������ֻ����������
            };

            // Act
            var patientDto = _mapper.Map<PatientDto>(patient);

            // Assert
            patientDto.Should().NotBeNull();
            patientDto.Gender.Should().Be(Gender.Female);
            patientDto.Name.Should().Be("女患者");
        }

        [Fact]
        public void Map_Patient_With_NullOptionalFields_Should_Success()
        {
            // Arrange
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                Name = "最小患者",
                Gender = Gender.Unknown,
                // Age������ֻ����������
                PhoneNumber = null,
                IdNumber = null,
                Address = null,
                PinYinCode = null,
                LastVisitTime = null,
                VisitCount = 0,
                DisableReason = null
            };

            // Act
            var patientDto = _mapper.Map<PatientDto>(patient);

            // Assert
            patientDto.Should().NotBeNull();
            patientDto.Name.Should().Be("最小患者");
            patientDto.Gender.Should().Be(Gender.Unknown);
            patientDto.Age.Should().Be(0);
            patientDto.PhoneNumber.Should().BeNull();
            patientDto.IdNumber.Should().BeNull();
            patientDto.Address.Should().BeNull();
            patientDto.PinYinCode.Should().BeNull();
        }

        [Fact]
        public void Map_Patient_With_SpecialCharacters_Should_Success()
        {
            // Arrange
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                Name = "欧阳·复姓",
                PinYinCode = "OYFX",
                IdNumber = "110101198801011234",
                Address = "北京市朝阳区建国门23号",
                PhoneNumber = "+86-138-1234-5678"
            };

            // Act
            var patientDto = _mapper.Map<PatientDto>(patient);

            // Assert
            patientDto.Should().NotBeNull();
            patientDto.Name.Should().Be("欧阳·复姓");
            patientDto.PinYinCode.Should().Be("OYFX");
            patientDto.IdNumber.Should().Be("110101198801011234");
            patientDto.Address.Should().Be("北京市朝阳区建国门23号");
            patientDto.PhoneNumber.Should().Be("+86-138-1234-5678");
        }
    }
}
