# Change: 药材编辑控件重构

## Why

当前药材编辑功能存在以下问题：

1. **职责分散** - 药材编辑逻辑分布在6+文件中，共约1400行代码
2. **过度协调** - PrescriptionPanelViewModel(646行)需协调5个Handler
3. **复用受限** - 方剂模块无法直接复用药材编辑逻辑
4. **测试困难** - 需要Mock多个依赖才能测试

**当前代码分布**:
```
PrescriptionPanelViewModel (646行) - 协调器
├── PrescriptionItemHandler (308行) - 药材项CRUD
├── PrescriptionCalculator (~80行) - 价格计算
├── PrescriptionValidator (~120行) - 校验逻辑
├── PrescriptionImportHandler (~150行) - 导入处理
└── HerbListEditor + HerbCardControl - UI渲染
```

**核心问题**: 处方模块需要了解药材编辑的所有细节，而非简单地获取编辑结果。

## What Changes

### 核心原则
- **两层控件架构**: HerbItemControl(单药材) + HerbListControl(药材列表)
- **控件集中定义**: 所有控件定义在 `Infrastructure/Controls`
- **高内聚输出**: 控件输出完整药材列表，调用方直接对象赋值
- **事件通知模式**: 控件通过事件通知外部变更（项目统一模式）
- **可复用**: 处方模块和方剂模块共用

### 两层控件架构

```
┌─────────────────────────────────────────────────────────────┐
│                     HerbListControl                          │
│  (药材列表控件 - 组成list + 查重)                            │
│                                                              │
│  输入属性:                                                   │
│  - AllHerbs: 药材库数据                                      │
│  - Columns: 每行药材个数(可配置)                             │
│                                                              │
│  输出属性:                                                   │
│  - HerbList: IReadOnlyList<HerbItemDto> 药材列表             │
│  - ItemCount: 药材项数量                                     │
│  - HasDuplicates: 是否有重复药材                             │
│  - IsValid: 是否通过校验                                     │
│                                                              │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐         │
│  │HerbItemControl│ │HerbItemControl│ │HerbItemControl│  ...   │
│  │  (药材1)     │ │  (药材2)     │ │  (药材3)     │         │
│  └──────────────┘ └──────────────┘ └──────────────┘         │
│                                                              │
│  事件: HerbListChanged                                       │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                     HerbItemControl                          │
│  (单药材控件 - 快速检索药材)                                 │
│                                                              │
│  输入属性:                                                   │
│  - AllHerbs: 药材库数据(用于自动补全)                        │
│                                                              │
│  输出属性:                                                   │
│  - HerbId: 药材ID                                            │
│  - HerbName: 药材名称                                        │
│  - Dosage: 剂量                                              │
│  - Unit: 单位(从药材库自动复制)                              │
│  - UnitPrice: 单价(从药材库自动复制，不显示)                 │
│  - DecocteMethod: 煎法                                       │
│  - IsDosageValid: 剂量是否有效                               │
│                                                              │
│  事件: ItemChanged                                           │
└─────────────────────────────────────────────────────────────┘
```

---

## HerbItemControl 功能清单

### 核心功能
| 功能 | 说明 |
|------|------|
| 药材检索 | 拼音码自动补全，快速检索药材 |
| 自动赋值 | 选择药材后自动复制：单位、单价 |
| 剂量输入 | 用户输入剂量 |
| 煎法输入 | 用户选择煎法 |
| 自动匹配 | 输入完全匹配时自动选中药材（待完善） |

### 键盘操作
| 按键 | 行为 |
|------|------|
| Enter | 选中建议项 / 跳转到剂量 / 生成新槽位 |
| Tab | 在字段间跳转 |

### 事件
| 事件 | 触发时机 |
|------|----------|
| ItemChanged | 药材选择、剂量、煎法变更时 |

---

## HerbListControl 功能清单

### 核心功能
| 功能 | 说明 |
|------|------|
| 组成列表 | 管理多个HerbItemControl组成药材列表 |
| 重复检测 | 检测重复药材并处理 |
| 紧凑列表 | 删除药材后，后面的自动往前靠 |
| 拖拽排序 | 支持拖拽调整药材顺序 |
| 批量导入 | 提供AddHerbs方法供外部调用 |

### 空槽位管理
- 始终只保留1个空槽
- 剂量输入后按回车 → 生成新空槽 + 光标跳转到新空槽的药材输入框

### 删除功能
- 每个药材项右侧有删除按钮
- 提供清空全部操作

### 布局配置
- 提供 `Columns` 属性配置每行药材个数

### 重复药材处理

