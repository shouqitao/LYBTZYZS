using Bogus;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Users.Tests.Base
{
    /// <summary>
    /// 测试数据生成器
    /// </summary>
    public static class TestDataGenerator
    {
        /// <summary>
        /// 用户数据生成器
        /// </summary>
        public static Faker<User> UserGenerator => new Faker<User>("zh_CN")
    .RuleFor(u => u.Id, f => Guid.NewGuid())
    .RuleFor(u => u.Username, f => f.Internet.UserName())
    .RuleFor(u => u.PasswordHash, f => f.Internet.Password(8))
    .RuleFor(u => u.RealName, f => f.Name.FullName())
    .RuleFor(u => u.PhoneNumber, f => f.Phone.PhoneNumber("1##########"))
    .RuleFor(u => u.PinYinCode, (f, u) => GetPinyinCode(u.RealName))
    .RuleFor(u => u.Status, f => f.PickRandom<CommonStatus>())
    .RuleFor(u => u.CreatedTime, f => f.Date.Recent(30)) // 修复：CreatedTime替代CreateTime
    .RuleFor(u => u.LastLoginTime, f => f.Date.Recent(10))
    .RuleFor(u => u.UpdateTime, f => f.Date.Recent(5))
    .RuleFor(u => u.Remark, f => f.Lorem.Sentence())
    .RuleFor(u => u.Specialty, f => f.Lorem.Words(3).ToString())
    .RuleFor(u => u.RegistrationFee, f => f.Random.Decimal(50, 300))
    .RuleFor(u => u.LicenseNumber, f => f.Random.Replace("######-####"))
    .RuleFor(u => u.Introduction, f => f.Lorem.Paragraph())
    .RuleFor(u => u.FailedLoginCount, f => 0)
    .RuleFor(u => u.LockoutEnd, f => null)
    .FinishWith((f, u) =>
    {
        // 确保用户名唯一性
        u.Username = $"{u.Username}_{DateTime.Now.Ticks}";
    });

        /// <summary>
        /// 创建测试用户
        /// </summary>
        public static User CreateTestUser(
            string? username = null, 
            string? realName = null,
            CommonStatus status = CommonStatus.Enabled)
        {
            var user = UserGenerator.Generate();
            
            if (!string.IsNullOrEmpty(username))
                user.Username = username;
            
            if (!string.IsNullOrEmpty(realName))
                user.RealName = realName;
                
            user.Status = status;
            
            return user;
        }

        /// <summary>
        /// 批量创建测试用户
        /// </summary>
        public static List<User> CreateTestUsers(int count, CommonStatus? status = null)
        {
            var generator = UserGenerator;
            
            if (status.HasValue)
                generator = generator.RuleFor(u => u.Status, status.Value);
                
            return generator.Generate(count);
        }

        /// <summary>
        /// 创建管理员测试用户
        /// </summary>
        public static User CreateAdminUser()
        {
            return CreateTestUser(
                username: "testadmin",
                realName: "测试管理员",
                status: CommonStatus.Enabled
            );
        }

        /// <summary>
        /// 创建禁用的测试用户
        /// </summary>
        public static User CreateDisabledUser()
        {
            return CreateTestUser(
                username: "disableduser",
                realName: "禁用用户",
                status: CommonStatus.Disabled
            );
        }

        /// <summary>
        /// 简单的拼音码生成（用于测试）
        /// </summary>
        private static string GetPinyinCode(string realName)
        {
            if (string.IsNullOrEmpty(realName))
                return "";

            // 简单的拼音首字母生成逻辑（测试用）
            return string.Join("", realName.Take(Math.Min(realName.Length, 6)).Select(c => char.ToUpper(c)));
        }
    }
}