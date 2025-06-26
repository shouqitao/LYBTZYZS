using LYBT.Common.Enums.Users;

namespace LYBT.Module.Users.Dtos {

    /// <summary>
    /// 用户分页与条件查询 DTO
    /// </summary>
    public class UserQueryDto {

        /// <summary>
        /// 关键词（支持用户名或真实姓名模糊查询）
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 用户角色（可选条件，枚举）
        /// </summary>
        public UserRole? Role { get; set; }

        /// <summary>
        /// 启用状态（可选条件）
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// 当前页码（默认1）
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// 每页条数（默认20）
        /// </summary>
        public int PageSize { get; set; } = 20;
    }
}