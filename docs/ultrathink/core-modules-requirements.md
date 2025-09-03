# 核心模块需求规范

**原则**: UltraThink 当前阶段不做功能扩展，以实现当前需求为前提，精简过多的设计

## 📋 核心设计原则

### 用户明确要求
- **字段保持原有结构**: 不扩展建议的新字段
- **删除配伍相关字段**: 简化设计，避免过度复杂
- **功能以记录为主**: 当前阶段只做简单记录，不做复杂业务逻辑

## 🌿 Herbs 药材模块

### 实体结构 (保持不变)
```csharp
public class Herb
{
    public Guid Id { get; set; }               // 主键
    public string Name { get; set; }           // 药材名称  
    public string? PinYinCode { get; set; }    // 拼音码(搜索)
    public string? Origin { get; set; }        // 产地
    public string? Spec { get; set; }          // 规格
    public string Unit { get; set; }           // 单位
    public decimal Price { get; set; }         // 单价
    public decimal? CostPrice { get; set; }    // 成本价
    public string? Effect { get; set; }        // 功效说明
    public string? Usage { get; set; }         // 用法用量
    public string? Remark { get; set; }        // 备注
    public CommonStatus Status { get; set; }   // 状态
}
```

### 功能要求
- **基础CRUD**: 药材增删改查
- **搜索功能**: 按名称、拼音码搜索
- **状态管理**: 启用/禁用药材
- **处方引用**: 供处方选择使用

## 📋 Prescriptions 处方模块

### 实体结构 (保持现有)
```csharp
public class Prescription
{
    public Guid Id { get; set; }                    // 处方ID
    public Guid MedicalCaseId { get; set; }         // 关联医疗案例
    public Guid PatientId { get; set; }             // 患者ID
    public string? Indication { get; set; }         // 适应症
    public int DosageCount { get; set; }            // 剂数
    public string? Advice { get; set; }             // 用法用量
    public string? FormulaSource { get; set; }      // 处方来源
    public decimal TotalPrice { get; set; }         // 总价格
    public CommonStatus Status { get; set; }        // 状态
}

public class PrescriptionItem  // 统一命名为 Herbs
{
    public Guid Id { get; set; }                    // 主键
    public Guid PrescriptionId { get; set; }        // 处方ID
    public Guid HerbId { get; set; }                // 药材ID
    public decimal Dosage { get; set; }             // 单剂剂量
    public decimal UnitPrice { get; set; }          // 单价
    public decimal TotalPrice { get; set; }         // 小计
}
```

### 功能要求
- **处方管理**: 基础CRUD操作
- **药材配伍**: 简单记录，不做复杂校验
- **价格计算**: 自动计算总价
- **验方应用**: 可选择验方模板

## 📚 Formula 验方模块

### 实体结构 (保持现有)
```csharp
public class Formula
{
    public Guid Id { get; set; }                    // 验方ID
    public string Name { get; set; }                // 验方名称
    public string? PinYinCode { get; set; }         // 拼音码
    public string? Source { get; set; }             // 方剂出处
    public string? Indication { get; set; }         // 主治功能
    public string? Composition { get; set; }        // 方剂组成
    public string? Usage { get; set; }              // 用法用量
    public string? Remark { get; set; }             // 备注说明
    public Guid? CreatedByUserId { get; set; }      // 创建者
    public bool IsShared { get; set; }              // 是否共享
    public CommonStatus Status { get; set; }        // 状态
}

public class FormulaHerbItem  // 验方药材组成
{
    public Guid FormulaId { get; set; }             // 验方ID
    public Guid HerbId { get; set; }                // 药材ID
    public decimal Dosage { get; set; }             // 用量
}
```

### 功能要求
- **验方管理**: 经典验方和个人验方
- **模板应用**: 可直接应用到处方
- **共享机制**: 验方可设置共享
- **简单记录**: 专注模板功能，不做复杂分析

## 🎯 开发约束

### 必须遵循
1. **保持现有字段结构**: 不增加新字段（除非用户明确确认）
2. **简化业务逻辑**: 专注数据记录，避免过度设计
3. **统一命名规范**: PrescriptionItems 统一称为 Herbs
4. **基础功能优先**: CRUD + 简单查询为主

### 严禁行为
1. **扩展配伍字段**: 不添加复杂的配伍禁忌检查
2. **过度业务逻辑**: 避免复杂的业务规则验证
3. **功能蔓延**: 不添加统计、分析等高级功能
4. **数据结构变更**: 除非获得明确授权

## 📝 实施指导

### 开发重点
- 保持UltraThink双层架构标准
- 实现基础的CRUD操作
- 确保前端调用接口稳定
- 维护现有业务流程不变

### 质量标准
- 零编译警告
- 接口向后兼容
- 数据完整性保证
- 简洁明了的代码结构

---

**文档版本**: v1.0  
**创建时间**: 2025-09-01  
**设计原则**: 实用主义，精简设计，避免过度工程化  
**用户确认**: 基于用户明确的简化要求制定