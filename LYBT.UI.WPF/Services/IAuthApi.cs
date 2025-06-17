using System.Threading.Tasks;
using Refit;
using LYBT.Module.Auth.Dtos;

namespace LYBT.UI.WPF.Services {
    /// <summary>
    /// Refit 登录接口
    /// </summary>
    public interface IAuthApi {
        [Post("api/auth/login")]
        Task<LoginResponseDto> LoginAsync([Body] LoginRequestDto dto);
    }
}
