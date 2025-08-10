using LYBT.Shared.Models.Core;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Users
{

    /// <summary>
    /// 用户实体类，数据库映射，继承共享基础模型
    /// </summary>
    public class UserModel : BaseUser
    {

        /// <summary>
        /// 密码哈希（敏感信息，仅后端使用）
        /// </summary>
        [Required]
        [DisplayName("密码哈希")]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// 失败登录次数（安全状态，仅后端使用）
        /// </summary>
        [DisplayName("失败登录次数")]
        public int FailedLoginCount { get; set; } = 0;

        /// <summary>
        /// 锁定结束时间（安全状态，仅后端使用）
        /// </summary>
        [DisplayName("锁定结束时间")]
        public DateTime? LockoutEnd { get; set; }
    }
}