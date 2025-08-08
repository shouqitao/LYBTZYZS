using Bogus;
using LYBT.Infrastructure.Options;
using LYBT.Models.Users;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;

namespace LYBT.Module.Auth.Tests.Base
{
    /// <summary>
    /// 认证模块测试数据生成器
    /// </summary>
    public static class AuthTestDataGenerator
    {
        private static readonly string[] UserNames = 
        {
            "doctor01", "doctor02", "nurse01", "nurse02", "admin01", 
            "receptionist", "pharmacist", "cashier", "manager"
        };

        private static readonly string[] RealNames =
        {
            "张医生", "李医生", "王护士", "刘护士", "陈管理",
            "赵前台", "孙药师", "周收银", "吴经理"
        };

        /// <summary>
        /// 测试用的标准密码
        /// </summary>
        public const string DefaultTestPassword = "TestPassword123";
        public const string AdminTestPassword = "Admin@123456";

        /// <summary>
        /// 用户数据生成器
        /// </summary>
        public static Faker<UserModel> UserGenerator => new Faker<UserModel>("zh_CN")
            .RuleFor(u => u.Id, f => Guid.NewGuid())
            .RuleFor(u => u.Username, f => f.PickRandom(UserNames))
            .RuleFor(u => u.RealName, f => f.PickRandom(RealNames))
            .RuleFor(u => u.PinYinCode, (f, u) => GetPinyinCode(u.RealName))
            .RuleFor(u => u.PasswordHash, f => PasswordHelper.Hash(DefaultTestPassword)) // 使用真实的密码哈希
            .RuleFor(u => u.Status, CommonStatus.Enabled)
            .RuleFor(u => u.CreateTime, f => f.Date.Recent(90))
            .RuleFor(u => u.LastLoginTime, f => f.Date.Recent(7))
            .RuleFor(u => u.FailedLoginCount, 0)
            .RuleFor(u => u.LockoutEnd, f => null)
            .RuleFor(u => u.PhoneNumber, f => f.Phone.PhoneNumber("1##########"));

        /// <summary>
        /// 登录请求数据生成器
        /// </summary>
        public static Faker<LoginRequestDto> LoginRequestGenerator => new Faker<LoginRequestDto>()
            .RuleFor(l => l.Username, f => f.PickRandom(UserNames))
            .RuleFor(l => l.Password, f => f.Internet.Password(8, false))
            .RuleFor(l => l.LoginType, "Password")
            .RuleFor(l => l.RememberMe, f => f.Random.Bool())
            .RuleFor(l => l.ClientIp, f => f.Internet.Ip())
            .RuleFor(l => l.UserAgent, f => f.Internet.UserAgent());

        /// <summary>
        /// 创建测试用户
        /// </summary>
        public static UserModel CreateTestUser(
            string? username = null,
            string? passwordHash = null,
            CommonStatus status = CommonStatus.Enabled,
            int failedLoginCount = 0,
            DateTime? lockoutEnd = null)
        {
            var user = UserGenerator.Generate();

            if (!string.IsNullOrEmpty(username))
                user.Username = username;

            if (!string.IsNullOrEmpty(passwordHash))
                user.PasswordHash = passwordHash;

            user.Status = status;
            user.FailedLoginCount = failedLoginCount;
            user.LockoutEnd = lockoutEnd;

            return user;
        }

        /// <summary>
        /// 创建启用的测试用户
        /// </summary>
        public static UserModel CreateEnabledUser(string? username = null)
        {
            return CreateTestUser(username: username, status: CommonStatus.Enabled);
        }

        /// <summary>
        /// 创建禁用的测试用户
        /// </summary>
        public static UserModel CreateDisabledUser(string? username = null)
        {
            return CreateTestUser(username: username, status: CommonStatus.Disabled);
        }

        /// <summary>
        /// 创建被锁定的测试用户
        /// </summary>
        public static UserModel CreateLockedUser(string? username = null, int failedCount = 5)
        {
            return CreateTestUser(
                username: username,
                status: CommonStatus.Enabled,
                failedLoginCount: failedCount,
                lockoutEnd: DateTime.Now.AddMinutes(15)
            );
        }

