using System;
using System.Collections.Generic;
using Bogus;
using LYBT.Entities.Users;
using LYBT.Entities.Patients;
using LYBT.Entities.Herbs;
using LYBT.Entities.Consultation;
using LYBT.Entities.Prescriptions;
using LYBT.Entities.MedicalCase;
using LYBT.Entities.Formula;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Tests.Backend.TestUtilities;

namespace LYBT.Tests.TestDataFactory
{
    /// <summary>
    /// 统一测试数据工厂 - 为所有测试模块提供一致的测试数据生成
    /// 基于Bogus库，支持中文环境，提供可重现的随机数据
    /// </summary>
    public class UnifiedTestDataFactory
    {
        private readonly Randomizer _randomizer;
        private const string Locale = "zh_CN";

        public UnifiedTestDataFactory(int? seed = null)
        {
            _randomizer = seed.HasValue ? new Randomizer(seed.Value) : new Randomizer();
            Randomizer.Seed = new Random();
        }

        #region 用户相关数据工厂

        /// <summary>
        /// 用户实体生成器
        /// </summary>
        public Faker<User> UserModelFaker => new Faker<User>(Locale)
            .RuleFor(u => u.Id, f => f.Random.Guid())
            .RuleFor(u => u.Username, f => $"{TestConstants.TestUsernamePrefix}{f.Internet.UserName()}")
            .RuleFor(u => u.RealName, f => f.Name.FullName())
            .RuleFor(u => u.PhoneNumber, f => f.PickRandom(TestConstants.ValidPhoneNumbers))
            .RuleFor(u => u.Role, f => f.PickRandom<UserRole>())
            .RuleFor(u => u.Status, f => f.PickRandom<CommonStatus>())
            .RuleFor(u => u.PasswordHash, f => "AQAAAAEAACcQAAAAEGwrB7Ri8YXw4qPHiXoTJQKoNccNRjvMFWuNi4W5YYp3DhIRRtxb0AHjD+WnzGLCmw==")
            .RuleFor(u => u.CreatedTime, f => f.Date.Between(TestConstants.TestBaseDateTime, DateTime.UtcNow))
            .RuleFor(u => u.PinYinCode, (f, u) => GeneratePinyinCode(u.RealName))
            .FinishWith((f, u) =>
            {
                u.UpdateTime = u.CreatedTime;
            });

        /// <summary>
        /// 用户创建DTO生成器
        /// </summary>
        public Faker<UserCreateDto> UserCreateDtoFaker => new Faker<UserCreateDto>(Locale)
            .RuleFor(u => u.Username, f => $"{TestConstants.TestUsernamePrefix}{f.Internet.UserName()}")
            .RuleFor(u => u.RealName, f => f.Name.FullName())
            .RuleFor(u => u.PhoneNumber, f => f.PickRandom(TestConstants.ValidPhoneNumbers))
            .RuleFor(u => u.Role, f => f.PickRandom<UserRole>())
            .RuleFor(u => u.Password, TestConstants.DefaultTestPassword);

        /// <summary>
        /// 用户更新DTO生成器
        /// </summary>
        public Faker<UserUpdateDto> UserUpdateDtoFaker => new Faker<UserUpdateDto>(Locale)
            .RuleFor(u => u.Id, f => f.Random.Guid())
            .RuleFor(u => u.RealName, f => f.Name.FullName())
            .RuleFor(u => u.PhoneNumber, f => f.PickRandom(TestConstants.ValidPhoneNumbers))
            .RuleFor(u => u.Role, f => f.PickRandom<UserRole>());

        #endregion

        #region 患者相关数据工厂

