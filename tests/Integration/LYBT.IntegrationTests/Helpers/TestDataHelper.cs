using System;
using System.Collections.Generic;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;

namespace LYBT.IntegrationTests.Helpers
{
    /// <summary>
    /// 测试数据帮助类
    /// </summary>
    public static class TestDataHelper
    {
        private static readonly Random _random = new Random();

        #region 患者测试数据

        /// <summary>
        /// 创建测试患者数据
        /// </summary>
        public static PatientCreateDto CreateTestPatient(string? nameSuffix = null)
        {
            var suffix = nameSuffix ?? GenerateRandomString(4);
            return new PatientCreateDto
            {
                Name = $"测试患者_{suffix}",
                Gender = _random.Next(2) == 0 ? Gender.Male : Gender.Female,
                Age = _random.Next(20, 80),
                PhoneNumber = GeneratePhoneNumber(),
                IdCard = GenerateIdCard(),
                Address = "测试地址",
                EmergencyContact = $"紧急联系人_{suffix}",
                EmergencyPhone = GeneratePhoneNumber(),
                MedicalHistory = "既往体健",
                AllergyHistory = "无药物过敏史"
            };
        }

        /// <summary>
        /// 生成多个测试患者
        /// </summary>
        public static List<PatientCreateDto> CreateTestPatients(int count)
        {
            var patients = new List<PatientCreateDto>();
            for (int i = 0; i < count; i++)
            {
                patients.Add(CreateTestPatient($"{i:D3}"));
            }
            return patients;
        }

        #endregion

        #region 用户测试数据

        /// <summary>
        /// 创建测试医生数据
        /// </summary>
        public static UserCreateDto CreateTestDoctor(string? nameSuffix = null)
        {
            var suffix = nameSuffix ?? GenerateRandomString(4);
            return new UserCreateDto
            {
                Username = $"doctor_{suffix}",
                Password = "Test@123456",
                RealName = $"测试医生_{suffix}",
                PhoneNumber = GeneratePhoneNumber(),
                Department = "中医内科",
                Title = "主治医师"
            };
        }

        #endregion

        #region 药材测试数据

        /// <summary>
        /// 创建测试药材数据
        /// </summary>
        public static List<HerbCreateDto> CreateTestHerbs()
        {
            return new List<HerbCreateDto>
            {
                new HerbCreateDto
                {
                    Name = "黄芪",
                    PinYinCode = "HQ",
                    Category = "补虚药",
                    Efficacy = "补气升阳，固表止汗",
                    Unit = "g",
                    UnitPrice = 0.5m,
                    Stock = 1000,
                    MinStock = 100
                },
                new HerbCreateDto
                {
                    Name = "当归",
                    PinYinCode = "DG",
                    Category = "补血药",
                    Efficacy = "补血活血，调经止痛",
                    Unit = "g",
                    UnitPrice = 0.8m,
                    Stock = 800,
                    MinStock = 80
                },
                new HerbCreateDto
                {
                    Name = "白术",
                    PinYinCode = "BZ",
                    Category = "补虚药",
                    Efficacy = "健脾益气，燥湿利水",
                    Unit = "g",
                    UnitPrice = 0.6m,
                    Stock = 600,
                    MinStock = 60
                },
                new HerbCreateDto
                {
                    Name = "茯苓",
                    PinYinCode = "FL",
                    Category = "利水渗湿药",
                    Efficacy = "利水渗湿，健脾宁心",
                    Unit = "g",
                    UnitPrice = 0.4m,
                    Stock = 1200,
                    MinStock = 120
                },
                new HerbCreateDto
                {
                    Name = "甘草",
                    PinYinCode = "GC",
                    Category = "补虚药",
                    Efficacy = "补脾益气，清热解毒",
                    Unit = "g",
                    UnitPrice = 0.3m,
                    Stock = 1500,
                    MinStock = 150
                }
            };
        }

        #endregion

        #region 看诊测试数据

        /// <summary>
        /// 创建测试四诊信息
        /// </summary>
        public static ConsultationUpdateDto CreateTestTCMDiagnosis()
        {
            var symptoms = new[]
            {
                "疲劳乏力", "食欲不振", "失眠多梦", "头晕目眩", "腰膝酸软"
            };

            var tongueDescriptions = new[]
            {
                "舌质淡，苔薄白", "舌质红，苔黄腻", "舌质暗，有瘀斑", "舌质胖大，有齿痕"
            };

            var pulseDescriptions = new[]
            {
                "脉细弱", "脉弦滑", "脉沉迟", "脉浮数"
            };

            var tcmDiagnoses = new[]
            {
                "气虚证", "血虚证", "阳虚证", "阴虚证", "气滞血瘀证"
            };

            return new ConsultationUpdateDto
            {
                Inspection = $"面色{GetRandomItem(new[] { "偏黄", "苍白", "潮红", "正常" })}，精神{GetRandomItem(new[] { "尚可", "萎靡", "亢奋" })}",
                AuscultationOlfaction = $"语音{GetRandomItem(new[] { "正常", "低微", "洪亮" })}，口气{GetRandomItem(new[] { "正常", "有异味", "清新" })}",
                Inquiry = $"主诉：{GetRandomItem(symptoms)}。现病史：症状持续约{_random.Next(1, 12)}月，{GetRandomItem(new[] { "逐渐加重", "时轻时重", "持续存在" })}",
                Palpation = $"腹部{GetRandomItem(new[] { "柔软", "略硬", "胀满" })}，{GetRandomItem(new[] { "无压痛", "轻压痛", "明显压痛" })}",
                TongueInspection = GetRandomItem(tongueDescriptions),
                PulseCondition = GetRandomItem(pulseDescriptions),
                TCMDiagnosis = GetRandomItem(tcmDiagnoses),
                Diagnosis = $"中医诊断：{GetRandomItem(tcmDiagnoses)}",
                Remark = "患者依从性良好，建议规律服药"
            };
        }

        #endregion

        #region 验证数据

        /// <summary>
        /// 创建无效的患者数据（用于测试验证）
        /// </summary>
        public static PatientCreateDto CreateInvalidPatient()
        {
            return new PatientCreateDto
            {
                Name = "", // 空名字
                Gender = (Gender)99, // 无效性别
                Age = -1, // 无效年龄
                PhoneNumber = "123", // 无效电话
                IdCard = "invalid", // 无效身份证
                Address = null
            };
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 生成随机字符串
        /// </summary>
        private static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = chars[_random.Next(chars.Length)];
            }
            return new string(result);
        }

        /// <summary>
        /// 生成随机手机号
        /// </summary>
        private static string GeneratePhoneNumber()
        {
            var prefixes = new[] { "138", "139", "186", "187", "188" };
            var prefix = prefixes[_random.Next(prefixes.Length)];
            var suffix = _random.Next(10000000, 99999999).ToString();
            return $"{prefix}{suffix}";
        }

        /// <summary>
        /// 生成随机身份证号（仅用于测试）
        /// </summary>
        private static string GenerateIdCard()
        {
            // 简化的身份证号生成（不符合真实规则，仅用于测试）
            var areaCode = "110101"; // 北京东城区
            var year = _random.Next(1950, 2000);
            var month = _random.Next(1, 13).ToString("D2");
            var day = _random.Next(1, 29).ToString("D2");
            var sequence = _random.Next(1000, 9999);
            return $"{areaCode}{year}{month}{day}{sequence}";
        }

        /// <summary>
        /// 从数组中随机获取一个元素
        /// </summary>
        private static T GetRandomItem<T>(T[] items)
        {
            return items[_random.Next(items.Length)];
        }

        #endregion
    }
}