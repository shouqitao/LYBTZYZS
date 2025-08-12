using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using LYBT.Models;
using LYBT.Shared.Models;

namespace LYBT.Tests.Core
{
    /// <summary>
    /// 测试数据构建器 - UltraThink测试框架
    /// </summary>
    public class TestDataBuilder
    {
        private readonly Faker _faker;

        public TestDataBuilder()
        {
            _faker = new Faker("zh_CN");
        }

        #region User Data Builders

        public UserModel BuildUser(Action<UserModel> customize = null)
        {
            var user = new UserModel
            {
                Id = Guid.NewGuid(),
                UserName = _faker.Internet.UserName(),
                RealName = _faker.Name.FullName(),
                Email = _faker.Internet.Email(),
                PhoneNumber = _faker.Phone.PhoneNumber("1##########"),
                PasswordHash = "AQAAAAEAACcQAAAAEGwrB7Ri8YXw4qPHiXoTJQKoNccNRjvMFWuNi4W5YYp3DhIRRtxb0AHjD+WnzGLCmw==",
                Role = UserRole.Doctor,
                IsActive = true,
                CreatedAt = DateTime.Now.AddDays(-_faker.Random.Int(1, 365)),
                UpdatedAt = DateTime.Now
            };

            customize?.Invoke(user);
            return user;
        }

        public List<UserModel> BuildUsers(int count, Action<UserModel> customize = null)
        {
            return Enumerable.Range(0, count)
                .Select(_ => BuildUser(customize))
                .ToList();
        }

        #endregion

        #region Patient Data Builders

        public PatientModel BuildPatient(Action<PatientModel> customize = null)
        {
            var patient = new PatientModel
            {
                Id = Guid.NewGuid(),
                Name = _faker.Name.FullName(),
                Gender = _faker.PickRandom(Gender.Male, Gender.Female),
                BirthDate = _faker.Date.Past(60, DateTime.Now.AddYears(-18)),
                IdNumber = GenerateIdNumber(),
                PhoneNumber = _faker.Phone.PhoneNumber("1##########"),
                Address = _faker.Address.FullAddress(),
                EmergencyContact = _faker.Name.FullName(),
                EmergencyPhone = _faker.Phone.PhoneNumber("1##########"),
                MedicalHistory = _faker.Lorem.Paragraph(),
                AllergyHistory = _faker.Lorem.Sentence(),
                IsActive = true,
                CreatedAt = DateTime.Now.AddDays(-_faker.Random.Int(1, 365)),
                UpdatedAt = DateTime.Now
            };

            customize?.Invoke(patient);
            return patient;
        }

        public List<PatientModel> BuildPatients(int count, Action<PatientModel> customize = null)
        {
            return Enumerable.Range(0, count)
                .Select(_ => BuildPatient(customize))
                .ToList();
        }

        #endregion

        #region Herb Data Builders

        public HerbModel BuildHerb(Action<HerbModel> customize = null)
        {
            var herbNames = new[] { "人参", "黄芪", "当归", "川芎", "白芍", "熟地黄", "甘草", "陈皮", "半夏", "茯苓" };
            
            var herb = new HerbModel
            {
                Id = Guid.NewGuid(),
                Name = _faker.PickRandom(herbNames),
                PinYin = "RenShen", // 简化处理
                Category = _faker.PickRandom("补益药", "解表药", "清热药", "理气药"),
                Nature = _faker.PickRandom("寒", "热", "温", "凉", "平"),
                Flavor = _faker.PickRandom("甘", "苦", "辛", "酸", "咸"),
                Meridian = _faker.PickRandom("肝", "心", "脾", "肺", "肾"),
                Efficacy = "补气养血，健脾益肺",
                Usage = "3-9g，水煎服",
                Contraindication = "实证、热证慎用",
                Price = _faker.Random.Decimal(10, 500),
                Unit = "g",
                Stock = _faker.Random.Decimal(0, 10000),
                MinStock = 100,
                IsActive = true,
                CreatedAt = DateTime.Now.AddDays(-_faker.Random.Int(1, 365)),
                UpdatedAt = DateTime.Now
            };

            customize?.Invoke(herb);
            return herb;
        }