        /// <summary>
        /// 患者实体生成器
        /// </summary>
        public Faker<Patient> PatientModelFaker => new Faker<Patient>(Locale)
            .RuleFor(p => p.Id, f => f.Random.Guid())
            .RuleFor(p => p.Name, f => f.Name.FullName())
            .RuleFor(p => p.PhoneNumber, f => f.PickRandom(TestConstants.ValidPhoneNumbers))
            .RuleFor(p => p.Gender, f => f.PickRandom<Gender>())
            .RuleFor(p => p.BirthDate, f => f.Date.Between(DateTime.Now.AddYears(-80), DateTime.Now.AddYears(-18)))
            .RuleFor(p => p.IdNumber, f => f.PickRandom(TestConstants.ValidIdCardNumbers))
            .RuleFor(p => p.Address, f => f.Address.FullAddress())
            .RuleFor(p => p.Status, f => f.PickRandom<CommonStatus>())
            .RuleFor(p => p.CreatedAt, f => f.Date.Between(TestConstants.TestBaseDateTime, DateTime.UtcNow))
            .RuleFor(p => p.PinYinCode, (f, p) => GeneratePinyinCode(p.Name))
            .FinishWith((f, p) =>
            {
                p.UpdateTime = p.CreatedAt;
            });

        /// <summary>
        /// 患者创建DTO生成器
        /// </summary>
        public Faker<PatientCreateDto> PatientCreateDtoFaker => new Faker<PatientCreateDto>(Locale)
            .RuleFor(p => p.Name, f => f.Name.FullName())
            .RuleFor(p => p.PhoneNumber, f => f.PickRandom(TestConstants.ValidPhoneNumbers))
            .RuleFor(p => p.Gender, f => f.PickRandom<Gender>())
            .RuleFor(p => p.BirthDate, f => f.Date.Between(DateTime.Now.AddYears(-80), DateTime.Now.AddYears(-18)))
            .RuleFor(p => p.IdNumber, f => f.PickRandom(TestConstants.ValidIdCardNumbers))
            .RuleFor(p => p.Address, f => f.Address.FullAddress());

        #endregion

        #region 中药材相关数据工厂

        /// <summary>
        /// 中药材实体生成器
        /// </summary>
        public Faker<Herb> HerbModelFaker => new Faker<Herb>(Locale)
            .RuleFor(h => h.Id, f => f.Random.Guid())
            .RuleFor(h => h.Name, f => f.PickRandom(TestConstants.TestHerbNames))
            .RuleFor(h => h.Price, f => f.Random.Decimal(1, 500))
            .RuleFor(h => h.Unit, f => f.PickRandom("克", "包", "粒", "片", "毫升"))
            .RuleFor(h => h.Status, f => f.PickRandom<CommonStatus>())
            .RuleFor(h => h.PinYinCode, (f, h) => GeneratePinyinCode(h.Name));

        /// <summary>
        /// 中药材创建DTO生成器
        /// </summary>
        public Faker<HerbCreateDto> HerbCreateDtoFaker => new Faker<HerbCreateDto>(Locale)
            .RuleFor(h => h.Name, f => f.PickRandom(TestConstants.TestHerbNames))
            .RuleFor(h => h.Price, f => f.Random.Decimal(1, 500))
            .RuleFor(h => h.Stock, f => f.Random.Int(0, 1000))
            .RuleFor(h => h.Unit, f => f.PickRandom("克", "包", "粒", "片", "毫升"));

        #endregion

        #region 医疗案例相关数据工厂

        /// <summary>
        /// 医疗案例实体生成器
        /// </summary>
        public Faker<MedicalCase> MedicalCaseModelFaker => new Faker<MedicalCase>(Locale)
            .RuleFor(m => m.Id, f => f.Random.Guid())
            .RuleFor(m => m.PatientId, f => f.Random.Guid())
            .RuleFor(m => m.Status, f => f.PickRandom<MedicalCaseStatus>())
            .RuleFor(m => m.PatientName, f => f.Name.FullName())
            .RuleFor(m => m.DoctorId, f => f.Random.Guid())
            .RuleFor(m => m.DoctorName, f => f.Name.FullName())
            .RuleFor(m => m.ConsultationDate, f => f.Date.Recent(30))
            .RuleFor(m => m.Remark, f => f.Lorem.Sentence(3, 8));

