using LYBT.Module.Auth.Dtos;
using Refit;

namespace LYBT.UI.WPF.Services {
    public interface IAuthApi {
        [Post("/api/Auth/login")]
        Task<LoginResponseDto> LoginAsync([Body] LoginRequestDto dto);
    }
}