        /// <summary>
        /// 创建多个测试用户
        /// </summary>
        public static List<UserModel> CreateTestUsers(int count, CommonStatus? status = null)
        {
            var generator = UserGenerator;

            if (status.HasValue)
                generator = generator.RuleFor(u => u.Status, status.Value);

            var users = generator.Generate(count);

            // 确保用户名唯一
            for (int i = 0; i < users.Count; i++)
            {
                users[i].Username = $"testuser{i + 1}";
            }

            return users;
        }

        /// <summary>
        /// 创建系统管理员用户
        /// </summary>
        public static UserModel CreateSysAdminUser()
        {
            return new UserModel
            {
                Id = Guid.NewGuid(),
                Username = "sysadmin",
                RealName = "系统管理员",
                PinYinCode = "XTGLY",
                Status = CommonStatus.Enabled,
                CreateTime = DateTime.Now,
                PasswordHash = "",
                FailedLoginCount = 0,
                LockoutEnd = null,
                LastLoginTime = null
            };
        }

        /// <summary>
        /// 创建登录请求
        /// </summary>
        public static LoginRequestDto CreateLoginRequest(
            string? username = null,
            string? password = null,
            string? loginType = "Password",
            bool rememberMe = false,
            string? clientIp = "127.0.0.1",
            string? userAgent = "Test-Agent")
        {
            var request = LoginRequestGenerator.Generate();

            if (!string.IsNullOrEmpty(username))
                request.Username = username;

            if (!string.IsNullOrEmpty(password))
                request.Password = password;

            request.LoginType = loginType;
            request.RememberMe = rememberMe;
            request.ClientIp = clientIp;
            request.UserAgent = userAgent;

            return request;
        }

        /// <summary>
        /// 创建有效的登录请求（用户名和密码匹配）
        /// </summary>
        public static (UserModel user, LoginRequestDto request) CreateValidLoginPair()
        {
            var password = "TestPassword123";
            var passwordHash = "$2a$11$abcdefghijklmnopqrstuvwxyz1234567890ABCDEFGHIJK"; // 模拟哈希
            
            var user = CreateTestUser(
                username: "validuser",
                passwordHash: passwordHash,
                status: CommonStatus.Enabled
            );

            var request = CreateLoginRequest(
                username: user.Username,
                password: password
            );

            return (user, request);
        }

        /// <summary>
        /// 创建AuthOptions配置
        /// </summary>
        public static AuthOptions CreateAuthOptions()
        {
            return new AuthOptions
            {
                SupportedLoginTypes = new List<string> { "Password", "Token" },
                MaxFailedLoginAttempts = 5,
                AccountLockoutDuration = TimeSpan.FromMinutes(15),
                EnableDetailedLoginLogging = true,
                DefaultSysAdminPassword = "Admin123!"
            };
        }

        /// <summary>
        /// 创建系统管理员密码修改请求
        /// </summary>
        public static ChangeSysAdminPasswordDto CreateChangePasswordRequest(
            string oldPassword = "oldpassword",
            string newPassword = "newpassword")
        {
            return new ChangeSysAdminPasswordDto
            {
                OldPassword = oldPassword,
                NewPassword = newPassword
            };
        }

        /// <summary>
        /// 创建登出请求
        /// </summary>
        public static LogoutRequestDto CreateLogoutRequest(string? username = null)
        {
            return new LogoutRequestDto
            {
                Username = username ?? "testuser"
            };
        }

        /// <summary>
        /// 简单的拼音码生成（用于测试）
        /// </summary>
        private static string GetPinyinCode(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "";

            // 简化的拼音码映射（仅用于测试）
            var pinyinMap = new Dictionary<string, string>
            {
                {"张医生", "ZYS"}, {"李医生", "LYS"}, {"王护士", "WHS"}, 
                {"刘护士", "LHS"}, {"陈管理", "CGL"}, {"赵前台", "ZQT"},
                {"孙药师", "SYS"}, {"周收银", "ZSY"}, {"吴经理", "WJL"},
                {"系统管理员", "XTGLY"}
            };

            return pinyinMap.TryGetValue(name, out var pinyin) ? pinyin : 
                   string.Join("", name.Take(Math.Min(name.Length, 6)).Select(c => char.ToUpper(c)));
        }
    }
}