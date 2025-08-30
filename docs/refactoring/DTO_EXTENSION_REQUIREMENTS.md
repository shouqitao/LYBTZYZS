# DTO扩展需求分析

> 分析日期：2025-01-17  
> 目标：定义从Info模型迁移到DTO所需的UI辅助属性

## 🎯 扩展原则

### 设计原则
1. **UI友好**: 添加直接支持UI绑定的计算属性
2. **权限控制**: 包含前端权限判断所需的属性
3. **状态管理**: 支持UI状态展示和交互控制
4. **性能优化**: 避免在UI层重复计算

### 属性分类
- **显示属性**: 用于UI展示的格式化文本
- **状态属性**: 控制UI元素状态的布尔值
- **权限属性**: 控制操作权限的标识
- **交互属性**: 支持UI交互的辅助属性

---

## 📋 UserDto扩展需求

### 当前UserDto属性
```csharp
public class UserDto : FullBaseDto, ICodeable
{
    public string Username { get; set; }
    public string RealName { get; set; }
    public string Role { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Avatar { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastLoginTime { get; set; }
    public string? LastLoginIp { get; set; }
    public bool IsActive { get; set; }
}
```

### 需要新增的UI属性
```csharp
public class UserDto : FullBaseDto, ICodeable
{
    // ... 现有属性 ...
    
    #region UI显示属性
    /// <summary>显示名称（优先显示真实姓名）</summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>完整显示名称（含用户名）</summary>
    public string FullDisplayName { get; set; } = string.Empty;
    
    /// <summary>角色显示文本</summary>
    public string RoleDisplayName { get; set; } = string.Empty;
    
    /// <summary>状态显示文本</summary>
    public string StatusText { get; set; } = string.Empty;
    
    /// <summary>状态颜色（用于UI显示）</summary>
    public string StatusColor { get; set; } = string.Empty;
    
    /// <summary>创建时间显示文本</summary>
    public string CreateTimeText { get; set; } = string.Empty;
    
    /// <summary>更新时间显示文本</summary>
    public string UpdateTimeText { get; set; } = string.Empty;
    #endregion
    
    #region UI状态属性
    /// <summary>是否被选中（用于批量操作）</summary>
    public bool IsSelected { get; set; }
    
    /// <summary>是否正在编辑</summary>
    public bool IsEditing { get; set; }
    
    /// <summary>是否正在加载</summary>
    public bool IsLoading { get; set; }
    #endregion
    
    #region UI权限属性
    /// <summary>是否为系统管理员</summary>
    public bool IsSysAdmin { get; set; }
    
    /// <summary>是否可以编辑</summary>
    public bool CanEdit { get; set; }
    
    /// <summary>是否可以删除</summary>
    public bool CanDelete { get; set; }
    
    /// <summary>是否可以重置密码</summary>
    public bool CanResetPassword { get; set; }
    #endregion
}
```

---

## 📋 PatientDto扩展需求

### 需要分析PatientInfo模型
让我先检查PatientInfo的特殊属性：

```csharp
// 基于分析，PatientDto需要新增：
public class PatientDto : FullBaseDto
{
    // ... 现有属性 ...
    
    #region UI显示属性
    /// <summary>患者显示名称</summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>年龄显示文本</summary>
    public string AgeDisplay { get; set; } = string.Empty;
    
    /// <summary>性别显示文本</summary>
    public string GenderDisplay { get; set; } = string.Empty;
    
    /// <summary>状态显示文本</summary>
    public string StatusText { get; set; } = string.Empty;
    
    /// <summary>最后就诊显示</summary>
    public string LastVisitDisplay { get; set; } = string.Empty;
    
    /// <summary>联系方式显示</summary>
    public string ContactDisplay { get; set; } = string.Empty;
    #endregion
    
    #region UI状态属性
    /// <summary>是否被选中</summary>
    public bool IsSelected { get; set; }
    
    /// <summary>是否正在加载</summary>
    public bool IsLoading { get; set; }
    
    /// <summary>是否有预约</summary>
    public bool HasAppointment { get; set; }
    #endregion
    
    #region UI权限属性
    /// <summary>是否可以编辑</summary>
    public bool CanEdit { get; set; }
    
    /// <summary>是否可以删除</summary>
    public bool CanDelete { get; set; }
    
    /// <summary>是否可以就诊</summary>
    public bool CanConsult { get; set; }
    #endregion
}
```

