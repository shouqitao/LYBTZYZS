using System.Threading.Tasks;
using LYBT.Entities.Users;
using LYBT.Entities.Users;

namespace LYBT.Module.Users.Services.Notification
{
    /// <summary>
    /// 用户通知服务接口
    /// UltraThink重构：专注于用户相关的通知功能
    /// </summary>
    public interface IUserNotificationService
    {
        /// <summary>
        /// 发送密码重置通知
        /// </summary>
        /// <param name="user">用户实体</param>        /// <returns>发送任务</returns>
        Task SendPasswordResetNotificationAsync(User user);

        /// <summary>
        /// 发送用户创建通知
        /// </summary>
        /// <param name="user">用户实体</param>        /// <param name="temporaryPassword">临时密码</param>        /// <returns>发送任务</returns>
        Task SendUserCreationNotificationAsync(User user, string temporaryPassword);

        /// <summary>
        /// 发送账户状态变更通知
        /// </summary>
        /// <param name="user">用户实体</param>        /// <param name="isEnabled">是否启用</param>
        /// <returns>发送任务</returns>
        Task SendAccountStatusChangeNotificationAsync(User user, bool isEnabled);
    }
}
