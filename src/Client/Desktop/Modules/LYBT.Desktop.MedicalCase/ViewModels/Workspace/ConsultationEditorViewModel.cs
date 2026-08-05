using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Composition;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Mappers;
using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.ViewModels.Workspace;

/// <summary>
/// 会诊（诊断）数据编辑的子级 ViewModel。
/// 包装 ConsultationItem 供 XAML 绑定，通过 ConsultationMapper 处理 DTO 初始化。
/// 取代 InitializeChildViewModels() 中逐字段复制的方式。
/// </summary>
public class ConsultationEditorViewModel : ChildViewModelBase
{
    private readonly IMedicalCaseWorkspaceContext _context;
    private readonly ConsultationMapper _mapper = new();
    private ConsultationItem _consultation = new();

    public ConsultationItem Consultation
    {
        get => _consultation;
        set => SetProperty(ref _consultation, value);
    }

    public ConsultationEditorViewModel(
        IMedicalCaseWorkspaceContext context, IWorkspaceHost host, ILoggerFactory loggerFactory)
        : base(host, loggerFactory)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// 从现有会诊 DTO 初始化（恢复/查看医案）。
    /// </summary>
    public void InitializeFromDto(ConsultationDetailDto dto)
    {
        Consultation = _mapper.ToItem(dto);
    }

    /// <summary>
    /// 为新建医案初始化。
    /// </summary>
    public void InitializeForNewCase(string patientName, Guid patientId, Guid userId)
    {
        Consultation.Reset();
        Consultation.PatientName = patientName;
        Consultation.PatientId = patientId;
        Consultation.UserId = userId;
        Consultation.MedicalCaseId = _context.MedicalCaseId;
    }

    public ConsultationInputDto? GetConsultationData() => Consultation.GetConsultationData();
    public bool Validate() => Consultation.Validate();
    public string ValidationMessage => Consultation.ValidationMessage;

    public void Reset() => Consultation.Reset();

}
