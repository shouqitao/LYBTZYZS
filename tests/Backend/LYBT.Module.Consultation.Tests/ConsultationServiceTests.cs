using AutoMapper;
using FluentAssertions;
using LYBT.Infrastructure.Data;
using LYBT.Module.Consultation.Services;
using LYBT.Models.Consultation;
using LYBT.Models.MedicalCase;
using LYBT.Models.Patients;
using LYBT.Models.Users;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Module.Consultation.Tests
{
    public class ConsultationServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly ConsultationService _service;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<ConsultationService>> _mockLogger;

        public ConsultationServiceTests()
        {
            // 设置内存数据库
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new AppDbContext(options);
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<ConsultationService>>();

            _service = new ConsultationService(_context, _mockMapper.Object, _mockLogger.Object);

            // 初始化测试数据
            InitializeTestData();
        }

        private void InitializeTestData()
        {
            // 添加测试患者
            var patient = new PatientModel
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "测试患者",
                Gender = Gender.Male,
                BirthDate = new DateTime(1990, 1, 1),
                PhoneNumber = "13800138000",
                Status = CommonStatus.Enabled
            };
            _context.Patients.Add(patient);

            // 添加测试医生
            var doctor = new UserModel
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Username = "testdoctor",
                RealName = "测试医生",
                Status = CommonStatus.Enabled,
                CreateTime = DateTime.Now
            };
            _context.Users.Add(doctor);

            // 添加测试医疗案例
            var medicalCase = new MedicalCaseModel
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                PatientId = patient.Id,
                UserId = doctor.Id,
                Status = MedicalCaseStatus.Registered,
                CreateTime = DateTime.Now
            };
            _context.MedicalCases.Add(medicalCase);

            _context.SaveChanges();
        }

        [Fact]
        public async Task StartConsultationAsync_WithValidData_ShouldCreateConsultation()
        {
            // Arrange
            var dto = new ConsultationStartDto
            {
                MedicalCaseId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                PatientId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                UserId = Guid.Parse("22222222-2222-2222-2222-222222222222")
            };

            var expectedDetailDto = new ConsultationDetailDto
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = dto.MedicalCaseId,
                PatientId = dto.PatientId,
                UserId = dto.UserId,
                PatientName = "测试患者",
                DoctorName = "测试医生"
            };

            // Act
            var result = await _service.StartConsultationAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.MedicalCaseId.Should().Be(dto.MedicalCaseId);
            result.PatientId.Should().Be(dto.PatientId);
            result.UserId.Should().Be(dto.UserId);

            // 验证数据库中创建了记录
            var consultationInDb = await _context.Consultations
                .FirstOrDefaultAsync(c => c.MedicalCaseId == dto.MedicalCaseId);
            consultationInDb.Should().NotBeNull();
            consultationInDb!.Status.Should().Be(CommonStatus.Enabled);

            // 验证医疗案例状态已更新
            var medicalCaseInDb = await _context.MedicalCases
                .FirstOrDefaultAsync(m => m.Id == dto.MedicalCaseId);
            medicalCaseInDb!.Status.Should().Be(MedicalCaseStatus.InConsultation);
        }

        [Fact]
        public async Task StartConsultationAsync_WithExistingConsultation_ShouldThrowException()
        {
            // Arrange
            var existingConsultation = new ConsultationModel
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                PatientId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Status = CommonStatus.Enabled,
                CreateTime = DateTime.Now,
                ConsultationTime = DateTime.Now
            };
            _context.Consultations.Add(existingConsultation);
            await _context.SaveChangesAsync();

            var dto = new ConsultationStartDto
            {
                MedicalCaseId = existingConsultation.MedicalCaseId,
                PatientId = existingConsultation.PatientId,
                UserId = existingConsultation.UserId
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _service.StartConsultationAsync(dto));
        }

        [Fact]
        public async Task UpdateConsultationAsync_WithValidData_ShouldUpdateConsultation()
        {
            // Arrange
            var consultation = new ConsultationModel
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                PatientId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Status = CommonStatus.Enabled,
                CreateTime = DateTime.Now,
                ConsultationTime = DateTime.Now,
                Diagnosis = "初始诊断"
            };
            _context.Consultations.Add(consultation);
            await _context.SaveChangesAsync();

            var updateDto = new ConsultationUpdateDto
            {
                Inspection = "面色偏白，精神欠佳",
                AuscultationOlfaction = "语声低微，无异常气味",
                Inquiry = "自诉疲乏无力，食欲不振",
                Palpation = "脉沉细无力",
                TongueInspection = "舌淡苔白",
                PulseCondition = "沉细",
                TCMDiagnosis = "脾虚气弱证",
                Diagnosis = "脾虚证",
                TreatmentPrinciple = "健脾益气",
                MedicalAdvice = "注意休息，清淡饮食"
            };

            // Act
            var result = await _service.UpdateConsultationAsync(consultation.Id, updateDto);

            // Assert
            result.Should().NotBeNull();
            
            // 验证数据库中的更新
            var updatedConsultation = await _context.Consultations
                .FirstOrDefaultAsync(c => c.Id == consultation.Id);
            updatedConsultation!.Inspection.Should().Be(updateDto.Inspection);
            updatedConsultation.TCMDiagnosis.Should().Be(updateDto.TCMDiagnosis);
            updatedConsultation.Diagnosis.Should().Be(updateDto.Diagnosis);
            updatedConsultation.UpdateTime.Should().NotBeNull();
        }

        [Fact]
        public async Task CompleteConsultationAsync_WithValidData_ShouldCompleteConsultation()
        {
            // Arrange
            var consultation = new ConsultationModel
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                PatientId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Status = CommonStatus.Enabled,
                CreateTime = DateTime.Now,
                ConsultationTime = DateTime.Now.AddMinutes(-5) // 5分钟前开始看诊
            };
            _context.Consultations.Add(consultation);

            // 更新医疗案例的ConsultationId
            var medicalCase = await _context.MedicalCases
                .FirstAsync(m => m.Id == consultation.MedicalCaseId);
            medicalCase.ConsultationId = consultation.Id;
            
            await _context.SaveChangesAsync();

            var completeDto = new ConsultationCompleteDto
            {
                Diagnosis = "脾虚证",
                TCMDiagnosis = "脾虚气弱证",
                TreatmentPrinciple = "健脾益气",
                MedicalAdvice = "按时服药，注意饮食"
            };

            // Act
            var result = await _service.CompleteConsultationAsync(consultation.Id, completeDto);

            // Assert
            result.Should().BeTrue();

            // 验证看诊记录已更新
            var completedConsultation = await _context.Consultations
                .FirstOrDefaultAsync(c => c.Id == consultation.Id);
            completedConsultation!.Diagnosis.Should().Be(completeDto.Diagnosis);
            completedConsultation.Duration.Should().BeGreaterThan(0);

            // 验证医疗案例状态已更新
            var completedMedicalCase = await _context.MedicalCases
                .FirstOrDefaultAsync(m => m.Id == consultation.MedicalCaseId);
            completedMedicalCase!.Status.Should().Be(MedicalCaseStatus.Completed);
            completedMedicalCase.CompleteTime.Should().NotBeNull();
        }

        [Fact]
        public async Task GetPatientHistoryAsync_ShouldReturnPatientConsultations()
        {
            // Arrange
            var patientId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var consultations = new List<ConsultationModel>
            {
                new ConsultationModel
                {
                    Id = Guid.NewGuid(),
                    PatientId = patientId,
                    UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    MedicalCaseId = Guid.NewGuid(),
                    Diagnosis = "诊断1",
                    Status = CommonStatus.Enabled,
                    ConsultationTime = DateTime.Now.AddDays(-10),
                    CreateTime = DateTime.Now.AddDays(-10)
                },
                new ConsultationModel
                {
                    Id = Guid.NewGuid(),
                    PatientId = patientId,
                    UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    MedicalCaseId = Guid.NewGuid(),
                    Diagnosis = "诊断2",
                    Status = CommonStatus.Enabled,
                    ConsultationTime = DateTime.Now.AddDays(-5),
                    CreateTime = DateTime.Now.AddDays(-5)
                }
            };
            _context.Consultations.AddRange(consultations);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetPatientHistoryAsync(patientId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().BeInDescendingOrder(c => c.ConsultationTime);
        }

        #region GetPagedAsync 测试

        [Fact]
        public async Task GetPagedAsync_WithBasicQuery_ShouldReturnPagedResult()
        {
            // Arrange
            var consultations = CreateTestConsultations(15);
            _context.Consultations.AddRange(consultations);
            await _context.SaveChangesAsync();

            var query = new ConsultationPagedQueryDto
            {
                PageIndex = 1,
                PageSize = 10
            };

            // Act
            var result = await _service.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(10);
            result.TotalCount.Should().Be(15);
            result.PageIndex.Should().Be(1);
            result.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task GetPagedAsync_WithPatientFilter_ShouldFilterByPatientId()
        {
            // Arrange
            var patientId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var consultations = CreateTestConsultations(5, patientId);
            var otherConsultations = CreateTestConsultations(3, Guid.NewGuid());
            _context.Consultations.AddRange(consultations);
            _context.Consultations.AddRange(otherConsultations);
            await _context.SaveChangesAsync();

            var query = new ConsultationPagedQueryDto
            {
                PageIndex = 1,
                PageSize = 20,
                PatientId = patientId
            };

            // Act
            var result = await _service.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(5);
            result.TotalCount.Should().Be(5);
            result.Data.Should().AllSatisfy(c => c.PatientId.Should().Be(patientId));
        }

        [Fact]
        public async Task GetPagedAsync_WithDoctorFilter_ShouldFilterByUserId()
        {
            // Arrange
            var doctorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var consultations = CreateTestConsultations(3, null, doctorId);
            var otherConsultations = CreateTestConsultations(2, null, Guid.NewGuid());
            _context.Consultations.AddRange(consultations);
            _context.Consultations.AddRange(otherConsultations);
            await _context.SaveChangesAsync();

            var query = new ConsultationPagedQueryDto
            {
                PageIndex = 1,
                PageSize = 20,
                UserId = doctorId
            };

            // Act
            var result = await _service.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(3);
            result.Data.Should().AllSatisfy(c => c.UserId.Should().Be(doctorId));
        }

        [Fact]
        public async Task GetPagedAsync_WithDateRange_ShouldFilterByDate()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(-7);
            var endDate = DateTime.Today;
            
            var validConsultations = new List<ConsultationModel>
            {
                CreateConsultation(consultationTime: DateTime.Today.AddDays(-3)),
                CreateConsultation(consultationTime: DateTime.Today.AddDays(-1))
            };
            
            var invalidConsultations = new List<ConsultationModel>
            {
                CreateConsultation(consultationTime: DateTime.Today.AddDays(-10)),
                CreateConsultation(consultationTime: DateTime.Today.AddDays(1))
            };
            
            _context.Consultations.AddRange(validConsultations);
            _context.Consultations.AddRange(invalidConsultations);
            await _context.SaveChangesAsync();

            var query = new ConsultationPagedQueryDto
            {
                PageIndex = 1,
                PageSize = 20,
                StartDate = startDate,
                EndDate = endDate
            };

            // Act
            var result = await _service.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Data.Should().AllSatisfy(c => 
                c.ConsultationTime.Should().BeAfter(startDate.AddMinutes(-1))
                .And.BeBefore(endDate.AddDays(1)));
        }

        [Fact]
        public async Task GetPagedAsync_WithDiagnosisKeyword_ShouldFilterByDiagnosis()
        {
            // Arrange
            var consultations = new List<ConsultationModel>
            {
                CreateConsultation(diagnosis: "感冒咳嗽", tcmDiagnosis: "风寒感冒"),
                CreateConsultation(diagnosis: "头痛失眠", tcmDiagnosis: "肝阳上亢"),
                CreateConsultation(diagnosis: "胃痛", tcmDiagnosis: "脾胃虚寒")
            };
            
            _context.Consultations.AddRange(consultations);
            await _context.SaveChangesAsync();

            var query = new ConsultationPagedQueryDto
            {
                PageIndex = 1,
                PageSize = 20,
                DiagnosisKeyword = "感冒"
            };

            // Act
            var result = await _service.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data.First().Diagnosis.Should().Contain("感冒");
        }

        [Fact]
        public async Task GetPagedAsync_WithEmptyResult_ShouldReturnEmptyPage()
        {
            // Arrange
            var query = new ConsultationPagedQueryDto
            {
                PageIndex = 1,
                PageSize = 10
            };

            // Act
            var result = await _service.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task GetPagedAsync_WithDisabledConsultations_ShouldExcludeDisabled()
        {
            // Arrange
            var enabledConsultations = CreateTestConsultations(3);
            var disabledConsultations = CreateTestConsultations(2);
            disabledConsultations.ForEach(c => c.Status = CommonStatus.Disabled);
            
            _context.Consultations.AddRange(enabledConsultations);
            _context.Consultations.AddRange(disabledConsultations);
            await _context.SaveChangesAsync();

            var query = new ConsultationPagedQueryDto
            {
                PageIndex = 1,
                PageSize = 20
            };

            // Act
            var result = await _service.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().HaveCount(3);
            result.TotalCount.Should().Be(3);
        }

        #endregion

        #region GetByIdAsync 测试

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnConsultation()
        {
            // Arrange
            var consultation = CreateConsultation();
            _context.Consultations.Add(consultation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByIdAsync(consultation.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(consultation.Id);
            result.PatientName.Should().Be("测试患者");
            result.DoctorName.Should().Be("测试医生");
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var result = await _service.GetByIdAsync(invalidId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_WithDisabledConsultation_ShouldReturnNull()
        {
            // Arrange
            var consultation = CreateConsultation();
            consultation.Status = CommonStatus.Disabled;
            _context.Consultations.Add(consultation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByIdAsync(consultation.Id);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetByMedicalCaseIdAsync 测试

        [Fact]
        public async Task GetByMedicalCaseIdAsync_WithValidMedicalCaseId_ShouldReturnConsultation()
        {
            // Arrange
            var medicalCaseId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var consultation = CreateConsultation(medicalCaseId: medicalCaseId);
            _context.Consultations.Add(consultation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByMedicalCaseIdAsync(medicalCaseId);

            // Assert
            result.Should().NotBeNull();
            result!.MedicalCaseId.Should().Be(medicalCaseId);
        }

        [Fact]
        public async Task GetByMedicalCaseIdAsync_WithInvalidMedicalCaseId_ShouldReturnNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var result = await _service.GetByMedicalCaseIdAsync(invalidId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByMedicalCaseIdAsync_WithDisabledConsultation_ShouldReturnNull()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var consultation = CreateConsultation(medicalCaseId: medicalCaseId);
            consultation.Status = CommonStatus.Disabled;
            _context.Consultations.Add(consultation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByMedicalCaseIdAsync(medicalCaseId);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetTodayConsultationsByDoctorAsync 测试

        [Fact]
        public async Task GetTodayConsultationsByDoctorAsync_WithTodayConsultations_ShouldReturnTodayOnly()
        {
            // Arrange
            var doctorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var todayConsultations = new List<ConsultationModel>
            {
                CreateConsultation(userId: doctorId, consultationTime: DateTime.Today.AddHours(9)),
                CreateConsultation(userId: doctorId, consultationTime: DateTime.Today.AddHours(14))
            };
            
            var yesterdayConsultations = new List<ConsultationModel>
            {
                CreateConsultation(userId: doctorId, consultationTime: DateTime.Today.AddDays(-1))
            };
            
            _context.Consultations.AddRange(todayConsultations);
            _context.Consultations.AddRange(yesterdayConsultations);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetTodayConsultationsByDoctorAsync(doctorId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().BeInAscendingOrder(c => c.ConsultationTime);
            result.Should().AllSatisfy(c => c.ConsultationTime.Date.Should().Be(DateTime.Today));
        }

        [Fact]
        public async Task GetTodayConsultationsByDoctorAsync_WithNonExistentDoctor_ShouldReturnEmpty()
        {
            // Arrange
            var doctorId = Guid.NewGuid();

            // Act
            var result = await _service.GetTodayConsultationsByDoctorAsync(doctorId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetTodayConsultationsByDoctorAsync_WithDisabledConsultations_ShouldExcludeDisabled()
        {
            // Arrange
            var doctorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var enabledConsultation = CreateConsultation(userId: doctorId, consultationTime: DateTime.Today.AddHours(10));
            var disabledConsultation = CreateConsultation(userId: doctorId, consultationTime: DateTime.Today.AddHours(15));
            disabledConsultation.Status = CommonStatus.Disabled;
            
            _context.Consultations.Add(enabledConsultation);
            _context.Consultations.Add(disabledConsultation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetTodayConsultationsByDoctorAsync(doctorId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().Id.Should().Be(enabledConsultation.Id);
        }

        #endregion

        #region GetDoctorConsultationCountAsync 测试

        [Fact]
        public async Task GetDoctorConsultationCountAsync_WithoutDateRange_ShouldReturnTotalCount()
        {
            // Arrange
            var doctorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var consultations = CreateTestConsultations(5, null, doctorId);
            var otherDoctorConsultations = CreateTestConsultations(3, null, Guid.NewGuid());
            
            _context.Consultations.AddRange(consultations);
            _context.Consultations.AddRange(otherDoctorConsultations);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetDoctorConsultationCountAsync(doctorId);

            // Assert
            result.Should().Be(5);
        }

        [Fact]
        public async Task GetDoctorConsultationCountAsync_WithDateRange_ShouldReturnFilteredCount()
        {
            // Arrange
            var doctorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var startDate = DateTime.Today.AddDays(-7);
            var endDate = DateTime.Today;
            
            var validConsultations = new List<ConsultationModel>
            {
                CreateConsultation(userId: doctorId, consultationTime: DateTime.Today.AddDays(-3)),
                CreateConsultation(userId: doctorId, consultationTime: DateTime.Today.AddDays(-1))
            };
            
            var invalidConsultations = new List<ConsultationModel>
            {
                CreateConsultation(userId: doctorId, consultationTime: DateTime.Today.AddDays(-10))
            };
            
            _context.Consultations.AddRange(validConsultations);
            _context.Consultations.AddRange(invalidConsultations);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetDoctorConsultationCountAsync(doctorId, startDate, endDate);

            // Assert
            result.Should().Be(2);
        }

        [Fact]
        public async Task GetDoctorConsultationCountAsync_WithNonExistentDoctor_ShouldReturnZero()
        {
            // Arrange
            var doctorId = Guid.NewGuid();

            // Act
            var result = await _service.GetDoctorConsultationCountAsync(doctorId);

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public async Task GetDoctorConsultationCountAsync_WithDisabledConsultations_ShouldExcludeDisabled()
        {
            // Arrange
            var doctorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var enabledConsultations = CreateTestConsultations(3, null, doctorId);
            var disabledConsultations = CreateTestConsultations(2, null, doctorId);
            disabledConsultations.ForEach(c => c.Status = CommonStatus.Disabled);
            
            _context.Consultations.AddRange(enabledConsultations);
            _context.Consultations.AddRange(disabledConsultations);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetDoctorConsultationCountAsync(doctorId);

            // Assert
            result.Should().Be(3);
        }

        #endregion

        #region UpdateStatusAsync 测试

        [Fact]
        public async Task UpdateStatusAsync_WithValidData_ShouldUpdateStatus()
        {
            // Arrange
            var consultation = CreateConsultation();
            _context.Consultations.Add(consultation);
            await _context.SaveChangesAsync();

            var newStatus = (int)CommonStatus.Disabled;
            var reason = "测试原因";

            // Act
            var result = await _service.UpdateStatusAsync(consultation.Id, newStatus, reason);

            // Assert
            result.Should().NotBeNull();
            
            var updatedConsultation = await _context.Consultations.FindAsync(consultation.Id);
            updatedConsultation!.Status.Should().Be(CommonStatus.Disabled);
            updatedConsultation.Remark.Should().Contain(reason);
            updatedConsultation.UpdateTime.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateStatusAsync_WithNonExistentId_ShouldThrowException()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            var newStatus = (int)CommonStatus.Disabled;

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _service.UpdateStatusAsync(invalidId, newStatus));
        }

        [Fact]
        public async Task UpdateStatusAsync_WithoutReason_ShouldUpdateWithoutRemark()
        {
            // Arrange
            var consultation = CreateConsultation();
            _context.Consultations.Add(consultation);
            await _context.SaveChangesAsync();

            var newStatus = (int)CommonStatus.Disabled;

            // Act
            var result = await _service.UpdateStatusAsync(consultation.Id, newStatus);

            // Assert
            result.Should().NotBeNull();
            
            var updatedConsultation = await _context.Consultations.FindAsync(consultation.Id);
            updatedConsultation!.Status.Should().Be(CommonStatus.Disabled);
            updatedConsultation.UpdateTime.Should().NotBeNull();
        }

        #endregion

        #region DeleteAsync 测试

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldSoftDelete()
        {
            // Arrange
            var consultation = CreateConsultation();
            _context.Consultations.Add(consultation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.DeleteAsync(consultation.Id);

            // Assert
            result.Should().BeTrue();
            
            var deletedConsultation = await _context.Consultations.FindAsync(consultation.Id);
            deletedConsultation!.Status.Should().Be(CommonStatus.Disabled);
            deletedConsultation.UpdateTime.Should().NotBeNull();
        }

        [Fact]
        public async Task DeleteAsync_WithNonExistentId_ShouldReturnFalse()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var result = await _service.DeleteAsync(invalidId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_WithAlreadyDeletedConsultation_ShouldStillReturnTrue()
        {
            // Arrange
            var consultation = CreateConsultation();
            consultation.Status = CommonStatus.Disabled;
            _context.Consultations.Add(consultation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.DeleteAsync(consultation.Id);

            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region 异常处理测试

        [Fact]
        public async Task StartConsultationAsync_WithNullMedicalCase_ShouldStillCreateConsultation()
        {
            // Arrange
            var dto = new ConsultationStartDto
            {
                MedicalCaseId = Guid.NewGuid(), // 不存在的医疗案例ID
                PatientId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                UserId = Guid.Parse("22222222-2222-2222-2222-222222222222")
            };

            // Act
            var result = await _service.StartConsultationAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.MedicalCaseId.Should().Be(dto.MedicalCaseId);
        }

        [Fact]
        public async Task UpdateConsultationAsync_WithNonExistentId_ShouldThrowException()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            var updateDto = new ConsultationUpdateDto
            {
                Diagnosis = "测试诊断"
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _service.UpdateConsultationAsync(invalidId, updateDto));
        }

        [Fact]
        public async Task CompleteConsultationAsync_WithNonExistentId_ShouldThrowException()
        {
            // Arrange
            var invalidId = Guid.NewGuid();
            var completeDto = new ConsultationCompleteDto
            {
                Diagnosis = "测试诊断"
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _service.CompleteConsultationAsync(invalidId, completeDto));
        }

        #endregion

        #region 辅助方法

        private ConsultationModel CreateConsultation(
            Guid? id = null,
            Guid? medicalCaseId = null,
            Guid? patientId = null,
            Guid? userId = null,
            DateTime? consultationTime = null,
            string diagnosis = "测试诊断",
            string tcmDiagnosis = "测试中医诊断",
            CommonStatus status = CommonStatus.Enabled)
        {
            return new ConsultationModel
            {
                Id = id ?? Guid.NewGuid(),
                MedicalCaseId = medicalCaseId ?? Guid.Parse("33333333-3333-3333-3333-333333333333"),
                PatientId = patientId ?? Guid.Parse("11111111-1111-1111-1111-111111111111"),
                UserId = userId ?? Guid.Parse("22222222-2222-2222-2222-222222222222"),
                ConsultationTime = consultationTime ?? DateTime.Now,
                CreateTime = DateTime.Now,
                Diagnosis = diagnosis,
                TCMDiagnosis = tcmDiagnosis,
                Status = status,
                Inspection = "面色红润",
                AuscultationOlfaction = "语声清晰",
                Inquiry = "主诉头痛",
                Palpation = "脉象平和",
                TongueInspection = "舌淡红苔薄白",
                PulseCondition = "平和",
                TreatmentPrinciple = "清热解毒",
                MedicalAdvice = "按时服药"
            };
        }

        private List<ConsultationModel> CreateTestConsultations(
            int count, 
            Guid? patientId = null, 
            Guid? userId = null)
        {
            var consultations = new List<ConsultationModel>();
            for (int i = 0; i < count; i++)
            {
                consultations.Add(CreateConsultation(
                    patientId: patientId ?? Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    userId: userId ?? Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    consultationTime: DateTime.Now.AddMinutes(-i * 30), // 每个看诊间隔30分钟
                    diagnosis: $"诊断{i + 1}",
                    tcmDiagnosis: $"中医诊断{i + 1}"
                ));
            }
            return consultations;
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}