**单个添加时：**
- 内嵌提示"当前药材已经存在无法添加"
- 禁止重复添加

**批量导入时：**
- 逐个弹窗提示"xx药材已经存在"
- 医生确认一个再弹窗下一个
- 直到所有重复药材确认完成

**重复剂量取值策略（可配置）：**
| 策略 | 说明 |
|------|------|
| 最大值 | 取两个剂量中的较大值（默认） |
| 最小值 | 取两个剂量中的较小值 |
| 和值 | 两个剂量相加 |
| 平均值 | 两个剂量的平均值 |
| 第一个值 | 保留第一个添加的剂量 |

### 事件
| 事件 | 触发时机 |
|------|----------|
| HerbListChanged | 列表发生任何变更时（添加、删除、修改） |

---

## Phase 划分

### Phase 1: 完善HerbItemControl (1天)
- [ ] 基于现有 `HerbCardControl` 重命名为 `HerbItemControl`
- [ ] 完善拼音码快速检索逻辑
- [ ] 完善自动匹配药材功能
- [ ] 选择药材后自动复制单位、单价
- [ ] 完善剂量校验逻辑
- [ ] 添加 `ItemChanged` 事件
- [ ] 键盘操作（Enter/Tab）

### Phase 2: 创建HerbListControl (1.5天)
- [ ] 创建 `HerbListControl` UserControl
- [ ] 内部使用 `ItemsControl` + `HerbItemControl`
- [ ] 实现空槽位管理（1个空槽，回车生成新槽）
- [ ] 实现紧凑列表（删除后自动前靠）
- [ ] 实现重复药材检测（单个禁止，批量逐个弹窗）
- [ ] 实现重复剂量取值策略（可配置）
- [ ] 实现拖拽排序
- [ ] 实现删除功能（单个+清空）
- [ ] 实现 `AddHerbs` 批量导入方法
- [ ] 实现 `Columns` 布局配置
- [ ] 添加 `HerbListChanged` 事件

### Phase 3: 集成到处方模块 (1天)
- [ ] 替换 `PrescriptionEditorPanel` 中的控件引用
- [ ] 简化 `PrescriptionPanelViewModel` 移除Handler协调代码
- [ ] 更新保存逻辑使用 `HerbList` 输出
- [ ] 外部提供导入按钮，调用控件 `AddHerbs` 方法

### Phase 4: 扩展复用与清理 (1天)
- [ ] 方剂模块复用 `HerbListControl`
- [ ] 删除冗余Handler文件
- [ ] 删除旧的 `HerbListEditor` 控件
- [ ] 编写单元测试

---

## Impact

### Affected Specs
- `desktop-medicalcase` - 处方面板简化
- `desktop-formula` - 方剂编辑复用
- `desktop-controls` - 新增控件规范

### Affected Code

**新增/修改文件**:
| 文件 | 变更类型 | 说明 |
|-----|---------|------|
| `Infrastructure/Controls/HerbItem/HerbItemControl.xaml` | RENAME | 从HerbCardControl重命名 |
| `Infrastructure/Controls/HerbItem/HerbItemControl.xaml.cs` | MODIFY | 增强功能 |
| `Infrastructure/Controls/HerbItem/HerbItemControlViewModel.cs` | CREATE | 内部ViewModel |
| `Infrastructure/Controls/HerbItem/HerbItemChangedEventArgs.cs` | CREATE | 事件参数 |
| `Infrastructure/Controls/HerbList/HerbListControl.xaml` | CREATE | 药材列表控件 |
| `Infrastructure/Controls/HerbList/HerbListControl.xaml.cs` | CREATE | 控件代码后台 |
| `Infrastructure/Controls/HerbList/HerbListControlViewModel.cs` | CREATE | 内部ViewModel |
| `Infrastructure/Controls/HerbList/HerbListChangedEventArgs.cs` | CREATE | 事件参数 |
| `Infrastructure/Models/HerbItemDto.cs` | CREATE | 药材项输出DTO |
| `Infrastructure/Models/DuplicateDosageStrategy.cs` | CREATE | 剂量取值策略枚举 |
| `MedicalCase/ViewModels/PrescriptionPanelViewModel.cs` | MODIFY | 大幅简化 |
| `MedicalCase/Controls/PrescriptionEditorPanel.xaml` | MODIFY | 替换为HerbListControl |

