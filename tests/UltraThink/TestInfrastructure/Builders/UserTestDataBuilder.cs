using System;
using LYBT.Models.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.UltraThink.TestInfrastructure.Builders;

namespace LYBT.Tests.UltraThink.TestInfrastructure.Builders
{
    /// <summary>
    /// 用户测试数据构建器 - UltraThink设计
    /// 职责单一：专注于User实体的测试数据生成
    /// 代码干净：流畅接口，清晰的方法命名
    /// 性能出色：延迟构建，高效生成
    /// </summary>
    public class UserTestDataBuilder : TestDataBuilder<UserModel, UserTestDataBuilder>
    {
        private static readonly string[] UserNames = 
        {
            "admin", "doctor1", "doctor2", "nurse1", "nurse2", 
            "pharmacist1", "receptionist1", "manager1", "operator1"
        };

        private static readonly string[] RealNames = 
        {
            "张三", "李四", "王五", "赵六", "陈七", 
            "刘八", "周九", "吴十", "郑十一", "冯十二"
        };

        private static readonly string[] Departments = 
        {
            "内科", "外科", "儿科", "妇科", "中医科", 
            "骨科", "皮肤科", "眼科", "耳鼻喉科", "口腔科"
        };

        public UserTestDataBuilder()
        {
            // 设置默认值
            WithCreatedAt(DateTime.UtcNow)
                .WithUpdatedAt(DateTime.UtcNow);
        }

        #region 基本属性构建方法

        public UserTestDataBuilder WithId(Guid id)
        {
            _buildActions.Add(u => u.Id = id);
            return this;
        }

        public UserTestDataBuilder WithUsername(string username)
        {
            _buildActions.Add(u => u.Username = username);
            return this;
        }

        public UserTestDataBuilder WithRandomUsername()
        {
            return WithUsername(UserNames[_random.Next(UserNames.Length)] + _random.Next(1000, 9999));
        }

        public UserTestDataBuilder WithRealName(string realName)
        {
            _buildActions.Add(u => u.RealName = realName);
            return this;
        }

        public UserTestDataBuilder WithRandomRealName()
        {
            return WithRealName(RealNames[_random.Next(RealNames.Length)]);
        }

        public UserTestDataBuilder WithPasswordHash(string passwordHash)
        {
            _buildActions.Add(u => u.PasswordHash = passwordHash);
            return this;
        }

        public UserTestDataBuilder WithPlainPassword(string password)
        {
            // 使用简单的Base64编码模拟密码哈希（实际应该使用BCrypt）
            var hash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
            return WithPasswordHash(hash);
        }

        public UserTestDataBuilder WithEmail(string email)
        {
            _buildActions.Add(u => u.Email = email);
            return this;
        }

        public UserTestDataBuilder WithPhone(string phone)
        {
            _buildActions.Add(u => u.Phone = phone);
            return this;
        }

        public UserTestDataBuilder WithRandomPhone()
        {
            return WithPhone(GeneratePhoneNumber());
        }

        public UserTestDataBuilder WithDepartment(string department)
        {
            _buildActions.Add(u => u.Department = department);
            return this;
        }

        public UserTestDataBuilder WithRandomDepartment()
        {
            return WithDepartment(Departments[_random.Next(Departments.Length)]);
        }

        #endregion

        #region 状态和权限构建方法

        public UserTestDataBuilder WithStatus(CommonStatus status)
        {
            _buildActions.Add(u => u.Status = status);
            return this;
        }

        public UserTestDataBuilder AsActive()
        {
            return WithStatus(CommonStatus.Enabled);
        }

        public UserTestDataBuilder AsInactive()
        {
            return WithStatus(CommonStatus.Disabled);
        }

        public UserTestDataBuilder WithRole(string role)
        {
            _buildActions.Add(u => u.Role = role);
            return this;
        }

        public UserTestDataBuilder AsAdmin()
        {
            return WithRole("Admin")
                .WithUsername("admin")
                .WithRealName("系统管理员");
        }

        public UserTestDataBuilder AsDoctor()
        {
            return WithRole("Doctor")
                .WithRandomDepartment();
        }

        public UserTestDataBuilder AsNurse()
        {
            return WithRole("Nurse")
                .WithRandomDepartment();
        }

        public UserTestDataBuilder AsReceptionist()
        {
            return WithRole("Receptionist")
                .WithDepartment("前台");
        }

        #endregion

        #region 登录相关构建方法

        public UserTestDataBuilder WithFailedLoginCount(int count)
        {
            _buildActions.Add(u => u.FailedLoginCount = count);
            return this;
        }

