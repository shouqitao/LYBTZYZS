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
/// 处方数据编辑的子级 ViewModel。
/// 包装 PrescriptionItemViewModel，处理 DTO 初始化和集合变更通知。
/// Items 集合变化时通知父级以重新计算状态（CanComplete、CanPrint）。
/// </summary>
public class PrescriptionEditorViewModel : ChildViewModelBase
{
    private readonly IMedicalCaseWorkspaceContext _context;
    private readonly PrescriptionMapper _mapper = new();

    private PrescriptionItemViewModel _prescription = new();

    public PrescriptionItemViewModel Prescription
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
    /// 处方是否包含任何药材条目。
    /// 父级用于状态计算（CanPrint、CanComplete）。
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
    /// 从现有处方 DTO 初始化（恢复/查看医案）。
    /// </summary>
    public void InitializeFromDto(PrescriptionDetailDto dto)
    {
        Prescription = _mapper.ToItem(dto);
    }

    /// <summary>
    /// 为新建医案初始化。
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
