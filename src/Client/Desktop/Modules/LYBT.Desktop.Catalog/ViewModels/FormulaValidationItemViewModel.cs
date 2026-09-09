using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Catalog.ViewModels
{
    /// <summary>
    /// 验方校验行 ViewModel（US-FORM-008——待绑定药材）
    ///
    /// 每行对应验方中的一味药材：
    /// - 已绑定（IsValidated=true）：只读展示系统药材名
    /// - 未绑定：显示原名（OriginalHerbName），用户从 AllHerbs 选择系统药材后点击「校验绑定」
    /// </summary>
    public partial class FormulaValidationItemViewModel : ObservableObject
    {
        /// <summary>验方药材项 ID（validate 端点 herbItemId）</summary>
        public Guid HerbItemId { get; init; }

        /// <summary>原名（未绑定前的原始药材名，如老系统导入名）</summary>
        public string? OriginalHerbName { get; init; }

        /// <summary>系统药材名（已绑定时 HerbName，未绑定可能为空）</summary>
        public string? BoundHerbName { get; init; }

        /// <summary>剂量</summary>
        public int Dosage { get; init; }

        /// <summary>单位</summary>
        public string Unit { get; init; } = string.Empty;

        /// <summary>是否已绑定系统药材</summary>
        public bool IsValidated { get; init; }

        /// <summary>展示名：系统药材名优先，空则回退原名</summary>
        public string DisplayName
            => !string.IsNullOrWhiteSpace(BoundHerbName) ? BoundHerbName! : OriginalHerbName ?? string.Empty;

        /// <summary>是否绑定操作进行中（防重复点击）</summary>
        [ObservableProperty]
        private bool _isBinding;

        /// <summary>用户选择的系统药材（ComboBox 选中项）</summary>
        [ObservableProperty]
        private HerbListDto? _selectedHerb;
    }
}
