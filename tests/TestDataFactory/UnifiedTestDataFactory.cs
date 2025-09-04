using System;
using System.Collections.Generic;
using Bogus;
using LYBT.Entities.Users;
using LYBT.Entities.Patients;
using LYBT.Entities.Herbs;
using LYBT.Entities.Consultations;
using LYBT.Entities.Prescriptions;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Formulas;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCases;
using LYBT.Shared.Models.Contracts.Consultations;
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
            Randomizer.Seed = _randomizer;
        }

        #region 用户相关数据工厂

        /// <summary>
        /// 用户实体生成器
        /// </summary>
        public Faker<UserModel> UserModelFaker => new Faker<UserModel>(Locale)
            .RuleFor(u => u.Id, f => f.Random.Guid())
            .RuleFor(u => u.Username, f => $"{TestConstants.TestUsernamePrefix}{f.Internet.UserName()}")
            .RuleFor(u => u.RealName, f => f.Name.FullName())
            .RuleFor(u => u.PhoneNumber, f => f.PickRandom(TestConstants.ValidPhoneNumbers))
            .RuleFor(u => u.Role, f => f.PickRandom<UserRole>())
            .RuleFor(u => u.Status, f => f.PickRandom<CommonStatus>())
            .RuleFor(u => u.PasswordHash, f => BCrypt.Net.BCrypt.HashPassword(TestConstants.DefaultTestPassword))
            .RuleFor(u => u.CreateTime, f => f.Date.Between(TestConstants.TestBaseDateTime, DateTime.UtcNow))
            .RuleFor(u => u.CreatedBy, f => f.Random.Guid())
            .RuleFor(u => u.CreatedByName, f => f.Name.FullName())
            .RuleFor(u => u.PinYinCode, (f, u) => GeneratePinyinCode(u.RealName))
            .FinishWith((f, u) =>
            {
                u.UpdateTime = u.CreateTime;
                u.UpdatedBy = u.CreatedBy;
                u.UpdatedByName = u.CreatedByName;
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
        public Faker<PatientModel> PatientModelFaker => new Faker<PatientModel>(Locale)
            .RuleFor(p => p.Id, f => f.Random.Guid())
            .RuleFor(p => p.Name, f => f.Name.FullName())
            .RuleFor(p => p.PhoneNumber, f => f.PickRandom(TestConstants.ValidPhoneNumbers))
            .RuleFor(p => p.Gender, f => f.PickRandom<Gender>())
            .RuleFor(p => p.BirthDate, f => f.Date.Between(DateTime.Now.AddYears(-80), DateTime.Now.AddYears(-18)))
            .RuleFor(p => p.IdCardNumber, f => f.PickRandom(TestConstants.ValidIdCardNumbers))
            .RuleFor(p => p.Address, f => f.Address.FullAddress())
            .RuleFor(p => p.Status, f => f.PickRandom<CommonStatus>())
            .RuleFor(p => p.CreateTime, f => f.Date.Between(TestConstants.TestBaseDateTime, DateTime.UtcNow))
            .RuleFor(p => p.CreatedBy, f => f.Random.Guid())
            .RuleFor(p => p.CreatedByName, f => f.Name.FullName())
            .RuleFor(p => p.PinYinCode, (f, p) => GeneratePinyinCode(p.Name))
            .FinishWith((f, p) =>
            {
                p.UpdateTime = p.CreateTime;
                p.UpdatedBy = p.CreatedBy;
                p.UpdatedByName = p.CreatedByName;
            });

        /// <summary>
        /// 患者创建DTO生成器
        /// </summary>
        public Faker<PatientCreateDto> PatientCreateDtoFaker => new Faker<PatientCreateDto>(Locale)
            .RuleFor(p => p.Name, f => f.Name.FullName())
            .RuleFor(p => p.PhoneNumber, f => f.PickRandom(TestConstants.ValidPhoneNumbers))
            .RuleFor(p => p.Gender, f => f.PickRandom<Gender>())
            .RuleFor(p => p.BirthDate, f => f.Date.Between(DateTime.Now.AddYears(-80), DateTime.Now.AddYears(-18)))
            .RuleFor(p => p.IdCardNumber, f => f.PickRandom(TestConstants.ValidIdCardNumbers))
            .RuleFor(p => p.Address, f => f.Address.FullAddress());

        #endregion

        #region 中药材相关数据工厂

        /// <summary>
        /// 中药材实体生成器
        /// </summary>
        public Faker<HerbModel> HerbModelFaker => new Faker<HerbModel>(Locale)
            .RuleFor(h => h.Id, f => f.Random.Guid())
            .RuleFor(h => h.Name, f => f.PickRandom(TestConstants.TestHerbNames))
            .RuleFor(h => h.Price, f => f.Random.Decimal(1, 500))
            .RuleFor(h => h.Stock, f => f.Random.Int(0, 1000))
            .RuleFor(h => h.Unit, f => f.PickRandom("克", "包", "粒", "片", "毫升"))
            .RuleFor(h => h.Status, f => f.PickRandom<CommonStatus>())
            .RuleFor(h => h.CreateTime, f => f.Date.Between(TestConstants.TestBaseDateTime, DateTime.UtcNow))
            .RuleFor(h => h.CreatedBy, f => f.Random.Guid())
            .RuleFor(h => h.CreatedByName, f => f.Name.FullName())
            .RuleFor(h => h.PinYinCode, (f, h) => GeneratePinyinCode(h.Name))
            .FinishWith((f, h) =>
            {
                h.UpdateTime = h.CreateTime;
                h.UpdatedBy = h.CreatedBy;
                h.UpdatedByName = h.CreatedByName;
            });

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
        public Faker<MedicalCaseModel> MedicalCaseModelFaker => new Faker<MedicalCaseModel>(Locale)
            .RuleFor(m => m.Id, f => f.Random.Guid())
            .RuleFor(m => m.PatientId, f => f.Random.Guid())
            .RuleFor(m => m.Status, f => f.PickRandom<MedicalCaseStatus>())
            .RuleFor(m => m.ChiefComplaint, f => f.Lorem.Sentence(5, 10))
            .RuleFor(m => m.PresentIllness, f => f.Lorem.Paragraph(2))
            .RuleFor(m => m.CreateTime, f => f.Date.Between(TestConstants.TestBaseDateTime, DateTime.UtcNow))
            .RuleFor(m => m.CreatedBy, f => f.Random.Guid())
            .RuleFor(m => m.CreatedByName, f => f.Name.FullName())
            .FinishWith((f, m) =>
            {
                m.UpdateTime = m.CreateTime;
                m.UpdatedBy = m.CreatedBy;
                m.UpdatedByName = m.CreatedByName;
            });

        /// <summary>
        /// 医疗案例创建DTO生成器
        /// </summary>
        public Faker<MedicalCaseCreateDto> MedicalCaseCreateDtoFaker => new Faker<MedicalCaseCreateDto>(Locale)
            .RuleFor(m => m.PatientId, f => f.Random.Guid())
            .RuleFor(m => m.ChiefComplaint, f => f.Lorem.Sentence(5, 10))
            .RuleFor(m => m.PresentIllness, f => f.Lorem.Paragraph(2));

        #endregion

        #region 看诊记录相关数据工厂

        /// <summary>
        /// 看诊记录实体生成器
        /// </summary>
        public Faker<ConsultationModel> ConsultationModelFaker => new Faker<ConsultationModel>(Locale)
            .RuleFor(c => c.Id, f => f.Random.Guid())
            .RuleFor(c => c.MedicalCaseId, f => f.Random.Guid())
            .RuleFor(c => c.Symptoms, f => f.Lorem.Sentence(3, 8))
            .RuleFor(c => c.Diagnosis, f => f.Lorem.Sentence(5, 10))
            .RuleFor(c => c.TreatmentPlan, f => f.Lorem.Paragraph(1))
            .RuleFor(c => c.TCM_Inspection, f => f.Lorem.Sentence(3, 6))
            .RuleFor(c => c.TCM_Auscultation, f => f.Lorem.Sentence(3, 6))
            .RuleFor(c => c.TCM_Interrogation, f => f.Lorem.Sentence(5, 10))
            .RuleFor(c => c.TCM_Palpation, f => f.Lorem.Sentence(3, 6))
            .RuleFor(c => c.CreateTime, f => f.Date.Between(TestConstants.TestBaseDateTime, DateTime.UtcNow))
            .RuleFor(c => c.CreatedBy, f => f.Random.Guid())
            .RuleFor(c => c.CreatedByName, f => f.Name.FullName())
            .FinishWith((f, c) =>
            {
                c.UpdateTime = c.CreateTime;
                c.UpdatedBy = c.CreatedBy;
                c.UpdatedByName = c.CreatedByName;
            });

        /// <summary>
        /// 看诊记录创建DTO生成器
        /// </summary>
        public Faker<ConsultationCreateDto> ConsultationCreateDtoFaker => new Faker<ConsultationCreateDto>(Locale)
            .RuleFor(c => c.MedicalCaseId, f => f.Random.Guid())
            .RuleFor(c => c.Symptoms, f => f.Lorem.Sentence(3, 8))
            .RuleFor(c => c.Diagnosis, f => f.Lorem.Sentence(5, 10))
            .RuleFor(c => c.TreatmentPlan, f => f.Lorem.Paragraph(1))
            .RuleFor(c => c.TCM_Inspection, f => f.Lorem.Sentence(3, 6))
            .RuleFor(c => c.TCM_Auscultation, f => f.Lorem.Sentence(3, 6))
            .RuleFor(c => c.TCM_Interrogation, f => f.Lorem.Sentence(5, 10))
            .RuleFor(c => c.TCM_Palpation, f => f.Lorem.Sentence(3, 6));

        #endregion

        #region 处方相关数据工厂

        /// <summary>
        /// 处方实体生成器
        /// </summary>
        public Faker<PrescriptionModel> PrescriptionModelFaker => new Faker<PrescriptionModel>(Locale)
            .RuleFor(p => p.Id, f => f.Random.Guid())
            .RuleFor(p => p.ConsultationId, f => f.Random.Guid())
            .RuleFor(p => p.PrescriptionName, f => f.Commerce.ProductName())
            .RuleFor(p => p.TotalPrice, f => f.Random.Decimal(50, 500))
            .RuleFor(p => p.Instructions, f => f.Lorem.Sentence(5, 10))
            .RuleFor(p => p.CreateTime, f => f.Date.Between(TestConstants.TestBaseDateTime, DateTime.UtcNow))
            .RuleFor(p => p.CreatedBy, f => f.Random.Guid())
            .RuleFor(p => p.CreatedByName, f => f.Name.FullName())
            .FinishWith((f, p) =>
            {
                p.UpdateTime = p.CreateTime;
                p.UpdatedBy = p.CreatedBy;
                p.UpdatedByName = p.CreatedByName;
            });

        #endregion

        #region 验方相关数据工厂

        /// <summary>
        /// 验方实体生成器
        /// </summary>
        public Faker<FormulaModel> FormulaModelFaker => new Faker<FormulaModel>(Locale)
            .RuleFor(f => f.Id, f => f.Random.Guid())
            .RuleFor(f => f.Name, f => f.PickRandom(TestConstants.TestFormulaNames))
            .RuleFor(f => f.Description, f => f.Lorem.Paragraph(1))
            .RuleFor(f => f.Indications, f => f.Lorem.Sentence(5, 10))
            .RuleFor(f => f.Status, f => f.PickRandom<CommonStatus>())
            .RuleFor(f => f.CreateTime, f => f.Date.Between(TestConstants.TestBaseDateTime, DateTime.UtcNow))
            .RuleFor(f => f.CreatedBy, f => f.Random.Guid())
            .RuleFor(f => f.CreatedByName, f => f.Name.FullName())
            .FinishWith((f, formula) =>
            {
                formula.UpdateTime = formula.CreateTime;
                formula.UpdatedBy = formula.CreatedBy;
                formula.UpdatedByName = formula.CreatedByName;
            });

        #endregion

        #region 特殊场景数据生成器

        /// <summary>
        /// 生成关联的医疗记录数据
        /// </summary>
        public class RelatedMedicalRecords
        {
            public PatientModel Patient { get; set; }
            public MedicalCaseModel MedicalCase { get; set; }
            public ConsultationModel Consultation { get; set; }
            public PrescriptionModel? Prescription { get; set; }
        }

        /// <summary>
        /// 生成完整的医疗记录（患者 -> 医案 -> 看诊 -> 处方）
        /// </summary>
        public RelatedMedicalRecords GenerateCompleteUserMedicalRecords(bool includePrescription = true)
        {
            var patient = PatientModelFaker.Generate();
            var medicalCase = MedicalCaseModelFaker.Generate() with { PatientId = patient.Id };
            var consultation = ConsultationModelFaker.Generate() with { MedicalCaseId = medicalCase.Id };
            var prescription = includePrescription 
                ? PrescriptionModelFaker.Generate() with { ConsultationId = consultation.Id }
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
        public List<PatientModel> GeneratePatientsForDoctor(Guid doctorId, string doctorName, int count = 5)
        {
            return PatientModelFaker
                .RuleFor(p => p.CreatedBy, doctorId)
                .RuleFor(p => p.CreatedByName, doctorName)
                .Generate(count);
        }

        /// <summary>
        /// 生成用户名冲突测试数据
        /// </summary>
        public List<UserCreateDto> GenerateUsernameConflictScenarios(string baseUsername = "testuser")
        {
            return new List<UserCreateDto>
            {
                UserCreateDtoFaker.Generate() with { Username = baseUsername },
                UserCreateDtoFaker.Generate() with { Username = baseUsername.ToUpper() },
                UserCreateDtoFaker.Generate() with { Username = baseUsername.ToLower() },
                UserCreateDtoFaker.Generate() with { Username = $" {baseUsername} " }
            };
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