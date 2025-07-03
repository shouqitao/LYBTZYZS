using LYBT.Module.Auth.Dtos;
using Refit;

namespace LYBT.UI.WPF.Apis {
    public interface IAuthApi {
        [Post("/api/Auth/login")]
        Task<LoginResponseDto> LoginAsync([Body] LoginRequestDto dto);
    }
}
