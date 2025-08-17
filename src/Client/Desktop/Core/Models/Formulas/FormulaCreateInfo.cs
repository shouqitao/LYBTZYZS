using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Desktop.Core.Models.Formulas
{
    /// <summary>
    /// 创建验方模板信息模型
    /// UltraThink四层架构：Layer 4 (Info) - UI专用的创建数据模型
    /// </summary>
    public class FormulaCreateInfo
    {
        /// <summary>
        /// 验方模板名称
        /// </summary>
        [Required(ErrorMessage = "验方模板名称不能为空")]
        [StringLength(100, ErrorMessage = "验方模板名称长度不能超过100个字符")]
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// 分类
        /// </summary>
        [Required(ErrorMessage = "分类不能为空")]
        public string Category { get; set; } = "其他";
        
        /// <summary>
        /// 主治功效/适应症
        /// </summary>
        public string? Indications { get; set; }
        
        /// <summary>
        /// 药材组成
        /// </summary>
        public List<FormulaHerbItem> Herbs { get; set; } = new();
        
        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }
        
        #region UI状态属性
        
        /// <summary>
        /// 是否正在提交
        /// </summary>
        public bool IsSubmitting { get; set; }
        
        /// <summary>
        /// 验证错误信息
        /// </summary>
        public Dictionary<string, string> ValidationErrors { get; set; } = new();
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 验证数据有效性
        /// </summary>
        public bool IsValid()
        {
            ValidationErrors.Clear();
            
            if (string.IsNullOrWhiteSpace(Name))
            {
                ValidationErrors[nameof(Name)] = "验方模板名称不能为空";
            }
            else if (Name.Length > 100)
            {
                ValidationErrors[nameof(Name)] = "验方模板名称长度不能超过100个字符";
            }
            
            if (string.IsNullOrWhiteSpace(Category))
            {
                ValidationErrors[nameof(Category)] = "分类不能为空";
            }
            
            if (Herbs == null || Herbs.Count == 0)
            {
                ValidationErrors[nameof(Herbs)] = "验方模板必须包含至少一味药材";
            }
            else
            {
                for (int i = 0; i < Herbs.Count; i++)
                {
                    var herb = Herbs[i];
                    if (string.IsNullOrWhiteSpace(herb.Name))
                    {
                        ValidationErrors[$"Herbs[{i}].Name"] = "药材名称不能为空";
                    }
                    
                    if (herb.Dosage <= 0)
                    {
                        ValidationErrors[$"Herbs[{i}].Dosage"] = "药材用量必须大于0";
                    }
                }
            }
            
            return ValidationErrors.Count == 0;
        }
        
        /// <summary>
        /// 添加药材
        /// </summary>
        public void AddHerb(string name, decimal dosage, string unit = "g")
        {
            Herbs.Add(new FormulaHerbItem
            {
                Name = name,
                Dosage = dosage,
                Unit = unit
            });
        }
        
        /// <summary>
        /// 移除药材
        /// </summary>
        public void RemoveHerb(int index)
        {
            if (index >= 0 && index < Herbs.Count)
            {
                Herbs.RemoveAt(index);
            }
        }
        
        /// <summary>
        /// 清空药材列表
        /// </summary>
        public void ClearHerbs()
        {
            Herbs.Clear();
        }
        
        #endregion
    }
}