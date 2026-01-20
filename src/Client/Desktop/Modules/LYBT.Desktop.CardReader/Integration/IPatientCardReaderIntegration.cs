using LYBT.Desktop.CardReader.Models;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.CardReader.Integration;

/// <summary>
/// 患者读卡器集成接口
/// 定义读卡结果与患者模块的集成契约
/// </summary>
public interface IPatientCardReaderIntegration
{
    /// <summary>
    /// 根据身份证号查找患者
    /// </summary>
    /// <param name="idNumber">身份证号</param>
    /// <returns>患者信息（如找到），否则返回null</returns>
    Task<PatientFromCardResult?> FindPatientByIdNumberAsync(string idNumber);

    /// <summary>
    /// 根据读卡结果快速创建患者
    /// </summary>
    /// <param name="cardResult">读卡结果</param>
    /// <returns>创建后的患者ID</returns>
    Task<Guid> QuickCreatePatientAsync(CardReadResult cardResult);

    /// <summary>
    /// 查找或创建患者
    /// 先查找，如果不存在则创建
    /// </summary>
    /// <param name="cardResult">读卡结果</param>
    /// <returns>患者信息</returns>
    Task<PatientFromCardResult> FindOrCreatePatientAsync(CardReadResult cardResult);

    /// <summary>
    /// 根据患者ID获取患者详情
    /// OpenSpec: integrate-cardreader-module - 供ViewModel获取完整患者信息
    /// </summary>
    /// <param name="patientId">患者ID</param>
    /// <returns>患者详情DTO（如找到），否则返回null</returns>
    Task<PatientDetailDto?> GetPatientDetailByIdAsync(Guid patientId);
}

/// <summary>
/// 从读卡结果匹配的患者信息
/// </summary>
public class PatientFromCardResult
{
    /// <summary>患者ID</summary>
    public Guid PatientId { get; init; }

    /// <summary>姓名</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>身份证号</summary>
    public string IdNumber { get; init; } = string.Empty;

    /// <summary>是否为新创建的患者</summary>
    public bool IsNewlyCreated { get; init; }

    /// <summary>最后就诊时间</summary>
    public DateTime? LastVisitTime { get; init; }

    /// <summary>就诊次数</summary>
    public int VisitCount { get; init; }
}

/// <summary>
/// 读卡器集成事件类型
/// </summary>
public enum CardReaderIntegrationEventType
{
    /// <summary>找到现有患者</summary>
    PatientFound,

    /// <summary>患者不存在，需要创建</summary>
    PatientNotFound,

    /// <summary>患者已创建</summary>
    PatientCreated,

    /// <summary>读卡失败</summary>
    ReadFailed
}

/// <summary>
/// 读卡器集成事件参数
/// </summary>
public class CardReaderIntegrationEventArgs : EventArgs
{
    /// <summary>事件类型</summary>
    public CardReaderIntegrationEventType EventType { get; init; }

    /// <summary>读卡结果（如有）</summary>
    public CardReadResult? CardResult { get; init; }

    /// <summary>患者信息（如有）</summary>
    public PatientFromCardResult? Patient { get; init; }

    /// <summary>错误信息（如有）</summary>
    public string? ErrorMessage { get; init; }
}
