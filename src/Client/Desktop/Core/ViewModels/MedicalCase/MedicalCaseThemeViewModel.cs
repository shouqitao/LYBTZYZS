using System.Windows.Media;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.ViewModels.MedicalCase {

    /// <summary>
    /// 医疗案例主题样式视图模型 - UltraThink架构Presentation Layer
    /// 专门处理医疗案例的主题、颜色、样式等视觉呈现逻辑
    /// </summary>
    public class MedicalCaseThemeViewModel : BindableBase {
        private readonly MedicalCaseDto _medicalCaseData;

        public MedicalCaseThemeViewModel(MedicalCaseDto medicalCaseData) {
            _medicalCaseData = medicalCaseData ?? throw new ArgumentNullException(nameof(medicalCaseData));
        }

        #region 主题配色

        /// <summary>主背景色</summary>
        public Brush BackgroundBrush => _medicalCaseData.CaseStatus switch {
            MedicalCaseStatus.Registered => new SolidColorBrush(Color.FromRgb(255, 248, 230)), // 浅橙色
            MedicalCaseStatus.InConsultation => new SolidColorBrush(Color.FromRgb(230, 245, 255)), // 浅蓝色
            MedicalCaseStatus.Completed => new SolidColorBrush(Color.FromRgb(230, 255, 230)), // 浅绿色
            MedicalCaseStatus.Cancelled => new SolidColorBrush(Color.FromRgb(255, 230, 230)), // 浅红色
            _ => new SolidColorBrush(Color.FromRgb(248, 248, 248)) // 默认灰色
        };

        /// <summary>边框颜色</summary>
        public Brush BorderBrush => _medicalCaseData.CaseStatus switch {
            MedicalCaseStatus.Registered => new SolidColorBrush(Color.FromRgb(255, 193, 7)), // 橙色
            MedicalCaseStatus.InConsultation => new SolidColorBrush(Color.FromRgb(0, 123, 255)), // 蓝色
            MedicalCaseStatus.Completed => new SolidColorBrush(Color.FromRgb(40, 167, 69)), // 绿色
            MedicalCaseStatus.Cancelled => new SolidColorBrush(Color.FromRgb(220, 53, 69)), // 红色
            _ => new SolidColorBrush(Color.FromRgb(206, 212, 218)) // 默认边框色
        };

        /// <summary>文本颜色</summary>
        public Brush TextBrush => _medicalCaseData.CaseStatus switch {
            MedicalCaseStatus.Cancelled => new SolidColorBrush(Color.FromRgb(108, 117, 125)), // 取消时灰色文本
            _ => new SolidColorBrush(Color.FromRgb(33, 37, 41)) // 默认深色文本
        };

        /// <summary>强调色（用于重要信息）</summary>
        public Brush AccentBrush => _medicalCaseData.CaseStatus == MedicalCaseStatus.Registered
            ? new SolidColorBrush(Color.FromRgb(220, 53, 69)) // UltraThink v2.0简化：按状态判断紧急程度
            : new SolidColorBrush(Color.FromRgb(76, 175, 80)); // 正常时绿色

        #endregion 主题配色

        #region 状态指示颜色

        /// <summary>状态图标颜色</summary>
        public Brush StatusIconBrush => _medicalCaseData.CaseStatus switch {
            MedicalCaseStatus.Registered => new SolidColorBrush(Color.FromRgb(255, 193, 7)), // 橙色
            MedicalCaseStatus.InConsultation => new SolidColorBrush(Color.FromRgb(0, 123, 255)), // 蓝色
            MedicalCaseStatus.Completed => new SolidColorBrush(Color.FromRgb(40, 167, 69)), // 绿色
            MedicalCaseStatus.Cancelled => new SolidColorBrush(Color.FromRgb(220, 53, 69)), // 红色
            _ => new SolidColorBrush(Color.FromRgb(108, 117, 125)) // 默认灰色
        };

        /// <summary>优先级颜色</summary>
        public Brush PriorityBrush => _medicalCaseData.CaseStatus switch {
            MedicalCaseStatus.Registered => new SolidColorBrush(Color.FromRgb(220, 53, 69)), // 红色 - 等待中
            MedicalCaseStatus.InConsultation => new SolidColorBrush(Color.FromRgb(255, 193, 7)), // 黄色 - 诊疗中
            MedicalCaseStatus.Completed => new SolidColorBrush(Color.FromRgb(40, 167, 69)), // 绿色 - 已完成
            _ => new SolidColorBrush(Color.FromRgb(108, 117, 125)) // 灰色 - 其他
        };

        /// <summary>紧急程度颜色</summary>
        public Brush UrgencyBrush => _medicalCaseData.CaseStatus == MedicalCaseStatus.Registered
            ? new SolidColorBrush(Color.FromRgb(220, 53, 69)) // UltraThink v2.0简化：按状态判断紧急程度
            : new SolidColorBrush(Color.FromRgb(40, 167, 69)); // 正常-绿色

        #endregion 状态指示颜色

        #region 进度条配色

        /// <summary>进度条背景色</summary>
        public Brush ProgressBackgroundBrush => new SolidColorBrush(Color.FromRgb(233, 236, 239));

        /// <summary>进度条前景色</summary>
        public Brush ProgressForegroundBrush => _medicalCaseData.CaseStatus switch {
            MedicalCaseStatus.Registered => new SolidColorBrush(Color.FromRgb(255, 193, 7)), // 橙色
            MedicalCaseStatus.InConsultation => new SolidColorBrush(Color.FromRgb(0, 123, 255)), // 蓝色
            MedicalCaseStatus.Completed => new SolidColorBrush(Color.FromRgb(40, 167, 69)), // 绿色
            MedicalCaseStatus.Cancelled => new SolidColorBrush(Color.FromRgb(220, 53, 69)), // 红色
            _ => new SolidColorBrush(Color.FromRgb(108, 117, 125)) // 灰色
        };

        /// <summary>进度百分比</summary>
        public double ProgressPercentage => _medicalCaseData.CaseStatus switch {
            MedicalCaseStatus.Registered => 25.0,
            MedicalCaseStatus.InConsultation => 75.0,
            MedicalCaseStatus.Completed => 100.0,
            MedicalCaseStatus.Cancelled => 0.0,
            _ => 0.0
        };

        #endregion 进度条配色

        #region 动态样式属性

        /// <summary>边框厚度</summary>
        public double BorderThickness => _medicalCaseData.CaseStatus switch {
            MedicalCaseStatus.Cancelled => 1.0,
            MedicalCaseStatus.Registered => 3.0, // UltraThink v2.0简化：等待中的案例边框较厚
            _ => 2.0
        };

        /// <summary>圆角半径</summary>
        public double CornerRadius => 6.0;

        /// <summary>阴影深度</summary>
        public double ShadowDepth => _medicalCaseData.CaseStatus switch {
            MedicalCaseStatus.Cancelled => 1.0, // 取消的案例阴影较浅
            MedicalCaseStatus.Registered => 5.0, // UltraThink v2.0简化：等待中的案例阴影更深
            _ => 3.0 // 其他状态正常阴影
        };

        /// <summary>透明度</summary>
        public double Opacity => _medicalCaseData.CaseStatus switch {
            MedicalCaseStatus.Cancelled => 0.6, // 取消的案例半透明
            _ => 1.0
        };

        #endregion 动态样式属性

        #region 时间相关配色

        /// <summary>持续时间文本颜色</summary>
        public Brush DurationBrush => _medicalCaseData.CaseStatus switch {
            MedicalCaseStatus.Registered => new SolidColorBrush(Color.FromRgb(220, 53, 69)), // UltraThink v2.0简化：等待中-红色
            MedicalCaseStatus.InConsultation => new SolidColorBrush(Color.FromRgb(255, 193, 7)), // 诊疗中-黄色
            MedicalCaseStatus.Completed => new SolidColorBrush(Color.FromRgb(40, 167, 69)), // 已完成-绿色
            _ => new SolidColorBrush(Color.FromRgb(33, 37, 41)) // 正常-深色
        };

        /// <summary>创建时间颜色</summary>
        public Brush CreateTimeBrush => _medicalCaseData.ConsultationDate.Date == DateTime.Today
            ? new SolidColorBrush(Color.FromRgb(0, 123, 255)) // UltraThink v2.0简化：今日案例-蓝色
            : new SolidColorBrush(Color.FromRgb(108, 117, 125)); // 历史案例-灰色

        #endregion 时间相关配色

        #region 患者信息配色

        /// <summary>患者姓名颜色</summary>
        public Brush PatientNameBrush => new SolidColorBrush(Color.FromRgb(33, 37, 41)); // 深色

        /// <summary>患者详细信息颜色</summary>
        public Brush PatientInfoBrush => new SolidColorBrush(Color.FromRgb(108, 117, 125)); // 灰色

        /// <summary>医生姓名颜色</summary>
        public Brush DoctorNameBrush => new SolidColorBrush(Color.FromRgb(0, 123, 255)); // 蓝色

        #endregion 患者信息配色

        #region 诊断相关配色

        /// <summary>主诉文本颜色</summary>
        public Brush ChiefComplaintBrush => string.IsNullOrWhiteSpace(_medicalCaseData.Remark)
            ? new SolidColorBrush(Color.FromRgb(158, 158, 158)) // UltraThink v2.0简化：无备注-浅灰色
            : new SolidColorBrush(Color.FromRgb(33, 37, 41)); // 有备注-深色

        /// <summary>诊断结果颜色</summary>
        public Brush DiagnosisBrush => _medicalCaseData.CaseStatus == MedicalCaseStatus.Completed
            ? new SolidColorBrush(Color.FromRgb(40, 167, 69)) // UltraThink v2.0简化：已完成-绿色
            : new SolidColorBrush(Color.FromRgb(158, 158, 158)); // 未完成-浅灰色

        #endregion 诊断相关配色

        #region 操作状态配色

        /// <summary>可操作状态颜色</summary>
        public Brush ActionableBrush => (_medicalCaseData.CaseStatus != MedicalCaseStatus.Cancelled && _medicalCaseData.CaseStatus != MedicalCaseStatus.Completed) ?
            new SolidColorBrush(Color.FromRgb(0, 123, 255)) : // UltraThink v2.0简化：非取消/完成状态可操作-蓝色
            new SolidColorBrush(Color.FromRgb(158, 158, 158)); // 不可操作-灰色

        /// <summary>警告状态颜色</summary>
        public Brush WarningBrush => _medicalCaseData.CaseStatus == MedicalCaseStatus.Registered ?
            new SolidColorBrush(Color.FromRgb(255, 193, 7)) : // UltraThink v2.0简化：等待中需要关注-黄色
            new SolidColorBrush(Color.FromRgb(40, 167, 69)); // 正常-绿色

        #endregion 操作状态配色

        #region 主题切换方法

        /// <summary>
        /// 获取卡片主题样式
        /// </summary>
        public (Brush Background, Brush Border, Brush Text, double BorderThickness) GetCardTheme() {
            return (BackgroundBrush, BorderBrush, TextBrush, BorderThickness);
        }

        /// <summary>
        /// 获取徽章主题样式
        /// </summary>
        public (Brush Background, Brush Text) GetBadgeTheme() {
            var background = _medicalCaseData.CaseStatus switch {
                MedicalCaseStatus.Registered => new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                MedicalCaseStatus.InConsultation => new SolidColorBrush(Color.FromRgb(0, 123, 255)),
                MedicalCaseStatus.Completed => new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                MedicalCaseStatus.Cancelled => new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                _ => new SolidColorBrush(Color.FromRgb(108, 117, 125))
            };

            var text = new SolidColorBrush(Colors.White);
            return (background, text);
        }

        /// <summary>
        /// 获取按钮主题样式
        /// </summary>
        public (Brush Background, Brush Text, Brush Border) GetButtonTheme(string buttonType) {
            return buttonType switch {
                "StartConsultation" => (
                    new SolidColorBrush(Color.FromRgb(0, 123, 255)),
                    new SolidColorBrush(Colors.White),
                    new SolidColorBrush(Color.FromRgb(0, 123, 255))
                ),
                "Complete" => (
                    new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                    new SolidColorBrush(Colors.White),
                    new SolidColorBrush(Color.FromRgb(40, 167, 69))
                ),
                "Cancel" => (
                    new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                    new SolidColorBrush(Color.FromRgb(33, 37, 41)),
                    new SolidColorBrush(Color.FromRgb(255, 193, 7))
                ),
                "Delete" => (
                    new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                    new SolidColorBrush(Colors.White),
                    new SolidColorBrush(Color.FromRgb(220, 53, 69))
                ),
                _ => (
                    new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                    new SolidColorBrush(Colors.White),
                    new SolidColorBrush(Color.FromRgb(108, 117, 125))
                )
            };
        }

        /// <summary>
        /// 获取优先级指示器样式
        /// </summary>
        public (Brush Background, string Text) GetPriorityIndicator() {
            // UltraThink v2.0简化：基于状态判断优先级
            var (background, text) = _medicalCaseData.CaseStatus switch {
                MedicalCaseStatus.Registered => (new SolidColorBrush(Color.FromRgb(220, 53, 69)), "高"), // 等待中-高优先级
                MedicalCaseStatus.InConsultation => (new SolidColorBrush(Color.FromRgb(255, 193, 7)), "中"), // 诊疗中-中优先级
                MedicalCaseStatus.Completed => (new SolidColorBrush(Color.FromRgb(40, 167, 69)), "低"), // 已完成-低优先级
                _ => (new SolidColorBrush(Color.FromRgb(108, 117, 125)), "正常")
            };

            return (background, text);
        }

        /// <summary>
        /// 获取时间轴样式
        /// </summary>
        public (Brush LineColor, Brush NodeColor, double NodeSize) GetTimelineStyle() {
            var lineColor = _medicalCaseData.CaseStatus switch {
                MedicalCaseStatus.Completed => new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                MedicalCaseStatus.Cancelled => new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                _ => new SolidColorBrush(Color.FromRgb(206, 212, 218))
            };

            var nodeColor = StatusIconBrush;
            // UltraThink v2.0简化：等待中的案例节点较大
            var nodeSize = _medicalCaseData.CaseStatus == MedicalCaseStatus.Registered ? 12.0 : 8.0;

            return (lineColor, nodeColor, nodeSize);
        }

        #endregion 主题切换方法

        #region 响应式主题

        /// <summary>
        /// 根据时间动态调整主题
        /// </summary>
        public void UpdateTimeBasedTheme() {
            // 触发所有时间相关属性的变化通知
            RaisePropertyChanged(nameof(DurationBrush));
            RaisePropertyChanged(nameof(CreateTimeBrush));
            RaisePropertyChanged(nameof(UrgencyBrush));
            RaisePropertyChanged(nameof(WarningBrush));
        }

        /// <summary>
        /// 设置高对比度主题
        /// </summary>
        public bool IsHighContrastMode { get; set; } = false;

        /// <summary>
        /// 获取高对比度样式
        /// </summary>
        public (Brush Background, Brush Foreground, Brush Border) GetHighContrastTheme() {
            if (!IsHighContrastMode) {
                return (BackgroundBrush, TextBrush, BorderBrush);
            }

            return (
                new SolidColorBrush(Colors.White),
                new SolidColorBrush(Colors.Black),
                new SolidColorBrush(Colors.Black)
            );
        }

        #endregion 响应式主题
    }
}
