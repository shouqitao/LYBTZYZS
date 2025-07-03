namespace LYBT.UI.WPF.Services.Api {
    using System.Collections.Generic;
    using LYBT.Module.Users.Dtos;

    /// <summary>
    /// 用户列表查询返回对象
    /// </summary>
    public class SearchUsersResponse {
        /// <summary>
        /// 属性 Total 的说明
        /// </summary>
        public int Total { get; set; }
        /// <summary>
        /// 属性 Users 的说明
        /// </summary>
        public List<UserDto> Users { get; set; } = new();
    }

    /// <summary>
    /// 简单的成功返回
    /// </summary>
    public class ApiSuccessResponse {
        /// <summary>
        /// 属性 Success 的说明
        /// </summary>
        public bool Success { get; set; }
        /// <summary>
        /// 属性 Count 的说明
        /// </summary>
        public int? Count { get; set; }
    }
}
