namespace LYBT.UI.WPF.Services.Api {
    using System.Collections.Generic;
    using LYBT.Module.Users.Dtos;

    /// <summary>
    /// 用户列表查询返回对象
    /// </summary>
    public class SearchUsersResponse {
        public int Total { get; set; }
        public List<UserDto> Users { get; set; } = new();
    }

    /// <summary>
    /// 简单的成功返回
    /// </summary>
    public class ApiSuccessResponse {
        public bool Success { get; set; }
        public int? Count { get; set; }
    }
}
