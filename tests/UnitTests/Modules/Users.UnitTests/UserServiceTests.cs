using LYBT.Infrastructure.Configuration.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using LYBT.Entities.Users;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Services;
using LYBT.Module.Users.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using LYBT.Module.Users.Tests.Base;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LYBT.Module.Users.Tests
{
    /// <summary>
    /// UserService 单元测试
    /// </summary>
    public class UserServiceTests
    {
        private readonly UserService _userService;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ILogger<UserBusinessService>> _mockLogger;
        private readonly UserOptions _userOptions;
        private readonly IMapper _mapper;
        private readonly List<User> _testUsers;

        public UserServiceTests()
{
    // 设置测试数据
    _testUsers = new List<User>();
    InitializeTestData();

    // 配置 UserOptions
    _userOptions = new UserOptions
    {
        EnableUserCache = false,
        MaxBatchOperationSize = 100,
        EnableDetailedAuditLogging = true,
        SendPasswordResetNotification = false
    };

    // 创建 Mock Repository
    _mockUserRepository = new Mock<IUserRepository>();
    SetupRepositoryMethods();

    // 创建 Mock Log Service
    _mockLogger = new Mock<ILogger<UserBusinessService>>();
    // 注: UltraThink架构中UserService不直接依赖日志服务

    // 创建 Mapper
    _mapper = CreateUserMapper();

    // 创建 Mock Services for UltraThink双层架构
    var mockQueryService = new Mock<IUserQueryService>();
    var mockBusinessService = new Mock<IUserBusinessService>();

    // 设置Mock QueryService的期望返回值
    SetupQueryServiceMocks(mockQueryService);

    // 设置Mock BusinessService的期望返回值  
    SetupBusinessServiceMocks(mockBusinessService);

    // 创建UserService实例 (使用新的双层架构)
    _userService = new UserService(
        mockQueryService.Object,
        mockBusinessService.Object
    );
}

        #region 初始化测试数据

        private void InitializeTestData()
        {
            // 创建测试用户数据
            for (int i = 0; i < 5; i++)
            {
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = $"testuser{i}",
                    RealName = $"测试用户{i}",
                    PinYinCode = $"CSYH{i}",
                    PhoneNumber = $"1380000000{i}",
                    PasswordHash = PasswordHelper.Hash("Test123!"),
                    Status = i % 2 == 0 ? CommonStatus.Enabled : CommonStatus.Disabled,
                    CreatedTime = DateTime.UtcNow.AddDays(-i),
                    UpdateTime = DateTime.UtcNow
                };
                _testUsers.Add(user);
            }
        }

        private void SetupRepositoryMethods()
        {
            // Setup GetPagedAsync
            _mockUserRepository
                .Setup(x => x.GetPagedAsync(It.IsAny<UserPagedQueryDto>(), It.IsAny<bool>()))
                .ReturnsAsync((UserPagedQueryDto query, bool includeDisabled) =>
                {
                    var filteredUsers = _testUsers.AsQueryable();

                    if (!includeDisabled)
                    {
                        filteredUsers = filteredUsers.Where(u => u.Status == CommonStatus.Enabled);
                    }

                    // 使用SearchKeyword而不是Search
                    if (!string.IsNullOrEmpty(query.SearchKeyword))
                    {
                        filteredUsers = filteredUsers.Where(u => 
                            u.Username.Contains(query.SearchKeyword) || 
                            u.RealName.Contains(query.SearchKeyword));
                    }

                    // 使用Username和RealName进行更精确的过滤
                    if (!string.IsNullOrEmpty(query.Username))
                    {
                        filteredUsers = filteredUsers.Where(u => u.Username.Contains(query.Username));
                    }

                    if (!string.IsNullOrEmpty(query.RealName))
                    {
                        filteredUsers = filteredUsers.Where(u => u.RealName.Contains(query.RealName));
                    }

                    var total = filteredUsers.Count();
                    var items = filteredUsers
                        .Skip((query.CurrentPage - 1) * query.PageSize)
                        .Take(query.PageSize)
                        .ToList();

                    return (items, total);
                });

            // Setup GetByIdAsync
            _mockUserRepository
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<bool>()))
                .ReturnsAsync((Guid id, bool includeDisabled) =>
                {
                    var user = _testUsers.FirstOrDefault(u => u.Id == id);
                    if (user != null && !includeDisabled && user.Status == CommonStatus.Disabled)
                    {
                        return null;
                    }
                    return user;
                });

            // Setup GetByUsernameAsync
            _mockUserRepository
                .Setup(x => x.GetByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync((string username) => _testUsers.FirstOrDefault(u => u.Username == username));

            // Setup AddAsync
            _mockUserRepository
                .Setup(x => x.AddAsync(It.IsAny<User>()))
                .ReturnsAsync((User user) => 
                {
                    _testUsers.Add(user);
                    return user;
                });

            // Setup UpdateAsync
            _mockUserRepository
                .Setup(x => x.UpdateAsync(It.IsAny<User>()))
                .ReturnsAsync((User user) =>
                {
                    var existing = _testUsers.FirstOrDefault(u => u.Id == user.Id);
                    if (existing != null)
                    {
                        _testUsers.Remove(existing);
                        _testUsers.Add(user);
                    }
                    return user;
                });

            // Setup UpdatePasswordAsync
            _mockUserRepository
                .Setup(x => x.UpdatePasswordAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync((Guid id, string passwordHash) =>
                {
                    var user = _testUsers.FirstOrDefault(u => u.Id == id);
                    if (user != null)
                    {
                        user.PasswordHash = passwordHash;
                        return true;
                    }
                    return false;
                });

            // Setup DisableAsync
            _mockUserRepository
                .Setup(x => x.DisableAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) =>
                {
                    var user = _testUsers.FirstOrDefault(u => u.Id == id);
                    if (user != null)
                    {
                        user.Status = CommonStatus.Disabled;
                        return true;
                    }
                    return false;
                });

            // Setup EnableAsync
            _mockUserRepository
                .Setup(x => x.EnableAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) =>
                {
                    var user = _testUsers.FirstOrDefault(u => u.Id == id);
                    if (user != null)
                    {
                        user.Status = CommonStatus.Enabled;
                        return true;
                    }
                    return false;
                });

            // Setup UpdateActiveStatusAsync (替代BatchUpdateStatusAsync)
            _mockUserRepository
                .Setup(x => x.UpdateActiveStatusAsync(It.IsAny<List<Guid>>(), It.IsAny<bool>()))
                .ReturnsAsync((List<Guid> ids, bool isActive) =>
                {
                    var count = 0;
                    foreach (var id in ids)
                    {
                        var user = _testUsers.FirstOrDefault(u => u.Id == id);
                        if (user != null)
                        {
                            user.Status = isActive ? CommonStatus.Enabled : CommonStatus.Disabled;
                            count++;
                        }
                    }
                    return count;
                });

            // Setup GetActiveUsersAsync
            _mockUserRepository
                .Setup(x => x.GetActiveUsersAsync())
                .ReturnsAsync(() => _testUsers.Where(u => u.Status == CommonStatus.Enabled).ToList());

            // Setup ExistsByUsernameAsync
            _mockUserRepository
                .Setup(x => x.ExistsByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync((string username) => _testUsers.Any(u => u.Username == username));

            // Setup GetUsersByIdsAsync
            _mockUserRepository
                .Setup(x => x.GetUsersByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<bool>()))
                .ReturnsAsync((List<Guid> ids, bool includeDisabled) =>
                {
                    var users = _testUsers.Where(u => ids.Contains(u.Id));
                    if (!includeDisabled)
                    {
                        users = users.Where(u => u.Status == CommonStatus.Enabled);
                    }
                    return users.ToList();
                });
        }

        private void SetupQueryServiceMocks(Mock<IUserQueryService> mockQueryService)
        {
            // 设置分页查询
            mockQueryService.Setup(x => x.GetPagedAsync(It.IsAny<UserPagedQueryDto>()))
                .ReturnsAsync((UserPagedQueryDto query) =>
                {
                    // 根据查询条件过滤用户
                    var filteredUsers = _testUsers.AsQueryable();
                    
                    // 按用户名精确搜索
                    if (!string.IsNullOrEmpty(query.Username))
                    {
                        filteredUsers = filteredUsers.Where(u => u.Username.Contains(query.Username));
                    }
                    
                    // 按真实姓名搜索
                    if (!string.IsNullOrEmpty(query.RealName))
                    {
                        filteredUsers = filteredUsers.Where(u => u.RealName.Contains(query.RealName));
                    }
                    
                    // 按关键字搜索（用户名或真实姓名）
                    if (!string.IsNullOrEmpty(query.SearchKeyword))
                    {
                        filteredUsers = filteredUsers.Where(u => 
                            u.Username.Contains(query.SearchKeyword) || 
                            u.RealName.Contains(query.SearchKeyword));
                    }
                    
                    // 按角色过滤
                    if (!string.IsNullOrEmpty(query.Role))
                    {
                        if (Enum.TryParse<UserRole>(query.Role, out var roleFilter))
                        {
                            filteredUsers = filteredUsers.Where(u => u.Role == roleFilter);
                        }
                    }
                    
                    // 按状态过滤
                    if (query.Status.HasValue)
                    {
                        filteredUsers = filteredUsers.Where(u => u.Status == query.Status.Value);
                    }
                    
                    var totalCount = filteredUsers.Count();
                    
                    // 分页
                    var users = filteredUsers
                        .Skip((query.CurrentPage - 1) * query.PageSize)
                        .Take(query.PageSize)
                        .ToList();
                    
                    var userDtos = users.Select(u => _mapper.Map<UserDto>(u)).ToList();
                    var pagedResult = new PagedResult<UserDto>
                    {
                        Items = userDtos,
                        TotalCount = totalCount,
                        CurrentPage = query.CurrentPage,
                        PageSize = query.PageSize
                    };
                    return ServiceResult<PagedResult<UserDto>>.Success(pagedResult);
                });

            // 设置根据ID查询
            mockQueryService.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) =>
                {
                    var user = _testUsers.FirstOrDefault(u => u.Id == id);
                    if (user == null)
                        return ServiceResult<UserDto>.Failure("用户不存在");
                    
                    var userDto = _mapper.Map<UserDto>(user);
                    return ServiceResult<UserDto>.Success(userDto);
                });

            // 设置根据用户名查询
            mockQueryService.Setup(x => x.GetByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync((string username) =>
                {
                    var user = _testUsers.FirstOrDefault(u => u.Username == username);
                    if (user == null)
                        return ServiceResult<UserDto>.Failure("用户不存在");
                    
                    var userDto = _mapper.Map<UserDto>(user);
                    return ServiceResult<UserDto>.Success(userDto);
                });

            // 设置获取活跃用户
            mockQueryService.Setup(x => x.GetActiveUsersAsync())
                .ReturnsAsync(() =>
                {
                    var activeUsers = _testUsers.Where(u => u.Status == CommonStatus.Enabled).ToList();
                    var userDtos = activeUsers.Select(u => _mapper.Map<UserDto>(u)).ToList();
                    return ServiceResult<List<UserDto>>.Success(userDtos);
                });

            // 设置搜索
            mockQueryService.Setup(x => x.SearchAsync(It.IsAny<string>()))
                .ReturnsAsync((string keyword) =>
                {
                    var users = _testUsers.Where(u => u.Username.Contains(keyword) || u.RealName.Contains(keyword)).ToList();
                    var userDtos = users.Select(u => _mapper.Map<UserDto>(u)).ToList();
                    return ServiceResult<List<UserDto>>.Success(userDtos);
                });

            // 设置获取角色
            mockQueryService.Setup(x => x.GetRolesAsync())
                .ReturnsAsync(() =>
                {
                    var roles = new List<object> { "Admin", "Doctor" };
                    return ServiceResult<List<object>>.Success(roles);
                });

            // 设置用户名验证
            mockQueryService.Setup(x => x.ValidateUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync((string username) =>
                {
                    var exists = _testUsers.Any(u => u.Username == username);
                    return ServiceResult<bool>.Success(!exists);
                });
        }

        private void SetupBusinessServiceMocks(Mock<IUserBusinessService> mockBusinessService)
        {
            // 设置创建用户 - CreateUserAsync方法（对应IUserBusinessService.CreateUserAsync）
            mockBusinessService.Setup(x => x.CreateUserAsync(It.IsAny<UserMutationDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserMutationDto dto, CancellationToken cancellationToken) =>
                {
                    // 检查用户名是否已存在
                    if (_testUsers.Any(u => u.Username == dto.Username))
                        return ServiceResult<UserDto>.Failure("用户名已存在");

                    var newUser = new User
                    {
                        Id = Guid.NewGuid(),
                        Username = dto.Username,
                        RealName = dto.RealName,
                        PhoneNumber = dto.PhoneNumber ?? string.Empty,
                        Role = UserRole.Doctor,
                        Status = CommonStatus.Enabled,
                        CreatedTime = DateTime.Now,
                        PasswordHash = "hashedpassword"
                    };
                    
                    _testUsers.Add(newUser);
                    var userDto = _mapper.Map<UserDto>(newUser);
                    return ServiceResult<UserDto>.Success(userDto);
                });

            // 设置创建用户 - CreateUserAsync方法（内部业务方法，保留备用）
            mockBusinessService.Setup(x => x.CreateUserAsync(It.IsAny<UserMutationDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserMutationDto dto, CancellationToken cancellationToken) =>
                {
                    // 检查用户名是否已存在
                    if (_testUsers.Any(u => u.Username == dto.Username))
                        return ServiceResult<UserDto>.Failure("用户名已存在");

                    var newUser = new User
                    {
                        Id = Guid.NewGuid(),
                        Username = dto.Username,
                        RealName = dto.RealName,
                        PhoneNumber = dto.PhoneNumber ?? string.Empty,
                        Role = UserRole.Doctor,
                        Status = CommonStatus.Enabled,
                        CreatedTime = DateTime.Now,
                        PasswordHash = "hashedpassword"
                    };
                    
                    _testUsers.Add(newUser);
                    var userDto = _mapper.Map<UserDto>(newUser);
                    return ServiceResult<UserDto>.Success(userDto);
                });

            // 设置更新用户 - UpdateUserAsync方法（对应IUserBusinessService.UpdateUserAsync）
            mockBusinessService.Setup(x => x.UpdateUserAsync(It.IsAny<Guid>(), It.IsAny<UserMutationDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid id, UserMutationDto dto, CancellationToken cancellationToken) =>
                {
                    // 使用传入的ID来查找用户
                    var user = _testUsers.FirstOrDefault(u => u.Id == id);
                    if (user == null)
                        return ServiceResult<UserDto>.Failure("用户不存在");

                    user.RealName = dto.RealName;
                    user.PhoneNumber = dto.PhoneNumber ?? string.Empty;
                    user.Role = UserRole.Doctor;
                    user.UpdateTime = DateTime.Now;

                    var userDto = _mapper.Map<UserDto>(user);
                    return ServiceResult<UserDto>.Success(userDto);
                });

            // 设置更新用户 - UpdateUserAsync方法（内部业务方法，保留备用）
            mockBusinessService.Setup(x => x.UpdateUserAsync(It.IsAny<Guid>(), It.IsAny<UserMutationDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid id, UserMutationDto dto, CancellationToken cancellationToken) =>
                {
                    var user = _testUsers.FirstOrDefault(u => u.Id == id);
                    if (user == null)
                        return ServiceResult<UserDto>.Failure("用户不存在");

                    user.RealName = dto.RealName;
                    user.PhoneNumber = dto.PhoneNumber ?? string.Empty;
                    user.Role = UserRole.Doctor;
                    user.UpdateTime = DateTime.Now;

                    var userDto = _mapper.Map<UserDto>(user);
                    return ServiceResult<UserDto>.Success(userDto);
                });

            // 设置删除用户
            mockBusinessService.Setup(x => x.DeleteUserAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) =>
                {
                    var user = _testUsers.FirstOrDefault(u => u.Id == id);
                    if (user == null)
                        return ServiceResult<bool>.Failure("用户不存在");

                    _testUsers.Remove(user);
                    return ServiceResult<bool>.Success(true);
                });

            // 设置禁用用户
            mockBusinessService.Setup(x => x.DisableAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) =>
                {
                    var user = _testUsers.FirstOrDefault(u => u.Id == id);
                    if (user == null)
                        return ServiceResult<bool>.Failure("用户不存在");

                    user.Status = CommonStatus.Disabled;
                    user.UpdateTime = DateTime.Now;
                    return ServiceResult<bool>.Success(true);
                });

            // 设置启用用户
            mockBusinessService.Setup(x => x.EnableAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) =>
                {
                    var user = _testUsers.FirstOrDefault(u => u.Id == id);
                    if (user == null)
                        return ServiceResult<bool>.Failure("用户不存在");

                    user.Status = CommonStatus.Enabled;
                    user.UpdateTime = DateTime.Now;
                    return ServiceResult<bool>.Success(true);
                });

            // 设置批量禁用
            mockBusinessService.Setup(x => x.BatchDisableAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync((List<Guid> ids) =>
                {
                    int count = 0;
                    foreach (var id in ids)
                    {
                        var user = _testUsers.FirstOrDefault(u => u.Id == id);
                        if (user != null)
                        {
                            user.Status = CommonStatus.Disabled;
                            user.UpdateTime = DateTime.Now;
                            count++;
                        }
                    }
                    return ServiceResult<int>.Success(count);
                });

            // 设置批量启用
            mockBusinessService.Setup(x => x.BatchEnableAsync(It.IsAny<List<Guid>>()))
                .ReturnsAsync((List<Guid> ids) =>
                {
                    int count = 0;
                    foreach (var id in ids)
                    {
                        var user = _testUsers.FirstOrDefault(u => u.Id == id);
                        if (user != null)
                        {
                            user.Status = CommonStatus.Enabled;
                            user.UpdateTime = DateTime.Now;
                            count++;
                        }
                    }
                    return ServiceResult<int>.Success(count);
                });

            // 设置重置密码
            mockBusinessService.Setup(x => x.ResetPasswordAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync((Guid id, string newPassword) =>
                {
                    var user = _testUsers.FirstOrDefault(u => u.Id == id);
                    if (user == null)
                        return ServiceResult<bool>.Failure("用户不存在");

                    user.PasswordHash = "new_hashed_password";
                    user.UpdateTime = DateTime.Now;
                    return ServiceResult<bool>.Success(true);
                });

            // 设置修改密码
            mockBusinessService.Setup(x => x.ChangePasswordAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((Guid id, string oldPassword, string newPassword) =>
                {
                    var user = _testUsers.FirstOrDefault(u => u.Id == id);
                    if (user == null)
                        return ServiceResult<bool>.Failure("用户不存在");

                    // 简单的密码验证逻辑（测试用户默认密码）
                    if (oldPassword != "Test123!")
                        return ServiceResult<bool>.Failure("原密码错误");

                    user.PasswordHash = "new_hashed_password";
                    user.UpdateTime = DateTime.Now;
                    return ServiceResult<bool>.Success(true);
                });

            // 设置修改用户资料
            mockBusinessService.Setup(x => x.ChangeProfileAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((Guid userId, string realName, string phoneNumber) =>
                {
                    var user = _testUsers.FirstOrDefault(u => u.Id == userId);
                    if (user == null)
                        return ServiceResult<bool>.Failure("用户不存在");

                    user.RealName = realName;
                    user.PhoneNumber = phoneNumber;
                    user.UpdateTime = DateTime.Now;
                    return ServiceResult<bool>.Success(true);
                });
        }


        private IMapper CreateUserMapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<User, UserDto>();
                cfg.CreateMap<UserMutationDto, User>()
                    .ForMember(dest => dest.Id, opt => opt.Ignore())
                    .ForMember(dest => dest.CreatedTime, opt => opt.Ignore())
                    .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                    .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());
            });

            return config.CreateMapper();
        }

        #endregion

        #region GetPagedAsync 测试

        [Fact]
        public async Task GetPagedAsync_Should_Return_Paginated_Users()
        {
            // Arrange
            var query = new UserPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 10
            };

            // Act
            var result = await _userService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(_testUsers.Count);
            result.Data.TotalCount.Should().Be(_testUsers.Count);
            result.Data.CurrentPage.Should().Be(1);
            result.Data.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task GetPagedAsync_Should_Filter_By_Username()
        {
            // Arrange
            var query = new UserPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 10,
                Username = "testuser0"
            };

            // Act
            var result = await _userService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(1);
            result.Data.Items.First().Username.Should().Be("testuser0");
        }

        [Fact]
        public async Task GetPagedAsync_Should_Filter_By_SearchKeyword()
        {
            // Arrange
            var query = new UserPagedQueryDto
            {
                CurrentPage = 1,
                PageSize = 10,
                SearchKeyword = "testuser0"
            };

            // Act
            var result = await _userService.GetPagedAsync(query);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(1);
            result.Data.Items.First().Username.Should().Be("testuser0");
        }

        #endregion

        #region GetByIdAsync 测试

        [Fact]
        public async Task GetByIdAsync_Should_Return_User_When_Exists()
        {
            // Arrange
            var userId = _testUsers.First().Id;

            // Act
            var result = await _userService.GetByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data!.Id.Should().Be(userId);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_Null_When_Not_Exists()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var result = await _userService.GetByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("用户不存在");
            result.Data.Should().BeNull();
        }

        #endregion

        #region CreateAsync 测试

        [Fact]
        public async Task CreateAsync_Should_Create_New_User_Successfully()
        {
            // Arrange
            var dto = new UserMutationDto
            {
                Username = "newuser",
                RealName = "新用户",
                PhoneNumber = "13800138000",
                IsCreateOperation = true
            };
            var operatorId = Guid.NewGuid();
            var operatorName = "管理员";

            // Act
            var result = await _userService.CreateAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data!.Username.Should().Be(dto.Username);
            result.Data.RealName.Should().Be(dto.RealName);

            // TODO: 验证日志记录 - 双层架构需要重新设计Mock配置
            // 当前使用委托模式，无法直接Mock底层服务的日志行为
        }

        [Fact]
        public async Task CreateAsync_Should_Throw_When_Username_Already_Exists()
        {
            // Arrange
            var dto = new UserMutationDto
            {
                Username = "testuser0", // 已存在的用户名
                RealName = "重复用户",
                PhoneNumber = "13800138000",
                IsCreateOperation = true
            };
            var operatorId = Guid.NewGuid();
            var operatorName = "管理员";

            // Act
            var result = await _userService.CreateAsync(dto);
            
            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("用户名已存在");
        }

        #endregion

        #region UpdateUserAsync 测试

        [Fact]
        public async Task UpdateUserAsync_Should_Update_User_Successfully()
        {
            // Arrange
            var existingUser = _testUsers.First();
            var dto = new UserMutationDto
            {
                Id = existingUser.Id,  // 关键：UpdateAsync委托需要dto.Id
                Username = existingUser.Username,
                RealName = "更新后的名称",
                PhoneNumber = "13900139000",
                IsCreateOperation = false
            };
            var operatorId = Guid.NewGuid();
            var operatorName = "管理员";

            // Act
            var result = await _userService.UpdateAsync(dto);

            // Assert
            result.IsSuccess.Should().BeTrue();

            // 注: UltraThink架构中UserService是纯委托模式，日志记录由BusinessService处理
        }

        #endregion

        #region DisableAsync/EnableAsync 测试

        [Fact]
        public async Task DisableAsync_Should_Disable_User_Successfully()
        {
            // Arrange
            var user = _testUsers.First(u => u.Status == CommonStatus.Enabled);
            var operatorId = Guid.NewGuid();
            var operatorName = "管理员";

            // Act
            var result = await _userService.DisableAsync(user.Id);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            user.Status.Should().Be(CommonStatus.Disabled);
        }

        [Fact]
        public async Task EnableAsync_Should_Enable_User_Successfully()
        {
            // Arrange
            var user = _testUsers.First(u => u.Status == CommonStatus.Disabled);
            var operatorId = Guid.NewGuid();
            var operatorName = "管理员";

            // Act
            var result = await _userService.EnableAsync(user.Id);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            user.Status.Should().Be(CommonStatus.Enabled);
        }

        #endregion

        #region BatchDisableAsync/BatchEnableAsync 测试

        [Fact]
        public async Task BatchDisableAsync_Should_Disable_Multiple_Users()
        {
            // Arrange
            var userIds = _testUsers.Where(u => u.Status == CommonStatus.Enabled)
                                   .Take(2)
                                   .Select(u => u.Id)
                                   .ToList();
            var operatorId = Guid.NewGuid();
            var operatorName = "管理员";

            // Act
            var result = await _userService.BatchDisableAsync(userIds);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(2);
            _testUsers.Where(u => userIds.Contains(u.Id))
                     .All(u => u.Status == CommonStatus.Disabled)
                     .Should().BeTrue();
        }

        #endregion

        #region ResetPasswordAsync 测试

        [Fact]
        public async Task ResetPasswordAsync_Should_Reset_Password_To_Default()
        {
            // Arrange
            var user = _testUsers.First();
            var operatorId = Guid.NewGuid();
            var operatorName = "管理员";

            // Act
            var newPassword = "NewPassword@123";
            var result = await _userService.ResetPasswordAsync(user.Id, newPassword);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();

            // 验证用户密码已更新（在Mock中已设置）
            var updatedUser = _testUsers.First(u => u.Id == user.Id);
            updatedUser.PasswordHash.Should().Be("new_hashed_password");
        }

        #endregion

        #region ChangePasswordAsync 测试

        [Fact]
        public async Task ChangePasswordAsync_Should_Change_Password_Successfully()
        {
            // Arrange
            var user = _testUsers.First();
            var oldPassword = "Test123!";
            var newPassword = "NewTest123!";

            // Act
            var result = await _userService.ChangePasswordAsync(user.Id, oldPassword, newPassword);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();

            // 验证用户密码已更新（在Mock中已设置）
            var updatedUser = _testUsers.First(u => u.Id == user.Id);
            updatedUser.PasswordHash.Should().Be("new_hashed_password");
        }

        [Fact]
        public async Task ChangePasswordAsync_Should_Return_Failure_When_Old_Password_Invalid()
        {
            // Arrange
            var user = _testUsers.First();
            var wrongOldPassword = "WrongPassword";
            var newPassword = "NewTest123!";

            // Act
            var result = await _userService.ChangePasswordAsync(user.Id, wrongOldPassword, newPassword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("原密码错误");
        }

        #endregion

        #region ChangeProfileAsync 测试

        [Fact]
        public async Task ChangeProfileAsync_Should_Update_Profile_Successfully()
        {
            // Arrange
            var user = _testUsers.First();
            var newRealName = "更新的姓名";
            var newPhoneNumber = "13999999999";

            // Act
            var dto = new ChangeProfileDto
            {
                UserId = user.Id,
                RealName = newRealName,
                PhoneNumber = newPhoneNumber
            };
            var result = await _userService.ChangeProfileAsync(dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();

            // 验证用户资料已更新（在Mock中已设置）
            var updatedUser = _testUsers.First(u => u.Id == user.Id);
            updatedUser.RealName.Should().Be(newRealName);
            updatedUser.PhoneNumber.Should().Be(newPhoneNumber);
        }

        #endregion

        #region GetRoles 测试

        [Fact]
        public async Task GetRoles_Should_Return_Available_Roles()
        {
            // Act
            var result = await _userService.GetRolesAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCountGreaterThan(0);
        }

        #endregion

        #region GetActiveUsersAsync 测试

        [Fact]
        public async Task GetActiveUsersAsync_Should_Return_Only_Enabled_Users()
        {
            // Act
            var result = await _userService.GetActiveUsersAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().OnlyContain(u => u.Status == CommonStatus.Enabled);
        }

        #endregion
    }
}