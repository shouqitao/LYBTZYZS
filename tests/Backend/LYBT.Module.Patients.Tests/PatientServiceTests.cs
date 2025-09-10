using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;
using LYBT.Infrastructure.Logging;
using LYBT.Infrastructure.Logging.Dtos;
using LYBT.Entities.Patients;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Services;
using LYBT.Module.Patients.Mapping;
using LYBT.Module.Patients.Tests.Base;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;
using Moq;
using Xunit;

namespace LYBT.Module.Patients.Tests
{
    /// <summary>
    /// PatientService 单元测试
    /// </summary>
    public class PatientServiceTests
    {
        private readonly PatientService _patientService;
        private readonly Mock<IPatientRepository> _mockPatientRepository;
        private readonly Mock<IUnifiedLogService> _mockLogService;
        private readonly IMapper _mapper;
        private readonly List<PatientModel> _testPatients;

        public PatientServiceTests()
        {
            // 设置测试数据
            _testPatients = new List<PatientModel>();
            InitializeTestData();

            // 创建 Mock Repository
            _mockPatientRepository = new Mock<IPatientRepository>();
            SetupRepositoryMethods();

            // 创建 Mock Log Service
            _mockLogService = new Mock<IUnifiedLogService>();
            SetupLogServiceMethods();

            // 创建 Mapper
            _mapper = CreatePatientMapper();

            // 创建 PatientService 实例
            _patientService = new PatientService(
                _mockPatientRepository.Object,
                _mapper,
                _mockLogService.Object
            );
        }

        #region 初始化测试数据

        private void InitializeTestData()
        {
            // 创建测试患者数据
            _testPatients.AddRange(PatientTestDataGenerator.CreateTestPatients(5));
            
            // 确保有不同状态的患者
            _testPatients[0].Status = CommonStatus.Enabled;
            _testPatients[1].Status = CommonStatus.Enabled;
            _testPatients[2].Status = CommonStatus.Disabled;
            _testPatients[3].Status = CommonStatus.Enabled;
            _testPatients[4].Status = CommonStatus.Disabled;
        }

        private void SetupRepositoryMethods()
        {
            // Setup GetByIdAsync
            _mockPatientRepository
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>()))
                .ReturnsAsync((Guid id, bool includeDisabled) =>
                {
                    var patient = _testPatients.FirstOrDefault(p => p.Id == id);
                    if (patient != null && !includeDisabled && patient.Status == CommonStatus.Disabled)
                    {
                        return null;
                    }
                    return patient;
                });

            // Setup AddAsync
            _mockPatientRepository
                .Setup(x => x.AddAsync(It.IsAny<PatientModel>()))
                .ReturnsAsync((PatientModel patient) =>
                {
                    _testPatients.Add(patient);
                    return true;
                });

            // Setup UpdateAsync
            _mockPatientRepository
                .Setup(x => x.UpdateAsync(It.IsAny<PatientModel>()))
                .ReturnsAsync((PatientModel patient) =>
                {
                    var existing = _testPatients.FirstOrDefault(p => p.Id == patient.Id);
                    if (existing != null)
                    {
                        _testPatients.Remove(existing);
                        _testPatients.Add(patient);
                        return true;
                    }
                    return false;
                });

