using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.MedicalCase.Interfaces;

/// <summary>
/// 医案工作台数据提供者接口
/// 由 MedicalCaseWorkspaceVM 实现，MedicalCaseCommandsVM 消费。
/// 替代 11 个 Func&lt;&gt; 委托属性，提供编译时类型安全。
/// </summary>
public interface IMedicalCaseDataProvider
{
    ConsultationInputDto? GetConsultationData();
    PrescriptionInputDto? GetPrescriptionData();
    IValidatable? GetConsultationValidator();
    IValidatable? GetPrescriptionValidator();
    IDataProvider? GetPrescriptionProvider();
    ConsultationItem? GetConsultationItem();
    PrescriptionItem? GetPrescriptionItem();
    IEnumerable<HerbListDto>? GetAllHerbs();
    string GetRemark();
    string GetEditReason();
    bool GetIsPrescriptionEnabled();
}
