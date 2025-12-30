# OpenSpec Proposal: standardize-usercontrol-organization

**Change ID**: standardize-usercontrol-organization
**Status**: applied
**Created**: 2025-12-30
**Applied**: 2025-12-30
**Author**: Claude Code
**Priority**: High
**Estimated Effort**: 3h (实际: 30min)

---

## 1. 设计原则

> **控件化既要考虑架构优越性，也要考虑控件设计优越性。不要为了控件化而控件化。**

### 1.1 控件化判断标准

| 维度 | 问题 | 标准 |
|------|------|------|
| **复用性** | 这个控件会被多处使用吗？ | >= 2处使用才值得控件化 |
| **集成度** | 使用控件比直接写XAML更简单吗？ | 绑定数 <= 3 为高集成度 |
| **维护性** | 控件化后维护成本降低了吗？ | 修改一处 vs 修改多处 |

### 1.2 当前控件评估

| 控件 | 复用次数 | 绑定数量 | 集成度 | 评价 |
|------|----------|----------|--------|------|
| PatientViewControl | 3次 | 10个 | 低 | 架构合理，设计需改进 |
| PatientEditControl | 3次 | 12个 | 低 | 架构合理，设计需改进 |
| HerbViewControl | 2次 | 15个 | 低 | 架构合理，设计需改进 |
| UserViewControl | 2次 | 10个 | 低 | 架构合理，设计需改进 |
| PatientInfoCardControl | 3+次 | 2个 | **高** | 设计优秀 |
| SearchBox | 5+次 | 3个 | **高** | 设计优秀 |
| StatusBadge | 10+次 | 2个 | **高** | 设计优秀 |

**结论**: 
- 现有控件的**架构是合理的**(都有复用)
- 但XxxViewControl/XxxEditControl的**设计集成度低**(绑定太多)

---

## 2. 问题对比

### 2.1 低集成度设计 (当前)

```xaml
<!-- PatientDetailView.xaml - 需要10个绑定 -->
<patientControls:PatientViewControl
    PatientName="{Binding Name}"
    PinYinCode="{Binding PinYinCode}"
    Gender="{Binding SelectedGender}"
    BirthDate="{Binding BirthDate}"
    Age="{Binding Age}"
    IdNumber="{Binding IdNumber}"
    PhoneNumber="{Binding PhoneNumber}"
    Address="{Binding Address}"
    Status="{Binding Status}"
    ShowStatus="{Binding IsEditOrViewMode}"/>
```

**问题**:
- 使用复杂，每处使用都要写10个绑定
- 属性变更需要同步修改控件定义和使用处
- 控件代码膨胀 (273行仅为26个属性)

### 2.2 高集成度设计 (PatientInfoCardControl示例)

```xaml
<!-- 只需2个绑定 -->
<controls:PatientInfoCardControl
    Patient="{Binding PatientInfo}"
    DisplayMode="Full"/>
```

**优点**:
- 使用简单，一个Model绑定搞定
- 控件代码精简 (100行)
- 属性变更只需修改Model类

---

## 3. 真正需要修复的问题

### 3.1 重复控件 (应合并)

| 重复对 | 分析 | 建议 |
|--------|------|------|
| StatusBadge + UnifiedStatusBadge | 功能重叠，两套实现 | **合并为StatusBadge** |
| DataGridToolbar + UnifiedManagementToolBar | 不同风格，各有用途 | **保留两个**，明确使用场景 |

### 3.2 位置混乱 (已在standardize-converter-organization处理)

- PatientCardDisplayModeToVisibilityConverter在Controls目录
- 已有专门提案处理

### 3.3 控件设计改进 (可选优化，非必须)

如果未来想提升控件设计：

```csharp
// 方案A: 保持现状，控件可用，只是使用略繁琐
// 方案B: 渐进改进，新增Model属性作为替代

public partial class PatientViewControl : UserControl
{
    // 保留现有26个属性 (向后兼容)
    
    // 新增: 高集成度入口
    public PatientDisplayModel? PatientModel
    {
        get => (PatientDisplayModel?)GetValue(PatientModelProperty);
        set => SetValue(PatientModelProperty, value);
    }
    
    // 当PatientModel设置时，自动填充各属性
    private static void OnPatientModelChanged(...)
    {
        control.PatientName = model.Name;
        control.Gender = model.Gender;
        // ...
    }
}
```

---