        public UserTestDataBuilder WithLockoutEnd(DateTime? lockoutEnd)
        {
            _buildActions.Add(u => u.LockoutEnd = lockoutEnd);
            return this;
        }

        public UserTestDataBuilder AsLockedOut(int hours = 1)
        {
            return WithLockoutEnd(DateTime.Now.AddHours(hours))
                .WithFailedLoginCount(5);
        }

        public UserTestDataBuilder WithLastLoginTime(DateTime? lastLoginTime)
        {
            _buildActions.Add(u => u.LastLoginTime = lastLoginTime);
            return this;
        }

        public UserTestDataBuilder WithLoginCount(int count)
        {
            _buildActions.Add(u => u.LoginCount = count);
            return this;
        }

        #endregion

        #region 审计字段构建方法

        public UserTestDataBuilder WithCreatedBy(string createdBy)
        {
            _buildActions.Add(u => u.CreatedBy = createdBy);
            return this;
        }

        public UserTestDataBuilder WithCreatedAt(DateTime createdAt)
        {
            _buildActions.Add(u => u.CreateTime = createdAt);
            return this;
        }

        public UserTestDataBuilder WithUpdatedBy(string updatedBy)
        {
            _buildActions.Add(u => u.UpdatedBy = updatedBy);
            return this;
        }

        public UserTestDataBuilder WithUpdatedAt(DateTime updatedAt)
        {
            _buildActions.Add(u => u.UpdateTime = updatedAt);
            return this;
        }

        #endregion

        #region 预设场景构建方法

        /// <summary>
        /// 构建一个有效的普通用户
        /// </summary>
        public UserTestDataBuilder AsValidUser()
        {
            return WithId(Guid.NewGuid())
                .WithRandomUsername()
                .WithRandomRealName()
                .WithPlainPassword("Test123456")
                .WithEmail("test@example.com")
                .WithRandomPhone()
                .WithRandomDepartment()
                .AsActive()
                .WithRole("User")
                .WithFailedLoginCount(0)
                .WithLockoutEnd(null)
                .WithLoginCount(0);
        }

        /// <summary>
        /// 构建系统管理员
        /// </summary>
        public UserTestDataBuilder AsSysAdmin()
        {
            return WithId(Guid.Empty)  // 系统管理员通常使用特殊ID
                .WithUsername("sysadmin")
                .WithRealName("系统管理员")
                .WithPlainPassword("Admin@123456")
                .WithEmail("admin@lybt.com")
                .WithPhone("13800138000")
                .AsActive()
                .WithRole("Admin")
                .WithFailedLoginCount(0)
                .WithLockoutEnd(null);
        }

        /// <summary>
        /// 构建一个新注册用户
        /// </summary>
        public UserTestDataBuilder AsNewUser()
        {
            return AsValidUser()
                .WithLoginCount(0)
                .WithLastLoginTime(null)
                .WithCreatedAt(DateTime.Now);
        }

        /// <summary>
        /// 构建一个频繁登录的活跃用户
        /// </summary>
        public UserTestDataBuilder AsActiveUser()
        {
            return AsValidUser()
                .WithLoginCount(100)
                .WithLastLoginTime(DateTime.Now.AddMinutes(-5))
                .WithFailedLoginCount(0);
        }

        /// <summary>
        /// 构建一个长期未登录的用户
        /// </summary>
        public UserTestDataBuilder AsInactiveUser()
        {
            return AsValidUser()
                .WithLoginCount(5)
                .WithLastLoginTime(DateTime.Now.AddMonths(-6))
                .AsInactive();
        }

        #endregion

        /// <summary>
        /// 应用默认值
        /// </summary>
        protected override void ApplyDefaults()
        {
            if (_entity.Id == Guid.Empty)
            {
                _entity.Id = Guid.NewGuid();
            }

            if (string.IsNullOrEmpty(_entity.Username))
            {
                _entity.Username = "user" + _random.Next(10000, 99999);
            }

            if (string.IsNullOrEmpty(_entity.RealName))
            {
                _entity.RealName = "测试用户";
            }

            if (string.IsNullOrEmpty(_entity.PasswordHash))
            {
                _entity.PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("DefaultPassword123"));
            }

            if (_entity.Status == 0)
            {
                _entity.Status = CommonStatus.Enabled;
            }

            if (string.IsNullOrEmpty(_entity.Role))
            {
                _entity.Role = "User";
            }

            if (_entity.CreateTime == default)
            {
                _entity.CreateTime = DateTime.UtcNow;
            }

            if (_entity.UpdateTime == default)
            {
                _entity.UpdateTime = DateTime.UtcNow;
            }
        }
    }
}