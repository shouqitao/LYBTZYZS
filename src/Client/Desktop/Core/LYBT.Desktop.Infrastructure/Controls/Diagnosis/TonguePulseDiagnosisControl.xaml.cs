using System.Windows;
using System.Windows.Controls;

namespace LYBT.Desktop.Infrastructure.Controls.Diagnosis;

/// <summary>
/// 舌诊脉诊组合控件
/// 提供舌诊和脉诊的输入字段和选项列表
/// 可复用的诊断输入组件，减少XAML代码重复
/// </summary>
public partial class TonguePulseDiagnosisControl : UserControl
{
    public TonguePulseDiagnosisControl()
    {
        InitializeComponent();
    }

    #region 舌诊 依赖属性

    public static readonly DependencyProperty TongueDiagnosisProperty =
        DependencyProperty.Register(nameof(TongueDiagnosis), typeof(string), typeof(TonguePulseDiagnosisControl),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public string? TongueDiagnosis
    {
        get => (string?)GetValue(TongueDiagnosisProperty);
        set => SetValue(TongueDiagnosisProperty, value);
    }

    #endregion

    #region 脉诊 依赖属性

    public static readonly DependencyProperty PulseDiagnosisProperty =
        DependencyProperty.Register(nameof(PulseDiagnosis), typeof(string), typeof(TonguePulseDiagnosisControl),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public string? PulseDiagnosis
    {
        get => (string?)GetValue(PulseDiagnosisProperty);
        set => SetValue(PulseDiagnosisProperty, value);
    }

    #endregion

    #region TabIndex 控制属性

    public static readonly DependencyProperty TongueTabIndexProperty =
        DependencyProperty.Register(nameof(TongueTabIndex), typeof(int), typeof(TonguePulseDiagnosisControl),
            new PropertyMetadata(0));

    public int TongueTabIndex
    {
        get => (int)GetValue(TongueTabIndexProperty);
        set => SetValue(TongueTabIndexProperty, value);
    }

    public static readonly DependencyProperty PulseTabIndexProperty =
        DependencyProperty.Register(nameof(PulseTabIndex), typeof(int), typeof(TonguePulseDiagnosisControl),
            new PropertyMetadata(1));

    public int PulseTabIndex
    {
        get => (int)GetValue(PulseTabIndexProperty);
        set => SetValue(PulseTabIndexProperty, value);
    }

    #endregion
}
