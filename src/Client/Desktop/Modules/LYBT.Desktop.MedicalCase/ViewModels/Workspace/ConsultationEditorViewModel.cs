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

    public ConsultationItem Consultation { get; } = new();

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
        var item = _mapper.ToItem(dto);
        CopyToConsultation(item);
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

    // Copy mapped item properties to our owned Consultation instance.
    // We maintain a single Consultation instance for stable XAML binding reference.
    private void CopyToConsultation(ConsultationItem source)
    {
        Consultation.Id = source.Id;
        Consultation.MedicalCaseId = source.MedicalCaseId;
        Consultation.PatientId = source.PatientId;
        Consultation.UserId = source.UserId;
        Consultation.PatientName = source.PatientName;
        Consultation.DoctorName = source.DoctorName;
        Consultation.PresentIllness = source.PresentIllness;
        Consultation.TongueDiagnosis = source.TongueDiagnosis;
        Consultation.PulseDiagnosis = source.PulseDiagnosis;
        Consultation.TcmDiagnosis = source.TcmDiagnosis;
        Consultation.CreatedAt = source.CreatedAt;
        Consultation.UpdatedAt = source.UpdatedAt;
    }
}
