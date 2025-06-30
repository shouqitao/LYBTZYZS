using LYBT.Common.Enums.Users;
using LYBT.Module.Auth.Dtos;
using Refit;
using System.Collections.Generic;
using System.Net.Http;

namespace LYBT.UI.WPF.Services {

    /// <summary>
    /// 认证服务实现：通过 WebAPI 调用进行真实登录验证
    /// </summary>
    public class AuthService : IAuthService {

        private readonly IAuthApi _authApi;

        public AuthService() {
            // TODO: 可从配置读取 API 地址
            var client = new HttpClient { BaseAddress = new Uri("http://localhost:5297") };
            _authApi = RestService.For<IAuthApi>(client);
        }

        public IList<UserRole>? Login(string userName, string password) {
            try {
                var result = _authApi.LoginAsync(new LoginRequestDto {
                    Username = userName,
                    Password = password
                }).GetAwaiter().GetResult();

                var roles = result.User.Roles;
                if (roles == null || roles.Count == 0)
                    roles = new List<UserRole> { result.User.Role };
                return roles;
            } catch (ApiException) {
                return null;
            }
        }
    }
}