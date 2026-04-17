using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace LYBT.Desktop.MedicalCase.Controls;

/// <summary>
/// 工作流步骤指示器控件
/// 显示5个步骤：四诊采集 → 中医辨证 → 处方决策 → 处方编辑 → 完成看诊
/// 高亮当前步骤，灰色显示已完成步骤
/// </summary>
public partial class WorkflowStepIndicator : UserControl
{
    public WorkflowStepIndicator()
    {
        InitializeComponent();
        Steps = new ObservableCollection<WorkflowStep>();
        UpdateSteps();
    }

    #region CurrentStep 依赖属性

    public static readonly DependencyProperty CurrentStepProperty =
        DependencyProperty.Register(nameof(CurrentStep), typeof(int), typeof(WorkflowStepIndicator),
            new PropertyMetadata(1, OnCurrentStepChanged));

    public int CurrentStep
    {
        get => (int)GetValue(CurrentStepProperty);
        set => SetValue(CurrentStepProperty, value);
    }

    private static void OnCurrentStepChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WorkflowStepIndicator indicator)
        {
            indicator.UpdateSteps();
        }
    }

    #endregion

    #region StepWidth 依赖属性

    public static readonly DependencyProperty StepWidthProperty =
        DependencyProperty.Register(nameof(StepWidth), typeof(double), typeof(WorkflowStepIndicator),
            new PropertyMetadata(120.0));

    public double StepWidth
    {
        get => (double)GetValue(StepWidthProperty);
        set => SetValue(StepWidthProperty, value);
    }

    #endregion

    #region Steps 集合属性

    public ObservableCollection<WorkflowStep> Steps { get; }

    #endregion

    /// <summary>
    /// 更新步骤显示状态
    /// </summary>
    private void UpdateSteps()
    {
        Steps.Clear();

        var stepDefinitions = new[]
        {
            new { Number = 1, Label = "四诊采集" },
            new { Number = 2, Label = "中医辨证" },
            new { Number = 3, Label = "处方决策" },
            new { Number = 4, Label = "处方编辑" },
            new { Number = 5, Label = "完成看诊" }
        };

        for (int i = 0; i < stepDefinitions.Length; i++)
        {
            var stepDef = stepDefinitions[i];
            int stepNumber = stepDef.Number;
            
            Steps.Add(new WorkflowStep
            {
                Number = stepNumber,
                Label = stepDef.Label,
                IsActive = stepNumber == CurrentStep,
                IsCompleted = stepNumber < CurrentStep,
                IsLast = i == stepDefinitions.Length - 1
            });
        }
    }
}

/// <summary>
/// 工作流步骤数据模型
/// </summary>
public class WorkflowStep
{
    public int Number { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsLast { get; set; }
}