## 4. 精简后的实施计划

### Phase 1: 合并StatusBadge (30min) - **必须做**

| Task | 说明 |
|------|------|
| 1.1 | 提取BadgeType枚举到单独文件 |
| 1.2 | 迁移UnifiedStatusBadge引用到StatusBadge |
| 1.3 | 删除UnifiedStatusBadge |

### Phase 2: 明确Toolbar使用规范 (15min) - **必须做**

| 控件 | 使用场景 |
|------|----------|
| DataGridToolbar | 标准CRUD页面，命令驱动 |
| UnifiedManagementToolBar | 自定义内容，slot驱动 |

文档化两者区别，不合并。

### Phase 3: 控件位置规范文档 (15min) - **必须做**

创建规范文档说明：
- Infrastructure/Controls/ - 通用控件 (>=2模块使用)
- Module/Controls/ - 模块专用控件

### Phase 4: 控件设计改进 (可选) - **低优先级**

为XxxViewControl添加高集成度Model属性入口，作为渐进改进。

---

## 5. 删除清单

```
仅删除:
- UnifiedStatusBadge.xaml
- UnifiedStatusBadge.xaml.cs

保留:
- DataGridToolbar (有明确使用场景)
- UnifiedManagementToolBar (有明确使用场景)
- 所有XxxViewControl/XxxEditControl (都有复用)
```

---

## 6. 规范输出

### 6.1 控件化决策流程图

```
需要提取控件吗？
    │
    ├─ 会被>=2处使用？
    │   ├─ 是 → 值得控件化
    │   └─ 否 → 直接写XAML，不要控件化
    │
    └─ 控件化后使用更简单？
        ├─ 是 (绑定<=3) → 高集成度设计
        └─ 否 (绑定>5) → 考虑Model模式优化
```

### 6.2 控件位置规范

```
Infrastructure/Controls/   通用控件
├── 被2个以上模块使用
├── 与具体业务无关
└── 示例: SearchBox, StatusBadge, MasterDetailLayout

Module/Controls/           模块专用控件
├── 仅在当前模块使用
├── 紧耦合模块业务
└── 示例: PatientViewControl, HerbEditControl
```

### 6.3 高集成度设计模式

```csharp
// 推荐: Model对象模式
public class PatientDisplayModel { ... }

public partial class PatientInfoCardControl : UserControl
{
    public PatientDisplayModel Patient { get; set; }  // 一个绑定搞定
}

// 避免: 细粒度属性模式 (除非有特殊需求)
public partial class PatientViewControl : UserControl
{
    public string PatientName { get; set; }
    public string Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    // ... 26个属性
}
```

---

## 7. 总结

| 问题类型 | 当前状态 | 行动 |
|----------|----------|------|
| 重复控件 (StatusBadge) | 存在 | **合并** |
| 重复控件 (Toolbar) | 各有用途 | **保留，文档化** |
| 控件架构 | 合理 | **保持** |
| 控件设计集成度 | 偏低 | **渐进优化** (非紧急) |
| 位置规范 | 缺失 | **创建文档** |

**核心理念**:
- 不为控件化而控件化
- 架构优越性 + 设计优越性 并重
- 已有控件架构合理，保持稳定
- 仅合并真正重复的控件

---

## 8. 实施记录 (2025-12-30)

### Phase 1: 合并StatusBadge - 已完成

| 步骤 | 操作 | 状态 |
|------|------|------|
| 1.1 | 创建 `BadgeType.cs` 提取枚举定义 | ✅ |
| 1.2 | 确认 UnifiedStatusBadge 无引用 | ✅ |
| 1.3 | 删除 `UnifiedStatusBadge.xaml` 和 `.xaml.cs` | ✅ |
| 1.4 | 更新 `StatusBadge.xaml.cs` 注释 | ✅ |
| 1.5 | 编译验证通过 (0错误, 0警告) | ✅ |

### 文件变更清单

```
新增:
+ Controls/BadgeType.cs                    # 独立枚举定义

删除:
- Controls/UnifiedStatusBadge.xaml         # 未使用控件
- Controls/UnifiedStatusBadge.xaml.cs      # 未使用控件

修改:
~ Controls/StatusBadge.xaml.cs             # 更新枚举引用注释
```

### Phase 2-4: 保留为文档规范

Toolbar使用规范、控件位置规范、高集成度设计模式 - 已在本proposal中文档化，作为未来开发参考。