        public List<HerbModel> BuildHerbs(int count, Action<HerbModel> customize = null)
        {
            return Enumerable.Range(0, count)
                .Select(_ => BuildHerb(customize))
                .ToList();
        }

        #endregion

        #region Prescription Data Builders

        public PrescriptionModel BuildPrescription(Guid? patientId = null, Guid? consultationId = null)
        {
            var prescription = new PrescriptionModel
            {
                Id = Guid.NewGuid(),
                PatientId = patientId ?? Guid.NewGuid(),
                ConsultationId = consultationId ?? Guid.NewGuid(),
                PrescriptionNo = $"RX{DateTime.Now:yyyyMMdd}{_faker.Random.Int(1000, 9999)}",
                Type = PrescriptionType.Decoction,
                Dosage = 7,
                DosageUnit = "剂",
                Usage = "每日1剂，水煎服，分2次温服",
                TotalAmount = _faker.Random.Decimal(50, 500),
                Status = PrescriptionStatus.Issued,
                IssuedDate = DateTime.Now,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            // 添加处方项
            prescription.Items = BuildPrescriptionItems(prescription.Id, _faker.Random.Int(3, 10));

            return prescription;
        }

        public List<PrescriptionItemModel> BuildPrescriptionItems(Guid prescriptionId, int count)
        {
            var herbs = BuildHerbs(count);
            return herbs.Select((herb, index) => new PrescriptionItemModel
            {
                Id = Guid.NewGuid(),
                PrescriptionId = prescriptionId,
                HerbId = herb.Id,
                HerbName = herb.Name,
                Dosage = _faker.Random.Decimal(3, 30),
                Unit = herb.Unit,
                Price = herb.Price,
                Subtotal = herb.Price * _faker.Random.Decimal(3, 30),
                ProcessingMethod = _faker.PickRandom("生", "炒", "蜜炙", "酒炙", "醋炙"),
                Notes = _faker.Lorem.Sentence(),
                SortOrder = index,
                CreatedAt = DateTime.Now
            }).ToList();
        }

        #endregion

        #region Formula Data Builders

        public FormulaModel BuildFormula(Action<FormulaModel> customize = null)
        {
            var formulaNames = new[] { "四君子汤", "四物汤", "补中益气汤", "六味地黄丸", "逍遥散" };
            
            var formula = new FormulaModel
            {
                Id = Guid.NewGuid(),
                Name = _faker.PickRandom(formulaNames),
                PinYin = "SiJunZiTang",
                Source = "《太平惠民和剂局方》",
                Composition = "人参、白术、茯苓、甘草",
                Dosage = "各等分，研末，每服6-9g",
                Efficacy = "益气健脾",
                Indications = "脾胃气虚证",
                Contraindications = "实证慎用",
                ModernApplication = "慢性胃炎、消化不良等",
                Type = FormulaType.Classical,
                Category = "补益剂",
                IsTemplate = true,
                IsActive = true,
                CreatedAt = DateTime.Now.AddDays(-_faker.Random.Int(1, 365)),
                UpdatedAt = DateTime.Now
            };

            customize?.Invoke(formula);
            return formula;
        }

        #endregion

        #region Helper Methods

        private string GenerateIdNumber()
        {
            var year = _faker.Random.Int(1950, 2000);
            var month = _faker.Random.Int(1, 12).ToString().PadLeft(2, '0');
            var day = _faker.Random.Int(1, 28).ToString().PadLeft(2, '0');
            var seq = _faker.Random.Int(100, 999);
            var gender = _faker.Random.Int(0, 9);
            
            return $"110101{year}{month}{day}{seq}{gender}";
        }

        #endregion
    }

    /// <summary>
    /// 测试数据种子
    /// </summary>
    public static class TestDataSeed
    {
        public static readonly Guid AdminUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid DoctorUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid PatientId1 = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid HerbId1 = Guid.Parse("44444444-4444-4444-4444-444444444444");
        public static readonly Guid FormulaId1 = Guid.Parse("55555555-5555-5555-5555-555555555555");
    }
}