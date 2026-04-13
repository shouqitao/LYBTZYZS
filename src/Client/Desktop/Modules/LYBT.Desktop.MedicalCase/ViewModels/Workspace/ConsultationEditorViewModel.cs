using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Composition;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Mappers;
using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.ViewModels.Workspace;

/// <summary>
/// Child VM for consultation (diagnosis) data editing.
/// Wraps ConsultationItem for XAML binding, handles DTO initialization via ConsultationMapper.
/// Replaces manual field-by-field copy in InitializeChildViewModels().
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
    /// Initialize from existing consultation DTO (resume/view case).
    /// </summary>
    public void InitializeFromDto(ConsultationDetailDto dto)
    {
        Consultation = _mapper.ToItem(dto);
    }

    /// <summary>
    /// Initialize for new case creation.
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
