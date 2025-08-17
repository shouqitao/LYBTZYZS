using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Desktop.Core.Models.Formulas
{
    /// <summary>
    /// 更新验方模板信息模型
    /// UltraThink四层架构：Layer 4 (Info) - UI专用的更新数据模型
    /// </summary>
    public class FormulaUpdateInfo
    {
        /// <summary>
        /// 验方模板ID
        /// </summary>
        [Required(ErrorMessage = "验方模板ID不能为空")]
        public Guid Id { get; set; }
        
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
        
        /// <summary>
        /// 版本号（用于并发控制）
        /// </summary>
        public string? Version { get; set; }
        
        #region UI状态属性
        
        /// <summary>
        /// 是否正在提交
        /// </summary>
        public bool IsSubmitting { get; set; }
        
        /// <summary>
        /// 是否有未保存的更改
        /// </summary>
        public bool HasUnsavedChanges { get; set; }
        
        /// <summary>
        /// 验证错误信息
        /// </summary>
        public Dictionary<string, string> ValidationErrors { get; set; } = new();
        
        /// <summary>
        /// 原始数据（用于检测更改）
        /// </summary>
        public FormulaInfo? OriginalData { get; set; }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 从FormulaInfo创建更新信息
        /// </summary>
        public static FormulaUpdateInfo FromFormulaInfo(FormulaInfo formulaInfo)
        {
            if (formulaInfo == null)
                throw new ArgumentNullException(nameof(formulaInfo));
                
            return new FormulaUpdateInfo
            {
                Id = formulaInfo.Id,
                Name = formulaInfo.Name,
                Category = formulaInfo.Category,
                Indications = formulaInfo.Indications,
                Herbs = new List<FormulaHerbItem>(formulaInfo.Herbs ?? new List<FormulaHerbItem>()),
                Remark = formulaInfo.Remark,
                OriginalData = formulaInfo
            };
        }
        
        /// <summary>
        /// 验证数据有效性
        /// </summary>
        public bool IsValid()
        {
            ValidationErrors.Clear();
            
            if (Id == Guid.Empty)
            {
                ValidationErrors[nameof(Id)] = "验方模板ID不能为空";
            }
            
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
        /// 检测是否有更改
        /// </summary>
        public bool DetectChanges()
        {
            if (OriginalData == null)
            {
                HasUnsavedChanges = true;
                return true;
            }
            
            HasUnsavedChanges = Name != OriginalData.Name ||
                               Category != OriginalData.Category ||
                               Indications != OriginalData.Indications ||
                               Remark != OriginalData.Remark ||
                               !AreHerbsEqual(Herbs, OriginalData.Herbs);
            
            return HasUnsavedChanges;
        }
        
        /// <summary>
        /// 比较药材列表是否相等
        /// </summary>
        private bool AreHerbsEqual(List<FormulaHerbItem> herbs1, List<FormulaHerbItem>? herbs2)
        {
            if (herbs2 == null)
                return herbs1.Count == 0;
                
            if (herbs1.Count != herbs2.Count)
                return false;
                
            for (int i = 0; i < herbs1.Count; i++)
            {
                var herb1 = herbs1[i];
                var herb2 = herbs2[i];
                
                if (herb1.Name != herb2.Name ||
                    herb1.Dosage != herb2.Dosage ||
                    herb1.Unit != herb2.Unit)
                {
                    return false;
                }
            }
            
            return true;
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
            DetectChanges();
        }
        
        /// <summary>
        /// 移除药材
        /// </summary>
        public void RemoveHerb(int index)
        {
            if (index >= 0 && index < Herbs.Count)
            {
                Herbs.RemoveAt(index);
                DetectChanges();
            }
        }
        
        /// <summary>
        /// 重置到原始状态
        /// </summary>
        public void Reset()
        {
            if (OriginalData != null)
            {
                Name = OriginalData.Name;
                Category = OriginalData.Category;
                Indications = OriginalData.Indications;
                Remark = OriginalData.Remark;
                Herbs = new List<FormulaHerbItem>(OriginalData.Herbs ?? new List<FormulaHerbItem>());
                HasUnsavedChanges = false;
                ValidationErrors.Clear();
            }
        }
        
        #endregion
    }
}