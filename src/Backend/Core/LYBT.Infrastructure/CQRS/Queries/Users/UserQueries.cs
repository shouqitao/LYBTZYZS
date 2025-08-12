using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.CQRS.Queries;
using LYBT.Infrastructure.Repositories;
using LYBT.Infrastructure.Repositories.Base;
using LYBT.Models;
using LYBT.Models.Users;
using LYBT.Shared.Models;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Interfaces.Caching;
using LYBT.Domain.Aggregates.UserAggregate.ValueObjects;

namespace LYBT.Infrastructure.CQRS.Queries.Users
{
    #region Query Definitions

    /// <summary>
    /// 根据ID获取用户查询
    /// </summary>
    public record GetUserByIdQuery : QueryBase<UserModel>
    {
        public Guid Id { get; init; }

        public GetUserByIdQuery(Guid id)
        {
            Id = id;
            // 用户查询默认缓存5分钟
            CacheExpiration = TimeSpan.FromMinutes(5);
        }

        public override string GenerateCacheKey()
        {
            return $"user:id:{Id}";
        }
    }

    /// <summary>
    /// 分页获取用户列表查询
    /// </summary>
    public record GetUsersPagedQuery : PagedQueryBase<UserModel>
    {
        public UserRole? Role { get; init; }
        public bool? IsActive { get; init; }
        public DateTime? StartDate { get; init; }
        public DateTime? EndDate { get; init; }

        public override string GenerateCacheKey()
        {
            var baseKey = base.GenerateCacheKey();
            return $"{baseKey}:role:{Role?.ToString() ?? "null"}:active:{IsActive?.ToString() ?? "null"}:start:{StartDate?.ToString("yyyyMMdd") ?? "null"}:end:{EndDate?.ToString("yyyyMMdd") ?? "null"}";
        }
    }

    /// <summary>
    /// 根据用户名获取用户查询
    /// </summary>
    public record GetUserByUsernameQuery : QueryBase<UserModel>
    {
        public string Username { get; init; }

        public GetUserByUsernameQuery(string username)
        {
            Username = username;
            CacheExpiration = TimeSpan.FromMinutes(10); // 用户名查询缓存较长时间
        }

        public override string GenerateCacheKey()
        {
            return $"user:username:{Username}";
        }
    }

    /// <summary>
    /// 获取用户统计信息查询
    /// </summary>
    public record GetUserStatisticsQuery : QueryBase<UserStatisticsDto>
    {
        public DateTime? StartDate { get; init; }
        public DateTime? EndDate { get; init; }

        public override string GenerateCacheKey()
        {
            return $"user:statistics:start:{StartDate?.ToString("yyyyMMdd") ?? "all"}:end:{EndDate?.ToString("yyyyMMdd") ?? "all"}";
        }
    }

    /// <summary>
    /// 搜索用户查询
    /// </summary>
    public record SearchUsersQuery : QueryBase<System.Collections.Generic.List<UserModel>>
    {
        public string Keyword { get; init; }

        public SearchUsersQuery(string keyword)
        {
            Keyword = keyword;
            // 搜索结果缓存时间较短
            CacheExpiration = TimeSpan.FromMinutes(2);
        }

        public override string GenerateCacheKey()
        {
            return $"user:search:{Keyword}";
        }
    }

    #endregion

    #region Query Handlers

    /// <summary>
    /// 获取用户by ID查询处理器
    /// </summary>
    public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserModel>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMemoryCacheService _cacheService;
        private readonly ILogger<GetUserByIdQueryHandler> _logger;

