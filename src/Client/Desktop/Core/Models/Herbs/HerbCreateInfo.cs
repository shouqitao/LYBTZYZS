using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Core.Models.Herbs
{
    /// <summary>
    /// 创建中药材信息模型
    /// UltraThink四层架构：Layer 4 (Info) - UI专用的创建数据模型
    /// </summary>
    public class HerbCreateInfo
    {
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
        /// 是否立即激活
        /// </summary>
        public bool IsActiveImmediately { get; set; } = true;
        
        #region UI状态属性
        
        /// <summary>
        /// 是否正在提交
        /// </summary>
        public bool IsSubmitting { get; set; }
        
        /// <summary>
        /// 验证错误信息
        /// </summary>
        public Dictionary<string, string> ValidationErrors { get; set; } = new();
        
        /// <summary>
        /// 可用分类列表
        /// </summary>
        public List<string> AvailableCategories { get; set; } = new();
        
        /// <summary>
        /// 可用单位列表
        /// </summary>
        public List<string> AvailableUnits { get; set; } = new() { "g", "kg", "支", "片", "丸", "袋", "盒" };
        
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
            }
        }
        
        /// <summary>
        /// 重置表单
        /// </summary>
        public void Reset()
        {
            Name = string.Empty;
            Alias = null;
            Effect = null;
            Nature = null;
            Meridian = null;
            Origin = null;
            Spec = null;
            Unit = "g";
            Price = 0;
            Stock = 0;
            Category = null;
            Supplier = null;
            Dosage = null;
            Contraindication = null;
            Precaution = null;
            PinYinCode = null;
            WuBiCode = null;
            Remark = null;
            IsActiveImmediately = true;
            IsSubmitting = false;
            ValidationErrors.Clear();
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