using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;

namespace LYBT.Infrastructure.Services
{
    /// <summary>
    /// 当前用户服务实现
    /// </summary>
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 获取当前用户ID
        /// </summary>
        public Guid? UserId
        {
            get
            {
                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    return userId;
                }
                return null;
            }
        }

        /// <summary>
        /// 获取当前用户名
        /// </summary>
        public string? UserName => _httpContextAccessor.HttpContext?.User?.Identity?.Name;

        /// <summary>
        /// 是否已认证
        /// </summary>
        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    }
}