using System.Threading.Tasks;
using Refit;
using LYBT.Module.Auth.Dtos;

namespace LYBT.UI.WPF.Services {
    /// <summary>
    /// Refit 登录接口
    /// </summary>
    public interface IAuthApi {
        // Refit requires routes to start with '/' to be considered relative to
        // the configured HTTP client's BaseAddress
        [Post("/api/auth/login")]
        Task<LoginResponseDto> LoginAsync([Body] LoginRequestDto dto);
    }
}
