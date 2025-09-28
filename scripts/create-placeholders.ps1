# PowerShell脚本 - 创建占位文件
Write-Host "创建占位文件以解决编译错误..." -ForegroundColor Green

# Users模块占位文件
$usersPath = "src/Server/Modules/LYBT.Module.Users"

# 创建目录
New-Item -Path "$usersPath/Interfaces" -ItemType Directory -Force | Out-Null
New-Item -Path "$usersPath/Validators" -ItemType Directory -Force | Out-Null
New-Item -Path "$usersPath/Profiles" -ItemType Directory -Force | Out-Null
New-Item -Path "$usersPath/Configuration" -ItemType Directory -Force | Out-Null
New-Item -Path "$usersPath/HealthChecks" -ItemType Directory -Force | Out-Null

# IUserQueryService.cs
@"
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Module.Users.Interfaces
{
    public interface IUserQueryService
    {
        Task<PagedResult<UserDto>> GetPagedUsersAsync(UserSearchDto searchDto);
        Task<UserDto?> GetUserByIdAsync(Guid userId);
    }
}
"@ | Set-Content "$usersPath/Interfaces/IUserQueryService.cs" -Encoding UTF8

# UserQueryService.cs
@"
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Module.Users.Repositories;

namespace LYBT.Module.Users.Services
{
    public class UserQueryService : IUserQueryService
    {
        private readonly IUserRepository _repository;

        public UserQueryService(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<PagedResult<UserDto>> GetPagedUsersAsync(UserSearchDto searchDto)
        {
            return await Task.FromResult(new PagedResult<UserDto>());
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid userId)
        {
            return await Task.FromResult<UserDto?>(null);
        }
    }
}
"@ | Set-Content "$usersPath/Services/UserQueryService.cs" -Encoding UTF8

# UserCreateDtoValidator.cs
@"
using FluentValidation;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Module.Users.Validators
{
    public class UserCreateDtoValidator : AbstractValidator<UserCreateDto>
    {
        public UserCreateDtoValidator()
        {
            RuleFor(x => x.UserName).NotEmpty();
        }
    }
}
"@ | Set-Content "$usersPath/Validators/UserCreateDtoValidator.cs" -Encoding UTF8

# UserUpdateDtoValidator.cs
@"
using FluentValidation;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Module.Users.Validators
{
    public class UserUpdateDtoValidator : AbstractValidator<UserUpdateDto>
    {
        public UserUpdateDtoValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
"@ | Set-Content "$usersPath/Validators/UserUpdateDtoValidator.cs" -Encoding UTF8

# UserMappingProfile.cs
@"
using AutoMapper;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Module.Users.Profiles
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<User, UserDto>();
            CreateMap<UserCreateDto, User>();
            CreateMap<UserUpdateDto, User>();
        }
    }
}
"@ | Set-Content "$usersPath/Profiles/UserMappingProfile.cs" -Encoding UTF8

# UsersModuleHealthCheck.cs
@"
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LYBT.Module.Users.HealthChecks
{
    public class UsersModuleHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Users module is healthy"));
        }
    }
}
"@ | Set-Content "$usersPath/HealthChecks/UsersModuleHealthCheck.cs" -Encoding UTF8

Write-Host "Users模块占位文件创建完成" -ForegroundColor Cyan

# 类似地为其他模块创建占位文件...
Write-Host "所有占位文件创建完成！" -ForegroundColor Green