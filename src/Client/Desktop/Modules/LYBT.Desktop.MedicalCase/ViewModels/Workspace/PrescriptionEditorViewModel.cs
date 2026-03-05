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

    public PrescriptionItem Prescription { get; } = new();

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
        var item = _mapper.ToItem(dto);
        CopyToPrescription(item);
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

    // Copy mapped item properties to our owned Prescription instance.
    // We maintain a single Prescription instance for stable XAML binding reference.
    private void CopyToPrescription(PrescriptionItem source)
    {
        Prescription.Id = source.Id;
        Prescription.PrescriptionNumber = source.PrescriptionNumber;
        Prescription.MedicalCaseId = source.MedicalCaseId;
        Prescription.DosageCount = source.DosageCount;
        Prescription.Usage = source.Usage;
        Prescription.Advice = source.Advice;
        Prescription.ReferencedFormulas = source.ReferencedFormulas;
        Prescription.Remark = source.Remark;
        Prescription.Discount = source.Discount;
        Prescription.SingleDosePrice = source.SingleDosePrice;
        Prescription.TotalWeight = source.TotalWeight;
        Prescription.Status = source.Status;
        Prescription.CreatedAt = source.CreatedAt;
        Prescription.UpdatedAt = source.UpdatedAt;
        Prescription.DuplicateWarning = source.DuplicateWarning;
        Prescription.MissingDrugWarning = source.MissingDrugWarning;

        // Copy Items collection: unsubscribe, replace, resubscribe
        Prescription.Items.CollectionChanged -= OnItemsCollectionChanged;
        Prescription.Items.Clear();
        foreach (var item in source.Items)
        {
            Prescription.Items.Add(item);
        }
        Prescription.Items.CollectionChanged += OnItemsCollectionChanged;
        Prescription.NotifyItemsChanged();
        OnPropertyChanged(nameof(HasItems));
    }
}
