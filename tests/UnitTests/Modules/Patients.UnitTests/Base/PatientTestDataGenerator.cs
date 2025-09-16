using Bogus;
using LYBT.Entities.Patients;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Patients.Tests.Base
{
    /// <summary>
    /// 患者测试数据生成器
    /// </summary>
    public static class PatientTestDataGenerator
    {
        /// <summary>
        /// 患者数据生成器
        /// </summary>
        public static Faker<PatientModel> PatientGenerator => new Faker<PatientModel>("zh_CN")
            .RuleFor(p => p.Id, f => Guid.NewGuid())
            .RuleFor(p => p.Name, f => f.Name.FullName())
            .RuleFor(p => p.PinYinCode, (f, p) => GetPinyinCode(p.Name))
            .RuleFor(p => p.WuBiCode, f => f.Random.AlphaNumeric(6).ToUpper())
            .RuleFor(p => p.Gender, f => f.PickRandom<Gender>())
            .RuleFor(p => p.Age, f => f.Random.Int(1, 90))
            .RuleFor(p => p.BirthDate, f => f.Date.Past(90, DateTime.Now.AddYears(-1)))
            .RuleFor(p => p.IdType, f => "身份证")
            .RuleFor(p => p.IdNumber, f => GenerateIdNumber())
            .RuleFor(p => p.PhoneNumber, f => f.Phone.PhoneNumber("1##########"))
            .RuleFor(p => p.Address, f => f.Address.FullAddress())
            .RuleFor(p => p.AllergyHistory, f => f.Lorem.Sentence())
            .RuleFor(p => p.Status, f => f.PickRandom<CommonStatus>())
            .RuleFor(p => p.CreateTime, f => f.Date.Recent(30))
            .RuleFor(p => p.UpdateTime, f => f.Date.Recent(5))
            .RuleFor(p => p.LastVisitTime, f => f.Date.Recent(10))
            .RuleFor(p => p.VisitCount, f => f.Random.Int(0, 20))
            .RuleFor(p => p.DisableReason, f => f.Lorem.Sentence())
            .RuleFor(p => p.CreatedBy, f => Guid.NewGuid())
            .RuleFor(p => p.UpdatedBy, f => Guid.NewGuid());

        /// <summary>
        /// 创建测试患者
        /// </summary>
        public static PatientModel CreateTestPatient(
            string? name = null,
            string? idNumber = null,
            string? phoneNumber = null,
            CommonStatus status = CommonStatus.Enabled)
        {
            var patient = PatientGenerator.Generate();

            if (!string.IsNullOrEmpty(name))
                patient.Name = name;

            if (!string.IsNullOrEmpty(idNumber))
                patient.IdNumber = idNumber;

            if (!string.IsNullOrEmpty(phoneNumber))
                patient.PhoneNumber = phoneNumber;

            patient.Status = status;

            return patient;
        }

        /// <summary>
        /// 批量创建测试患者
        /// </summary>
        public static List<PatientModel> CreateTestPatients(int count, CommonStatus? status = null)
        {
            var generator = PatientGenerator;

            if (status.HasValue)
                generator = generator.RuleFor(p => p.Status, status.Value);

            return generator.Generate(count);
        }

        /// <summary>
        /// 创建启用的测试患者
        /// </summary>
        public static PatientModel CreateEnabledPatient()
        {
            return CreateTestPatient(status: CommonStatus.Enabled);
        }

        /// <summary>
        /// 创建禁用的测试患者
        /// </summary>
        public static PatientModel CreateDisabledPatient()
        {
            return CreateTestPatient(status: CommonStatus.Disabled);
        }

        /// <summary>
        /// 创建具有特定身份证号的患者
        /// </summary>
        public static PatientModel CreatePatientWithIdNumber(string idNumber)
        {
            return CreateTestPatient(idNumber: idNumber);
        }

        /// <summary>
        /// 创建具有特定手机号的患者
        /// </summary>
        public static PatientModel CreatePatientWithPhoneNumber(string phoneNumber)
        {
            return CreateTestPatient(phoneNumber: phoneNumber);
        }

        /// <summary>
        /// 简单的拼音码生成（用于测试）
        /// </summary>
        private static string GetPinyinCode(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "";

            // 简单的拼音首字母生成逻辑（测试用）
            return string.Join("", name.Take(Math.Min(name.Length, 6)).Select(c => char.ToUpper(c)));
        }

        /// <summary>
        /// 生成模拟身份证号（测试用）
        /// </summary>
        private static string GenerateIdNumber()
        {
            var random = new Random();
            var year = random.Next(1950, 2005);
            var month = random.Next(1, 13).ToString("D2");
            var day = random.Next(1, 29).ToString("D2");
            var areaCode = "110101"; // 北京东城区
            var sequence = random.Next(100, 999);
            var last = random.Next(0, 10);

            return $"{areaCode}{year}{month}{day}{sequence}{last}";
        }
    }
}