        public GetUserByIdQueryHandler(
            IUserRepository userRepository,
            IMemoryCacheService cacheService,
            ILogger<GetUserByIdQueryHandler> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<UserModel> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("处理获取用户查询: {UserId}", request.Id);

            // 尝试从缓存获取
            if (request.EnableCache)
            {
                var cacheKey = request.GenerateCacheKey();
                var cachedUser = await _cacheService.GetAsync<UserModel>(cacheKey);
                if (cachedUser != null)
                {
                    _logger.LogDebug("从缓存获取用户成功: {UserId}", request.Id);
                    return cachedUser;
                }
            }

            // 从数据库获取
            var user = await _userRepository.GetByIdAsNoTrackingAsync(request.Id);
            
            // 缓存结果
            if (request.EnableCache && user != null)
            {
                var cacheKey = request.GenerateCacheKey();
                await _cacheService.SetAsync(cacheKey, user, request.CacheExpiration);
                _logger.LogDebug("用户查询结果已缓存: {UserId}", request.Id);
            }

            return user;
        }
    }

    /// <summary>
    /// 分页获取用户查询处理器
    /// </summary>
    public class GetUsersPagedQueryHandler : IQueryHandler<GetUsersPagedQuery, PagedResult<UserModel>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMemoryCacheService _cacheService;
        private readonly ILogger<GetUsersPagedQueryHandler> _logger;

        public GetUsersPagedQueryHandler(
            IUserRepository userRepository,
            IMemoryCacheService cacheService,
            ILogger<GetUsersPagedQueryHandler> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PagedResult<UserModel>> Handle(GetUsersPagedQuery request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("处理分页用户查询: Page {PageIndex}, Size {PageSize}", request.PageIndex, request.PageSize);

            // 尝试从缓存获取
            if (request.EnableCache)
            {
                var cacheKey = request.GenerateCacheKey();
                var cachedResult = await _cacheService.GetAsync<PagedResult<UserModel>>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogDebug("从缓存获取分页用户列表成功");
                    return cachedResult;
                }
            }

            // 构建查询DTO
            var queryDto = new UserPagedQueryDto
            {
                PageIndex = request.PageIndex,
                PageSize = request.PageSize,
                SearchTerm = request.SearchTerm,
                SortField = request.SortField,
                SortDirection = request.SortDirection,
                Role = request.Role,
                IsActive = request.IsActive,
                StartDate = request.StartDate,
                EndDate = request.EndDate
            };

            // 从数据库获取
            var result = await _userRepository.GetPagedAsync(queryDto);
            
            // 缓存结果
            if (request.EnableCache && result != null)
            {
                var cacheKey = request.GenerateCacheKey();
                await _cacheService.SetAsync(cacheKey, result, request.CacheExpiration ?? TimeSpan.FromMinutes(5));
                _logger.LogDebug("分页用户查询结果已缓存");
            }

            return result;
        }
    }

    /// <summary>
    /// 根据用户名获取用户查询处理器
    /// </summary>
    public class GetUserByUsernameQueryHandler : IQueryHandler<GetUserByUsernameQuery, UserModel>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMemoryCacheService _cacheService;
        private readonly ILogger<GetUserByUsernameQueryHandler> _logger;

        public GetUserByUsernameQueryHandler(
            IUserRepository userRepository,
            IMemoryCacheService cacheService,
            ILogger<GetUserByUsernameQueryHandler> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<UserModel> Handle(GetUserByUsernameQuery request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("处理根据用户名获取用户查询: {Username}", request.Username);

            if (string.IsNullOrEmpty(request.Username))
            {
                return null;
            }

            // 尝试从缓存获取
            if (request.EnableCache)
            {
                var cacheKey = request.GenerateCacheKey();
                var cachedUser = await _cacheService.GetAsync<UserModel>(cacheKey);
                if (cachedUser != null)
                {
                    _logger.LogDebug("从缓存获取用户成功: {Username}", request.Username);
                    return cachedUser;
                }
            }

            // 从数据库获取
            var user = await _userRepository.GetByUserNameAsync(request.Username);
            
            // 缓存结果
            if (request.EnableCache && user != null)
            {
                var cacheKey = request.GenerateCacheKey();
                await _cacheService.SetAsync(cacheKey, user, request.CacheExpiration);
                _logger.LogDebug("用户查询结果已缓存: {Username}", request.Username);
            }

            return user;
        }
    }

    /// <summary>
    /// 获取用户统计信息查询处理器
    /// </summary>
    public class GetUserStatisticsQueryHandler : IQueryHandler<GetUserStatisticsQuery, UserStatisticsDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMemoryCacheService _cacheService;
        private readonly ILogger<GetUserStatisticsQueryHandler> _logger;

        public GetUserStatisticsQueryHandler(
            IUserRepository userRepository,
            IMemoryCacheService cacheService,
            ILogger<GetUserStatisticsQueryHandler> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<UserStatisticsDto> Handle(GetUserStatisticsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("处理用户统计查询: {StartDate} - {EndDate}", request.StartDate, request.EndDate);

            // 尝试从缓存获取
            if (request.EnableCache)
            {
                var cacheKey = request.GenerateCacheKey();
                var cachedStats = await _cacheService.GetAsync<UserStatisticsDto>(cacheKey);
                if (cachedStats != null)
                {
                    _logger.LogDebug("从缓存获取用户统计成功");
                    return cachedStats;
                }
            }

            // 从数据库获取
            var stats = await _userRepository.GetStatisticsAsync(request.StartDate, request.EndDate);
            
            // 缓存结果 - 统计数据缓存时间较长
            if (request.EnableCache && stats != null)
            {
                var cacheKey = request.GenerateCacheKey();
                await _cacheService.SetAsync(cacheKey, stats, request.CacheExpiration ?? TimeSpan.FromMinutes(15));
                _logger.LogDebug("用户统计查询结果已缓存");
            }

            return stats;
        }
    }

    /// <summary>
    /// 搜索用户查询处理器
    /// </summary>
    public class SearchUsersQueryHandler : IQueryHandler<SearchUsersQuery, System.Collections.Generic.List<UserModel>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMemoryCacheService _cacheService;
        private readonly ILogger<SearchUsersQueryHandler> _logger;

        public SearchUsersQueryHandler(
            IUserRepository userRepository,
            IMemoryCacheService cacheService,
            ILogger<SearchUsersQueryHandler> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<System.Collections.Generic.List<UserModel>> Handle(SearchUsersQuery request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("处理搜索用户查询: {Keyword}", request.Keyword);

            if (string.IsNullOrEmpty(request.Keyword))
            {
                return new System.Collections.Generic.List<UserModel>();
            }

            // 尝试从缓存获取
            if (request.EnableCache)
            {
                var cacheKey = request.GenerateCacheKey();
                var cachedResults = await _cacheService.GetAsync<System.Collections.Generic.List<UserModel>>(cacheKey);
                if (cachedResults != null)
                {
                    _logger.LogDebug("从缓存获取用户搜索结果成功: {Keyword}", request.Keyword);
                    return cachedResults;
                }
            }

            // 从数据库搜索
            var results = await _userRepository.SearchAsync(request.Keyword);
            
            // 缓存结果
            if (request.EnableCache && results != null)
            {
                var cacheKey = request.GenerateCacheKey();
                await _cacheService.SetAsync(cacheKey, results, request.CacheExpiration);
                _logger.LogDebug("用户搜索结果已缓存: {Keyword}", request.Keyword);
            }

            return results;
        }
    }

    #endregion
}

/// <summary>
/// 用户分页查询DTO - 与现有UserPagedQueryDto保持一致
/// </summary>
public class UserPagedQueryDto : IPagedQuery<UserModel>
{
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 20;
    public string SearchTerm { get; set; }
    public string SortField { get; set; }
    public string SortDirection { get; set; } = "desc";
    
    public UserRole? Role { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}