---

## 📋 HerbDto扩展需求

```csharp
public class HerbDto : FullBaseDto
{
    // ... 现有属性 ...
    
    #region UI显示属性
    /// <summary>药材显示名称（含规格）</summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>价格显示文本</summary>
    public string PriceDisplay { get; set; } = string.Empty;
    
    /// <summary>库存状态显示</summary>
    public string StockStatusText { get; set; } = string.Empty;
    
    /// <summary>状态颜色</summary>
    public string StatusColor { get; set; } = string.Empty;
    
    /// <summary>规格显示</summary>
    public string SpecificationDisplay { get; set; } = string.Empty;
    #endregion
    
    #region UI状态属性
    /// <summary>是否库存充足</summary>
    public bool IsStockSufficient { get; set; }
    
    /// <summary>是否缺货</summary>
    public bool IsOutOfStock { get; set; }
    
    /// <summary>是否可用</summary>
    public bool IsAvailable { get; set; }
    
    /// <summary>是否被选中</summary>
    public bool IsSelected { get; set; }
    #endregion
    
    #region UI权限属性
    /// <summary>是否可以编辑</summary>
    public bool CanEdit { get; set; }
    
    /// <summary>是否可以删除</summary>
    public bool CanDelete { get; set; }
    
    /// <summary>是否可以进货</summary>
    public bool CanPurchase { get; set; }
    #endregion
}
```

---

## 📋 ConsultationDto扩展需求

```csharp
public class ConsultationDto : FullBaseDto
{
    // ... 现有属性 ...
    
    #region UI显示属性
    /// <summary>患者显示信息</summary>
    public string PatientDisplay { get; set; } = string.Empty;
    
    /// <summary>医生显示信息</summary>
    public string DoctorDisplay { get; set; } = string.Empty;
    
    /// <summary>状态显示文本</summary>
    public string StatusText { get; set; } = string.Empty;
    
    /// <summary>诊疗时长显示</summary>
    public string DurationDisplay { get; set; } = string.Empty;
    
    /// <summary>诊断结果简述</summary>
    public string DiagnosisSummary { get; set; } = string.Empty;
    #endregion
    
    #region UI状态属性
    /// <summary>是否进行中</summary>
    public bool IsInProgress { get; set; }
    
    /// <summary>是否已完成</summary>
    public bool IsCompleted { get; set; }
    
    /// <summary>是否可以开方</summary>
    public bool CanPrescribe { get; set; }
    
    /// <summary>是否被选中</summary>
    public bool IsSelected { get; set; }
    #endregion
    
    #region UI权限属性
    /// <summary>是否可以编辑</summary>
    public bool CanEdit { get; set; }
    
    /// <summary>是否可以删除</summary>
    public bool CanDelete { get; set; }
    
    /// <summary>是否可以结束</summary>
    public bool CanFinish { get; set; }
    #endregion
}
```

---

## 📋 PrescriptionDto扩展需求

```csharp
public class PrescriptionDto : FullBaseDto
{
    // ... 现有属性 ...
    
    #region UI显示属性
    /// <summary>患者信息显示</summary>
    public string PatientInfoDisplay { get; set; } = string.Empty;
    
    /// <summary>医生信息显示</summary>
    public string DoctorDisplay { get; set; } = string.Empty;
    
    /// <summary>总价显示</summary>
    public string TotalPriceDisplay { get; set; } = string.Empty;
    
    /// <summary>药材数量显示</summary>
    public string HerbCountDisplay { get; set; } = string.Empty;
    
    /// <summary>状态显示文本</summary>
    public string StatusText { get; set; } = string.Empty;
    
    /// <summary>用法用量显示</summary>
    public string UsageDisplay { get; set; } = string.Empty;
    #endregion
    
    #region UI状态属性
    /// <summary>是否可以配药</summary>
    public bool CanDispense { get; set; }
    
    /// <summary>是否已配药</summary>
    public bool IsDispensed { get; set; }
    
    /// <summary>是否被选中</summary>
    public bool IsSelected { get; set; }
    #endregion
    
    #region UI权限属性
    /// <summary>是否可以编辑</summary>
    public bool CanEdit { get; set; }
    
    /// <summary>是否可以删除</summary>
    public bool CanDelete { get; set; }
    
    /// <summary>是否可以打印</summary>
    public bool CanPrint { get; set; }
    #endregion
}
```

