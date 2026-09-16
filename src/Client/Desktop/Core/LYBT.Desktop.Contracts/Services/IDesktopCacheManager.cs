namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// Desktop 缓存管理器接口 -- 统一管理所有客户端缓存的失效。
/// </summary>
/// <remarks>
/// 缓存条目由传输层（<c>CachingHttpMessageHandler</c>）生产与自动失效；
/// 本接口提供「业务语义触发」的显式失效入口（例如状态机切换、登出、后台同步完成后），
/// 覆盖传输层无法感知的场景。
/// </remarks>
public interface IDesktopCacheManager
{
    /// <summary>
    /// 使患者相关缓存失效 (PatientSearchCache)
    /// </summary>
    void InvalidatePatientCaches();

    /// <summary>
    /// 使医案相关缓存失效 (UnfinishedCaseHandler + PendingQueue)
    /// </summary>
    void InvalidateMedicalCaseCaches();

    /// <summary>
    /// 使药材相关缓存失效
    /// </summary>
    void InvalidateHerbCaches();

    /// <summary>
    /// 使验方相关缓存失效
    /// </summary>
    void InvalidateFormulaCaches();

    /// <summary>
    /// 使用户相关缓存失效
    /// </summary>
    void InvalidateUserCaches();

    /// <summary>
    /// 使挂号相关缓存失效（含跨域联动：医案）
    /// </summary>
    void InvalidateRegistrationCaches();

    /// <summary>
    /// 使报表相关缓存失效
    /// </summary>
    void InvalidateReportCaches();

    /// <summary>
    /// 清空全部缓存（登出、切换连接模式、切换用户时调用）
    /// </summary>
    void InvalidateAll();
}
