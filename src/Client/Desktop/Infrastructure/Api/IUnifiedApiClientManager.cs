using LYBT.Shared.Interfaces.Api;
using System.Threading.Tasks;

namespace LYBT.Desktop.Infrastructure.Api;

/// <summary>
/// 统一API客户端管理器接口 - UltraThink统一架构核心组件
/// 职责：管理所有业务模块的API客户端，提供统一的访问入口
/// </summary>
public interface IUnifiedApiClientManager
{
    #region 8个核心业务模块API客户端

    /// <summary>
    /// 身份认证API客户端
    /// </summary>
    IAuthApi AuthApi { get; }

    /// <summary>
    /// 用户管理API客户端
    /// </summary>
    IUserApi UserApi { get; }

    /// <summary>
    /// 患者管理API客户端
    /// </summary>
    IPatientApi PatientApi { get; }

    /// <summary>
    /// 医案管理API客户端
    /// </summary>
    IMedicalCaseApi MedicalCaseApi { get; }

    /// <summary>
    /// 看诊诊断API客户端
    /// </summary>
    IConsultationApi ConsultationApi { get; }

    /// <summary>
    /// 处方管理API客户端
    /// </summary>
    IPrescriptionApi PrescriptionApi { get; }

    /// <summary>
    /// 中药材管理API客户端
    /// </summary>
    IHerbApi HerbApi { get; }

    /// <summary>
    /// 验方管理API客户端
    /// </summary>
    IFormulaApi FormulaApi { get; }

    #endregion

    #region 统一管理方法

    /// <summary>
    /// 设置认证令牌
    /// </summary>
    /// <param name="token">JWT令牌</param>
    void SetAuthorizationToken(string token);

    /// <summary>
    /// 更新API基地址
    /// </summary>
    /// <param name="baseUrl">新的基地址</param>
    void UpdateBaseAddress(string baseUrl);

    /// <summary>
    /// 检查API连接健康状态
    /// </summary>
    Task<bool> CheckHealthAsync();

    /// <summary>
    /// 获取当前API基地址
    /// </summary>
    string? GetCurrentBaseAddress();

    #endregion
}