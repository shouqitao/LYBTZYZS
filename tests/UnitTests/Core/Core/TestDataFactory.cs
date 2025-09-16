using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using LYBT.Entities.Herbs;
using LYBT.Entities.Patients;
using LYBT.Entities.Users;
using LYBT.Entities.Consultations;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Tests.Backend.Core
{
    /// <summary>
    /// 测试数据生成工厂 - 基于Bogus提供一致的测试数据生成
    /// 支持数据驱动测试和边界值测试场景
    /// </summary>
    public class TestDataFactory
    {
        private readonly Randomizer _random;
        
        public TestDataFactory(int? seed = null)
        {
            _random = seed.HasValue ? new Randomizer(seed.Value) : new Randomizer();
        }

        #region 用户相关数据生成

        /// <summary>
        /// 生成用户模型
        /// </summary>
        public Faker<UserModel> UserModelFaker => new Faker<UserModel>("zh_CN")
            .RuleFor(u => u.Id, f => f.Random.Guid())
            .RuleFor(u => u.Username, f => f.Internet.UserName())
            .RuleFor(u => u.RealName, f => f.Name.FullName())
            .RuleFor(u => u.PhoneNumber, f => f.Phone.PhoneNumber())
            .RuleFor(u => u.Role, f => f.PickRandom<UserRole>())
            .RuleFor(u => u.Status, f => f.PickRandom<CommonStatus>())
            .RuleFor(u => u.CreateTime, f => f.Date.Past())
            .RuleFor(u => u.CreatedBy, f => f.Random.Guid())
            .RuleFor(u => u.CreatedByName, f => f.Name.FullName())
            .RuleFor(u => u.PinYinCode, (f, u) => GetPinyin(u.RealName))
            .RuleFor(u => u.PasswordHash, f => f.Internet.Password(10, false, "", "Test123!"));

        /// <summary>
        /// 生成用户创建DTO
        /// </summary>
        public Faker<UserCreateDto> UserCreateDtoFaker => new Faker<UserCreateDto>("zh_CN")
            .RuleFor(u => u.Username, f => f.Internet.UserName())
            .RuleFor(u => u.RealName, f => f.Name.FullName())
            .RuleFor(u => u.PhoneNumber, f => f.Phone.PhoneNumber())
            .RuleFor(u => u.Role, f => f.PickRandom<UserRole>())
            .RuleFor(u => u.Password, f => "Test123!");

        /// <summary>
        /// 生成用户更新DTO
        /// </summary>
        public Faker<UserUpdateDto> UserUpdateDtoFaker => new Faker<UserUpdateDto>("zh_CN")
            .RuleFor(u => u.Id, f => f.Random.Guid())
            .RuleFor(u => u.RealName, f => f.Name.FullName())
            .RuleFor(u => u.PhoneNumber, f => f.Phone.PhoneNumber())
            .RuleFor(u => u.Role, f => f.PickRandom<UserRole>());

        #endregion

        #region 患者相关数据生成

        /// <summary>
        /// 生成患者模型
        /// </summary>
        public Faker<PatientModel> PatientModelFaker => new Faker<PatientModel>("zh_CN")
            .RuleFor(p => p.Id, f => f.Random.Guid())
            .RuleFor(p => p.Name, f => f.Name.FullName())
            .RuleFor(p => p.PhoneNumber, f => f.Phone.PhoneNumber())
            .RuleFor(p => p.Gender, f => f.PickRandom<Gender>())
            .RuleFor(p => p.BirthDate, f => f.Date.Past(80, DateTime.Now.AddYears(-18)))
            .RuleFor(p => p.IdCardNumber, f => GenerateIdCardNumber(f))
            .RuleFor(p => p.Address, f => f.Address.FullAddress())
            .RuleFor(p => p.Status, f => f.PickRandom<CommonStatus>())
            .RuleFor(p => p.CreateTime, f => f.Date.Past())
            .RuleFor(p => p.CreatedBy, f => f.Random.Guid())
            .RuleFor(p => p.CreatedByName, f => f.Name.FullName())
            .RuleFor(p => p.PinYinCode, (f, p) => GetPinyin(p.Name));

        /// <summary>
        /// 生成患者创建DTO
        /// </summary>
        public Faker<PatientCreateDto> PatientCreateDtoFaker => new Faker<PatientCreateDto>("zh_CN")
            .RuleFor(p => p.Name, f => f.Name.FullName())
            .RuleFor(p => p.PhoneNumber, f => f.Phone.PhoneNumber())
            .RuleFor(p => p.Gender, f => f.PickRandom<Gender>())
            .RuleFor(p => p.BirthDate, f => f.Date.Past(80, DateTime.Now.AddYears(-18)))
            .RuleFor(p => p.IdCardNumber, f => GenerateIdCardNumber(f))
            .RuleFor(p => p.Address, f => f.Address.FullAddress());

        #endregion

        #region 中药材相关数据生成

        /// <summary>
        /// 生成中药材模型
        /// </summary>
        public Faker<HerbModel> HerbModelFaker => new Faker<HerbModel>("zh_CN")
            .RuleFor(h => h.Id, f => f.Random.Guid())
            .RuleFor(h => h.Name, f => f.PickRandom(ChineseHerbNames))
            .RuleFor(h => h.Price, f => f.Random.Decimal(1, 500))
            .RuleFor(h => h.Stock, f => f.Random.Int(0, 1000))
            .RuleFor(h => h.Unit, f => f.PickRandom("g", "包", "粒", "片", "ml"))
            .RuleFor(h => h.Status, f => f.PickRandom<CommonStatus>())
            .RuleFor(h => h.CreateTime, f => f.Date.Past())
            .RuleFor(h => h.CreatedBy, f => f.Random.Guid())
            .RuleFor(h => h.CreatedByName, f => f.Name.FullName())
            .RuleFor(h => h.PinYinCode, (f, h) => GetPinyin(h.Name));

        /// <summary>
        /// 生成中药材创建DTO
        /// </summary>
        public Faker<HerbCreateDto> HerbCreateDtoFaker => new Faker<HerbCreateDto>("zh_CN")
            .RuleFor(h => h.Name, f => f.PickRandom(ChineseHerbNames))
            .RuleFor(h => h.Price, f => f.Random.Decimal(1, 500))
            .RuleFor(h => h.Stock, f => f.Random.Int(0, 1000))
            .RuleFor(h => h.Unit, f => f.PickRandom("g", "包", "粒", "片", "ml"));

        #endregion

        #region 数据驱动测试支持

        /// <summary>
        /// 生成边界值测试数据
        /// </summary>
        public static IEnumerable<object[]> GetBoundaryTestData<T>()
        {
            var type = typeof(T);
            
            if (type == typeof(int))
            {
                yield return new object[] { int.MinValue };
                yield return new object[] { -1 };
                yield return new object[] { 0 };
                yield return new object[] { 1 };
                yield return new object[] { int.MaxValue };
            }
            else if (type == typeof(decimal))
            {
                yield return new object[] { decimal.MinValue };
                yield return new object[] { -1m };
                yield return new object[] { 0m };
                yield return new object[] { 1m };
                yield return new object[] { decimal.MaxValue };
            }
            else if (type == typeof(string))
            {
                yield return new object[] { null! };
                yield return new object[] { "" };
                yield return new object[] { " " };
                yield return new object[] { "a" };
                yield return new object[] { new string('a', 255) };
                yield return new object[] { new string('a', 1000) };
            }
        }

        /// <summary>
        /// 生成无效GUID测试数据
        /// </summary>
        public static IEnumerable<object[]> GetInvalidGuidTestData()
        {
            yield return new object[] { Guid.Empty };
            yield return new object[] { new Guid("00000000-0000-0000-0000-000000000000") };
        }

        /// <summary>
        /// 生成有效GUID测试数据
        /// </summary>
        public static IEnumerable<object[]> GetValidGuidTestData()
        {
            yield return new object[] { Guid.NewGuid() };
            yield return new object[] { Guid.NewGuid() };
            yield return new object[] { Guid.NewGuid() };
        }

        /// <summary>
        /// 生成分页参数测试数据
        /// </summary>
        public static IEnumerable<object[]> GetPaginationTestData()
        {
            // page, pageSize, expectedValid
            yield return new object[] { 1, 10, true };
            yield return new object[] { 1, 20, true };
            yield return new object[] { 2, 15, true };
            yield return new object[] { 0, 10, false }; // 无效页码
            yield return new object[] { -1, 10, false }; // 负页码
            yield return new object[] { 1, 0, false }; // 无效页大小
            yield return new object[] { 1, -1, false }; // 负页大小
            yield return new object[] { 1, 101, false }; // 过大页大小
        }

        #endregion

        #region 专门的测试场景生成器

        /// <summary>
        /// 生成用户名冲突场景测试数据
        /// </summary>
        public IEnumerable<UserCreateDto> GenerateUsernameConflictScenarios()
        {
            var baseUsername = "testuser";
            
            // 完全相同
            yield return UserCreateDtoFaker.Generate() with { Username = baseUsername };
            
            // 大小写不同
            yield return UserCreateDtoFaker.Generate() with { Username = baseUsername.ToUpper() };
            yield return UserCreateDtoFaker.Generate() with { Username = baseUsername.ToLower() };
            
            // 带空格
            yield return UserCreateDtoFaker.Generate() with { Username = $" {baseUsername} " };
        }

        /// <summary>
        /// 生成密码复杂度测试数据
        /// </summary>
        public static IEnumerable<object[]> GetPasswordComplexityTestData()
        {
            // password, expectedValid, reason
            yield return new object[] { "123456", false, "纯数字" };
            yield return new object[] { "abcdef", false, "纯字母" };
            yield return new object[] { "ABCDEF", false, "纯大写字母" };
            yield return new object[] { "Test123", false, "缺少特殊字符" };
            yield return new object[] { "Test123!", true, "符合要求" };
            yield return new object[] { "Test@123", true, "符合要求" };
            yield return new object[] { "a1!", false, "长度不足" };
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 生成身份证号
        /// </summary>
        private string GenerateIdCardNumber(Faker f)
        {
            var prefix = f.Random.Replace("######");
            var birthYear = f.Date.Past(50, DateTime.Now.AddYears(-18)).ToString("yyyyMMdd");
            var suffix = f.Random.Replace("###");
            return prefix + birthYear + suffix;
        }

        /// <summary>
        /// 获取拼音码（简化版）
        /// </summary>
        private string GetPinyin(string? chinese)
        {
            if (string.IsNullOrEmpty(chinese)) return string.Empty;
            
            // 简化的拼音转换，实际项目可能使用专门的拼音库
            return chinese.Substring(0, Math.Min(chinese.Length, 2)).ToUpper();
        }

        /// <summary>
        /// 常见中药材名称
        /// </summary>
        private static readonly string[] ChineseHerbNames = 
        {
            "人参", "白术", "茯苓", "甘草", "当归", "川芎", "白芍", "熟地黄",
            "党参", "黄芪", "白扁豆", "山药", "薏苡仁", "莲子", "大枣", "桂圆肉",
            "枸杞子", "菊花", "金银花", "连翘", "板蓝根", "大青叶", "蒲公英", "鱼腥草",
            "麻黄", "桂枝", "杏仁", "石膏", "知母", "桑叶", "菊花", "薄荷",
            "柴胡", "黄芩", "半夏", "生姜", "大枣", "人参", "甘草", "陈皮"
        };

        #endregion
    }
}