using LYBT.Desktop.MedicalCase.Models; // Issue #1806: ConsultationStep枚举
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Services;

/// <summary>
/// 医案流程管理器 - 负责三步流程状态管理和导航协调
/// Issue #1806: 从MedicalCaseFlowViewModel提取流程管理逻辑(~250行)
/// </summary>
public class MedicalCaseFlowManager
{
    private readonly ILogger<MedicalCaseFlowManager> _logger;

    private ConsultationStep _currentStep = ConsultationStep.Consultation;

    /// <summary>
    /// 当前流程步骤
    /// </summary>
    public ConsultationStep CurrentStep
    {
        get => _currentStep;
        private set
        {
            if (_currentStep != value)
            {
                _currentStep = value;
                OnStepChanged();
            }
        }
    }

    /// <summary>
    /// 当前步骤名称文本
    /// </summary>
    public string CurrentStepText { get; private set; } = "辨证";

    /// <summary>
    /// 是否可以返回上一步
    /// </summary>
    public bool CanGoBack => CurrentStep > ConsultationStep.Consultation;

    /// <summary>
    /// 是否可以前进下一步
    /// </summary>
    public bool CanGoNext => CurrentStep < ConsultationStep.Completion;

    /// <summary>
    /// 下一步按钮文字
    /// </summary>
    public string NextButtonText => CurrentStep == ConsultationStep.Completion ? "完成病案" : "下一步";

    /// <summary>
    /// 上一步按钮文字
    /// </summary>
    public string PreviousButtonText => "上一步";

    /// <summary>
    /// 步骤变更事件
    /// </summary>
    public event EventHandler<StepChangedEventArgs>? StepChanged;

    public MedicalCaseFlowManager(ILogger<MedicalCaseFlowManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        UpdateCurrentStepText();
    }

    /// <summary>
    /// 前进到下一步
    /// </summary>
    public bool MoveToNextStep()
    {
        if (!CanGoNext)
        {
            _logger.LogWarning("已是最后一步，无法前进");
            return false;
        }

        try
        {
            var nextStep = (ConsultationStep)((int)CurrentStep + 1);
            _logger.LogInformation("从 {CurrentStep} 前进到 {NextStep}", CurrentStep, nextStep);
            CurrentStep = nextStep;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "前进到下一步时发生异常");
            return false;
        }
    }

    /// <summary>
    /// 返回上一步
    /// </summary>
    public bool MoveToPreviousStep()
    {
        if (!CanGoBack)
        {
            _logger.LogWarning("已是第一步，无法返回");
            return false;
        }

        try
        {
            var previousStep = (ConsultationStep)((int)CurrentStep - 1);
            _logger.LogInformation("从 {CurrentStep} 返回到 {PreviousStep}", CurrentStep, previousStep);
            CurrentStep = previousStep;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "返回上一步时发生异常");
            return false;
        }
    }

    /// <summary>
    /// 直接设置到指定步骤
    /// </summary>
    public void SetStep(ConsultationStep step)
    {
        _logger.LogInformation("设置当前步骤为：{Step}", step);
        CurrentStep = step;
    }

    /// <summary>
    /// 重置到初始步骤
    /// </summary>
    public void Reset()
    {
        _logger.LogInformation("重置流程到初始步骤");
        CurrentStep = ConsultationStep.Consultation;
    }

    /// <summary>
    /// 获取步骤对应的View名称
    /// </summary>
    public string GetViewNameForStep(ConsultationStep step)
    {
        return step switch
        {
            ConsultationStep.Consultation => "ConsultationFormView",
            ConsultationStep.Prescription => "PrescriptionEditorView",
            ConsultationStep.Completion => "CompletionView",
            _ => throw new ArgumentOutOfRangeException(nameof(step), $"未知步骤：{step}")
        };
    }

    /// <summary>
    /// 获取步骤显示名称
    /// </summary>
    public string GetStepDisplayName(ConsultationStep step)
    {
        return step switch
        {
            ConsultationStep.Consultation => "辨证",
            ConsultationStep.Prescription => "施治",
            ConsultationStep.Completion => "完成",
            _ => string.Empty
        };
    }

    /// <summary>
    /// 验证是否可以执行下一步
    /// </summary>
    public bool ValidateCanExecuteNext(bool isBusy)
    {
        // 如果正在处理中，禁用下一步
        if (isBusy)
        {
            return false;
        }

        // 所有步骤都允许前进（数据验证在业务层处理）
        return CurrentStep switch
        {
            ConsultationStep.Consultation => true, // Step 1: 辨证（可选，允许前进）
            ConsultationStep.Prescription => true, // Step 2: 施治（可选，允许前进）
            ConsultationStep.Completion => true,   // Step 3: 完成确认
            _ => false
        };
    }

    /// <summary>
    /// 更新当前步骤名称文本
    /// </summary>
    private void UpdateCurrentStepText()
    {
        CurrentStepText = GetStepDisplayName(CurrentStep);
    }

    /// <summary>
    /// 触发步骤变更事件
    /// </summary>
    private void OnStepChanged()
    {
        UpdateCurrentStepText();

        StepChanged?.Invoke(this, new StepChangedEventArgs
        {
            PreviousStep = CurrentStep,
            CurrentStep = CurrentStep,
            StepText = CurrentStepText,
            CanGoBack = CanGoBack,
            CanGoNext = CanGoNext,
            NextButtonText = NextButtonText,
            PreviousButtonText = PreviousButtonText
        });
    }
}

/// <summary>
/// 步骤变更事件参数
/// </summary>
public class StepChangedEventArgs : EventArgs
{
    public ConsultationStep PreviousStep { get; set; }
    public ConsultationStep CurrentStep { get; set; }
    public string StepText { get; set; } = string.Empty;
    public bool CanGoBack { get; set; }
    public bool CanGoNext { get; set; }
    public string NextButtonText { get; set; } = string.Empty;
    public string PreviousButtonText { get; set; } = string.Empty;
}
