using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Users {

    /// <summary>
    /// 用户分页查询DTO - 前后端共享API契约
    /// 用于用户管理的分页查询和筛选
    /// </summary>
    public class UserPagedQueryDto : PaginationRequest {

        /// <summary>用户名关键词</summary>
        [DisplayName("用户名")]
        public string? Username { get; set; }

        /// <summary>真实姓名关键词</summary>
        [DisplayName("真实姓名")]
        public string? RealName { get; set; }

        /// <summary>角色筛选</summary>
        [DisplayName("用户角色")]
        public UserRole? Role { get; set; }

        /// <summary>邮箱关键词</summary>
        [DisplayName("邮箱")]
        public string? Email { get; set; }

        /// <summary>电话关键词</summary>
        [DisplayName("电话")]
        public string? PhoneNumber { get; set; }

        /// <summary>部门关键词</summary>
        [DisplayName("部门")]
        public string? Department { get; set; }

        /// <summary>职位关键词</summary>
        [DisplayName("职位")]
        public string? Position { get; set; }

        /// <summary>是否启用</summary>
        [DisplayName("是否启用")]
        public bool? IsActive { get; set; }

        /// <summary>是否在线</summary>
        [DisplayName("是否在线")]
        public bool? IsOnline { get; set; }

        /// <summary>创建日期范围-开始日期</summary>
        [DisplayName("创建开始日期")]
        public DateTime? CreateStartDate { get; set; }

        /// <summary>创建日期范围-结束日期</summary>
        [DisplayName("创建结束日期")]
        public DateTime? CreateEndDate { get; set; }

        /// <summary>最后登录日期范围-开始日期</summary>
        [DisplayName("登录开始日期")]
        public DateTime? LoginStartDate { get; set; }

        /// <summary>最后登录日期范围-结束日期</summary>
        [DisplayName("登录结束日期")]
        public DateTime? LoginEndDate { get; set; }

        /// <summary>按拼音码搜索</summary>
        [DisplayName("拼音码")]
        public string? PinyinCode { get; set; }

        /// <summary>按五笔码搜索</summary>
        [DisplayName("五笔码")]
        public string? WuBiCode { get; set; }

        /// <summary>是否包含已禁用用户</summary>
        [DisplayName("包含已禁用")]
        public bool IncludeInactive { get; set; } = false;
    }
}