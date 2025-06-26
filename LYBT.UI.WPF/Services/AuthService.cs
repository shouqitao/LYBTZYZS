using LYBT.Common.Enums.Users;

namespace LYBT.UI.WPF.Services {

    /// <summary>
    /// 认证服务实现：提供模拟的登录验证逻辑
    /// </summary>
    public class AuthService : IAuthService {

        public UserRole? Login(string userName, string password) {
            // 根据用户名简单判定用户角色并验证密码
            if (userName == "admin" && password == "123")
                return UserRole.Admin;
            if (userName == "doctor" && password == "123")
                return UserRole.DiagnosingDoctor;
            if (userName == "treatment" && password == "123")
                return UserRole.TreatmentDoctor;
            if (userName == "pharmacy" && password == "123")
                return UserRole.PharmacyStaff;
            if (userName == "register" && password == "123")
                return UserRole.RegistrationStaff;
            return null;
        }
    }
}