**需要清理的旧代码**:
| 文件 | 操作 | 原因 |
|-----|------|------|
| `Infrastructure/Controls/HerbCardControl.xaml` | DELETE | 重命名为HerbItemControl后删除原文件 |
| `Infrastructure/Controls/HerbCardControl.xaml.cs` | DELETE | 重命名为HerbItemControl后删除原文件 |
| `Infrastructure/Controls/HerbListEditor.xaml` | DELETE | 被HerbListControl替代 |
| `Infrastructure/Controls/HerbListEditor.xaml.cs` | DELETE | 被HerbListControl替代 |
| `MedicalCase/ViewModels/Components/PrescriptionItemHandler.cs` | DELETE | 药材项CRUD逻辑迁移到HerbListControl |
| `MedicalCase/ViewModels/Components/PrescriptionImportHandler.cs` | DELETE | 导入功能改为外部调用AddHerbs |
| `MedicalCase/ViewModels/Components/PrescriptionValidator.cs` | DELETE | 校验逻辑迁移到控件 |
| `MedicalCase/ViewModels/Components/PrescriptionCalculator.cs` | KEEP | 价格计算保留在处方模块(剂数相关) |
| `MedicalCase/Services/PrescriptionCalculator.cs` | EVALUATE | 评估是否与上面重复 |
| `Prescriptions/Models/Items/PrescriptionHerbItem.cs` | DELETE | 被HerbItemDto替代 |

**需要更新的测试**:
| 文件 | 操作 | 原因 |
|-----|------|------|
| `PrescriptionHerbItemTests.cs` | UPDATE/DELETE | 更新为HerbItemDto测试或删除 |
| `PrescriptionHerbItemPriceTests.cs` | UPDATE/DELETE | 价格计算测试需重新评估 |

**保留不动的文件**:
| 文件 | 原因 |
|-----|------|
| `PrescriptionSaveHandler.cs` | 保存逻辑保留 |
| `PrescriptionPrintHandler.cs` | 打印逻辑保留 |
| `Shared/PrescriptionBusinessRuleValidator.cs` | 业务规则校验保留 |
| `Shared/PrescriptionInputDtoValidator.cs` | DTO校验保留 |

### Breaking Changes
- `HerbCardControl` 重命名为 `HerbItemControl`
- `HerbListEditor` 控件将被 `HerbListControl` 替代
- `PrescriptionItemHandler` 将被删除
- `PrescriptionImportHandler` 将被删除
- `PrescriptionValidator` 将被删除
- `PrescriptionHerbItem` 模型将被 `HerbItemDto` 替代

---

## 目标使用方式

**XAML中使用HerbListControl**:
```xml
<controls:HerbListControl
    AllHerbs="{Binding AllHerbs}"
    Columns="4"
    HerbList="{Binding HerbItems, Mode=OneWayToSource}"
    HerbListChanged="OnHerbListChanged" />
```

**简化后的处方保存**:
```csharp
public async Task<SaveResult> SaveAsync()
{
    var prescription = new PrescriptionInputDto
    {
        Items = _herbListControl.HerbList,  // 直接对象赋值
        DosageCount = DosageCount,
        Usage = Usage
    };
    return await _saveHandler.SaveAsync(prescription);
}
```

**批量导入（外部调用）**:
```csharp
// 父级ViewModel处理导入对话框
private async void OnImportFormulaClick()
{
    var herbs = await ShowFormulaSelectDialog();
    if (herbs != null)
    {
        _herbListControl.AddHerbs(herbs);
    }
}
```

---

## Success Metrics

| 指标 | 当前 | 目标 |
|------|------|------|
| 药材编辑相关代码行数 | ~1400行 | ~600行 (-57%) |
| 碎片化文件数 | 6+个 | 2个(HerbItemControl+HerbListControl) |
| PrescriptionPanelVM行数 | 646行 | ~200行 |
| 模块间复用 | 无 | 处方+方剂共用 |
| Mock依赖数(测试) | 5个 | 1个 |

---

## Dependencies

- 无外部依赖
- 与 `simplify-workspace-event-architecture` 提案可并行

---

## Risks

| 风险 | 影响 | 缓解措施 |
|-----|------|---------|
| 重构范围较大 | 中 | 分Phase实施，每Phase验证 |
| HerbCardControl重命名 | 低 | 全局搜索替换 |
| 控件内部状态管理 | 中 | 使用内部ViewModel封装 |
| 方剂模块影响 | 中 | Phase 4再处理方剂复用 |
| 拖拽排序实现复杂 | 中 | 可使用现有拖拽库 |

---

## Timeline Estimate

| Phase | 工时 |
|-------|------|
| Phase 1: 完善HerbItemControl | 1天 |
| Phase 2: 创建HerbListControl | 1.5天 |
| Phase 3: 集成到处方模块 | 1天 |
| Phase 4: 扩展复用与清理 | 1天 |
| **总计** | **4.5天** |
