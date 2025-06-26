using LYBT.Common.Enums.Users;
using System.Collections.Generic;

namespace LYBT.UI.WPF.Services {

    /// <summary>
    /// 认证服务实现：提供模拟的登录验证逻辑
    /// </summary>
    public class AuthService : IAuthService {

        private readonly Dictionary<string, (string Password, List<UserRole> Roles)> _accounts = new()
        {
            ["admin"] = ("123", new List<UserRole> { UserRole.Admin }),
            ["doctor"] = ("123", new List<UserRole> { UserRole.DiagnosingDoctor }),
            ["treatment"] = ("123", new List<UserRole> { UserRole.TreatmentDoctor }),
            ["pharmacy"] = ("123", new List<UserRole> { UserRole.PharmacyStaff }),
            ["register"] = ("123", new List<UserRole> { UserRole.RegistrationStaff }),
            // 多权限测试账号
            ["admin_pharmacy"] = ("123", new List<UserRole> { UserRole.Admin, UserRole.PharmacyStaff }),
            ["doctor_admin"] = ("123", new List<UserRole> { UserRole.DiagnosingDoctor, UserRole.Admin }),
            ["doctor_pharmacy_register"] = ("123", new List<UserRole> { UserRole.DiagnosingDoctor, UserRole.PharmacyStaff, UserRole.RegistrationStaff })
        };

        public IList<UserRole>? Login(string userName, string password) {
            if (_accounts.TryGetValue(userName, out var info) && info.Password == password)
                return info.Roles;
            return null;
        }
    }
}