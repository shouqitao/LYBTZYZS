using System.Windows.Media;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.ViewModels.Prescriptions
{

    /// <summary>
    /// 处方主题样式视图模型 - UltraThink架构Presentation Layer
    /// 专门处理处方的主题、颜色、样式等视觉呈现逻辑
    /// </summary>
    public class PrescriptionThemeViewModel : BindableBase
    {
        private readonly PrescriptionDto _prescriptionData;

        public PrescriptionThemeViewModel(PrescriptionDto prescriptionData)
        {
            _prescriptionData = prescriptionData ?? throw new ArgumentNullException(nameof(prescriptionData));
        }

        #region 主题配色

        /// <summary>主背景色</summary>
        public Brush BackgroundBrush => _prescriptionData.Status switch
        {
            CommonStatus.Disabled => new SolidColorBrush(Color.FromRgb(255, 248, 230)), // 浅黄色 - 草稿状态
            CommonStatus.Enabled => new SolidColorBrush(Color.FromRgb(230, 255, 230)), // 浅绿色 - 启用状态
            _ => new SolidColorBrush(Color.FromRgb(248, 248, 248)) // 默认灰色
        };

        /// <summary>边框颜色</summary>
        public Brush BorderBrush => _prescriptionData.Status switch
        {
            CommonStatus.Disabled => new SolidColorBrush(Color.FromRgb(255, 193, 7)), // 黄色 - 草稿状态
            CommonStatus.Enabled => new SolidColorBrush(Color.FromRgb(40, 167, 69)), // 绿色 - 启用状态
            _ => new SolidColorBrush(Color.FromRgb(206, 212, 218)) // 默认边框色
        };

        /// <summary>文本颜色</summary>
        public Brush TextBrush => new SolidColorBrush(Color.FromRgb(33, 37, 41)); // 统一深色文本

        /// <summary>强调色（用于重要信息）</summary>
        public Brush AccentBrush => HasDiscount ?
            new SolidColorBrush(Color.FromRgb(255, 87, 34)) : // 橙色表示有折扣
            new SolidColorBrush(Color.FromRgb(76, 175, 80)); // 绿色表示正常价格

        #endregion 主题配色

        #region 状态指示颜色

        /// <summary>支付状态颜色</summary>
        public Brush PaymentStatusBrush => IsPaid ?
            new SolidColorBrush(Color.FromRgb(40, 167, 69)) : // 已支付-绿色
            new SolidColorBrush(Color.FromRgb(255, 193, 7)); // 未支付-黄色

        /// <summary>发药状态颜色</summary>
        public Brush DispenseStatusBrush => IsDispensed ?
            new SolidColorBrush(Color.FromRgb(108, 117, 125)) : // 已发药-灰色
            new SolidColorBrush(Color.FromRgb(0, 123, 255)); // 未发药-蓝色

        /// <summary>完成状态颜色</summary>
        public Brush CompletionStatusBrush => IsCompleted ?
            new SolidColorBrush(Color.FromRgb(40, 167, 69)) : // 已完成-绿色
            new SolidColorBrush(Color.FromRgb(255, 193, 7)); // 未完成-黄色

        /// <summary>优先级颜色</summary>
        public Brush PriorityBrush
        {
            get
            {
                if (NeedsPayment)
                {
                    return new SolidColorBrush(Color.FromRgb(220, 53, 69)); // 待付款-红色
                }

                if (CanDispense)
                {
                    return new SolidColorBrush(Color.FromRgb(255, 193, 7)); // 可发药-黄色
                }

                if (IsCompleted)
                {
                    return new SolidColorBrush(Color.FromRgb(40, 167, 69)); // 已完成-绿色
                }

                return new SolidColorBrush(Color.FromRgb(108, 117, 125)); // 草稿-灰色
            }
        }

        #endregion 状态指示颜色

        #region 进度条配色

        /// <summary>进度条背景色</summary>
        public Brush ProgressBackgroundBrush => new SolidColorBrush(Color.FromRgb(233, 236, 239));

        /// <summary>进度条前景色</summary>
        public Brush ProgressForegroundBrush
        {
            get
            {
                var percentage = GetCompletionPercentage();
                return percentage switch
                {
                    >= 100 => new SolidColorBrush(Color.FromRgb(40, 167, 69)), // 完成-绿色
                    >= 60 => new SolidColorBrush(Color.FromRgb(255, 193, 7)), // 进行中-黄色
                    >= 30 => new SolidColorBrush(Color.FromRgb(255, 87, 34)), // 开始-橙色
                    _ => new SolidColorBrush(Color.FromRgb(220, 53, 69)) // 很少-红色
                };
            }
        }

        #endregion 进度条配色

        #region 动态样式属性

        /// <summary>边框厚度</summary>
        public double BorderThickness => _prescriptionData.Status switch
        {
            CommonStatus.Disabled => 1.0,
            _ => 2.0
        };

        /// <summary>圆角半径</summary>
        public double CornerRadius => 6.0;

        /// <summary>阴影深度</summary>
        public double ShadowDepth => 3.0; // 统一阴影深度

        /// <summary>透明度</summary>
        public double Opacity => 1.0; // 统一不透明

        #endregion 动态样式属性

        #region 金额相关配色

        /// <summary>总金额文本颜色</summary>
        public Brush TotalAmountBrush => IsFree ?
            new SolidColorBrush(Color.FromRgb(40, 167, 69)) : // 免费-绿色
            new SolidColorBrush(Color.FromRgb(33, 37, 41)); // 收费-默认色

        /// <summary>折扣信息颜色</summary>
        public Brush DiscountBrush => new SolidColorBrush(Color.FromRgb(255, 87, 34)); // 橙色

        /// <summary>节省金额颜色</summary>
        public Brush SavingsBrush => new SolidColorBrush(Color.FromRgb(76, 175, 80)); // 绿色

        #endregion 金额相关配色

        #region 图标配色

        /// <summary>状态图标颜色</summary>
        public Brush StatusIconBrush => _prescriptionData.Status switch
        {
            CommonStatus.Disabled => new SolidColorBrush(Color.FromRgb(108, 117, 125)), // 灰色
            CommonStatus.Enabled => new SolidColorBrush(Color.FromRgb(40, 167, 69)), // 绿色
            _ => new SolidColorBrush(Color.FromRgb(108, 117, 125)) // 默认灰色
        };

        /// <summary>支付图标颜色</summary>
        public Brush PaymentIconBrush => IsPaid ?
            new SolidColorBrush(Color.FromRgb(40, 167, 69)) : // 已支付-绿色
            new SolidColorBrush(Color.FromRgb(255, 193, 7)); // 未支付-黄色

        /// <summary>发药图标颜色</summary>
        public Brush DispenseIconBrush => IsDispensed ?
            new SolidColorBrush(Color.FromRgb(108, 117, 125)) : // 已发药-灰色
            new SolidColorBrush(Color.FromRgb(0, 123, 255)); // 未发药-蓝色

        #endregion 图标配色

        #region 中医特色主题

        /// <summary>获取中医药材等级配色</summary>
        public Brush GetHerbGradeBrush(int herbCount)
        {
            return herbCount switch
            {
                >= 15 => new SolidColorBrush(Color.FromRgb(156, 39, 176)), // 紫色-复方
                >= 10 => new SolidColorBrush(Color.FromRgb(63, 81, 181)), // 靛蓝-中方
                >= 5 => new SolidColorBrush(Color.FromRgb(3, 169, 244)), // 蓝色-小方
                >= 1 => new SolidColorBrush(Color.FromRgb(76, 175, 80)), // 绿色-单味
                _ => new SolidColorBrush(Color.FromRgb(158, 158, 158)) // 灰色-无药
            };
        }

        /// <summary>获取处方复杂度颜色</summary>
        public Brush GetComplexityBrush()
        {
            var complexity = HerbCount switch
            {
                >= 15 => "复杂方",
                >= 10 => "中等方",
                >= 5 => "简单方",
                >= 1 => "单味药",
                _ => "空方"
            };

            return complexity switch
            {
                "复杂方" => new SolidColorBrush(Color.FromRgb(233, 30, 99)), // 粉红色
                "中等方" => new SolidColorBrush(Color.FromRgb(255, 152, 0)), // 橙色
                "简单方" => new SolidColorBrush(Color.FromRgb(139, 195, 74)), // 浅绿色
                "单味药" => new SolidColorBrush(Color.FromRgb(121, 85, 72)), // 棕色
                _ => new SolidColorBrush(Color.FromRgb(158, 158, 158)) // 灰色
            };
        }

        #endregion 中医特色主题

        #region 主题切换方法

        /// <summary>
        /// 获取卡片主题样式
        /// </summary>
        public (Brush Background, Brush Border, Brush Text, double BorderThickness) GetCardTheme()
        {
            return (BackgroundBrush, BorderBrush, TextBrush, BorderThickness);
        }

        /// <summary>
        /// 获取徽章主题样式
        /// </summary>
        public (Brush Background, Brush Text) GetBadgeTheme()
        {
            var background = _prescriptionData.Status switch
            {
                CommonStatus.Disabled => new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                CommonStatus.Enabled => new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                _ => new SolidColorBrush(Color.FromRgb(108, 117, 125))
            };

            var text = new SolidColorBrush(Colors.White);
            return (background, text);
        }

        /// <summary>
        /// 获取按钮主题样式
        /// </summary>
        public (Brush Background, Brush Text, Brush Border) GetButtonTheme(string buttonType)
        {
            return buttonType switch
            {
                "Primary" => (
                    new SolidColorBrush(Color.FromRgb(0, 123, 255)),
                    new SolidColorBrush(Colors.White),
                    new SolidColorBrush(Color.FromRgb(0, 123, 255))),
                "Success" => (
                    new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                    new SolidColorBrush(Colors.White),
                    new SolidColorBrush(Color.FromRgb(40, 167, 69))),
                "Warning" => (
                    new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                    new SolidColorBrush(Color.FromRgb(33, 37, 41)),
                    new SolidColorBrush(Color.FromRgb(255, 193, 7))),
                "Danger" => (
                    new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                    new SolidColorBrush(Colors.White),
                    new SolidColorBrush(Color.FromRgb(220, 53, 69))),
                _ => (
                    new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                    new SolidColorBrush(Colors.White),
                    new SolidColorBrush(Color.FromRgb(108, 117, 125)))
            };
        }

        #endregion 主题切换方法

        #region 简化的业务属性 - UltraThink v2.0

        /// <summary>是否有折扣</summary>
        private bool HasDiscount => _prescriptionData.Discount < 1.0m;

        /// <summary>是否已支付 - 简化为启用状态表示已支付</summary>
        private bool IsPaid => _prescriptionData.Status == CommonStatus.Enabled;

        /// <summary>是否已发药 - 简化为启用状态表示已发药</summary>
        private bool IsDispensed => _prescriptionData.Status == CommonStatus.Enabled;

        /// <summary>是否已完成 - 简化为启用状态表示已完成</summary>
        private bool IsCompleted => _prescriptionData.Status == CommonStatus.Enabled;

        /// <summary>是否需要支付 - 简化为禁用状态表示需要支付</summary>
        private bool NeedsPayment => _prescriptionData.Status == CommonStatus.Disabled;

        /// <summary>是否可以发药 - 简化为启用状态表示可以发药</summary>
        private bool CanDispense => _prescriptionData.Status == CommonStatus.Enabled;

        /// <summary>是否免费</summary>
        private bool IsFree => _prescriptionData.TotalPrice == 0;

        /// <summary>药材数量</summary>
        private int HerbCount => _prescriptionData.Items?.Count ?? 0;

        /// <summary>完成百分比 - 简化逻辑</summary>
        private int GetCompletionPercentage()
        {
            return _prescriptionData.Status switch
            {
                CommonStatus.Enabled => 100, // 启用状态表示完成
                CommonStatus.Disabled => 50, // 禁用状态表示进行中
                _ => 0
            };
        }

        #endregion 简化的业务属性 - UltraThink v2.0
    }
}