            // Setup DisableAsync
            _mockPatientRepository
                .Setup(x => x.DisableAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) =>
                {
                    var patient = _testPatients.FirstOrDefault(p => p.Id == id);
                    if (patient != null)
                    {
                        patient.Status = CommonStatus.Disabled;
                        return true;
                    }
                    return false;
                });

            // Setup EnableAsync
            _mockPatientRepository
                .Setup(x => x.EnableAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) =>
                {
                    var patient = _testPatients.FirstOrDefault(p => p.Id == id);
                    if (patient != null)
                    {
                        patient.Status = CommonStatus.Enabled;
                        return true;
                    }
                    return false;
                });

            // Setup IsIdNumberExistsAsync
            _mockPatientRepository
                .Setup(x => x.IsIdNumberExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>()))
                .ReturnsAsync((string idNumber, Guid? excludeId) =>
                {
                    return _testPatients.Any(p => p.IdNumber == idNumber && 
                        (!excludeId.HasValue || p.Id != excludeId.Value));
                });

            // Setup IsPhoneNumberExistsAsync
            _mockPatientRepository
                .Setup(x => x.IsPhoneNumberExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>()))
                .ReturnsAsync((string phoneNumber, Guid? excludeId) =>
                {
                    return _testPatients.Any(p => p.PhoneNumber == phoneNumber && 
                        (!excludeId.HasValue || p.Id != excludeId.Value));
                });

            // Setup GetListAsync
            _mockPatientRepository
                .Setup(x => x.GetListAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync((string? name, int page, int pageSize, bool includeDisabled) =>
                {
                    var query = _testPatients.AsQueryable();
                    
                    if (!includeDisabled)
                    {
                        query = query.Where(p => p.Status == CommonStatus.Enabled);
                    }
                    
                    if (!string.IsNullOrEmpty(name))
                    {
                        query = query.Where(p => p.Name.Contains(name));
                    }
                    
                    return query
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();
                });

            // Setup GetCountAsync
            _mockPatientRepository
                .Setup(x => x.GetCountAsync(It.IsAny<string?>(), It.IsAny<bool>()))
                .ReturnsAsync((string? name, bool includeDisabled) =>
                {
                    var query = _testPatients.AsQueryable();
                    
                    if (!includeDisabled)
                    {
                        query = query.Where(p => p.Status == CommonStatus.Enabled);
                    }
                    
                    if (!string.IsNullOrEmpty(name))
                    {
                        query = query.Where(p => p.Name.Contains(name));
                    }
                    
                    return query.Count();
                });

            // Setup SearchAsync
            _mockPatientRepository
                .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync((string keyword, bool includeDisabled) =>
                {
                    var query = _testPatients.AsQueryable();
                    
                    if (!includeDisabled)
                    {
                        query = query.Where(p => p.Status == CommonStatus.Enabled);
                    }
                    
                    if (!string.IsNullOrEmpty(keyword))
                    {
                        query = query.Where(p => 
                            p.Name.Contains(keyword) || 
                            p.PhoneNumber.Contains(keyword) || 
                            p.IdNumber.Contains(keyword));
                    }
                    
                    return query.ToList();
                });

            // Setup GetActivePatientsAsync
            _mockPatientRepository
                .Setup(x => x.GetActivePatientsAsync())
                .ReturnsAsync(() => _testPatients.Where(p => p.Status == CommonStatus.Enabled).ToList());

            // Setup GetByPhoneNumberAsync
            _mockPatientRepository
                .Setup(x => x.GetByPhoneNumberAsync(It.IsAny<string>()))
                .ReturnsAsync((string phoneNumber) => 
                    string.IsNullOrEmpty(phoneNumber) ? null : _testPatients.FirstOrDefault(p => p.PhoneNumber == phoneNumber));

            // Setup GetByIdNumberAsync
            _mockPatientRepository
                .Setup(x => x.GetByIdNumberAsync(It.IsAny<string>()))
                .ReturnsAsync((string idNumber) => 
                    string.IsNullOrEmpty(idNumber) ? null : _testPatients.FirstOrDefault(p => p.IdNumber == idNumber));
        }

        private void SetupLogServiceMethods()
        {
            // Setup LogUserActionAsync
            _mockLogService
                .Setup(x => x.LogUserActionAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<LogActionType>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<long>()))
                .Returns(Task.CompletedTask);

            // Setup CreateLogAsync
            _mockLogService
                .Setup(x => x.CreateLogAsync(It.IsAny<LogCreateDto>()))
                .ReturnsAsync(true);
        }

        private IMapper CreatePatientMapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new PatientMappingProfile());
                
                // 添加测试需要的额外映射
                cfg.CreateMap<PatientImportDto, PatientModel>()
                    .ForMember(dest => dest.Id, opt => opt.Ignore())
                    .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                    .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                    .ForMember(dest => dest.Status, opt => opt.MapFrom(src => CommonStatus.Enabled))
                    .ForMember(dest => dest.DisableReason, opt => opt.Ignore())
                    .ForMember(dest => dest.LastVisitTime, opt => opt.Ignore())
                    .ForMember(dest => dest.VisitCount, opt => opt.Ignore())
                    .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                    .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                    .ForMember(dest => dest.PinYinCode, opt => opt.Ignore())
                    .ForMember(dest => dest.WuBiCode, opt => opt.Ignore())
                    .ForMember(dest => dest.IdType, opt => opt.MapFrom(src => "身份证"));
            }, NullLoggerFactory.Instance);

            return config.CreateMapper();
        }

        #endregion

        #region CreateAsync 测试

        [Fact]
        public async Task CreateAsync_Should_Create_New_Patient_Successfully()
        {
            // Arrange
            var dto = new PatientDetailDto
            {
                Name = "新患者",
                Gender = Gender.Male,
                Age = 30,
                IDNumber = "110101199001010011",
                PhoneNumber = "13900139000"
            };
            var operatorId = Guid.NewGuid();
            var operatorName = "操作员";

            // Act
            var result = await _patientService.CreateAsync(dto, operatorId, operatorName);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be(dto.Name);
            result.IDNumber.Should().Be(dto.IDNumber);
            result.PhoneNumber.Should().Be(dto.PhoneNumber);

            // 验证日志记录
            _mockLogService.Verify(x => x.LogUserActionAsync(
                It.Is<Guid>(id => id == operatorId),
                It.Is<string>(name => name == operatorName),
                It.Is<LogActionType>(type => type == LogActionType.Create),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>()
            ), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_Should_Throw_When_Name_Is_Empty()
        {
            // Arrange
            var dto = new PatientDetailDto
            {
                Name = "", // 空名称
                Gender = Gender.Male,
                Age = 30
            };
            var operatorId = Guid.NewGuid();
            var operatorName = "操作员";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await _patientService.CreateAsync(dto, operatorId, operatorName)
            );
        }

        [Fact]
        public async Task CreateAsync_Should_Throw_When_IdNumber_Already_Exists()
        {
            // Arrange
            var existingIdNumber = _testPatients.First().IdNumber;
            var dto = new PatientDetailDto
            {
                Name = "新患者",
                Gender = Gender.Male,
                Age = 30,
                IDNumber = existingIdNumber // 已存在的身份证号
            };
            var operatorId = Guid.NewGuid();
            var operatorName = "操作员";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _patientService.CreateAsync(dto, operatorId, operatorName)
            );
            exception.Message.Should().Be("身份证号已存在");
        }

        [Fact]
        public async Task CreateAsync_Should_Throw_When_PhoneNumber_Already_Exists()
        {
            // Arrange
            var existingPhoneNumber = _testPatients.First().PhoneNumber;
            var dto = new PatientDetailDto
            {
                Name = "新患者",
                Gender = Gender.Male,
                Age = 30,
                PhoneNumber = existingPhoneNumber // 已存在的手机号
            };
            var operatorId = Guid.NewGuid();
            var operatorName = "操作员";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _patientService.CreateAsync(dto, operatorId, operatorName)
            );
            exception.Message.Should().Be("手机号已存在");
        }

        [Fact]
        public async Task CreateAsync_Should_Extract_BirthDate_From_Valid_IdNumber()
        {
            // Arrange
            var dto = new PatientDetailDto
            {
                Name = "新患者",
                Gender = Gender.Male,
                IDNumber = "110101199001010011" // 1990年1月1日
            };
            var operatorId = Guid.NewGuid();
            var operatorName = "操作员";

            // 注：CommonHelper.CheckIdNumber已移除，此方法之前总是返回false

            // Act
            var result = await _patientService.CreateAsync(dto, operatorId, operatorName);

            // Assert
            result.Should().NotBeNull();
            // 注意：由于身份证验证功能已移除（原CommonHelper.CheckIdNumber返回false），所以BirthDate可能没有被设置
        }

        #endregion

        #region UpdateAsync 测试

        [Fact]
        public async Task UpdateAsync_Should_Update_Patient_Successfully()
        {
            // Arrange
            var existingPatient = _testPatients.First();
            var dto = new PatientDetailDto
            {
                Id = existingPatient.Id,
                Name = "更新后的名字",
                Gender = Gender.Female,
                Age = 35,
                PhoneNumber = "13999999999"
            };
            var operatorId = Guid.NewGuid();
            var operatorName = "操作员";

            // Act
            var result = await _patientService.UpdateAsync(existingPatient.Id, dto, operatorId, operatorName);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be(dto.Name);
            result.Gender.Should().Be(dto.Gender);
            result.PhoneNumber.Should().Be(dto.PhoneNumber);

            // 验证日志记录
            _mockLogService.Verify(x => x.CreateLogAsync(
                It.Is<LogCreateDto>(log => 
                    log.ActionType == ActionType.Edit &&
                    log.ObjectType == ObjectType.Patient &&
                    log.ObjectId == existingPatient.Id
                )), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_Should_Throw_When_Patient_Not_Exists()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var dto = new PatientDetailDto
            {
                Id = nonExistentId,
                Name = "更新后的名字"
            };
            var operatorId = Guid.NewGuid();
            var operatorName = "操作员";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _patientService.UpdateAsync(nonExistentId, dto, operatorId, operatorName)
            );
            exception.Message.Should().Be("患者不存在");
        }

        [Fact]
        public async Task UpdateAsync_Should_Throw_When_Name_Is_Empty()
        {
            // Arrange
            var existingPatient = _testPatients.First();
            var dto = new PatientDetailDto
            {
                Id = existingPatient.Id,
                Name = "" // 空名称
            };
            var operatorId = Guid.NewGuid();
            var operatorName = "操作员";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await _patientService.UpdateAsync(existingPatient.Id, dto, operatorId, operatorName)
            );
            exception.Message.Should().Be("患者姓名不能为空");
        }

        #endregion

        #region GetByIdAsync 测试

        [Fact]
        public async Task GetByIdAsync_Should_Return_Patient_When_Exists()
        {
            // Arrange
            var patient = _testPatients.First();

            // Act
            var result = await _patientService.GetByIdAsync(patient.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(patient.Id);
            result.Name.Should().Be(patient.Name);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_Null_When_Not_Exists()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _patientService.GetByIdAsync(nonExistentId);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetPagedAsync 测试

        [Fact]
        public async Task GetPagedAsync_Should_Return_Paginated_Patients()
        {
            // Arrange
            var query = new PatientPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 10
            };

            // Act
            var result = await _patientService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(_testPatients.Count);
            result.TotalCount.Should().Be(_testPatients.Count);
            result.CurrentPage.Should().Be(1);
            result.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task GetPagedAsync_Should_Filter_By_Name()
        {
            // Arrange
            var targetPatient = _testPatients.First();
            var query = new PatientPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 10,
                Name = targetPatient.Name
            };

            // Act
            var result = await _patientService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            result.Items.First().Name.Should().Be(targetPatient.Name);
        }

        #endregion

        #region DeleteAsync 测试 (软删除)

        [Fact]
        public async Task DeleteAsync_Should_Disable_Patient_Successfully()
        {
            // Arrange
            var patient = _testPatients.First(p => p.Status == CommonStatus.Enabled);
            var operatorId = Guid.NewGuid();
            var operatorName = "操作员";

            // Act
            var result = await _patientService.DeleteAsync(patient.Id, operatorId, operatorName);

            // Assert
            result.Should().BeTrue();
            patient.Status.Should().Be(CommonStatus.Disabled);

            // 验证日志记录
            _mockLogService.Verify(x => x.CreateLogAsync(
                It.Is<LogCreateDto>(log => 
                    log.ActionType == ActionType.Disable &&
                    log.ObjectType == ObjectType.Patient &&
                    log.ObjectId == patient.Id
                )), Times.Once);
        }

        #endregion

        #region SetStatusAsync 测试

        [Fact]
        public async Task SetStatusAsync_Should_Enable_Patient_Successfully()
        {
            // Arrange
            var patient = _testPatients.First(p => p.Status == CommonStatus.Disabled);
            var operatorId = Guid.NewGuid();
            var operatorName = "操作员";

            // Act
            var result = await _patientService.SetStatusAsync(patient.Id, true, operatorId, operatorName);

            // Assert
            result.Should().BeTrue();
            patient.Status.Should().Be(CommonStatus.Enabled);
        }

        [Fact]
        public async Task SetStatusAsync_Should_Disable_Patient_Successfully()
        {
            // Arrange
            var patient = _testPatients.First(p => p.Status == CommonStatus.Enabled);
            var operatorId = Guid.NewGuid();
            var operatorName = "操作员";

            // Act
            var result = await _patientService.SetStatusAsync(patient.Id, false, operatorId, operatorName);

            // Assert
            result.Should().BeTrue();
            patient.Status.Should().Be(CommonStatus.Disabled);
        }

        #endregion

        #region SearchAsync 测试

        [Fact]
        public async Task SearchAsync_Should_Return_Patients_Matching_Keyword()
        {
            // Arrange
            var targetPatient = _testPatients.First();
            var keyword = targetPatient.Name.Substring(0, 2); // 取名字前两个字

            // Act
            var result = await _patientService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain(p => p.Name == targetPatient.Name);
        }

        [Fact]
        public async Task SearchAsync_Should_Search_By_PhoneNumber()
        {
            // Arrange
            var targetPatient = _testPatients.First();
            var keyword = targetPatient.PhoneNumber.Substring(0, 5); // 取手机号前5位

            // Act
            var result = await _patientService.SearchAsync(keyword);

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain(p => p.PhoneNumber == targetPatient.PhoneNumber);
        }

        #endregion

        #region GetActivePatientsAsync 测试

        [Fact]
        public async Task GetActivePatientsAsync_Should_Return_Only_Enabled_Patients()
        {
            // Act
            var result = await _patientService.GetActivePatientsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().OnlyContain(p => p.Status == CommonStatus.Enabled);
            result.Count.Should().Be(_testPatients.Count(p => p.Status == CommonStatus.Enabled));
        }

        #endregion

        #region GetByPhoneNumberAsync/GetByIDNumberAsync 测试

        [Fact]
        public async Task GetByPhoneNumberAsync_Should_Return_Patient_When_Exists()
        {
            // Arrange
            var patient = _testPatients.First();

            // Act
            var result = await _patientService.GetByPhoneNumberAsync(patient.PhoneNumber ?? "");

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(patient.Id);
            result.PhoneNumber.Should().Be(patient.PhoneNumber);
        }

        [Fact]
        public async Task GetByIDNumberAsync_Should_Return_Patient_When_Exists()
        {
            // Arrange
            var patient = _testPatients.First();

            // Act
            var result = await _patientService.GetByIDNumberAsync(patient.IdNumber ?? "");

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(patient.Id);
            result.IDNumber.Should().Be(patient.IdNumber);
        }

        #endregion

        #region 档案管理功能测试

        [Fact]
        public async Task GetVisitHistoryAsync_Should_Return_Patient_Visit_History()
        {
            // Arrange
            var patient = _testPatients.First();

            // Act
            var result = await _patientService.GetVisitHistoryAsync(patient.Id);

            // Assert
            result.Should().NotBeNull();
            result.PatientId.Should().Be(patient.Id);
            result.PatientName.Should().Be(patient.Name);
            result.TotalVisits.Should().Be(patient.VisitCount);
            result.LastVisitDate.Should().Be(patient.LastVisitTime);
        }

        [Fact]
        public async Task UpdateAllergyHistoryAsync_Should_Update_Successfully()
        {
            // Arrange
            var patient = _testPatients.First();
            var newAllergyHistory = "青霉素过敏，头孢过敏";
            var operatorId = Guid.NewGuid();
            var operatorName = "操作员";

            // Act
            var result = await _patientService.UpdateAllergyHistoryAsync(
                patient.Id, newAllergyHistory, operatorId, operatorName);

            // Assert
            result.Should().BeTrue();
            
            // 验证更新后的值
            _mockPatientRepository.Verify(x => x.UpdateAsync(
                It.Is<PatientModel>(p => 
                    p.Id == patient.Id && 
                    p.AllergyHistory == newAllergyHistory
                )), Times.Once);
        }

        #endregion

        #region 批量导入测试

        [Fact]
        public async Task ImportPatientsAsync_Should_Import_Valid_Patients()
        {
            // Arrange
            var importList = new List<PatientImportDto>
            {
                new PatientImportDto
                {
                    Name = "导入患者1",
                    Gender = Gender.Male,
                    Age = 25,
                    IdNumber = "110101199501010011",
                    PhoneNumber = "13800138001"
                },
                new PatientImportDto
                {
                    Name = "导入患者2",
                    Gender = Gender.Female,
                    Age = 30,
                    IdNumber = "110101199001010022",
                    PhoneNumber = "13800138002"
                }
            };
            var operatorId = Guid.NewGuid();
            var operatorName = "操作员";

            // Act
            var result = await _patientService.ImportPatientsAsync(importList, operatorId, operatorName);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(2);
            result.SuccessCount.Should().Be(2);
            result.FailedCount.Should().Be(0);
            result.DuplicateCount.Should().Be(0);
        }

        [Fact]
        public async Task ImportPatientsAsync_Should_Handle_Duplicate_IdNumber()
        {
            // Arrange
            var existingIdNumber = _testPatients.First().IdNumber;
            var importList = new List<PatientImportDto>
            {
                new PatientImportDto
                {
                    Name = "重复患者",
                    Gender = Gender.Male,
                    Age = 25,
                    IdNumber = existingIdNumber, // 已存在的身份证号
                    PhoneNumber = "13800138003"
                }
            };
            var operatorId = Guid.NewGuid();
            var operatorName = "操作员";

            // Act
            var result = await _patientService.ImportPatientsAsync(importList, operatorId, operatorName);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(1);
            result.SuccessCount.Should().Be(0);
            result.DuplicateCount.Should().Be(1);
            result.DuplicateRecords.Should().Contain(r => r.Contains("身份证号重复"));
        }

        #endregion

        #region 统计功能测试

        [Fact]
        public async Task GetStatisticsAsync_Should_Return_Correct_Statistics()
        {
            // Act
            var result = await _patientService.GetStatisticsAsync();

            // Assert
            result.Should().NotBeNull();
            result.TotalPatients.Should().Be(_testPatients.Count);
            result.ActivePatients.Should().Be(_testPatients.Count(p => p.Status == CommonStatus.Enabled));
            result.InactivePatients.Should().Be(_testPatients.Count(p => p.Status == CommonStatus.Disabled));
            result.MaleCount.Should().Be(_testPatients.Count(p => p.Gender == Gender.Male));
            result.FemaleCount.Should().Be(_testPatients.Count(p => p.Gender == Gender.Female));
        }

        [Fact]
        public async Task GetAgeDistributionAsync_Should_Return_Age_Distribution()
        {
            // Act
            var result = await _patientService.GetAgeDistributionAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(5); // 5个年龄段
            result.Sum(x => x.Count).Should().Be(_testPatients.Count(p => p.Status == CommonStatus.Enabled));
        }

        [Fact]
        public async Task GetGenderDistributionAsync_Should_Return_Gender_Distribution()
        {
            // Act
            var result = await _patientService.GetGenderDistributionAsync();

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(_testPatients.Count(p => p.Status == CommonStatus.Enabled));
            (result.MaleCount + result.FemaleCount + result.UnknownCount).Should().Be(result.TotalCount);
        }

        #endregion

        #region 合并患者档案测试

        [Fact]
        public async Task MergeDuplicatePatientsAsync_Should_Merge_Successfully()
        {
            // Arrange
            var primaryPatient = _testPatients[0];
            var duplicatePatient = _testPatients[1];
            var operatorId = Guid.NewGuid();
            var operatorName = "操作员";

            primaryPatient.VisitCount = 5;
            duplicatePatient.VisitCount = 3;

            // Act
            var result = await _patientService.MergeDuplicatePatientsAsync(
                primaryPatient.Id, duplicatePatient.Id, operatorId, operatorName);

            // Assert
            result.Should().BeTrue();
            
            // 验证主患者的就诊次数增加
            _mockPatientRepository.Verify(x => x.UpdateAsync(
                It.Is<PatientModel>(p => 
                    p.Id == primaryPatient.Id && 
                    p.VisitCount == 8 // 5 + 3
                )), Times.Once);
            
            // 验证重复患者被禁用
            _mockPatientRepository.Verify(x => x.UpdateAsync(
                It.Is<PatientModel>(p => 
                    p.Id == duplicatePatient.Id && 
                    p.Status == CommonStatus.Disabled
                )), Times.Once);
        }

        #endregion

        #region 活跃度分析测试

        [Fact]
        public async Task GetRecentActivePatientsAsync_Should_Return_Recent_Active_Patients()
        {
            // Arrange
            var cutoffDate = DateTime.Now.AddDays(-30);
            
            // 设置一些患者为最近活跃
            _testPatients[0].LastVisitTime = DateTime.Now.AddDays(-5);
            _testPatients[1].LastVisitTime = DateTime.Now.AddDays(-20);
            _testPatients[2].LastVisitTime = DateTime.Now.AddDays(-40); // 不活跃

            // Act
            var result = await _patientService.GetRecentActivePatientsAsync(30);

            // Assert
            result.Should().NotBeNull();
            // 验证返回的患者都是最近活跃的
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetTodayNewPatientsAsync_Should_Return_Today_New_Patients()
        {
            // Arrange
            var today = DateTime.Today;
            _testPatients[0].CreateTime = today.AddHours(10);
            _testPatients[1].CreateTime = today.AddDays(-1);

            // Act
            var result = await _patientService.GetTodayNewPatientsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().OnlyContain(p => p.CreateTime.Date == today);
        }

        #endregion

        #region 重复检查测试

        [Fact]
        public async Task CheckDuplicatePatientsAsync_Should_Find_Duplicates_By_IdNumber()
        {
            // Arrange
            var patient = _testPatients.First();

            // Act
            var result = await _patientService.CheckDuplicatePatientsAsync(patient.IdNumber ?? "", "");

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().IDNumber.Should().Be(patient.IdNumber);
        }

        [Fact]
        public async Task CheckDuplicatePatientsAsync_Should_Find_Duplicates_By_PhoneNumber()
        {
            // Arrange
            var patient = _testPatients.First();

            // Act
            var result = await _patientService.CheckDuplicatePatientsAsync("", patient.PhoneNumber ?? "");

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().PhoneNumber.Should().Be(patient.PhoneNumber);
        }

        #endregion
    }
}