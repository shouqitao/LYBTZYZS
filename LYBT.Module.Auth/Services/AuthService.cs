using System;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Module.Auth.Dtos;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Users.Dtos;
using LYBT.Module.Users.Models;

namespace LYBT.Module.Auth.Services {
    /// <summary>
    /// 登录验证服务实现
    /// </summary>
    public class AuthService : IAuthService {
        private readonly IAuthRepository _authRepository;
        private readonly IMapper _mapper;

        public AuthService(IAuthRepository authRepository, IMapper mapper) {
            _authRepository = authRepository;
            _mapper = mapper;
        }

        public async Task<UserDto?> LoginAsync(LoginRequestDto dto) {
            var user = await _authRepository.GetByUsernameAsync(dto.Username);
            if (user == null)
                return null;
            if (!user.IsActive)
                return null;
            if (user.PasswordHash != HashPassword(dto.Password))
                return null;

            user.LastLoginTime = DateTime.Now;
            await _authRepository.UpdateLastLoginTimeAsync(user.Id, user.LastLoginTime.Value);

            return _mapper.Map<UserDto>(user);
        }

        private static string HashPassword(string password) {
            using var sha = System.Security.Cryptography.SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password)));
        }
    }
}
