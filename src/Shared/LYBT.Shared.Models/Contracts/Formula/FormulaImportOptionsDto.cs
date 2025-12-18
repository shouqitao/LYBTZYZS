using System.ComponentModel;

namespace LYBT.Shared.Models.Contracts.Formula
{
    /// <summary>
    /// 验方导入选项DTO
    /// </summary>
    public class FormulaImportOptionsDto
    {

        [DisplayName("跳过重复验方")]
        public bool SkipDuplicates { get; set; } = true;

        [DisplayName("更新已存在验方")]
        public bool UpdateExisting { get; set; } = false;

        [DisplayName("自动匹配中药材")]
        public bool AutoMatchHerbs { get; set; } = true;

        [DisplayName("创建不存在的中药材")]
        public bool CreateMissingHerbs { get; set; } = false;

        [DisplayName("默认共享设置")]
        public bool DefaultIsShared { get; set; } = false;

        [DisplayName("导入批次号")]
        public string? ImportBatch { get; set; }

        [DisplayName("数据来源")]
        public string? DataSource { get; set; } = "老系统导入";
    }
}
