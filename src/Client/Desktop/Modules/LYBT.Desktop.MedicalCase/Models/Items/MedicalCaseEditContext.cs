using CommunityToolkit.Mvvm.ComponentModel;

namespace LYBT.Desktop.MedicalCase.Models.Items;

/// <summary>
/// 医案编辑上下文 - 诊断信息编辑
/// 用于编辑→取消→恢复场景
/// 属性对齐 MedicalCaseDetailModel 中的可编辑诊断字段
/// </summary>
public class MedicalCaseEditContext : ObservableObject
{
    private string? _presentIllness;
    private string? _tongueDiagnosis;
    private string? _pulseDiagnosis;
    private string? _tcmDiagnosis;
    private string? _remark;

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

    /// <summary>备注</summary>
    public string? Remark
    {
        get => _remark;
        set => SetProperty(ref _remark, value);
    }

    /// <summary>创建空上下文</summary>
    public static MedicalCaseEditContext CreateNew()
    {
        return new MedicalCaseEditContext();
    }

    /// <summary>从详情模型初始化</summary>
    public void InitializeFromModel(MedicalCaseDetailModel model)
    {
        PresentIllness = model.PresentIllness;
        TongueDiagnosis = model.TongueDiagnosis;
        PulseDiagnosis = model.PulseDiagnosis;
        TcmDiagnosis = model.TcmDiagnosis;
    }

    /// <summary>应用到详情模型</summary>
    public void ApplyToModel(MedicalCaseDetailModel model)
    {
        model.PresentIllness = PresentIllness;
        model.TongueDiagnosis = TongueDiagnosis;
        model.PulseDiagnosis = PulseDiagnosis;
        model.TcmDiagnosis = TcmDiagnosis;
    }

    /// <summary>克隆上下文</summary>
    public MedicalCaseEditContext Clone()
    {
        return new MedicalCaseEditContext
        {
            PresentIllness = PresentIllness,
            TongueDiagnosis = TongueDiagnosis,
            PulseDiagnosis = PulseDiagnosis,
            TcmDiagnosis = TcmDiagnosis,
            Remark = Remark
        };
    }
}
