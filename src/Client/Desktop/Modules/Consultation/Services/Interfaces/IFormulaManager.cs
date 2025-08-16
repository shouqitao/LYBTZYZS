using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Core.Events;
using LYBT.Desktop.Core.Enums;

// UltraThink重构: 统一FormulaInfo和FormulaDto，使用FormulaDto作为统一模型
using FormulaInfo = LYBT.Shared.Models.Contracts.Formula.FormulaDto;

namespace LYBT.Desktop.Consultation.Services.Interfaces
{
    /// <summary>
    /// 验方管理器接口
    /// </summary>
    public interface IFormulaManager
    {
        /// <summary>
        /// 应用验方模板到处方
        /// </summary>
        /// <param name="formula">验方模板</param>
        /// <returns>生成的处方项目列表</returns>
        List<PrescriptionItemInfo> ApplyFormulaTemplate(FormulaInfo formula);

        /// <summary>
        /// 合并验方到现有处方
        /// </summary>
        /// <param name="formula">验方模板</param>
        /// <param name="existingItems">现有处方项目</param>
        /// <param name="mergeMode">合并模式：Replace替换，Append追加，Merge合并同药材</param>
        /// <returns>合并后的处方项目列表</returns>
        List<PrescriptionItemInfo> MergeFormulaToPrescription(
            FormulaInfo formula, 
            IEnumerable<PrescriptionItemInfo> existingItems,
            FormulaMergeMode mergeMode = FormulaMergeMode.Merge);

        /// <summary>
        /// 创建自定义验方
        /// </summary>
        /// <param name="name">验方名称</param>
        /// <param name="items">处方项目列表</param>
        /// <param name="description">验方描述</param>
        /// <returns>创建的验方信息</returns>
        Task<FormulaInfo?> CreateCustomFormulaAsync(
            string name, 
            IEnumerable<PrescriptionItemInfo> items,
            string? description = null);

        /// <summary>
        /// 验证验方是否可用
        /// </summary>
        /// <param name="formula">验方模板</param>
        /// <returns>验证结果和错误信息</returns>
        (bool IsValid, string? ErrorMessage) ValidateFormula(FormulaInfo formula);

        /// <summary>
        /// 计算验方价格
        /// </summary>
        /// <param name="formula">验方模板</param>
        /// <returns>总价格</returns>
        decimal CalculateFormulaPrice(FormulaInfo formula);

        /// <summary>
        /// 获取常用验方列表
        /// </summary>
        /// <param name="count">数量限制</param>
        Task<List<FormulaInfo>> GetFrequentlyUsedFormulasAsync(int count = 10);

        /// <summary>
        /// 按症状推荐验方
        /// </summary>
        /// <param name="symptoms">症状关键词列表</param>
        /// <returns>推荐的验方列表</returns>
        Task<List<FormulaInfo>> RecommendFormulasBySymptoms(IEnumerable<string> symptoms);
    }
}