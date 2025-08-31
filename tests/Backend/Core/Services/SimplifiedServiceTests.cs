using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Herbs;
using LYBT.Entities.Users;
using LYBT.Module.Herbs.Services.Core;
using LYBT.Module.Users.Services.Core;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LYBT.Tests.Backend.Core.Services
{
    /// <summary>
    /// 简化Service层测试 - Phase F高价值测试集合
    /// 专注于核心Service功能验证，避免复杂依赖
    /// 使用InMemoryDatabase实现快速、可靠的单元测试
    /// </summary>
    public class SimplifiedServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public SimplifiedServiceTests()
        {
            // 设置内存数据库
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            // 配置AutoMapper
            var config = new MapperConfiguration(cfg =>
            {
                // Herb映射
                cfg.CreateMap<HerbModel, HerbDto>();
                cfg.CreateMap<HerbModel, HerbDetailDto>();
                cfg.CreateMap<HerbCreateDto, HerbModel>()
                    .ForMember(dest => dest.Id, opt => opt.Ignore())
                    .ForMember(dest => dest.CreateTime, opt => opt.Ignore())
                    .ForMember(dest => dest.UpdateTime, opt => opt.Ignore())
                    .ForMember(dest => dest.Status, opt => opt.MapFrom(src => CommonStatus.Enabled));

                // User映射
                cfg.CreateMap<UserModel, UserDto>();
                cfg.CreateMap<UserCreateDto, UserModel>()
                    .ForMember(dest => dest.Id, opt => opt.Ignore())
                    .ForMember(dest => dest.CreatedTime, opt => opt.Ignore())
                    .ForMember(dest => dest.UpdatedTime, opt => opt.Ignore())
                    .ForMember(dest => dest.Status, opt => opt.MapFrom(src => CommonStatus.Enabled));
            }, NullLoggerFactory.Instance);
            
            _mapper = config.CreateMapper();
        }

        #region Herb Service Core Tests

        [Fact]
        public async Task HerbService_CreateAndRetrieve_Success()
        {
            // Arrange
            var herbService = new HerbServiceCore(_context, _mapper);
            var createDto = new HerbCreateDto
            {
                Name = "测试药材",
                Origin = "云南",
                Price = 88.88m,
                Unit = "g",
                Effect = "清热解毒"
            };

            // Act - Create
            var createResult = await herbService.CreateAsync(createDto);
            
            // Act - Retrieve
            var retrieveResult = await herbService.GetByIdAsync(createResult.Data.Id);

            // Assert
            Assert.True(createResult.IsSuccess);
            Assert.NotNull(createResult.Data);
            Assert.Equal("测试药材", createResult.Data.Name);
            Assert.Equal(88.88m, createResult.Data.Price);

            Assert.True(retrieveResult.IsSuccess);
            Assert.NotNull(retrieveResult.Data);
            Assert.Equal("测试药材", retrieveResult.Data.Name);
        }

        [Fact]
        public async Task HerbService_SearchFunctionality_ReturnsCorrectResults()
        {
            // Arrange
            var herbService = new HerbServiceCore(_context, _mapper);
            
            // 添加测试数据
            await _context.Herbs.AddRangeAsync(new[]
            {
                new HerbModel { Id = Guid.NewGuid(), Name = "麻黄", PinYinCode = "MH", Status = CommonStatus.Enabled },
                new HerbModel { Id = Guid.NewGuid(), Name = "黄芩", PinYinCode = "HQ", Status = CommonStatus.Enabled },
                new HerbModel { Id = Guid.NewGuid(), Name = "桂枝", PinYinCode = "GZ", Status = CommonStatus.Enabled }
            });
            await _context.SaveChangesAsync();

            // Act - 按名称搜索
            var searchResult = await herbService.SearchAsync("黄");

            // Assert
            Assert.True(searchResult.IsSuccess);
            Assert.Equal(2, searchResult.Data.Count);
            Assert.All(searchResult.Data, herb => Assert.Contains("黄", herb.Name));
        }

        [Fact]
        public async Task HerbService_UpdatePrice_Success()
        {
            // Arrange
            var herbService = new HerbServiceCore(_context, _mapper);
            var herb = new HerbModel 
            { 
                Id = Guid.NewGuid(), 
                Name = "价格测试药材", 
                Price = 50m,
                Status = CommonStatus.Enabled 
            };
            await _context.Herbs.AddAsync(herb);
            await _context.SaveChangesAsync();

            var priceUpdate = new HerbPriceUpdateDto
            {
                Id = herb.Id,
                Price = 100m
            };

            // Act
            var updateResult = await herbService.UpdatePriceAsync(priceUpdate);

            // Assert
            Assert.True(updateResult.IsSuccess);
            
            // 验证数据库中的价格已更新
            var updatedHerb = await _context.Herbs.FindAsync(herb.Id);
            Assert.Equal(100m, updatedHerb.Price);
        }

        #endregion

        #region User Service Core Tests

        [Fact]
        public async Task UserService_CreateAndValidateUser_Success()
        {
            // Arrange  
            var userService = new UserServiceCore(_context, _mapper);
            var createDto = new UserCreateDto
            {
                Username = "testuser",
                RealName = "测试用户",
                Email = "test@example.com",
                Phone = "13800138000",
                Role = UserRole.Doctor
            };

            // Act - Create
            var createResult = await userService.CreateAsync(createDto);
            
            // Act - Retrieve
            var retrieveResult = await userService.GetByIdAsync(createResult.Data.Id);

            // Assert
            Assert.True(createResult.IsSuccess);
            Assert.NotNull(createResult.Data);
            Assert.Equal("testuser", createResult.Data.Username);
            Assert.Equal("测试用户", createResult.Data.RealName);
            Assert.Equal(UserRole.Doctor, createResult.Data.Role);

            Assert.True(retrieveResult.IsSuccess);
            Assert.Equal("testuser", retrieveResult.Data.Username);
        }

        [Fact]
        public async Task UserService_GetUsersByRole_ReturnsFilteredResults()
        {
            // Arrange
            var userService = new UserServiceCore(_context, _mapper);
            
            // 添加不同角色用户
            await _context.Users.AddRangeAsync(new[]
            {
                new UserModel { Id = Guid.NewGuid(), Username = "doctor1", RealName = "医生1", Role = UserRole.Doctor, Status = CommonStatus.Enabled },
                new UserModel { Id = Guid.NewGuid(), Username = "doctor2", RealName = "医生2", Role = UserRole.Doctor, Status = CommonStatus.Enabled },
                new UserModel { Id = Guid.NewGuid(), Username = "admin1", RealName = "管理员1", Role = UserRole.Admin, Status = CommonStatus.Enabled }
            });
            await _context.SaveChangesAsync();

            // Act
            var doctorsResult = await userService.GetByRoleAsync(UserRole.Doctor);

            // Assert
            Assert.True(doctorsResult.IsSuccess);
            Assert.Equal(2, doctorsResult.Data.Count);
            Assert.All(doctorsResult.Data, user => Assert.Equal(UserRole.Doctor, user.Role));
        }

        [Fact]
        public async Task UserService_UpdateUserStatus_Success()
        {
            // Arrange
            var userService = new UserServiceCore(_context, _mapper);
            var user = new UserModel
            {
                Id = Guid.NewGuid(),
                Username = "statustest",
                RealName = "状态测试",
                Status = CommonStatus.Enabled
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act - 禁用用户
            var disableResult = await userService.SetStatusAsync(user.Id, CommonStatus.Disabled);

            // Assert
            Assert.True(disableResult.IsSuccess);
            
            // 验证数据库中状态已更新
            var updatedUser = await _context.Users.FindAsync(user.Id);
            Assert.Equal(CommonStatus.Disabled, updatedUser.Status);
        }

        #endregion

        #region Integration Tests - Cross Service

        [Fact]
        public async Task Services_Integration_HerbAndUserWorkflowComplete()
        {
            // Arrange
            var herbService = new HerbServiceCore(_context, _mapper);
            var userService = new UserServiceCore(_context, _mapper);

            // 创建用户
            var userDto = new UserCreateDto
            {
                Username = "herbmanager",
                RealName = "药材管理员",
                Role = UserRole.Doctor
            };

            // 创建药材
            var herbDto = new HerbCreateDto
            {
                Name = "集成测试药材",
                Price = 45.6m,
                Unit = "g"
            };

            // Act
            var userResult = await userService.CreateAsync(userDto);
            var herbResult = await herbService.CreateAsync(herbDto);

            // Assert - 验证两个服务都成功创建
            Assert.True(userResult.IsSuccess);
            Assert.True(herbResult.IsSuccess);
            Assert.Equal("herbmanager", userResult.Data.Username);
            Assert.Equal("集成测试药材", herbResult.Data.Name);

            // 验证数据库中同时存在两条记录
            var userCount = await _context.Users.CountAsync();
            var herbCount = await _context.Herbs.CountAsync();
            Assert.True(userCount > 0);
            Assert.True(herbCount > 0);
        }

        #endregion

        #region Performance and Edge Case Tests

        [Fact]
        public async Task HerbService_BulkOperations_PerformanceAcceptable()
        {
            // Arrange
            var herbService = new HerbServiceCore(_context, _mapper);
            var importData = new List<HerbImportDto>();

            // 创建100个导入项
            for (int i = 1; i <= 100; i++)
            {
                importData.Add(new HerbImportDto
                {
                    Name = $"批量药材{i}",
                    Price = i * 10m,
                    Unit = "g"
                });
            }

            // Act
            var startTime = DateTime.UtcNow;
            var importResult = await herbService.ImportHerbsAsync(importData);
            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            // Assert
            Assert.True(importResult.IsSuccess);
            Assert.Equal(100, importResult.Data); // 成功导入数量
            Assert.True(duration.TotalSeconds < 10, $"批量操作耗时 {duration.TotalSeconds} 秒，超过10秒性能要求");

            // 验证数据库中确实有100条记录
            var totalHerbs = await _context.Herbs.CountAsync();
            Assert.Equal(100, totalHerbs);
        }

        [Fact]
        public async Task Services_ErrorHandling_GracefulFailure()
        {
            // Arrange
            var herbService = new HerbServiceCore(_context, _mapper);

            // Act - 尝试获取不存在的记录
            var nonExistentId = Guid.NewGuid();
            var result = await herbService.GetByIdAsync(nonExistentId);

            // Assert - 应该返回失败但不抛出异常
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("未找到", result.ErrorMessage);
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}