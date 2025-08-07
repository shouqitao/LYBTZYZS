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

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}