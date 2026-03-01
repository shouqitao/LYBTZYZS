namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// Desktop 缓存管理器接口 -- 统一管理所有客户端缓存的失效
/// </summary>
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
    /// 使所有缓存失效 (Sync 完成后调用)
    /// </summary>
    void InvalidateAll();
}