        /// <summary>
        /// 医疗案例创建DTO生成器
        /// </summary>
        public Faker<MedicalCaseCreateDto> MedicalCaseCreateDtoFaker => new Faker<MedicalCaseCreateDto>(Locale)
            .RuleFor(m => m.PatientId, f => f.Random.Guid())
            .RuleFor(m => m.DoctorId, f => f.Random.Guid())
            .RuleFor(m => m.DiagnosisSummary, f => f.Lorem.Sentence(3, 8))
            .RuleFor(m => m.Remark, f => f.Lorem.Sentence(3, 8));

        #endregion

        #region 看诊记录相关数据工厂

        /// <summary>
        /// 看诊记录实体生成器
        /// </summary>
        public Faker<Consultation> ConsultationModelFaker => new Faker<Consultation>(Locale)
            .RuleFor(c => c.Id, f => f.Random.Guid())
            .RuleFor(c => c.MedicalCaseId, f => f.Random.Guid())
            .RuleFor(c => c.PatientId, f => f.Random.Guid())
            .RuleFor(c => c.UserId, f => f.Random.Guid())
            .RuleFor(c => c.ChiefComplaint, f => f.Lorem.Sentence(3, 8))
            .RuleFor(c => c.PresentIllness, f => f.Lorem.Paragraph(1))
            .RuleFor(c => c.TCMDiagnosis, f => f.Lorem.Sentence(5, 10))
            .RuleFor(c => c.TreatmentPrinciple, f => f.Lorem.Paragraph(1))
            .RuleFor(c => c.Inspection, f => f.Lorem.Sentence(3, 6))
            .RuleFor(c => c.AuscultationOlfaction, f => f.Lorem.Sentence(3, 6))
            .RuleFor(c => c.Inquiry, f => f.Lorem.Sentence(5, 10))
            .RuleFor(c => c.Palpation, f => f.Lorem.Sentence(3, 6))
            .RuleFor(c => c.Status, f => f.PickRandom<CommonStatus>());

        /// <summary>
        /// 看诊记录创建DTO生成器
        /// </summary>
        public Faker<ConsultationCreateDto> ConsultationCreateDtoFaker => new Faker<ConsultationCreateDto>(Locale)
            .RuleFor(c => c.MedicalCaseId, f => f.Random.Guid())
            .RuleFor(c => c.PatientId, f => f.Random.Guid())
            .RuleFor(c => c.DoctorId, f => f.Random.Guid())
            .RuleFor(c => c.ChiefComplaint, f => f.Lorem.Sentence(3, 8))
            .RuleFor(c => c.PresentIllness, f => f.Lorem.Paragraph(1));

        #endregion

        #region 处方相关数据工厂

        /// <summary>
        /// 处方实体生成器
        /// </summary>
        public Faker<Prescription> PrescriptionModelFaker => new Faker<Prescription>(Locale)
            .RuleFor(p => p.Id, f => f.Random.Guid())
            .RuleFor(p => p.MedicalCaseId, f => f.Random.Guid())
            .RuleFor(p => p.PatientId, f => f.Random.Guid())
            .RuleFor(p => p.UserId, f => f.Random.Guid())
            .RuleFor(p => p.Indication, f => f.Lorem.Sentence(5, 10))
            .RuleFor(p => p.DosageCount, f => f.Random.Int(3, 15))
            .RuleFor(p => p.Discount, f => f.Random.Decimal(0.8m, 1.0m))
            .RuleFor(p => p.Advice, f => f.Lorem.Sentence(5, 10))
            .RuleFor(p => p.FormulaSource, f => f.PickRandom("四君子汤", "六君子汤", "补中益气汤"))
            .RuleFor(p => p.Status, f => f.PickRandom<PrescriptionStatus>())
            .RuleFor(p => p.Remark, f => f.Lorem.Sentence(3, 8))
;

        #endregion

        #region 验方相关数据工厂

