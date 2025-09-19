using System.ComponentModel;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Shared.Interfaces.Services
{

    /// <summary>
    /// 用户服务接口 - UltraThink双层架构标准
    /// </summary>
    /// <remarks>
    /// <para>架构设计: UltraThink双层架构 - Module委托 → QueryService/BusinessService专业分工</para>
    /// <para>业务范围: 医生和管理员用户的完整生命周期管理</para>
    /// <para>安全特性: RBAC权限控制、密码安全策略、操作审计日志</para>
    /// <para>技术特性: ServiceResult统一结果包装、异步优先设计、DTO模式规范</para>
    /// </remarks>
    [Description("用户管理服务 - 医生/管理员账户管理、权限控制、密码安全")]
    public interface IUserService
    {

        #region 查询操作 - QueryService专业负责

        /// <summary>
        /// 根据ID获取用户详情
        /// </summary>
        /// <param name="id">用户唯一标识</param>
        /// <returns>用户详细信息，包含角色、状态、创建时间等</returns>
        /// <remarks>
        /// <para>委托: Module → QueryService.GetUserByIdAsync</para>
        /// <para>缓存: 用户信息缓存10分钟，减少数据库查询</para>
        /// <para>权限: 需要有效JWT令牌，用户只能查询自己或管理员查询所有</para>
        /// </remarks>
        Task<ServiceResult<UserDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 分页查询用户列表
        /// </summary>
        /// <param name="query">分页查询参数 - 包含排序、筛选、分页信息</param>
        /// <returns>分页用户列表和总数统计</returns>
        /// <remarks>
        /// <para>委托: Module → QueryService.GetUsersPagedAsync</para>
        /// <para>功能: 支持角色筛选、状态筛选、关键字搜索、多字段排序</para>
        /// <para>权限: 仅管理员可执行，医生无权查看其他用户列表</para>
        /// </remarks>
        Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserPagedQueryDto query);

        /// <summary>
        /// 根据用户名查找用户
        /// </summary>
        /// <param name="userName">用户名 - 系统内唯一标识</param>
        /// <returns>匹配的用户信息</returns>
        /// <remarks>
        /// <para>委托: Module → QueryService.GetUserByUsernameAsync</para>
        /// <para>用途: 登录验证、用户名唯一性检查、用户查找</para>
        /// <para>缓存: 根据用户名缓存5分钟，提升登录性能</para>
        /// </remarks>
        Task<ServiceResult<UserDto>> GetByUsernameAsync(string userName);

        /// <summary>
        /// 获取活跃用户列表
        /// </summary>
        /// <returns>所有启用状态的用户列表</returns>
        /// <remarks>
        /// <para>委托: Module → QueryService.GetActiveUsersAsync</para>
        /// <para>用途: 用户选择器、权限分配、工作安排显示</para>
        /// <para>筛选: 仅返回Status=Active的用户，按创建时间排序</para>
        /// </remarks>
        Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync();

        /// <summary>
        /// 关键字搜索用户
        /// </summary>
        /// <param name="keyword">搜索关键字 - 匹配用户名、显示名、邮箱</param>
        /// <returns>匹配的用户列表</returns>
        /// <remarks>
        /// <para>委托: Module → QueryService.SearchUsersAsync</para>
        /// <para>搜索: 用户名、显示名、邮箱模糊匹配，不区分大小写</para>
        /// <para>限制: 最多返回50个结果，按相关度排序</para>
        /// </remarks>
        Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword);

        #endregion 查询操作 - QueryService专业负责

        #region 业务操作 - BusinessService专业负责

        /// <summary>
        /// 创建新用户 - UltraThink统一DTO模式
        /// </summary>
        /// <param name="dto">用户创建数据 - 包含基本信息、角色、密码</param>
        /// <returns>创建的用户信息</returns>
        /// <remarks>
        /// <para>委托: Module → BusinessService.CreateUserAsync</para>
        /// <para>验证: 用户名唯一性、密码强度、邮箱格式、必填字段</para>
        /// <para>处理: 密码PBKDF2哈希、默认状态设置、创建时间记录</para>
        /// <para>审计: 记录用户创建操作日志</para>
        /// </remarks>
        Task<ServiceResult<UserDto>> CreateAsync(UserMutationDto dto);

        /// <summary>
        /// 更新用户信息 - UltraThink消除ID参数重复
        /// </summary>
        /// <param name="dto">用户更新数据 - 包含ID和待更新字段</param>
        /// <returns>更新后的用户信息</returns>
        /// <remarks>
        /// <para>委托: Module → BusinessService.UpdateUserAsync</para>
        /// <para>验证: 数据合规性、权限检查、业务规则验证</para>
        /// <para>处理: 字段更新、更新时间记录、缓存失效</para>
        /// <para>限制: 不允许修改用户名、创建时间、ID等关键字段</para>
        /// </remarks>
        Task<ServiceResult<UserDto>> UpdateAsync(UserMutationDto dto);

        /// <summary>
        /// 删除用户 (软删除)
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <returns>删除操作结果</returns>
        /// <remarks>
        /// <para>委托: Module → BusinessService.DeleteUserAsync</para>
        /// <para>软删除: 设置DeletedAt时间戳，保留数据用于审计</para>
        /// <para>影响: 用户立即无法登录，相关会话失效</para>
        /// <para>限制: 不能删除当前登录用户，不能删除系统管理员</para>
        /// </remarks>
        Task<ServiceResult<bool>> DeleteAsync(Guid id);

        #endregion 业务操作 - BusinessService专业负责

        #region 状态管理 - BusinessService批量操作

        /// <summary>
        /// 启用用户账户
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <returns>启用操作结果</returns>
        /// <remarks>
        /// <para>委托: Module → BusinessService.EnableUserAsync</para>
        /// <para>效果: 用户可以正常登录和使用系统功能</para>
        /// <para>审计: 记录状态变更操作和操作人信息</para>
        /// </remarks>
        Task<ServiceResult<bool>> EnableAsync(Guid id);

        /// <summary>
        /// 禁用用户账户
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <returns>禁用操作结果</returns>
        /// <remarks>
        /// <para>委托: Module → BusinessService.DisableUserAsync</para>
        /// <para>效果: 用户无法登录，已有会话立即失效</para>
        /// <para>场景: 员工离职、安全事件、临时限制访问</para>
        /// </remarks>
        Task<ServiceResult<bool>> DisableAsync(Guid id);

        /// <summary>
        /// 批量启用用户
        /// </summary>
        /// <param name="ids">用户ID列表</param>
        /// <returns>成功启用的用户数量</returns>
        /// <remarks>
        /// <para>委托: Module → BusinessService.BatchEnableUsersAsync</para>
        /// <para>事务: 使用数据库事务保证操作原子性</para>
        /// <para>返回: 实际成功启用的数量，可能少于请求数量</para>
        /// </remarks>
        Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids);

        /// <summary>
        /// 批量禁用用户
        /// </summary>
        /// <param name="ids">用户ID列表</param>
        /// <returns>成功禁用的用户数量</returns>
        /// <remarks>
        /// <para>委托: Module → BusinessService.BatchDisableUsersAsync</para>
        /// <para>安全: 自动跳过当前操作用户，防止自锁</para>
        /// <para>效果: 所有目标用户会话立即失效</para>
        /// </remarks>
        Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids);

        #endregion 状态管理 - BusinessService批量操作

        #region 密码管理 - BusinessService安全操作

        /// <summary>
        /// 管理员重置用户密码
        /// </summary>
        /// <param name="id">目标用户ID</param>
        /// <param name="newPassword">新密码 - 必须符合密码强度要求</param>
        /// <returns>重置操作结果</returns>
        /// <remarks>
        /// <para>委托: Module → BusinessService.ResetUserPasswordAsync</para>
        /// <para>验证: 新密码强度检查、密码历史检查</para>
        /// <para>安全: PBKDF2哈希存储、强制用户下次登录修改密码</para>
        /// <para>权限: 仅管理员可执行，记录详细审计日志</para>
        /// </remarks>
        Task<ServiceResult<bool>> ResetPasswordAsync(Guid id, string newPassword);

        /// <summary>
        /// 用户修改自己的密码
        /// </summary>
        /// <param name="id">用户ID</param>
        /// <param name="oldPassword">原密码 - 用于身份验证</param>
        /// <param name="newPassword">新密码</param>
        /// <returns>密码修改结果</returns>
        /// <remarks>
        /// <para>委托: Module → BusinessService.ChangeUserPasswordAsync</para>
        /// <para>验证: 原密码正确性、新密码强度、密码不重复</para>
        /// <para>安全: 修改后强制重新登录，使所有会话失效</para>
        /// <para>审计: 记录密码修改操作，不记录密码内容</para>
        /// </remarks>
        Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword);

        #endregion 密码管理 - BusinessService安全操作

        #region 个人信息管理 - BusinessService

        /// <summary>
        /// 修改用户个人资料 - UltraThink DTO模式
        /// </summary>
        /// <param name="dto">个人资料修改数据</param>
        /// <returns>修改操作结果</returns>
        /// <remarks>
        /// <para>委托: Module → BusinessService.ChangeUserProfileAsync</para>
        /// <para>范围: 显示名、邮箱、电话等非敏感个人信息</para>
        /// <para>限制: 不允许修改用户名、角色、状态等关键信息</para>
        /// <para>权限: 用户只能修改自己的信息</para>
        /// </remarks>
        Task<ServiceResult<bool>> ChangeProfileAsync(ChangeProfileDto dto);

        #endregion 个人信息管理 - BusinessService

        #region 辅助功能 - QueryService支持

        /// <summary>
        /// 获取系统角色列表
        /// </summary>
        /// <returns>可用角色列表 - Admin、Doctor</returns>
        /// <remarks>
        /// <para>委托: Module → QueryService.GetSystemRolesAsync</para>
        /// <para>用途: 用户创建界面、角色选择器、权限管理</para>
        /// <para>缓存: 角色信息缓存1小时，减少枚举查询</para>
        /// </remarks>
        Task<ServiceResult<List<object>>> GetRolesAsync();

        /// <summary>
        /// 验证用户名可用性
        /// </summary>
        /// <param name="userName">待验证的用户名</param>
        /// <returns>true: 可用; false: 已存在</returns>
        /// <remarks>
        /// <para>委托: Module → QueryService.ValidateUsernameAsync</para>
        /// <para>用途: 用户注册时实时验证、用户名唯一性检查</para>
        /// <para>规则: 不区分大小写检查、包含软删除用户的检查</para>
        /// </remarks>
        Task<ServiceResult<bool>> ValidateUsernameAsync(string userName);

        #endregion 辅助功能 - QueryService支持
    }
}
