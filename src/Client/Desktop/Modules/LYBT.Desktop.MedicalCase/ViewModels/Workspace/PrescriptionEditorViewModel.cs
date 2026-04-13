using System.Collections.Specialized;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Composition;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Mappers;
using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.ViewModels.Workspace;

/// <summary>
/// Child VM for prescription data editing.
/// Wraps PrescriptionItem, handles DTO initialization and collection change notifications.
/// Notifies parent when Items collection changes for state recalculation (CanComplete, CanPrint).
/// </summary>
public class PrescriptionEditorViewModel : ChildViewModelBase
{
    private readonly IMedicalCaseWorkspaceContext _context;
    private readonly PrescriptionMapper _mapper = new();

    private PrescriptionItem _prescription = new();

    public PrescriptionItem Prescription
    {
        get => _prescription;
        set
        {
            if (SetProperty(ref _prescription, value))
            {
                _prescription.Items.CollectionChanged -= OnItemsCollectionChanged;
                _prescription.Items.CollectionChanged += OnItemsCollectionChanged;
                OnPropertyChanged(nameof(HasItems));
            }
        }
    }

    /// <summary>
    /// Whether the prescription has any herb items.
    /// Used by parent for state computation (CanPrint, CanComplete).
    /// </summary>
    public bool HasItems => Prescription.HasItems;

    public PrescriptionEditorViewModel(
        IMedicalCaseWorkspaceContext context, IWorkspaceHost host, ILoggerFactory loggerFactory)
        : base(host, loggerFactory)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        Prescription.Items.CollectionChanged += OnItemsCollectionChanged;
    }

    /// <summary>
    /// Initialize from existing prescription DTO (resume/view case).
    /// </summary>
    public void InitializeFromDto(PrescriptionDetailDto dto)
    {
        Prescription = _mapper.ToItem(dto);
    }

    /// <summary>
    /// Initialize for new case creation.
    /// </summary>
    public void InitializeForNewCase()
    {
        Prescription.Clear();
        Prescription.MedicalCaseId = _context.MedicalCaseId;
    }

    public PrescriptionInputDto? GetPrescriptionData() => Prescription.GetPrescriptionData();
    public bool Validate() => Prescription.Validate();
    public string ValidationMessage => Prescription.ValidationMessage;

    public void Reset()
    {
        Prescription.Reset();
        OnPropertyChanged(nameof(HasItems));
    }

    public override void Dispose()
    {
        Prescription.Items.CollectionChanged -= OnItemsCollectionChanged;
        base.Dispose();
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Prescription.NotifyItemsChanged();
        OnPropertyChanged(nameof(HasItems));
        Host.NotifyStateChanged();
    }
}
