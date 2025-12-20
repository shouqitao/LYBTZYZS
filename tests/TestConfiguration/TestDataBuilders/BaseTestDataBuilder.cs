
/* 项目“LYBT.Tests.Configuration(net8.0-windows)”的未合并的更改
在此之前:
using System;
using System.Collections.Generic;
在此之后:
using System.Collections.Generic;
*/
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Tests.Common.TestDataBuilders
{
    /// <summary>
    /// 测试数据构建器基类 - 提供标准的测试数据创建模式
    /// 使用Builder模式来创建测试数据
    /// </summary>
    /// <typeparam name="T">要构建的数据类型</typeparam>
    public abstract class BaseTestDataBuilder<T> where T : class, new()
    {
        protected T _entity;

        protected BaseTestDataBuilder()
        {
            _entity = new T();
            SetDefaults();
        }

        /// <summary>
        /// 设置默认值 - 子类必须实现
        /// </summary>
        protected abstract void SetDefaults();

        /// <summary>
        /// 构建最终实体
        /// </summary>
        public virtual T Build()
        {
            return _entity;
        }

        /// <summary>
        /// 隐式转换操作符
        /// </summary>
        public static implicit operator T(BaseTestDataBuilder<T> builder)
        {
            return builder.Build();
        }
    }

    /// <summary>
    /// 分页查询测试数据构建器
    /// </summary>
    public class PagedQueryBuilder : BaseTestDataBuilder<PagedQueryBaseDto>
    {
        public PagedQueryBuilder()
        {
        }

        protected override void SetDefaults()
        {
            _entity.PageIndex = 1;
            _entity.PageSize = 20;
            _entity.Keyword = string.Empty;
        }

        public PagedQueryBuilder WithPageIndex(int pageIndex)
        {
            _entity.PageIndex = pageIndex;
            return this;
        }

        public PagedQueryBuilder WithPageSize(int pageSize)
        {
            _entity.PageSize = pageSize;
            return this;
        }

        public PagedQueryBuilder WithKeyword(string? keyword)
        {
            _entity.Keyword = keyword ?? string.Empty;
            return this;
        }

        public PagedQueryBuilder WithSorting(string sortField, bool descending = false)
        {
            _entity.SortField = sortField;
            _entity.IsDescending = descending;
            return this;
        }
    }

    /// <summary>
    /// 分页结果测试数据构建器
    /// </summary>
    public class PagedResultBuilder<T> where T : class
    {
        private readonly List<T> _items = new();
        private int _totalCount = 0;
        private int _pageIndex = 1;
        private int _pageSize = 20;

        public PagedResultBuilder()
        {
        }

        public PagedResultBuilder<T> WithItems(IEnumerable<T> items)
        {
            _items.Clear();
            _items.AddRange(items);
            _totalCount = _items.Count;
            return this;
        }

        public PagedResultBuilder<T> WithItems(params T[] items)
        {
            return WithItems((IEnumerable<T>)items);
        }

        public PagedResultBuilder<T> WithTotalCount(int totalCount)
        {
            _totalCount = totalCount;
            return this;
        }

        public PagedResultBuilder<T> WithPageInfo(int currentPage, int pageSize)
        {
            _pageIndex = currentPage;
            _pageSize = pageSize;
            return this;
        }

        public PagedResult<T> Build()
        {
            return new PagedResult<T>(_items, _totalCount, _pageIndex, _pageSize);
        }

        public static implicit operator PagedResult<T>(PagedResultBuilder<T> builder)
        {
            return builder.Build();
        }
    }

    /// <summary>
    /// API响应测试数据构建器
    /// </summary>
    public class ApiResponseBuilder<T>
    {
        private bool _success = true;
        private string? _message;
        private T? _data;

        public ApiResponseBuilder()
        {
        }

        public ApiResponseBuilder<T> WithSuccess(bool success)
        {
            _success = success;
            return this;
        }

        public ApiResponseBuilder<T> WithMessage(string message)
        {
            _message = message;
            return this;
        }

        public ApiResponseBuilder<T> WithData(T data)
        {
            _data = data;
            return this;
        }

        public ApiResponse<T> Build()
        {
            return new ApiResponse<T>
            {
                Success = _success,
                Message = _message ?? string.Empty,
                Data = _data
            };
        }

        public static implicit operator ApiResponse<T>(ApiResponseBuilder<T> builder)
        {
            return builder.Build();
        }
    }

    /// <summary>
    /// 测试用户数据构建器
    /// OpenSpec: dto-architecture-specification - 统一使用UserDetailDto
    /// </summary>
    public class UserDetailDtoBuilder : BaseTestDataBuilder<UserDetailDto>
    {
        public UserDetailDtoBuilder()
        {
        }

        protected override void SetDefaults()
        {
            _entity.Id = Guid.NewGuid();
            _entity.UserName = "testuser";
            _entity.RealName = "测试用户";
            _entity.Email = "test@example.com";
            _entity.PhoneNumber = "13800138000";
            _entity.Role = UserRole.Doctor;
            _entity.Status = CommonStatus.Enabled;
            _entity.CreatedAt = DateTime.UtcNow;
            _entity.UpdatedAt = DateTime.UtcNow;
        }

        public UserDetailDtoBuilder WithId(Guid id)
        {
            _entity.Id = id;
            return this;
        }

        public UserDetailDtoBuilder WithUserName(string userName)
        {
            _entity.UserName = userName;
            return this;
        }

        public UserDetailDtoBuilder WithRealName(string realName)
        {
            _entity.RealName = realName;
            return this;
        }

        public UserDetailDtoBuilder WithRole(UserRole role)
        {
            _entity.Role = role;
            return this;
        }

        public UserDetailDtoBuilder AsDoctor()
        {
            return WithRole(UserRole.Doctor);
        }

        public UserDetailDtoBuilder AsAdmin()
        {
            return WithRole(UserRole.Admin);
        }

        public UserDetailDtoBuilder AsPharmacist()
        {
            return WithRole(UserRole.Doctor); // Pharmacist角色已统一到Doctor
        }

        public UserDetailDtoBuilder Inactive()
        {
            _entity.Status = CommonStatus.Disabled;
            return this;
        }
    }

    /// <summary>
    /// 测试日期时间工具类
    /// </summary>
    public static class TestDateTime
    {
        public static DateTime Now => DateTime.UtcNow;

        public static DateTime Yesterday => Now.AddDays(-1);

        public static DateTime Tomorrow => Now.AddDays(1);

        public static DateTime LastWeek => Now.AddDays(-7);

        public static DateTime NextWeek => Now.AddDays(7);

        public static DateTime LastMonth => Now.AddMonths(-1);

        public static DateTime NextMonth => Now.AddMonths(1);

        public static DateTime At(int year, int month, int day) =>
            new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime At(int year, int month, int day, int hour, int minute) =>
            new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);
    }
}