        /// <summary>
        /// 验方实体生成器
        /// </summary>
        public Faker<Formula> FormulaModelFaker => new Faker<Formula>(Locale)
            .RuleFor(f => f.Id, f => f.Random.Guid())
            .RuleFor(f => f.Name, f => f.PickRandom(TestConstants.TestFormulaNames))
            .RuleFor(f => f.Effect, f => f.Lorem.Paragraph(1))
            .RuleFor(f => f.Usage, f => f.Lorem.Sentence(5, 10))
            .RuleFor(f => f.Property, f => f.Lorem.Sentence(3, 6))
            .RuleFor(f => f.Status, f => f.PickRandom<CommonStatus>())
            .RuleFor(f => f.IsShared, f => f.Random.Bool())
            .RuleFor(f => f.Remark, f => f.Lorem.Sentence(3, 8))
;

        #endregion

        #region 特殊场景数据生成器

        /// <summary>
        /// 生成关联的医疗记录数据
        /// </summary>
        public class RelatedMedicalRecords
        {
            public Patient Patient { get; set; }
            public MedicalCase MedicalCase { get; set; }
            public Consultation Consultation { get; set; }
            public Prescription? Prescription { get; set; }
        }

        /// <summary>
        /// 生成完整的医疗记录（患者 -> 医案 -> 看诊 -> 处方）
        /// </summary>
        public RelatedMedicalRecords GenerateCompleteUserMedicalRecords(bool includePrescription = true)
        {
            var patient = PatientModelFaker.Generate();
            var medicalCase = MedicalCaseModelFaker.Generate();
            medicalCase.PatientId = patient.Id;
            var consultation = ConsultationModelFaker.Generate();
            consultation.MedicalCaseId = medicalCase.Id;
            var prescription = includePrescription 
                ? PrescriptionModelFaker.Generate()
                : null;

            return new RelatedMedicalRecords
            {
                Patient = patient,
                MedicalCase = medicalCase,
                Consultation = consultation,
                Prescription = prescription
            };
        }

        /// <summary>
        /// 生成指定医生的患者数据
        /// </summary>
        public List<Patient> GeneratePatientsForDoctor(Guid doctorId, string doctorName, int count = 5)
        {
            return PatientModelFaker
                .RuleFor(p => p.CreatedBy, doctorId)
                .Generate(count);
        }

        /// <summary>
        /// 生成用户名冲突测试数据
        /// </summary>
        public List<UserCreateDto> GenerateUsernameConflictScenarios(string baseUsername = "testuser")
        {
            var result = new List<UserCreateDto>();
            
            var user1 = UserCreateDtoFaker.Generate();
            user1.Username = baseUsername;
            result.Add(user1);
            
            var user2 = UserCreateDtoFaker.Generate();
            user2.Username = baseUsername.ToUpper();
            result.Add(user2);
            
            var user3 = UserCreateDtoFaker.Generate();
            user3.Username = baseUsername.ToLower();
            result.Add(user3);
            
            var user4 = UserCreateDtoFaker.Generate();
            user4.Username = $" {baseUsername} ";
            result.Add(user4);
            
            return result;
        }

        #endregion

        #region 批量数据生成器

        /// <summary>
        /// 生成大数据集用于性能测试
        /// </summary>
        public List<T> GeneratePerformanceTestDataSet<T>(Faker<T> faker) where T : class
        {
            return faker.Generate(TestConstants.PerformanceTestDataSetSize);
        }

        /// <summary>
        /// 生成分页测试数据
        /// </summary>
        public List<T> GeneratePagedTestData<T>(Faker<T> faker, int totalCount, int pageSize, int currentPage) where T : class
        {
            var allData = faker.Generate(totalCount);
            var skip = (currentPage - 1) * pageSize;
            return allData.Skip(skip).Take(pageSize).ToList();
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 生成拼音码（简化版）
        /// </summary>
        private static string GeneratePinyinCode(string? chinese)
        {
            if (string.IsNullOrEmpty(chinese)) return string.Empty;
            
            // 简化的拼音转换，实际项目可能使用专门的拼音库
            // 这里仅用于测试目的
            return chinese.Length <= 2 ? chinese : chinese.Substring(0, 2);
        }

        #endregion
    }
}