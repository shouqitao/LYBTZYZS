using System;
using System.Windows.Media;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.ViewModels.MedicalCase
{
    /// <summary>
    /// 病例展示主题 ViewModel（统一使用 Active/Closed 语义）。
    /// </summary>
    public class MedicalCaseThemeViewModel : BindableBase
    {
        private readonly MedicalCaseDto _medicalCaseData;

        public MedicalCaseThemeViewModel(MedicalCaseDto medicalCaseData)
        {
            _medicalCaseData = medicalCaseData ?? throw new ArgumentNullException(nameof(medicalCaseData));
        }

        // 基础配色
        public Brush BackgroundBrush => _medicalCaseData.CaseStatus switch
        {
            MedicalCaseStatus.Active => new SolidColorBrush(Color.FromRgb(230, 245, 255)),
            MedicalCaseStatus.Closed => new SolidColorBrush(Color.FromRgb(230, 255, 230)),
            _ => new SolidColorBrush(Color.FromRgb(248, 248, 248))
        };

        public Brush BorderBrush => _medicalCaseData.CaseStatus switch
        {
            MedicalCaseStatus.Active => new SolidColorBrush(Color.FromRgb(0, 123, 255)),
            MedicalCaseStatus.Closed => new SolidColorBrush(Color.FromRgb(40, 167, 69)),
            _ => new SolidColorBrush(Color.FromRgb(206, 212, 218))
        };

        public Brush TextBrush => _medicalCaseData.CaseStatus switch
        {
            MedicalCaseStatus.Closed => new SolidColorBrush(Color.FromRgb(108, 117, 125)),
            _ => new SolidColorBrush(Color.FromRgb(33, 37, 41))
        };

        public Brush AccentBrush => _medicalCaseData.CaseStatus == MedicalCaseStatus.Active
            ? new SolidColorBrush(Color.FromRgb(220, 53, 69))
            : new SolidColorBrush(Color.FromRgb(76, 175, 80));

        // 状态指示
        public Brush StatusIconBrush => _medicalCaseData.CaseStatus switch
        {
            MedicalCaseStatus.Active => new SolidColorBrush(Color.FromRgb(0, 123, 255)),
            MedicalCaseStatus.Closed => new SolidColorBrush(Color.FromRgb(40, 167, 69)),
            _ => new SolidColorBrush(Color.FromRgb(108, 117, 125))
        };

        public Brush PriorityBrush => _medicalCaseData.CaseStatus switch
        {
            MedicalCaseStatus.Active => new SolidColorBrush(Color.FromRgb(0, 123, 255)),
            MedicalCaseStatus.Closed => new SolidColorBrush(Color.FromRgb(40, 167, 69)),
            _ => new SolidColorBrush(Color.FromRgb(108, 117, 125))
        };

        public double ProgressPercentage => _medicalCaseData.CaseStatus switch
        {
            MedicalCaseStatus.Active => 50.0,
            MedicalCaseStatus.Closed => 100.0,
            _ => 0.0
        };

        // 动态样式参数
        public double BorderThickness => _medicalCaseData.CaseStatus switch
        {
            MedicalCaseStatus.Closed => 1.0,
            _ => 2.0
        };

        public double CornerRadius => 6.0;

        public double ShadowDepth => _medicalCaseData.CaseStatus switch
        {
            MedicalCaseStatus.Closed => 1.0,
            _ => 3.0
        };

        public double Opacity => _medicalCaseData.CaseStatus switch
        {
            MedicalCaseStatus.Closed => 0.95,
            _ => 1.0
        };

        // 时间信息配色
        public Brush DurationBrush => _medicalCaseData.CaseStatus switch
        {
            MedicalCaseStatus.Active => new SolidColorBrush(Color.FromRgb(255, 193, 7)),
            MedicalCaseStatus.Closed => new SolidColorBrush(Color.FromRgb(40, 167, 69)),
            _ => new SolidColorBrush(Color.FromRgb(33, 37, 41))
        };

        public Brush CreateTimeBrush => _medicalCaseData.CaseStatus switch
        {
            MedicalCaseStatus.Active => new SolidColorBrush(Color.FromRgb(0, 123, 255)),
            MedicalCaseStatus.Closed => new SolidColorBrush(Color.FromRgb(108, 117, 125)),
            _ => new SolidColorBrush(Color.FromRgb(108, 117, 125))
        };

        public Brush UrgencyBrush => _medicalCaseData.CaseStatus switch
        {
            MedicalCaseStatus.Active => new SolidColorBrush(Color.FromRgb(255, 87, 34)),
            MedicalCaseStatus.Closed => new SolidColorBrush(Color.FromRgb(108, 117, 125)),
            _ => new SolidColorBrush(Color.FromRgb(108, 117, 125))
        };

        public Brush WarningBrush => new SolidColorBrush(Color.FromRgb(255, 193, 7));

        // 主题片段获取
        public (Brush Background, Brush Text, Brush Border) GetButtonTheme(string buttonType)
        {
            return buttonType switch
            {
                "StartConsultation" => (
                    new SolidColorBrush(Color.FromRgb(0, 123, 255)),
                    new SolidColorBrush(Colors.White),
                    new SolidColorBrush(Color.FromRgb(0, 123, 255))),
                "Complete" => (
                    new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                    new SolidColorBrush(Colors.White),
                    new SolidColorBrush(Color.FromRgb(40, 167, 69))),
                "Cancel" => (
                    new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                    new SolidColorBrush(Color.FromRgb(33, 37, 41)),
                    new SolidColorBrush(Color.FromRgb(255, 193, 7))),
                "Delete" => (
                    new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                    new SolidColorBrush(Colors.White),
                    new SolidColorBrush(Color.FromRgb(220, 53, 69))),
                _ => (
                    new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                    new SolidColorBrush(Colors.White),
                    new SolidColorBrush(Color.FromRgb(108, 117, 125)))
            };
        }

        public (Brush Background, string Text) GetPriorityIndicator()
        {
            var (background, text) = _medicalCaseData.CaseStatus switch
            {
                MedicalCaseStatus.Active => (new SolidColorBrush(Color.FromRgb(220, 53, 69)), "急"),
                MedicalCaseStatus.Closed => (new SolidColorBrush(Color.FromRgb(40, 167, 69)), "完"),
                _ => (new SolidColorBrush(Color.FromRgb(108, 117, 125)), "—")
            };

            return (background, text);
        }

        public (Brush LineColor, Brush NodeColor, double NodeSize) GetTimelineStyle()
        {
            var lineColor = _medicalCaseData.CaseStatus switch
            {
                MedicalCaseStatus.Active => new SolidColorBrush(Color.FromRgb(0, 123, 255)),
                MedicalCaseStatus.Closed => new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                _ => new SolidColorBrush(Color.FromRgb(206, 212, 218))
            };

            var nodeColor = StatusIconBrush;
            var nodeSize = _medicalCaseData.CaseStatus == MedicalCaseStatus.Active ? 12.0 : 8.0;
            return (lineColor, nodeColor, nodeSize);
        }

        // 响应式与可访问性
        public void UpdateTimeBasedTheme()
        {
            RaisePropertyChanged(nameof(DurationBrush));
            RaisePropertyChanged(nameof(CreateTimeBrush));
            RaisePropertyChanged(nameof(UrgencyBrush));
            RaisePropertyChanged(nameof(WarningBrush));
        }

        public bool IsHighContrastMode { get; set; } = false;

        public (Brush Background, Brush Foreground, Brush Border) GetHighContrastTheme()
        {
            if (!IsHighContrastMode)
            {
                return (BackgroundBrush, TextBrush, BorderBrush);
            }

            return (
                new SolidColorBrush(Colors.White),
                new SolidColorBrush(Colors.Black),
                new SolidColorBrush(Colors.Black));
        }
    }
}

