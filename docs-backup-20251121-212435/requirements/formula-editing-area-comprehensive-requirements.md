# 经验方药材编辑区功能需求确认文档

**文档编号**: REQ-Formula-Editing-Area-001
**创建日期**: 2025-11-17
**版本**: v2.0
**状态**: 📝 需求讨论
**优先级**: P0（核心功能优化）
**预估工作量**: 根据确认范围决定
**相关Issue**: #2149（已完成基础实现）
**相关文档**:
- [Issue #2149 Formula药材编辑功能](https://github.com/shouqitao/LYBTZYZS/issues/2149)
- [Server端Formula设计](../explanation/architecture/server/formula-design.md)
- [Client端Formula设计](../explanation/architecture/client/formula-design.md)
- [验方药材管理重构需求](formula-herb-management-refactoring-requirements.md)

---

## 📋 执行摘要

### 需求背景

基于Graphiti和GitHub Issue #2149的分析，经验方药材编辑功能已完成基础实现：
- ✅ 4列UniformGrid卡片布局
- ✅ 拼音码智能匹配（7级评分算法）
- ✅ 焦点管理（水平优先遍历）
- ✅ 重复检测和自动合并
- ✅ 空槽位自动管理

本次需求讨论的目的是**系统化梳理完整功能需求**，确认后续扩展方向和优先级。

### 核心问题

**需要用户确认的关键问题**：

1. **MVP范围界定**：本次是仅优化现有UI交互，还是增加高级功能（模板管理、配伍禁忌等）？
2. **自由处方支持**：是否允许添加药材库中不存在的药材？
3. **方剂模板共享**：是否需要支持公开/私有模板功能？
4. **剂量单位标准化**：单位字段是否需要从自由输入改为下拉选择？
5. **配伍禁忌规则库**：是否需要内置中医配伍禁忌检查（十八反、十九畏）？

---

## 第1章：当前实现状态分析（Issue #2149完成内容）

### 1.1 已实现功能清单

| 功能模块 | 实现状态 | 技术实现 | 关键文件 |
|---------|---------|---------|---------|
| **4列卡片布局** | ✅ 已完成 | ItemsControl + UniformGrid | FormulaDetailView.xaml:348-369 |
| **HerbCardControl** | ✅ 已完成 | 自定义UserControl | HerbCardControl.xaml |
| **拼音码过滤** | ✅ 已完成 | 7级智能评分算法 | FormulaHerbItemViewModel.cs:207-262 |
| **FilteredHerbs** | ✅ 已完成 | ObservableCollection动态过滤 | FormulaHerbItemViewModel.cs:68-72 |
| **焦点管理** | ✅ 已完成 | 水平优先遍历（Enter跳转） | HerbCardControl.xaml.cs:206-244 |
| **重复检测** | ✅ 已完成 | 剂量输入完成时检测并合并 | FormulaDetailViewModel.cs:815-860 |
| **空槽位管理** | ✅ 已完成 | 自动保持4个空槽 | FormulaDetailViewModel.cs:898-910 |
| **AllHerbs数据源** | ✅ 已完成 | 分页加载（100条/页） | FormulaDetailViewModel.cs:417-447 |
| **跨模块依赖** | ✅ 已完成 | IContainerProvider延迟解析 | HerbsModule.cs:29 |

### 1.2 技术架构现状

**前端架构（Client端）**：
```
FormulaDetailView.xaml
├── ItemsControl (ItemsSource=HerbItems)
│   └── UniformGrid (Columns=4)
│       └── HerbCardControl
│           ├── ComboBox (HerbName, FilteredHerbs)
│           ├── TextBox (Quantity)
│           ├── TextBlock (Unit)
│           └── Button (Delete)
│
FormulaDetailViewModel
├── HerbItems: ObservableCollection<FormulaHerbItemViewModel>
├── AllHerbs: ObservableCollection<HerbDto>
├── Commands
│   ├── DeleteHerbCommand
│   ├── DosageCompletedCommand
│   └── HerbSelectedCommand
└── Components (当前为内联实现，可组件化)
```

**后端架构（Server端）**：
```
FormulaController (14个API端点)
├── POST /api/formulas
├── GET /api/formulas/{id}
├── PUT /api/formulas/{id}
├── DELETE /api/formulas/{id}
├── POST /api/formulas/batch-delete
├── GET /api/formulas (分页)
├── POST /api/formulas/import (Excel)
├── GET /api/formulas/export
├── POST /api/formulas/{id}/validate-herb/{herbItemId}
└── GET /api/formulas/pending-validation

FormulaService (14个业务方法)
├── CreateAsync / UpdateAsync / DeleteAsync
├── BatchDeleteAsync (最大100条)
├── ImportFromExcelAsync (主从表格式)
├── ExportAsync
├── ValidateFormulaHerbAsync (延迟绑定验证)
└── GetPendingValidationFormulasAsync

FormulaRepository (7个数据方法)
├── GetBaseQuery (Include Herbs + 软删除过滤)
├── GetByIdWithHerbsAsync
├── GetPagedWithDetailsAsync
└── GetPendingValidationFormulasAsync

Formula Entity (聚合根)
├── Id, Name, Effect, Usage, Property, Category
├── ValidationStatus: Draft | Validated
├── IsShared: bool (权限控制)
└── Herbs: List<FormulaHerbItem> (1:N)

FormulaHerbItem Entity (值对象，支持延迟绑定)
├── HerbId: Guid? (可空，支持自由处方)
├── OriginalHerbName: string? (原始名称)
├── IsValidated: bool (是否已验证)
├── HerbName, Quantity, Unit
├── ProcessingMethod, Remark
└── SortOrder (排序序号)
```

### 1.3 运行时错误修复记录

Issue #2149实施过程中修复的5个运行时错误：

| 错误编号 | 错误类型 | 根因 | 修复方案 | 文件位置 |
|---------|---------|------|---------|---------|
| Error 1 | API 400 Bad Request | pageSize=1000超限（max 100） | 分页循环加载 | FormulaDetailViewModel.cs:417 |
| Error 2 | ProcessingMethod绑定错误 | 属性名不匹配 | 改为Remark | HerbCardControl.xaml:122 |
| Error 3 | Difficulty绑定错误 | FormulaDto无此属性 | 移除Difficulty显示 | FormulaDetailView.xaml:159 |
| Error 4 | CreatedAt绑定错误 | 缺少"Formula."前缀 | 修正绑定路径 | FormulaDetailView.xaml:284 |
| Error 5 | UpdatedAt绑定错误 | 缺少"Formula."前缀 | 修正绑定路径 | FormulaDetailView.xaml:298 |

---

## 第2章：功能性需求梳理

### 2.1 已实现的核心功能（FR-001 ~ FR-008）

#### FR-001: 药材快速输入（拼音码匹配）
**当前实现**: ✅ 完整实现
- **技术方案**: 7级智能评分算法
  - 100分：名称完全匹配
  - 90分：拼音码完全匹配
  - 80分：名称前缀匹配（如"当"匹配"当归"）
  - 70分：拼音码前缀匹配（如"dg"匹配"danggui"）
  - 50分：名称包含匹配
  - 40分：拼音码包含匹配
  - 30分：拼音码模糊跳跃匹配
- **性能**: 最多显示5个匹配结果，实时过滤<100ms
- **用户体验**: ComboBox自动弹出过滤列表

**验收标准**:
- [x] 输入"dg"自动匹配"当归"
- [x] 匹配结果按分数降序排列
- [x] ComboBox只显示药材名称（不包含拼音码）
- [x] FilteredHerbs实时响应HerbName变更

---

#### FR-002: 4列卡片式布局
**当前实现**: ✅ 完整实现
- **技术方案**: ItemsControl + UniformGrid (Columns=4)
- **响应式**: 固定4列，超出自动换行，支持ScrollViewer
- **用户体验**: 每个药材独立卡片，视觉清晰

**验收标准**:
- [x] 药材以4列网格排列
- [x] 超过4个药材自动换行
- [x] 支持垂直滚动

---

#### FR-003: 键盘快捷键导航
**当前实现**: ✅ 完整实现
- **Enter键逻辑**: ComboBox Enter → TextBox（剂量）
- **Enter键逻辑**: TextBox Enter → 下一个ComboBox（水平优先）
- **Shift+Delete**: 删除当前药材

**验收标准**:
- [x] Enter键自动跳转到剂量输入
- [x] 剂量输入后Enter跳转到下一个药材
- [x] Shift+Delete删除当前药材
- [x] 剂量输入框获得焦点时自动全选

---

#### FR-004: 重复药材自动检测和合并
**当前实现**: ✅ 完整实现
- **检测时机**: 剂量输入完成时（DosageCompleted事件）
- **合并策略**: 取较大剂量值
- **提示方式**: 弹窗提示"某某药材有重复，剂量改为XXg（取较大值）"

**验收标准**:
- [x] 同一药材重复添加时自动检测
- [x] 合并后保留较大剂量
- [x] 删除重复项
- [x] 提示用户合并结果

---

#### FR-005: 空槽位自动管理
**当前实现**: ✅ 完整实现
- **规则**: 至少保持4个空槽位（HerbId == Guid.Empty）
- **触发时机**: 剂量输入完成、删除药材后
- **自动前移**: 删除药材后，后续药材自动前移

**验收标准**:
- [x] 始终保持至少4个空槽
- [x] 删除药材后自动前移
- [x] 用户在第2个空槽输入时，完成后自动调整

---

#### FR-006: 药材信息自动填充
**当前实现**: ✅ 完整实现
- **触发时机**: ComboBox SelectionChanged事件
- **填充字段**: Unit（单位）
- **Command**: HerbSelectedCommand

**验收标准**:
- [x] 选择药材后自动填充单位
- [x] 单位从药材库HerbDto.Unit获取
- [x] 默认单位为"g"

---

#### FR-007: 只读/编辑模式切换
**当前实现**: ✅ 完整实现
- **IsEditMode**: DependencyProperty
- **控件绑定**: ComboBox.IsEnabled, TextBox.IsEnabled, Button.IsEnabled
- **视图模式**: 所有输入控件禁用

**验收标准**:
- [x] 编辑模式下可操作所有控件
- [x] 查看模式下所有控件禁用
- [x] IsEditMode状态正确传递到HerbCardControl

---

#### FR-008: 焦点管理（水平优先遍历）
**当前实现**: ✅ 完整实现
- **遍历规则**: 当前索引+1（水平方向下一个卡片）
- **实现方式**: FindParentItemsControl + ItemContainerGenerator
- **延迟Focus**: Dispatcher.BeginInvoke确保UI已渲染

**验收标准**:
- [x] Enter键跳转到下一个药材ComboBox
- [x] 水平优先（先第2列，再第3列...）
- [x] 到达最后一列后不再跳转

---

### 2.2 待确认的扩展功能（FR-009 ~ FR-018）

#### FR-009: 方剂模板管理（保存/加载常用方剂组合）
**业务场景**: 医生创建"白虎汤"模板，后续可快速套用

**技术方案**:
- **Server端**: FormulaService已实现（GetTemplatesAsync）
- **Client端**: 需要新增UI界面
  - FormulaTemplateSelectionDialog（模板选择对话框）
  - 应用模板后自动填充HerbItems

**实现复杂度**: ⭐⭐☆☆☆ (2/5)
**预估工作量**: 0.5天

**验收标准**:
- [ ] 保存当前验方为模板（设置IsShared=true）
- [ ] 打开模板选择对话框
- [ ] 选择模板后自动填充药材列表
- [ ] 支持公开/私有模板（IsShared字段）

**❓ 用户确认**:
- **Q1**: 是否需要此功能？优先级？
- **Q2**: 模板是否需要分类管理（内科方/外科方/妇科方/儿科方）？
- **Q3**: 模板应用后是否允许修改药材和剂量？

---

#### FR-010: 药材剂量建议（基于年龄/体重/病情）
**业务场景**: 系统根据患者年龄/体重提示剂量范围

**技术方案**:
- **数据来源**: Herb Entity增加DosageMin/DosageMax字段
- **UI提示**: 剂量输入框旁显示"建议剂量: 9-15g"
- **验证**: 超出范围时黄色警告（不阻止保存）

**实现复杂度**: ⭐⭐⭐☆☆ (3/5)
**预估工作量**: 1天

**验收标准**:
- [ ] Herb Entity增加DosageMin/DosageMax字段
- [ ] 药材库数据导入建议剂量范围
- [ ] HerbCardControl显示剂量建议
- [ ] 超出范围时显示警告（不阻止保存）

**❓ 用户确认**:
- **Q1**: 是否需要此功能？优先级？
- **Q2**: 剂量建议是否需要区分成人/儿童？
- **Q3**: 超出剂量范围是否需要强制确认？

---

#### FR-011: 配伍禁忌检查（十八反、十九畏）
**业务场景**: 添加"甘草+甘遂"时警告配伍禁忌

**技术方案**:
- **规则存储**: 配置文件（JSON）或数据库表
- **检查时机**: 剂量输入完成时
- **提示方式**: 弹窗警告"甘草+甘遂属十八反，请确认"

**实现复杂度**: ⭐⭐⭐⭐☆ (4/5)
**预估工作量**: 1.5天

**验收标准**:
- [ ] 内置十八反、十九畏规则库
- [ ] 添加药材时自动检测配伍禁忌
- [ ] 弹窗警告但不阻止保存
- [ ] 警告信息记录到Remark字段

**❓ 用户确认**:
- **Q1**: 是否需要此功能？优先级（建议P2-P3）？
- **Q2**: 配伍禁忌是否需要分级（强制阻止/警告/提示）？
- **Q3**: 规则库是否需要支持自定义扩展？

---

#### FR-012: 方剂总剂量计算和统计
**业务场景**: 显示方剂总剂量、平均剂量、药材数量

**技术方案**:
- **Client端**: FormulaCalculator组件（参考Client端设计文档）
- **计算属性**:
  - TotalQuantity: 所有药材剂量之和
  - AverageQuantity: 平均剂量
  - HerbCount: 药材数量（已实现）

**实现复杂度**: ⭐☆☆☆☆ (1/5)
**预估工作量**: 0.2天

**验收标准**:
- [ ] StatusBar显示"总剂量: XXg"
- [ ] StatusBar显示"平均剂量: XXg"
- [ ] 药材数量统计（已实现）

**❓ 用户确认**:
- **Q1**: 是否需要显示总剂量和平均剂量？
- **Q2**: 是否需要显示单剂量和总剂量（如7剂）？

---

#### FR-013: 药材成本估算
**业务场景**: 显示方剂成本（单价×剂量之和）

**技术方案**:
- **数据来源**: HerbDto.Price字段
- **计算属性**: TotalPrice（已在FormulaDto中实现）
- **UI显示**: StatusBar显示"总价: ¥XX.XX"

**实现复杂度**: ⭐☆☆☆☆ (1/5)
**预估工作量**: 0.1天

**验收标准**:
- [ ] 自动计算总价（已在DTO实现）
- [ ] FormulaDetailView显示总价
- [ ] 单个药材显示小计（UnitPrice × Quantity）

**❓ 用户确认**:
- **Q1**: 是否需要在编辑界面显示价格？（当前Issue #2149移除了价格显示）
- **Q2**: 价格是否需要动态更新（药材库价格变动）？

---

#### FR-014: 批量导入药材（从文本粘贴）
**业务场景**: 医生从Excel或文本粘贴药材清单

**技术方案**:
- **输入格式**: "当归 10g\n黄芪 15g\n甘草 6g"
- **解析逻辑**: 正则表达式提取药材名称和剂量
- **自动匹配**: 调用TryMatchHerbAsync

**实现复杂度**: ⭐⭐⭐☆☆ (3/5)
**预估工作量**: 0.5天

**验收标准**:
- [ ] 支持多行文本粘贴
- [ ] 自动解析药材名称和剂量
- [ ] 自动匹配药材库（拼音码匹配）
- [ ] 显示匹配结果（成功/失败数量）

**❓ 用户确认**:
- **Q1**: 是否需要此功能？优先级？
- **Q2**: 输入格式是否需要支持多种（空格分隔/逗号分隔/制表符分隔）？

---

#### FR-015: 方剂历史版本管理
**业务场景**: 查看方剂修改历史，回退到之前版本

**技术方案**:
- **Server端**: FormulaVersion Entity（参考Server端设计文档第10.3节）
- **Client端**: FormulaVersionView（版本对比界面）
- **数据存储**: JSON序列化快照

**实现复杂度**: ⭐⭐⭐⭐⭐ (5/5)
**预估工作量**: 2天

**验收标准**:
- [ ] 每次保存时创建版本快照
- [ ] 显示版本历史列表（Version, CreatedAt, CreatedBy）
- [ ] 支持版本对比（Diff药材组成）
- [ ] 支持回退到历史版本

**❓ 用户确认**:
- **Q1**: 是否需要此功能？优先级（建议P3）？
- **Q2**: 版本是否需要保留全部历史还是仅最近N个版本？

---

#### FR-016: 方剂分享和导出（XML/JSON）
**业务场景**: 导出方剂为标准格式，分享给其他医生

**技术方案**:
- **导出格式**: JSON（机器可读）或XML（标准中医格式）
- **导入功能**: 从JSON/XML导入方剂
- **Server端**: FormulaService.ExportAsync（已实现Excel导出）

**实现复杂度**: ⭐⭐☆☆☆ (2/5)
**预估工作量**: 0.5天

**验收标准**:
- [ ] 导出为JSON格式
- [ ] 导出为XML格式（可选）
- [ ] 从JSON导入方剂
- [ ] 导入时自动匹配药材库

**❓ 用户确认**:
- **Q1**: 是否需要此功能？优先级？
- **Q2**: 导出格式是否需要符合特定中医标准（如HL7 FHIR）？

---

#### FR-017: 剂量单位标准化（下拉选择）
**业务场景**: 防止单位输入错误，统一使用"g/克/片/粒"等标准单位

**技术方案**:
- **UI变更**: TextBlock改为ComboBox
- **单位列表**: ["g", "克", "片", "粒", "ml", "毫升", "个", "枚"]
- **默认值**: "g"

**实现复杂度**: ⭐☆☆☆☆ (1/5)
**预估工作量**: 0.2天

**验收标准**:
- [ ] 单位使用下拉选择而非自由输入
- [ ] 支持常用8种单位
- [ ] 默认单位为"g"
- [ ] 保存时验证单位有效性

**❓ 用户确认**:
- **Q1**: 是否需要单位标准化？（当前为自由输入）
- **Q2**: 单位列表是否需要可配置？

---

#### FR-018: 自由处方支持（药材库外药材）
**业务场景**: 添加临时药材（如"家传秘方药材"），不在药材库中

**技术方案**:
- **Entity支持**: FormulaHerbItem.HerbId已支持Nullable（已实现）
- **UI支持**: ComboBox允许自定义输入（IsEditable=True）
- **验证逻辑**: HerbId为null时标记为IsValidated=false

**实现复杂度**: ⭐⭐☆☆☆ (2/5)
**预估工作量**: 0.3天

**验收标准**:
- [ ] 允许输入药材库外的药材名称
- [ ] HerbId保存为null
- [ ] ValidationStatus标记为Draft
- [ ] 后续可通过ValidateFormulaHerbAsync匹配

**❓ 用户确认**:
- **Q1**: 是否需要支持自由处方？
- **Q2**: 自由处方是否需要医生特殊权限？
- **Q3**: 自由处方药材是否允许保存到药材库（审核后）？

---

## 第3章：业务规则和约束

### 3.1 数据验证规则

| 规则编号 | 规则描述 | 验证时机 | 错误级别 |
|---------|---------|---------|---------|
| BR-001 | 药材名称不能为空 | 保存时 | Error |
| BR-002 | 剂量必须在0.1-500之间 | 输入时 | Error |
| BR-003 | 单位不能为空 | 保存时 | Error |
| BR-004 | 方剂至少包含1味药材 | 保存时 | Error |
| BR-005 | 药材名称最大长度100字符 | 输入时 | Error |
| BR-006 | 重复药材自动合并 | 剂量完成时 | Warning |
| BR-007 | 配伍禁忌警告（如开启） | 剂量完成时 | Warning |
| BR-008 | 剂量超出建议范围（如开启） | 输入时 | Warning |

### 3.2 权限控制规则

| 规则编号 | 规则描述 | 影响范围 |
|---------|---------|---------|
| BR-009 | Doctor角色可创建/编辑私有方剂 | CRUD操作 |
| BR-010 | Doctor角色可查看公开方剂（IsShared=true） | Read操作 |
| BR-011 | Admin角色可编辑所有方剂 | 全部操作 |
| BR-012 | 方剂创建者默认为当前登录用户 | Create操作 |

### 3.3 UI交互规则

| 规则编号 | 规则描述 | 用户体验影响 |
|---------|---------|-------------|
| BR-013 | 至少保持4个空槽位 | 流畅输入体验 |
| BR-014 | 剂量输入框获得焦点时全选 | 快速修改剂量 |
| BR-015 | Enter键触发焦点跳转 | 键盘快捷操作 |
| BR-016 | Shift+Delete删除当前药材 | 快捷删除 |
| BR-017 | 拼音码匹配最多显示5个结果 | 避免列表过长 |
| BR-018 | 药材选择后自动填充单位 | 减少输入 |

---

## 第4章：架构约束和技术要求

### 4.1 技术栈约束

| 技术栈 | 版本 | 约束说明 |
|-------|------|---------|
| .NET | 8.0 | 前后端统一使用.NET 8 |
| WPF | .NET 8 | Desktop UI框架 |
| Prism.DryIoc | 9.0 | MVVM框架+DI容器 |
| EF Core | 8.0 | ORM，禁止原始SQL |
| SQL Server | 2019+ | 关系型数据库 |

### 4.2 架构模式约束

**Client端（UltraThink双层架构）**:
- ✅ Module委托层 + QueryService/BusinessService
- ✅ 统一IService接口，无重复IModule
- ✅ ViewModel组件化（DataManager/CommandHandler/Calculator/Validator）
- ❌ 禁止使用MediatR（违反MVP原则）
- ❌ 禁止使用AutoMapper（前端手动映射）

**Server端（传统三层架构）**:
- ✅ Controller → Service → Repository → EF Core
- ✅ 聚合根模式（Formula聚合FormulaHerbItem）
- ✅ 所有数据库操作必须异步（Async/Await）
- ✅ 使用LINQ查询，禁止原始SQL
- ❌ 禁止使用Redis（违反MVP原则）

### 4.3 性能要求

| 性能指标 | 目标值 | 测量方法 |
|---------|-------|---------|
| 拼音码过滤响应时间 | <100ms | UI线程计时 |
| 药材列表加载时间 | <500ms | API响应时间 |
| 方剂保存时间 | <1s | API响应时间 |
| 重复检测时间 | <50ms | 内存操作 |

### 4.4 兼容性要求

| 兼容性项 | 要求 |
|---------|------|
| Windows版本 | Windows 10 1809+ |
| .NET Runtime | .NET 8.0+ |
| SQL Server | 2019+ |
| 屏幕分辨率 | 1366x768+ |

---

## 第5章：数据模型设计

### 5.1 实体关系图

```
Formula (聚合根)
├── Id: Guid
├── Name: string (max 200)
├── Effect: string? (功效)
├── Indication: string? (主治) - 重构新增
├── Usage: string? (用法)
├── Property: string? (性味)
├── Category: string? (分类)
├── ValidationStatus: enum (Draft | Validated)
├── IsShared: bool (公开/私有)
├── CreatedBy: Guid
├── CreatedAt: DateTime
├── UpdatedAt: DateTime
├── IsDeleted: bool (软删除)
└── Herbs: List<FormulaHerbItem> (1:N)

FormulaHerbItem (值对象，支持延迟绑定)
├── Id: Guid
├── FormulaId: Guid (外键)
├── HerbId: Guid? (可空，支持自由处方)
├── OriginalHerbName: string? (原始名称)
├── IsValidated: bool (是否已验证)
├── HerbName: string (当前名称)
├── Quantity: decimal (剂量)
├── Unit: string (单位)
├── ProcessingMethod: string? (炮制方法)
├── Remark: string? (备注)
└── SortOrder: int (排序序号)
```

### 5.2 DTO设计

**FormulaDto** (Server → Client):
```csharp
public class FormulaDto {
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Effect { get; set; }
    public string? Indication { get; set; }
    public FormulaValidationStatus ValidationStatus { get; set; }
    public bool IsShared { get; set; }
    public List<FormulaHerbItemDto> Herbs { get; set; }
    public decimal TotalPrice { get; set; }  // 计算属性
    public int HerbCount { get; set; }       // 计算属性
}
```

**FormulaInputDto** (Client → Server):
```csharp
public class FormulaInputDto {
    public string Name { get; set; }
    public string? Effect { get; set; }
    public string? Indication { get; set; }
    public bool IsShared { get; set; }
    public List<FormulaHerbItemInputDto> Herbs { get; set; }
}
```

**FormulaHerbItemInputDto** (支持延迟绑定):
```csharp
public class FormulaHerbItemInputDto {
    public Guid? HerbId { get; set; }        // 可空
    public string HerbName { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; }
    public string? ProcessingMethod { get; set; }
}
```

---

## 第6章：开放问题和决策点

### 6.1 MVP范围界定（Q1）

**问题**: 本次需求范围是仅优化现有UI交互，还是增加高级功能？

**选项**:
- **选项A（MVP）**: 仅优化Issue #2149已实现功能，不增加新功能
  - 优势：工作量小（0天），风险低
  - 劣势：功能不够完善
- **选项B（增量）**: 增加2-3个高优先级功能（如FR-012总剂量统计、FR-017单位标准化）
  - 优势：提升用户体验，工作量可控（0.5天）
  - 劣势：需要回归测试
- **选项C（完整）**: 实现所有18个功能需求
  - 优势：功能完整
  - 劣势：工作量大（8-10天），违反MVP原则

**推荐**: 选项B（增量），优先实现FR-012、FR-017

---

### 6.2 自由处方支持（Q2）

**问题**: 是否允许添加药材库中不存在的药材？

**选项**:
- **选项A（允许）**: HerbId为null，保存为Draft状态
  - 优势：灵活性高，支持家传秘方
  - 劣势：数据质量降低
- **选项B（禁止）**: 必须从药材库选择
  - 优势：数据质量高
  - 劣势：灵活性差

**推荐**: 选项A（允许），但需要ValidationStatus标记

---

### 6.3 方剂模板共享（Q3）

**问题**: 是否需要支持公开/私有模板功能？

**选项**:
- **选项A（支持）**: 使用IsShared字段区分
  - 优势：团队协作，知识共享
  - 劣势：需要权限控制逻辑
- **选项B（仅私有）**: 所有模板仅创建者可见
  - 优势：简单
  - 劣势：无法团队共享

**推荐**: 选项A（支持），IsShared字段已存在

---

### 6.4 配伍禁忌规则库（Q4）

**问题**: 是否需要内置中医配伍禁忌检查（十八反、十九畏）？

**选项**:
- **选项A（内置）**: 预置规则库，自动检查
  - 优势：医疗安全性高
  - 劣势：规则维护复杂，工作量大（1.5天）
- **选项B（不内置）**: 医生自行判断
  - 优势：简单
  - 劣势：安全性低

**推荐**: 选项B（不内置），作为P3优先级待办

---

### 6.5 剂量单位标准化（Q5）

**问题**: 单位字段是否需要从自由输入改为下拉选择？

**选项**:
- **选项A（下拉选择）**: ComboBox限定8种常用单位
  - 优势：数据一致性高
  - 劣势：灵活性稍差
- **选项B（自由输入）**: 保持当前TextBlock
  - 优势：灵活
  - 劣势：可能出现"克/g/公克"等不一致

**推荐**: 选项A（下拉选择），工作量小（0.2天）

---

### 6.6 历史版本管理（Q6）

**问题**: 是否需要保留方剂修改历史版本？

**选项**:
- **选项A（支持）**: FormulaVersion表存储历史快照
  - 优势：可追溯，支持回退
  - 劣势：存储空间增加，工作量大（2天）
- **选项B（不支持）**: 仅保存最新版本
  - 优势：简单
  - 劣势：无法追溯

**推荐**: 选项B（不支持），作为P3优先级待办

---

## 第7章：推荐实施计划

### 7.1 Phase 1: 基础优化（0.5天）

**目标**: 完善Issue #2149已实现功能

**任务清单**:
- [ ] FR-012: 总剂量统计显示（StatusBar）
- [ ] FR-017: 剂量单位标准化（下拉选择）
- [ ] 回归测试：验证拼音码匹配、焦点管理、重复检测

**验收标准**:
- [ ] StatusBar显示总剂量和平均剂量
- [ ] 单位使用ComboBox选择
- [ ] 所有Issue #2149功能正常运行

---

### 7.2 Phase 2: 扩展功能（可选，1-2天）

**前置条件**: 用户确认需要以下功能

**任务清单**:
- [ ] FR-009: 方剂模板管理（如用户确认Q1）
- [ ] FR-013: 药材成本估算（如用户确认Q2）
- [ ] FR-018: 自由处方支持（如用户确认Q3）

---

### 7.3 Phase 3: 高级功能（待定，2-3天）

**前置条件**: 用户确认需要以下高级功能

**任务清单**:
- [ ] FR-010: 药材剂量建议
- [ ] FR-011: 配伍禁忌检查
- [ ] FR-014: 批量导入药材
- [ ] FR-015: 历史版本管理

---

## 附录A：7级拼音码匹配算法详解

**算法代码**（已实现）:
```csharp
private int GetMatchScore(HerbDto herb, string searchText) {
    // 1. 名称完全匹配：100分
    if (herbName == searchText) return 100;

    // 2. 拼音码完全匹配：90分
    if (pinyinCode == searchText) return 90;

    // 3. 名称前缀匹配：80分（例如：输入"当"匹配"当归"）
    if (herbName.StartsWith(searchText)) return 80;

    // 4. 拼音码前缀匹配：70分（例如：输入"dg"匹配"danggui"）
    if (pinyinCode.StartsWith(searchText)) return 70;

    // 5. 名称包含匹配：50分
    if (herbName.Contains(searchText)) return 50;

    // 6. 拼音码包含匹配：40分
    if (pinyinCode.Contains(searchText)) return 40;

    // 7. 拼音码模糊匹配：30分（跳跃式匹配）
    if (IsPinyinFuzzyMatch(pinyinCode, searchText)) return 30;

    return 0; // 无匹配
}
```

**性能优化**:
- 最多返回5个匹配结果（Take(5)）
- 过滤操作<100ms（内存LINQ查询）

---

## 附录B：焦点管理水平优先遍历逻辑

**遍历规则**:
```
[药材1] [药材2] [药材3] [药材4]
[药材5] [药材6] [药材7] [药材8]

Enter键遍历顺序:
药材1.ComboBox → 药材1.Quantity → 药材2.ComboBox → 药材2.Quantity → ...
```

**实现代码**（已实现）:
```csharp
private void MoveFocusToNextHerbName() {
    var itemsControl = FindParentItemsControl(this);
    var currentIndex = itemsControl.Items.IndexOf(DataContext);
    int nextIndex = currentIndex + 1;  // 水平优先：+1

    if (nextIndex < itemsControl.Items.Count) {
        var nextContainer = itemsControl.ItemContainerGenerator
            .ContainerFromIndex(nextIndex) as ContentPresenter;
        var nextHerbCard = FindVisualChild<HerbCardControl>(nextContainer);
        nextHerbCard.HerbNameComboBox.Focus();
    }
}
```

---

## 附录C：相关文档索引

| 文档名称 | 路径 | 说明 |
|---------|------|------|
| Issue #2149 | GitHub | 已完成的基础实现 |
| Server端Formula设计 | docs/explanation/architecture/server/formula-design.md | 后端API和数据模型 |
| Client端Formula设计 | docs/explanation/architecture/client/formula-design.md | 前端MVVM架构 |
| 验方药材管理重构需求 | docs/requirements/formula-herb-management-refactoring-requirements.md | 2025-11-10重构需求 |

---

## 第8章：用户确认检查清单

**请用户逐项确认以下问题，以便生成精确的技术设计文档**：

### 核心问题（必答）

- [ ] **Q1-MVP范围**: 选择Phase 1（基础优化）/ Phase 2（扩展功能）/ Phase 3（高级功能）？
- [ ] **Q2-自由处方**: 是否允许添加药材库外的药材（IsShared=Draft）？
- [ ] **Q3-模板共享**: 是否需要公开/私有模板（IsShared字段）？
- [ ] **Q4-配伍禁忌**: 是否需要十八反、十九畏检查（优先级P2-P3）？
- [ ] **Q5-单位标准化**: 是否改为下拉选择（推荐实施）？
- [ ] **Q6-版本管理**: 是否需要历史版本回退功能（优先级P3）？

### 细节问题（可选）

- [ ] **Q7-总剂量显示**: 是否需要显示单剂/总剂（如7剂）？
- [ ] **Q8-价格显示**: 是否在编辑界面显示药材价格和总价？
- [ ] **Q9-批量导入**: 是否需要文本粘贴批量导入功能？
- [ ] **Q10-剂量建议**: 是否需要基于年龄/体重的剂量建议？

---

**下一步**: 用户确认后，调用`lybtzyzs-design-generator` skill生成技术设计文档。