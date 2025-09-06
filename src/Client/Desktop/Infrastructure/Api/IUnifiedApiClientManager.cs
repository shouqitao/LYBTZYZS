using LYBT.Shared.Interfaces.Api;

namespace LYBT.Desktop.Infrastructure.Api;

/// <summary>
/// 统一API客户端管理器接口 - 企业级HTTP客户端集中管理
/// 采用UltraThink架构标准，提供统一的API访问入口
/// 集中管理所有8个业务模块的HTTP客户端，支持认证、健康检查和配置管理
/// 提供类型安全的REST API访问，适配小型诊所部署环境
/// 支持JWT认证、连接池管理、重试策略等企业级功能
/// </summary>
public interface IUnifiedApiClientManager : IDisposable {

    #region 8个核心业务模块API客户端

    /// <summary>
    /// 获取身份认证API客户端
    /// 用于处理用户登录、注销、令牌刷新等认证操作
    /// </summary>
    /// <value>JWT认证和会话管理的API客户端</value>
    IAuthApi AuthApi { get; }

    /// <summary>
    /// 获取用户管理API客户端
    /// 用于处理用户CRUD操作、角色管理等功能
    /// </summary>
    /// <value>用户信息维护和角色控制的API客户端</value>
    IUserApi UserApi { get; }

    /// <summary>
    /// 获取患者档案API客户端
    /// 用于处理患者信息管理、病历查询等功能
    /// </summary>
    /// <value>患者档案管理和查询的API客户端</value>
    IPatientApi PatientApi { get; }

    /// <summary>
    /// 获取医疗案例API客户端
    /// 用于处理医疗案例管理、诊疗流程控制功能
    /// </summary>
    /// <value>医疗案例和流程管理的API客户端</value>
    IMedicalCaseApi MedicalCaseApi { get; }

    /// <summary>
    /// 获取诊疗咨询API客户端
    /// 用于处理中医四诊、辨证论治等诊疗操作
    /// </summary>
    /// <value>中医诊疗和数据记录的API客户端</value>
    IConsultationApi ConsultationApi { get; }

    /// <summary>
    /// 获取处方管理API客户端
    /// 用于处理处方开具、药材配伍、打印输出等功能
    /// </summary>
    /// <value>处方编制和输出管理的API客户端</value>
    IPrescriptionApi PrescriptionApi { get; }

    /// <summary>
    /// 获取中药材管理API客户端
    /// 用于处理中药材信息维护、用法管理功能
    /// </summary>
    /// <value>中药材信息和规格管理的API客户端</value>
    IHerbApi HerbApi { get; }

    /// <summary>
    /// 获取验方管理API客户端
    /// 用于处理经典验方、个人验方模板管理功能
    /// </summary>
    /// <value>验方模板和组合管理的API客户端</value>
    IFormulaApi FormulaApi { get; }

    #endregion 8个核心业务模块API客户端

    #region 统一管理方法

    /// <summary>
    /// 设置认证令牌
    /// 用于JWT Bearer Token认证，支持令牌设置和清除
    /// </summary>
    /// <param name="token">JWT认证令牌，空值将清除当前令牌</param>
    /// <exception cref="ArgumentException">当令牌格式无效时可能抛出</exception>
    void SetAuthorizationToken(string? token);

    /// <summary>
    /// 更新API基地址
    /// 动态切换API服务器地址，支持开发、测试、生产环境切换
    /// </summary>
    /// <param name="baseUrl">新的API基地址，必须是有效的绝对URL</param>
    /// <exception cref="ArgumentException">当基地址为空或格式无效时抛出</exception>
    /// <exception cref="UriFormatException">当URL格式错误时抛出</exception>
    void UpdateBaseAddress(string baseUrl);

    /// <summary>
    /// 检查API连接健康状态
    /// 向服务器发送健康检查请求，验证连接和服务可用性
    /// </summary>
    /// <returns>如果API服务健康则返回 true；否则返回 false</returns>
    /// <exception cref="OperationCanceledException">当操作被取消时抛出</exception>
    Task<bool> CheckHealthAsync();

    /// <summary>
    /// 获取当前API基地址
    /// 返回当前配置的API服务器基地址信息
    /// </summary>
    /// <returns>当前配置的API基地址，如果未设置则返回 null</returns>
    string? GetCurrentBaseAddress();

    /// <summary>
    /// 获取连接状态信息
    /// 提供详细的连接状态和配置信息，用于诊断和监控
    /// </summary>
    /// <returns>包含连接状态、配置信息的状态对象</returns>
    Task<ApiConnectionStatus> GetConnectionStatusAsync();

    #endregion 统一管理方法
}

/// <summary>
/// API连接状态信息
/// 包含连接健康状态、配置信息和性能指标
/// </summary>
public class ApiConnectionStatus {

    /// <summary>
    /// 获取或设置连接是否健康
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// 获取或设置当前基地址
    /// </summary>
    public string? BaseAddress { get; set; }

    /// <summary>
    /// 获取或设置是否已设置认证令牌
    /// </summary>
    public bool HasAuthToken { get; set; }

    /// <summary>
    /// 获取或设置最后检查时间
    /// </summary>
    public DateTime LastCheckTime { get; set; }

    /// <summary>
    /// 获取或设置响应时间（毫秒）
    /// </summary>
    public double ResponseTimeMs { get; set; }

    /// <summary>
    /// 获取或设置状态消息
    /// </summary>
    public string? StatusMessage { get; set; }
}
