using LYBT.Common.Enums.Users;
using System.ComponentModel;

namespace LYBT.Module.Users.Models.Dtos {

    /// <summary>
    /// 用户分页与条件查询 DTO
    /// </summary>
    public class UserQueryDto {

        /// <summary>
        /// 关键词（支持用户名或真实姓名模糊查询）
        /// </summary>
        [DisplayName("关键词（支持用户名或真实姓名模糊查询）")]
        public string? Keyword { get; set; }

        /// <summary>
        /// 用户角色列表（可选，多选）
        /// </summary>
        [DisplayName("用户角色列表（可选，多选）")]
        public List<UserRole>? Roles { get; set; }

        /// <summary>
        /// 启用状态（可选条件）
        /// </summary>
        [DisplayName("启用状态（可选条件）")]
        public bool? IsActive { get; set; }

        /// <summary>
        /// 当前页码（默认1）
        /// </summary>
        [DisplayName("当前页码（默认1）")]
        public int Page { get; set; } = 1;

        /// <summary>
        /// 每页条数（默认20）
        /// </summary>
        [DisplayName("每页条数（默认20）")]
        public int PageSize { get; set; } = 20;
    }
}