---

## 📋 FormulaDto扩展需求

```csharp
public class FormulaDto : FullBaseDto
{
    // ... 现有属性 ...
    
    #region UI显示属性
    /// <summary>验方显示名称</summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>适用症显示</summary>
    public string IndicationDisplay { get; set; } = string.Empty;
    
    /// <summary>药材数量显示</summary>
    public string HerbCountDisplay { get; set; } = string.Empty;
    
    /// <summary>成本显示</summary>
    public string CostDisplay { get; set; } = string.Empty;
    
    /// <summary>类别显示</summary>
    public string CategoryDisplay { get; set; } = string.Empty;
    #endregion
    
    #region UI状态属性
    /// <summary>是否常用</summary>
    public bool IsFrequentlyUsed { get; set; }
    
    /// <summary>是否被选中</summary>
    public bool IsSelected { get; set; }
    
    /// <summary>是否可用</summary>
    public bool IsAvailable { get; set; }
    #endregion
    
    #region UI权限属性
    /// <summary>是否可以编辑</summary>
    public bool CanEdit { get; set; }
    
    /// <summary>是否可以删除</summary>
    public bool CanDelete { get; set; }
    
    /// <summary>是否可以应用</summary>
    public bool CanApply { get; set; }
    #endregion
}
```

---

## 📋 MedicalCaseDto扩展需求

```csharp
public class MedicalCaseDto : FullBaseDto
{
    // ... 现有属性 ...
    
    #region UI显示属性
    /// <summary>患者显示信息</summary>
    public string PatientDisplay { get; set; } = string.Empty;
    
    /// <summary>医生显示信息</summary>
    public string DoctorDisplay { get; set; } = string.Empty;
    
    /// <summary>状态显示文本</summary>
    public string StatusText { get; set; } = string.Empty;
    
    /// <summary>时间计算显示</summary>
    public string TimeSpanDisplay { get; set; } = string.Empty;
    
    /// <summary>关联信息显示</summary>
    public string RelatedInfoDisplay { get; set; } = string.Empty;
    #endregion
    
    #region UI状态属性
    /// <summary>是否有关联处方</summary>
    public bool HasPrescription { get; set; }
    
    /// <summary>是否已结案</summary>
    public bool IsClosed { get; set; }
    
    /// <summary>是否被选中</summary>
    public bool IsSelected { get; set; }
    #endregion
    
    #region UI权限属性
    /// <summary>是否可以编辑</summary>
    public bool CanEdit { get; set; }
    
    /// <summary>是否可以删除</summary>
    public bool CanDelete { get; set; }
    
    /// <summary>是否可以结案</summary>
    public bool CanClose { get; set; }
    #endregion
}
```

---

## 🔧 实施策略

### Phase 1: 核心DTO扩展（UserDto, PatientDto）
**目标**: 扩展最频繁使用的DTO
**工作量**: 0.5天
**风险**: 低

### Phase 2: 业务DTO扩展（HerbDto, ConsultationDto, PrescriptionDto）
**目标**: 扩展主要业务流程DTO
**工作量**: 0.5天
**风险**: 中

### Phase 3: 辅助DTO扩展（FormulaDto, MedicalCaseDto）
**目标**: 完善辅助功能DTO
**工作量**: 0.25天
**风险**: 低

### Phase 4: Server层AutoMapper适配
**目标**: 更新映射配置以填充UI属性
**工作量**: 0.5天
**风险**: 中

## ✅ 验证标准

1. **属性完整性**: 所有Info模型的UI属性都有对应的DTO属性
2. **类型安全**: 所有新增属性都有明确的类型定义
3. **命名一致**: 遵循现有的命名约定
4. **性能可接受**: 扩展后的DTO序列化性能不显著下降
5. **兼容性**: 不破坏现有的API契约

这个扩展需求为后续的DTO修改提供了完整的蓝图。