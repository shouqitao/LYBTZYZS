namespace LYBT.UI.WPF.Apis {
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
    // 已删除本地ApiSuccessResponse，统一使用LYBT.Common.Models.ApiSuccessResponse
}
