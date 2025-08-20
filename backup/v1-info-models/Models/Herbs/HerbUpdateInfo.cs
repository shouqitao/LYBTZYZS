using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Core.Models.Herbs
{
    /// <summary>
    /// 更新中药材信息模型
    /// UltraThink四层架构：Layer 4 (Info) - UI专用的更新数据模型
    /// </summary>
    public class HerbUpdateInfo
    {
        /// <summary>
        /// 中药材ID
        /// </summary>
        [Required(ErrorMessage = "中药材ID不能为空")]
        public Guid Id { get; set; }
        
        /// <summary>
        /// 中药材名称
        /// </summary>
        [Required(ErrorMessage = "中药材名称不能为空")]
        [StringLength(100, ErrorMessage = "中药材名称长度不能超过100个字符")]
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// 别名
        /// </summary>
        [StringLength(200, ErrorMessage = "别名长度不能超过200个字符")]
        public string? Alias { get; set; }
        
        /// <summary>
        /// 功效
        /// </summary>
        [StringLength(500, ErrorMessage = "功效描述长度不能超过500个字符")]
        public string? Effect { get; set; }
        
        /// <summary>
        /// 性味
        /// </summary>
        [StringLength(100, ErrorMessage = "性味描述长度不能超过100个字符")]
        public string? Nature { get; set; }
        
        /// <summary>
        /// 归经
        /// </summary>
        [StringLength(100, ErrorMessage = "归经描述长度不能超过100个字符")]
        public string? Meridian { get; set; }
        
        /// <summary>
        /// 产地
        /// </summary>
        [StringLength(100, ErrorMessage = "产地长度不能超过100个字符")]
        public string? Origin { get; set; }
        
        /// <summary>
        /// 规格
        /// </summary>
        [StringLength(100, ErrorMessage = "规格长度不能超过100个字符")]
        public string? Spec { get; set; }
        
        /// <summary>
        /// 单位
        /// </summary>
        [Required(ErrorMessage = "单位不能为空")]
        [StringLength(10, ErrorMessage = "单位长度不能超过10个字符")]
        public string Unit { get; set; } = "g";
        
        /// <summary>
        /// 单价
        /// </summary>
        [Required(ErrorMessage = "单价不能为空")]
        [Range(0.01, 9999.99, ErrorMessage = "单价必须在0.01到9999.99之间")]
        public decimal Price { get; set; }
        
        /// <summary>
        /// 库存数量
        /// </summary>
        [Range(0, 999999.99, ErrorMessage = "库存数量必须在0到999999.99之间")]
        public decimal Stock { get; set; }
        
        /// <summary>
        /// 分类
        /// </summary>
        [StringLength(50, ErrorMessage = "分类长度不能超过50个字符")]
        public string? Category { get; set; }
        
        /// <summary>
        /// 供应商
        /// </summary>
        [StringLength(100, ErrorMessage = "供应商长度不能超过100个字符")]
        public string? Supplier { get; set; }
        
        /// <summary>
        /// 用法用量
        /// </summary>
        [StringLength(200, ErrorMessage = "用法用量长度不能超过200个字符")]
        public string? Dosage { get; set; }
        
        /// <summary>
        /// 禁忌
        /// </summary>
        [StringLength(300, ErrorMessage = "禁忌描述长度不能超过300个字符")]
        public string? Contraindication { get; set; }
        
        /// <summary>
        /// 注意事项
        /// </summary>
        [StringLength(300, ErrorMessage = "注意事项长度不能超过300个字符")]
        public string? Precaution { get; set; }
        
        /// <summary>
        /// 拼音码
        /// </summary>
        [StringLength(100, ErrorMessage = "拼音码长度不能超过100个字符")]
        public string? PinYinCode { get; set; }
        
        /// <summary>
        /// 五笔码
        /// </summary>
        [StringLength(100, ErrorMessage = "五笔码长度不能超过100个字符")]
        public string? WuBiCode { get; set; }
        
        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
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
        public HerbInfo? OriginalData { get; set; }
        
        /// <summary>
        /// 可用分类列表
        /// </summary>
        public List<string> AvailableCategories { get; set; } = new();
        
        /// <summary>
        /// 可用单位列表
        /// </summary>
        public List<string> AvailableUnits { get; set; } = new() { "g", "kg", "支", "片", "丸", "袋", "盒" };
        
        /// <summary>
        /// 是否允许修改名称
        /// </summary>
        public bool CanEditName { get; set; } = true;
        
        /// <summary>
        /// 是否允许修改价格
        /// </summary>
        public bool CanEditPrice { get; set; } = true;
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 从HerbInfo创建更新信息
        /// </summary>
        public static HerbUpdateInfo FromHerbInfo(HerbInfo herbInfo)
        {
            if (herbInfo == null)
                throw new ArgumentNullException(nameof(herbInfo));
                
            return new HerbUpdateInfo
            {
                Id = herbInfo.Id,
                Name = herbInfo.Name,
                // Alias = herbInfo.Alias, // 属性不存在：HerbInfo.Alias
                Effect = herbInfo.Effect,
                // Nature = herbInfo.Nature, // 属性不存在：HerbInfo.Nature
                // Meridian = herbInfo.Meridian, // 属性不存在：HerbInfo.Meridian
                Origin = herbInfo.Origin,
                Spec = herbInfo.Spec,
                Unit = herbInfo.Unit,
                Price = herbInfo.Price,
                Stock = herbInfo.Stock,
                Category = herbInfo.Category,
                Supplier = herbInfo.Supplier,
                // Dosage = herbInfo.Dosage, // 属性不存在：HerbInfo.Dosage
                // Contraindication = herbInfo.Contraindication, // 属性不存在：HerbInfo.Contraindication
                // Precaution = herbInfo.Precaution, // 属性不存在：HerbInfo.Precaution
                PinYinCode = herbInfo.PinYinCode,
                // WuBiCode = herbInfo.WuBiCode, // 属性不存在：HerbInfo.WuBiCode
                Remark = herbInfo.Remark,
                OriginalData = herbInfo,
                CanEditName = true, // 一般情况下允许修改名称
                CanEditPrice = true // 一般情况下允许修改价格
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
                ValidationErrors[nameof(Id)] = "中药材ID不能为空";
            }
            
            if (string.IsNullOrWhiteSpace(Name))
            {
                ValidationErrors[nameof(Name)] = "中药材名称不能为空";
            }
            else if (Name.Length > 100)
            {
                ValidationErrors[nameof(Name)] = "中药材名称长度不能超过100个字符";
            }
            
            if (string.IsNullOrWhiteSpace(Unit))
            {
                ValidationErrors[nameof(Unit)] = "单位不能为空";
            }
            else if (Unit.Length > 10)
            {
                ValidationErrors[nameof(Unit)] = "单位长度不能超过10个字符";
            }
            
            if (Price <= 0)
            {
                ValidationErrors[nameof(Price)] = "单价必须大于0";
            }
            else if (Price > 9999.99m)
            {
                ValidationErrors[nameof(Price)] = "单价不能超过9999.99";
            }
            
            if (Stock < 0)
            {
                ValidationErrors[nameof(Stock)] = "库存数量不能为负数";
            }
            else if (Stock > 999999.99m)
            {
                ValidationErrors[nameof(Stock)] = "库存数量不能超过999999.99";
            }
            
            if (!string.IsNullOrEmpty(Alias) && Alias.Length > 200)
            {
                ValidationErrors[nameof(Alias)] = "别名长度不能超过200个字符";
            }
            
            if (!string.IsNullOrEmpty(Effect) && Effect.Length > 500)
            {
                ValidationErrors[nameof(Effect)] = "功效描述长度不能超过500个字符";
            }
            
            if (!string.IsNullOrEmpty(Category) && Category.Length > 50)
            {
                ValidationErrors[nameof(Category)] = "分类长度不能超过50个字符";
            }
            
            if (!string.IsNullOrEmpty(Remark) && Remark.Length > 500)
            {
                ValidationErrors[nameof(Remark)] = "备注长度不能超过500个字符";
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
                               // Alias != OriginalData.Alias || // 属性不存在：HerbInfo.Alias
                               Effect != OriginalData.Effect ||
                               // Nature != OriginalData.Nature || // 属性不存在：HerbInfo.Nature
                               // Meridian != OriginalData.Meridian || // 属性不存在：HerbInfo.Meridian
                               Origin != OriginalData.Origin ||
                               Spec != OriginalData.Spec ||
                               Unit != OriginalData.Unit ||
                               Price != OriginalData.Price ||
                               Stock != OriginalData.Stock ||
                               Category != OriginalData.Category ||
                               Supplier != OriginalData.Supplier ||
                               // Dosage != OriginalData.Dosage || // 属性不存在：HerbInfo.Dosage
                               // Contraindication != OriginalData.Contraindication || // 属性不存在：HerbInfo.Contraindication
                               // Precaution != OriginalData.Precaution || // 属性不存在：HerbInfo.Precaution
                               PinYinCode != OriginalData.PinYinCode ||
                               // WuBiCode != OriginalData.WuBiCode || // 属性不存在：HerbInfo.WuBiCode
                               Remark != OriginalData.Remark;
            
            return HasUnsavedChanges;
        }
        
        /// <summary>
        /// 自动生成拼音码和五笔码
        /// </summary>
        public void GenerateCodes()
        {
            if (!string.IsNullOrEmpty(Name))
            {
                // 简单的拼音码生成（实际应该使用拼音库）
                PinYinCode = GeneratePinYinCode(Name);
                
                // 简单的五笔码生成（实际应该使用五笔编码库）
                WuBiCode = GenerateWuBiCode(Name);
                
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
                // Alias = OriginalData.Alias; // 属性不存在：HerbInfo.Alias
                Effect = OriginalData.Effect;
                // Nature = OriginalData.Nature; // 属性不存在：HerbInfo.Nature
                // Meridian = OriginalData.Meridian; // 属性不存在：HerbInfo.Meridian
                Origin = OriginalData.Origin;
                Spec = OriginalData.Spec;
                Unit = OriginalData.Unit;
                Price = OriginalData.Price;
                Stock = OriginalData.Stock;
                Category = OriginalData.Category;
                Supplier = OriginalData.Supplier;
                // Dosage = OriginalData.Dosage; // 属性不存在：HerbInfo.Dosage
                // Contraindication = OriginalData.Contraindication; // 属性不存在：HerbInfo.Contraindication
                // Precaution = OriginalData.Precaution; // 属性不存在：HerbInfo.Precaution
                PinYinCode = OriginalData.PinYinCode;
                // WuBiCode = OriginalData.WuBiCode; // 属性不存在：HerbInfo.WuBiCode
                Remark = OriginalData.Remark;
                HasUnsavedChanges = false;
                ValidationErrors.Clear();
            }
        }
        
        /// <summary>
        /// 计算总价值
        /// </summary>
        public decimal CalculateTotalValue()
        {
            return Price * Stock;
        }
        
        /// <summary>
        /// 生成拼音码（简化版）
        /// </summary>
        private string GeneratePinYinCode(string name)
        {
            // 这里应该使用专业的拼音库，暂时返回首字母
            if (string.IsNullOrEmpty(name))
                return string.Empty;
                
            return name.Substring(0, Math.Min(name.Length, 10)).ToUpper();
        }
        
        /// <summary>
        /// 生成五笔码（简化版）
        /// </summary>
        private string GenerateWuBiCode(string name)
        {
            // 这里应该使用专业的五笔编码库，暂时返回简化版
            if (string.IsNullOrEmpty(name))
                return string.Empty;
                
            return name.Substring(0, Math.Min(name.Length, 10)).ToUpper();
        }
        
        #endregion
    }
}