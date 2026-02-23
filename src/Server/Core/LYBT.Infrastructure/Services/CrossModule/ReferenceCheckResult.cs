namespace LYBT.Infrastructure.Services.CrossModule;

/// <summary>
/// 跨模块引用检查结果 (统一 DTO，替代模块内 HerbReferenceCheckDto/PatientReferenceCheckDto)
/// </summary>
public record ReferenceCheckResult(bool HasReferences, int ReferenceCount, string? Message = null);
