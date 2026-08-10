using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.MedicalCase.Models.Items;

/// <summary>
/// 医案编辑上下文 - 完整编辑会话
/// 承载可编辑状态（诊断字段 + 处方行集合 + 状态），支持 BeginEdit/Commit/Cancel 与脏检测。
/// 由 CommandService 与 LifecycleService 共享（模块内单例注册），保证编辑会话状态一致。
/// </summary>
public class MedicalCaseEditContext : ObservableObject
{
    private string? _presentIllness;
    private string? _tongueDiagnosis;
    private string? _pulseDiagnosis;
    private string? _tcmDiagnosis;
    private string? _remark;
    private MedicalCaseStatus _status = MedicalCaseStatus.Suspended;
    private ObservableCollection<PrescriptionItemModel> _prescriptionItems = new();

    // 会话底层模型与基线快照
    private MedicalCaseDetailModel? _currentModel;
    private MedicalCaseDetailModel? _originalModel;
    private string? _originalRemark;

    /// <summary>现病史</summary>
    public string? PresentIllness
    {
        get => _presentIllness;
        set => SetProperty(ref _presentIllness, value);
    }

    /// <summary>舌诊</summary>
    public string? TongueDiagnosis
    {
        get => _tongueDiagnosis;
        set => SetProperty(ref _tongueDiagnosis, value);
    }

    /// <summary>脉诊</summary>
    public string? PulseDiagnosis
    {
        get => _pulseDiagnosis;
        set => SetProperty(ref _pulseDiagnosis, value);
    }

    /// <summary>中医诊断</summary>
    public string? TcmDiagnosis
    {
        get => _tcmDiagnosis;
        set => SetProperty(ref _tcmDiagnosis, value);
    }

    /// <summary>备注（上下文自有，MedicalCaseDetailModel 不承载）</summary>
    public string? Remark
    {
        get => _remark;
        set => SetProperty(ref _remark, value);
    }

    /// <summary>状态</summary>
    public MedicalCaseStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    /// <summary>处方行集合</summary>
    public ObservableCollection<PrescriptionItemModel> PrescriptionItems
    {
        get => _prescriptionItems;
        set => SetProperty(ref _prescriptionItems, value);
    }

    /// <summary>当前编辑中的详情模型（BeginEdit 时装载）</summary>
    public MedicalCaseDetailModel? CurrentModel => _currentModel;

    /// <summary>是否有未提交的变更（对比 BeginEdit/Commit 基线）</summary>
    public bool IsDirty => _currentModel != null && _originalModel != null && HasEditableChanges();

    /// <summary>创建空上下文</summary>
    public static MedicalCaseEditContext CreateNew()
    {
        return new MedicalCaseEditContext();
    }

    /// <summary>开始编辑会话：装载模型并从模型快照初始化可编辑状态</summary>
    public void BeginEdit(MedicalCaseDetailModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        _currentModel = model;
        _originalModel = model.Clone();
        _originalRemark = Remark;
        InitializeFromModel(model);
    }

    /// <summary>提交：将可编辑状态应用到当前模型并前移基线</summary>
    public void Commit()
    {
        if (_currentModel == null) return;
        ApplyToModel(_currentModel);
        _originalModel = _currentModel.Clone();
        _originalRemark = Remark;
    }

    /// <summary>取消：从基线快照恢复当前模型与可编辑状态</summary>
    public void Cancel()
    {
        if (_originalModel == null) return;
        _currentModel = _originalModel.Clone();
        Remark = _originalRemark;
        InitializeFromModel(_currentModel);
    }

    /// <summary>清空会话</summary>
    public void Clear()
    {
        _currentModel = null;
        _originalModel = null;
        _originalRemark = null;
    }

    /// <summary>从详情模型初始化可编辑字段（Remark 为上下文自有，Model 不承载）</summary>
    public void InitializeFromModel(MedicalCaseDetailModel model)
    {
        PresentIllness = model.PresentIllness;
        TongueDiagnosis = model.TongueDiagnosis;
        PulseDiagnosis = model.PulseDiagnosis;
        TcmDiagnosis = model.TcmDiagnosis;
        Status = model.Status;
        PrescriptionItems = new ObservableCollection<PrescriptionItemModel>(model.PrescriptionItems.Select(i => i.Clone()));
    }

    /// <summary>将可编辑字段应用到详情模型</summary>
    public void ApplyToModel(MedicalCaseDetailModel model)
    {
        model.PresentIllness = PresentIllness;
        model.TongueDiagnosis = TongueDiagnosis;
        model.PulseDiagnosis = PulseDiagnosis;
        model.TcmDiagnosis = TcmDiagnosis;
        model.Status = Status;
        model.PrescriptionItems = new ObservableCollection<PrescriptionItemModel>(PrescriptionItems.Select(i => i.Clone()));
    }

    /// <summary>克隆上下文（含可编辑状态）</summary>
    public MedicalCaseEditContext Clone()
    {
        return new MedicalCaseEditContext
        {
            PresentIllness = PresentIllness,
            TongueDiagnosis = TongueDiagnosis,
            PulseDiagnosis = PulseDiagnosis,
            TcmDiagnosis = TcmDiagnosis,
            Remark = Remark,
            Status = Status,
            PrescriptionItems = new ObservableCollection<PrescriptionItemModel>(PrescriptionItems.Select(i => i.Clone()))
        };
    }

    private bool HasEditableChanges()
    {
        if (_originalModel == null) return false;
        return PresentIllness != _originalModel.PresentIllness ||
               TongueDiagnosis != _originalModel.TongueDiagnosis ||
               PulseDiagnosis != _originalModel.PulseDiagnosis ||
               TcmDiagnosis != _originalModel.TcmDiagnosis ||
               Remark != _originalRemark ||
               Status != _originalModel.Status ||
               !PrescriptionItemsEqual(PrescriptionItems, _originalModel.PrescriptionItems);
    }

    private static bool PrescriptionItemsEqual(ObservableCollection<PrescriptionItemModel> a, ObservableCollection<PrescriptionItemModel> b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null) return false;
        if (a.Count != b.Count) return false;

        for (var i = 0; i < a.Count; i++)
        {
            var x = a[i];
            var y = b[i];
            if (x.Id != y.Id ||
                x.HerbId != y.HerbId ||
                x.HerbName != y.HerbName ||
                x.Unit != y.Unit ||
                x.UnitPrice != y.UnitPrice ||
                x.Dosage != y.Dosage ||
                x.Usage != y.Usage ||
                x.DecocteMethod != y.DecocteMethod ||
                x.Role != y.Role ||
                x.Remark != y.Remark)
            {
                return false;
            }
        }

        return true;
    }
}
