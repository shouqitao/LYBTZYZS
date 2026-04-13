# 医案工作台当前状态文档

> **文档版本**: v1.0  
> **创建日期**: 2026-04-10  
> **最后更新**: 2026-04-10  
> **状态**: 审计完成，待优化  
> **范围**: MedicalCase 模块完整架构、功能实现、数据流、UI 交互

---

## 一、架构概览

### 1.1 模块定位

MedicalCase 是 LYBTZYZS 系统的**核心诊疗模块**，负责医案（MedicalCase）的全生命周期管理：

- **层级**: Client Modules 层 (`LYBT.Desktop.MedicalCase`)
- **职责**: 医案诊疗流程编排、诊断数据采集、处方开具、医案管理
- **架构模式**: Master-Detail + Composite ViewModel（部分实现）
- **DDD 定位**: MedicalCase 是唯一聚合根，Consultation（诊断）和 Prescription（处方）是内部实体

### 1.2 技术栈

| 技术 | 版本/框架 |
|------|-----------|
| 运行时 | .NET 8.0 Windows |
| UI 框架 | WPF + Prism.DryIoc 8.1.97 |
| MVVM 基类 | CommunityToolkit.Mvvm (source generators) |
| 对象映射 | Riok.Mapperly (编译时映射) |
| 数据访问 | Refit HTTP 客户端 + EF Core (Server 层) |
| 本地存储 | SQLite (Local 模式) |
| 远程存储 | SQL Server (Remote 模式) |

### 1.3 模块依赖关系

```
LYBT.Desktop.MedicalCase
├── 依赖模块
│   ├── LYBT.Desktop.Patients (患者档案)
│   ├── LYBT.Desktop.Herbs (药材库)
│   └── LYBT.Desktop.Formula (验方库)
├── 被依赖模块
│   ├── LYBT.Desktop.Admin (系统管理)
│   └── LYBT.Desktop.Clinical (临床工作台)
└── 共享层
    ├── LYBT.Shared.Models (DTO 契约)
    ├── LYBT.Shared.Validators (FluentValidation)
    └── LYBT.Desktop.* (Core 层基础设施)
```

---

## 二、代码结构

### 2.1 目录结构

```
src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/
├── Controls/                          # 可复用 UI 控件
│   ├── MedicalCaseEditControl.xaml    # 医案编辑控件 (392 行)
│   ├── MedicalCaseEditControl.xaml.cs # 18 个 DependencyProperty
│   ├── MedicalCaseViewControl.xaml    # 医案只读预览控件
│   ├── MedicalCaseViewControl.xaml.cs # 10 个 DependencyProperty
│   ├── MedicalCaseMasterDetailControl.xaml  # Master-Detail 容器
│   └── MedicalCaseMasterDetailControl.xaml.cs
├── ViewModels/                        # 视图模型
│   ├── MedicalCaseMasterDetailViewModel.cs  # 核心 VM (327 行)
│   └── Workspace/                     # 工作区子 VM (Composite 模式)
│       ├── ConsultationEditorViewModel.cs   # 诊断编辑器 VM
│       ├── PrescriptionEditorViewModel.cs   # 处方编辑器 VM
│       └── MedicalCaseCommandsViewModel.cs  # 命令 VM (455 行)
├── Models/                            # 数据模型
│   ├── MedicalCaseDetailModel.cs      # 医案详情模型 (ValidatableModelBase)
│   ├── WorkspaceState.cs              # 工作区状态 (immutable record)
│   ├── EditState.cs                   # 编辑状态枚举
│   ├── EditType.cs                    # 编辑类型枚举
│   ├── WorkspaceMode.cs               # 工作区模式枚举
│   ├── MedicalCaseNavigationParameters.cs  # 导航参数封装
│   └── Items/
│       ├── ConsultationItem.cs        # 诊断数据项
│       └── PrescriptionItem.cs        # 处方数据项
├── Mappers/                           # 对象映射
│   ├── MedicalCaseDetailModelMapper.cs     # DetailModel 映射
│   ├── ConsultationMapper.cs               # 诊断映射
│   ├── PrescriptionMapper.cs               # 处方映射
│   └── MedicalCaseCloneMapper.cs           # 深拷贝映射
├── Services/                          # 业务服务
│   ├── MedicalCaseService.cs          # 聚合根门面服务
│   └── Interfaces/
│       └── IMedicalCaseService.cs     # 服务接口 (Query+Command+Lifecycle)
├── Repositories/                      # 数据仓储
│   ├── MedicalCaseRepository.cs       # 仓储实现
│   └── Interfaces/
│       └── IMedicalCaseRepository.cs  # 仓储接口
├── Dialogs/                           # 弹窗组件
│   ├── FormulaImportDialog.xaml/.cs   # 验方导入弹窗
│   ├── HistoryCopyDialog.xaml/.cs     # 历史处方复制弹窗
│   └── UnsavedChangesDialog.xaml/.cs  # 未保存修改确认弹窗
├── Extensions/
│   └── PrescriptionImportExtensions.cs  # 处方导入扩展方法
└── MedicalCaseModule.cs               # Prism 模块注册入口
```

### 2.2 核心类职责

| 类名 | 行数 | 职责 | 状态 |
|------|------|------|------|
| `MedicalCaseMasterDetailViewModel` | 327 | 列表加载、详情加载、保存、删除、药材预加载 | ✅ 稳定 |
| `MedicalCaseDetailModel` | ~200 | 医案详情数据模型，含计算属性 | ✅ 稳定 |
| `MedicalCaseService` | ~400 | 聚合根门面，CRUD+ 生命周期操作 | ✅ 稳定 |
| `MedicalCaseCommandsViewModel` | 455 | 命令聚合 (保存/挂起/完成/打印/导入) | ✅ 稳定 |
| `ConsultationEditorViewModel` | ~100 | 诊断数据包装，初始化逻辑 | ✅ 稳定 |
| `PrescriptionEditorViewModel` | ~100 | 处方数据包装，集合变更通知 | ✅ 稳定 |
| `MedicalCaseEditControl` | 392 XAML | 编辑表单 UI，支持 Full/Compact 双模式 | ✅ 稳定 |
| `WorkspaceState` | ~50 | 不可变状态记录 (C# record) | ✅ 稳定 |

---

## 三、已实现功能清单

### 3.1 医案管理功能（Management 模式）

| 功能 | 状态 | 入口 | 说明 |
|------|------|------|------|
| 医案列表查询 | ✅ 已实现 | Sidebar → 医案管理 | 分页查询，支持关键词搜索 |
| 医案详情查看 | ✅ 已实现 | 列表选中项 | 只读模式，显示完整医案信息 |
| 医案编辑 | ✅ 已实现 | 点击"编辑"按钮 | 切换到编辑模式，支持保存 |
| 医案删除 | ✅ 已实现 | 列表操作列 | 软删除，支持恢复 |
| 处方药材编辑 | ✅ 已实现 | EditControl 内 | 添加/删除/修改药材，价格自动计算 |
| 验方导入 | ✅ 已实现 | "套验方"按钮 | FormulaImportDialog 弹窗选择 |
| 历史处方复制 | ✅ 已实现 | "历史处方"按钮 | HistoryCopyDialog 双栏选择 |
| 打印预览 | ✅ 已实现 | "打印"按钮 | 处方打印模板，A4 格式 |
| 药材拼音补全 | ✅ 已实现 | 输入药材名 | 自动匹配药材库，拼音首字母搜索 |

### 3.2 临床看诊功能（Clinical 模式）

| 功能 | 状态 | 入口 | 说明 |
|------|------|------|------|
| 患者选择 | ✅ 已实现 | Clinical 工作台 → 开始接诊 | PatientSelectionControl 左右分栏 |
| 待诊队列 | ✅ 已实现 | 患者选择页 | PendingQueueManager 加载待诊患者 |
| 医案创建 | ✅ 已实现 | 选择患者后 | 自动创建 Active 状态医案 |
| 诊断填写 | ✅ 已实现 | EditControl 诊断区 | 现病史/舌诊/脉诊/中医诊断 |
| 处方开具 | ✅ 已实现 | EditControl 处方区 | 手工输入/验方导入/历史复制 |
| 医案挂起 | ⚠️ 部分实现 | 待确认入口 | API 已支持，UI 入口待确认 |
| 医案完成 | ⚠️ 部分实现 | 待确认入口 | API 已支持，UI 入口待确认 |
| BR-001 碰撞处理 | ⚠️ 部分实现 | 创建医案时 | UnfinishedCaseHandler 已实现 |
| BR-002 离开决策 | ❌ 未实现 | 离开编辑页时 | UnsavedChangesDialog 已创建但未集成 |
| BR-003 完成校验 | ❌ 未实现 | 点击"完成看诊"时 | 服务端校验已实现，客户端预校验缺失 |

### 3.3 基础设施

| 功能 | 状态 | 说明 |
|------|------|------|
| Local/Remote 模式切换 | ✅ 已实现 | 通过 DataSource 抽象层支持 |
| JWT 认证 | ✅ 已实现 | AuthorizationMessageHandler 自动注入 |
| 错误处理 | ✅ 已实现 | 统一 ErrorHandler + 用户友好提示 |
| 日志记录 | ✅ 已实现 | [SVC] 前缀日志，结构化日志 |
| 缓存管理 | ✅ 已实现 | DesktopCacheManager 医案缓存失效 |
| 数据验证 | ⚠️ 部分实现 | 仅 TcmDiagnosis 有验证错误显示 |

---

## 四、数据流分析

### 4.1 当前数据流架构

```
┌─────────────────────────────────────────────────────────────┐
│                    MedicalCaseMasterDetailViewModel         │
│                                                             │
│  ┌──────────────────┐  ┌──────────────┐  ┌───────────────┐ │
│  │ Consultation     │  │ Prescription │  │ MedicalCase   │ │
│  │ (ConsultationItem)│  │ (PrescriptionItem)│  │ DetailModel   │ │
│  │                  │  │              │  │               │ │
│  │ - PresentIllness │  │ - DosageCount│  │ - PatientName │ │
│  │ - TongueDiagnosis│  │ - Usage      │  │ - Status      │ │
│  │ - PulseDiagnosis │  │ - Remark     │  │ - Remark      │ │
│  │ - TcmDiagnosis   │  │ - Items[]    │  │ - DoseCount   │ │
│  └────────┬─────────┘  └──────┬───────┘  └───────┬───────┘ │
│           │                   │                   │         │
│           └───────────────────┼───────────────────┘         │
│                               │                             │
│                    ┌──────────▼──────────┐                  │
│                    │  InitializeEditModels │                  │
│                    │  (数据拆分/同步方法)   │                  │
│                    └─────────────────────┘                  │
└─────────────────────────────────────────────────────────────┘
```

### 4.2 数据加载流程

```
1. 用户选择医案列表项
   ↓
2. LoadDetailAsync(item) 被调用
   ↓
3. _repository.GetByIdAsync(item.Id) 获取 MedicalCaseDetailDto
   ↓
4. _mapper.ToItem(dto) 转换为 MedicalCaseDetailModel
   ↓
5. InitializeEditModels(detail) 拆分数据:
   ├── 创建 ConsultationItem (从 detail 的诊断字段)
   ├── 创建 PrescriptionItem (从 detail 的处方的字段)
   └── 加载 PrescriptionItems 集合 (药材列表)
   ↓
6. MasterDetailServices.DetailEditor.LoadDetail(detail) 加载到编辑器
   ↓
7. XAML 绑定到 EditControl 的 DependencyProperty
```

### 4.3 数据保存流程

```
1. 用户点击"保存"按钮
   ↓
2. SaveDetailAsync(detail) 被调用
   ↓
3. 从 Consultation 构建 ConsultationInputDto
   ↓
4. 从 Prescription 构建 PrescriptionInputDto (含 Items 集合)
   ↓
5. 组装 MedicalCaseInputDto (聚合 DTO)
   ↓
6. _repository.SaveAsync(detail.Id, aggregateDto) 保存
   ↓
7. _cacheManager.InvalidateMedicalCaseCaches() 清除缓存
   ↓
8. 刷新列表
```

### 4.4 数据流问题诊断

| 问题 | 严重性 | 说明 |
|------|--------|------|
| **三套数据模型并存** | 🔴 高 | Consultation + Prescription + MedicalCaseDetailModel 独立管理，同步复杂 |
| **InitializeEditModels 职责过重** | 🟡 中 | 负责 DTO→Item 转换、数据拆分、价格计算、集合加载 |
| **Remark 数据源不一致** | 🔴 高 | `detail.Remark` 与 `Prescription.Remark` 并存，保存时使用后者 |
| **TcmDiagnosis 双写** | 🟡 中 | `Consultation.TcmDiagnosis` 和 `detail.TcmDiagnosis` 可能不同步 |
| **价格计算时机** | 🟢 低 | `SingleDosePrice` 在初始化时计算一次，药材变更后需手动触发 |

---

## 五、UI 交互现状

### 5.1 MedicalCaseEditControl 布局

#### Full 模式（Management 场景）

```
┌──────────────────────────────────────────────────────┐
│ 患者信息卡片（只读）                                    │
│ 患者姓名 | 就诊日期 | 接诊医生 | 状态                    │
├──────────────────────────────────────────────────────┤
│ 诊疗信息卡片（可编辑）                                  │
│ 现病史 (整行, TabIndex=1)                              │
│ 舌诊 (左, TabIndex=2) | 脉诊 (右, TabIndex=3)          │
│ 中医诊断* (整行, TabIndex=4)                            │
├──────────────────────────────────────────────────────┤
│ 处方药材卡片（可编辑）                                  │
│ 剂数 | 方源                                            │
│ ┌────────────────────────────────────────────────┐   │
│ │ HerbListControl (4 列药材编辑)                     │   │
│ └────────────────────────────────────────────────┘   │
├──────────────────────────────────────────────────────┤
│ 备注卡片（可编辑）                                     │
│ 备注文本框                                             │
├──────────────────────────────────────────────────────┤
│ 系统信息卡片（只读）                                    │
│ 创建时间 | 更新时间                                    │
└──────────────────────────────────────────────────────┘
```

#### Compact 模式（Clinical 场景）

```
┌──────────────────────────────────────────────────────┐
│ [套验方] [历史处方] [清空]                    (右上角工具条)│
├──────────────────────────────────────────────────────┤
│ 诊断区 (Border 容器)                                   │
│ 现病史 (TabIndex=8)                                    │
│ 舌诊 (左, TabIndex=9) | 脉诊 (右, TabIndex=10)         │
│ 中医诊断* (TabIndex=11, ValidatingTextBoxStyle)        │
├──────────────────────────────────────────────────────┤
│ 处方区 (Border 容器)                                   │
│ ┌────────────────────────────────────────────────┐   │
│ │ HerbListControl (4 列药材编辑)                     │   │
│ └────────────────────────────────────────────────┘   │
│ 共 X 味药材 | 剂数:[7] | 用法:[下拉] | 总价:¥315.00      │
└──────────────────────────────────────────────────────┘
```

### 5.2 验证框架应用现状

| 字段 | 验证样式 | 错误显示 | 验证规则 |
|------|----------|----------|----------|
| 中医诊断 | ✅ ValidatingTextBoxStyle | ✅ ValidationErrorMessageVisibleStyle | 必填 |
| 现病史 | ⚠️ EditableTextBoxStyle | ❌ 无 | 无 |
| 舌诊 | ⚠️ EditableTextBoxStyle | ❌ 无 | 无 |
| 脉诊 | ⚠️ EditableTextBoxStyle | ❌ 无 | 无 |
| 剂数 | ⚠️ EditableTextBoxStyle | ❌ 无 | 无 |
| 用法 | ⚠️ ComboBox (FilterComboBox) | ❌ 无 | 无 |
| 备注 | ⚠️ EditableTextBoxStyle | ❌ 无 | 无 |

### 5.3 TabIndex 分布

| 区域 | TabIndex 范围 | 说明 |
|------|---------------|------|
| Full 模式 - 患者信息 | 无 | 只读字段，不参与 Tab |
| Full 模式 - 诊断区 | 1-4 | 现病史→舌诊→脉诊→中医诊断 |
| Full 模式 - 处方区 | 5-7 | 工具条按钮 |
| Full 模式 - 备注 | 无 | 未设置 TabIndex |
| Compact 模式 - 诊断区 | 8-11 | 现病史→舌诊→脉诊→中医诊断 |
| Compact 模式 - 处方区 | 12-13 | 剂数→用法 |

---

## 六、与 PRD 的差距分析

### 6.1 功能差距

| PRD 要求 | 当前状态 | 差距说明 | 优先级 |
|----------|----------|----------|--------|
| **US-MC-011: Clinical/Management 模式区分** | ⚠️ 部分实现 | WorkspaceState 已创建，但 UI 未区分 | P0 |
| **US-MC-006: 挂起医案** | ⚠️ API 已支持 | UI 入口待确认，按钮未集成 | P0 |
| **US-MC-007: 完成医案** | ⚠️ API 已支持 | UI 入口待确认，BR-003 校验缺失 | P0 |
| **US-MC-008: 取消医案** | ⚠️ API 已支持 | UI 入口待确认 | P1 |
| **BR-001: 碰撞处理** | ✅ 已实现 | UnfinishedCaseHandler 处理多医生场景 | - |
| **BR-002: 离开决策** | ❌ 未实现 | UnsavedChangesDialog 已创建但未集成 | P0 |
| **BR-003: 完成校验** | ⚠️ 服务端已实现 | 客户端预校验缺失 | P1 |
| **US-MC-015: 打印流程** | ✅ 已实现 | 打印预览+PrintCount 回写 | - |
| **US-MC-016: 验方导入** | ✅ 已实现 | FormulaImportDialog 弹窗 | - |
| **US-MC-017: 待诊队列** | ✅ 已实现 | PendingQueueManager + 队列视图 | - |
| **US-MC-018: 历史处方复制** | ✅ 已实现 | HistoryCopyDialog 双栏选择 | - |

### 6.2 交互差距

| PRD 要求 | 当前状态 | 差距说明 |
|----------|----------|----------|
| **Clinical 模式底部按钮**: [挂起] [打印] [完成] | ❌ 缺失 | EditControl 无底部按钮区 |
| **Management 模式底部按钮**: [编辑] [打印] | ⚠️ 部分实现 | 按钮存在但未按模式区分 |
| **离开保护弹窗** | ❌ 缺失 | 无 BR-002 决策流程 |
| **完成前校验提示** | ❌ 缺失 | 无客户端预校验 |
| **医案状态显示** | ✅ 已实现 | StatusBadge 组件显示状态 |
| **实时保存** | ❌ 缺失 | 需手动点击保存 |

---

## 七、已知问题与风险

### 7.1 严重问题 (P0)

| 问题 | 影响 | 根因 | 修复方案 |
|------|------|------|----------|
| **IsEnabled 作用域错误** | 新建医案时诊断区不可编辑 | `IsEnabled="{Binding IsPrescriptionEnabled}"` 绑定在包含诊断区的容器上 | 将 IsEnabled 移到仅处方区控件 |
| **EnterEditMode 绑定错误** | Management 模式无法切换编辑 | XAML 绑定路径错误 | 修正为 `{Binding Commands.EnterEditModeCommand}` |
| **Remark 数据源不一致** | 显示值与保存值可能不同 | `detail.Remark` 与 `Prescription.Remark` 并存 | 统一使用 `detail.Remark` |

### 7.2 中等问题 (P1)

| 问题 | 影响 | 说明 |
|------|------|------|
| **三套数据模型并存** | 维护成本高，易出 Bug | Consultation + Prescription + DetailModel 独立管理 |
| **验证覆盖不全** | 用户可能提交无效数据 | 仅中医诊断有验证，其他字段缺失 |
| **价格计算不实时** | 总价可能不准确 | 药材变更后需手动触发价格重算 |
| **Compact 模式无 ScrollViewer** | 内容过多时可能截断 | Full 模式有 ScrollViewer，Compact 模式缺失 |

### 7.3 低等问题 (P2)

| 问题 | 影响 | 说明 |
|------|------|------|
| **跨模块 EditControl 不一致** | 用户体验不统一 | ScrollViewer/Remark 位置/审计字段显示不一致 |
| **TabIndex 不连续** | Tab 导航体验差 | Full/Compact 模式 TabIndex 范围不连续 |
| **审计字段仅 Full 模式显示** | Compact 模式缺少审计信息 | 创建时间/更新时间仅在 Full 模式显示 |

---

## 八、重构进展追踪

### 8.1 已完成的重构任务

| 任务 | 状态 | 完成日期 | 说明 |
|------|------|----------|------|
| WorkspaceState 改为 immutable record | ✅ 完成 | 2026-03-05 | 替换旧 ObservableObject 版本 |
| Coordinator 合并到 Service | ✅ 完成 | 2026-03-05 | commit ece8c5d0a |
| 旧 StateMachine 删除 | ✅ 完成 | 2026-03-05 | 替换为 WorkspaceState record |
| Composite VM 基础设施创建 | ✅ 完成 | 2026-03-05 | ConsultationEditorVM, PrescriptionEditorVM, CommandsVM |
| 弹窗组件创建 | ✅ 完成 | 2026-03-05 | FormulaImportDialog, HistoryCopyDialog, UnsavedChangesDialog |
| 诊断区布局优化 (3 行) | ✅ 完成 | 2026-04-09 | 现病史/舌诊 + 脉诊/中医诊断 |
| 处方区 HerbListControl 集成 | ✅ 完成 | 2026-04-09 | 统一药材编辑控件 |

### 8.2 待完成的重构任务

| 任务 | 优先级 | 预计工作量 | 依赖 |
|------|--------|------------|------|
| 修复 IsEnabled 作用域 | P0 | 1 小时 | 无 |
| 修复 EnterEditMode 绑定 | P0 | 30 分钟 | 无 |
| 统一 Remark 数据源 | P0 | 1 小时 | 无 |
| 补全验证错误显示 | P1 | 2 小时 | 无 |
| 统一数据流 (消除三模型) | P1 | 4-6 小时 | 验证补全 |
| 实现 Clinical/Management 模式 | P1 | 1-2 天 | 数据流统一 |
| 集成 BR-002 离开决策 | P1 | 2 小时 | 模式区分 |
| Composite VM Parent 重写 | P2 | 2-3 天 | 子 VM 稳定 |
| XAML 绑定路径更新 | P2 | 1 天 | Parent 重写 |
| 删除旧 Handler 文件 | P2 | 1 小时 | XAML 更新 |
| 全量验证 | P2 | 1 天 | 所有任务完成 |

---

## 九、关键文件索引

### 9.1 核心文件

| 文件 | 路径 | 行数 | 职责 |
|------|------|------|------|
| MedicalCaseMasterDetailViewModel | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseMasterDetailViewModel.cs` | 327 | 核心 VM |
| MedicalCaseDetailModel | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/MedicalCaseDetailModel.cs` | ~200 | 数据模型 |
| MedicalCaseEditControl | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseEditControl.xaml` | 392 | 编辑控件 |
| MedicalCaseEditControl.cs | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseEditControl.xaml.cs` | ~200 | 18 个 DP |
| MedicalCaseService | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseService.cs` | ~400 | 聚合服务 |
| MedicalCaseCommandsViewModel | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/Workspace/MedicalCaseCommandsViewModel.cs` | 455 | 命令 VM |

### 9.2 弹窗组件

| 文件 | 路径 | 职责 |
|------|------|------|
| FormulaImportDialog | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Dialogs/FormulaImportDialog.xaml` | 验方导入 |
| HistoryCopyDialog | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Dialogs/HistoryCopyDialog.xaml` | 历史处方复制 |
| UnsavedChangesDialog | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Dialogs/UnsavedChangesDialog.xaml` | 未保存修改确认 |

### 9.3 相关模块文件

| 文件 | 路径 | 职责 |
|------|------|------|
| PatientSelectionControl | `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Controls/PatientSelectionControl.xaml` | 患者选择 |
| PendingQueueManager | `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Services/PendingQueueManager.cs` | 待诊队列 |
| UnfinishedCaseHandler | `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Services/UnfinishedCaseHandler.cs` | 未完成医案处理 |

---

## 十、测试覆盖现状

### 10.1 测试项目

| 测试项目 | 测试数 | 说明 |
|----------|--------|------|
| LYBT.Tests.Server | 1185 | Server 层测试 (SQL Server + Respawn) |
| LYBT.Tests.Desktop | 760 | Desktop 层测试 (SQLite InMemory) |
| LYBT.Tests.Architecture | 76 | 架构守卫测试 |

### 10.2 MedicalCase 相关测试

| 测试类型 | 覆盖范围 | 缺失 |
|----------|----------|------|
| ViewModel 单元测试 | LoadList, LoadDetail, Save, Delete | 状态操作 (挂起/完成) |
| 集成测试 | API 调用，数据持久化 | BR-002/BR-003 校验 |
| 架构测试 | 模块依赖，命名规范 | Composite VM 模式验证 |
| UI 测试 | ❌ 缺失 | 无 XAML 绑定验证测试 |

---

## 十一、下一步行动计划

### 阶段 1: 快速修复（P0，预计 2.5 小时）

1. ✅ 修复 IsEnabled 作用域 Bug
2. ✅ 修复 EnterEditMode 绑定错误
3. ✅ 统一 Remark 数据源
4. ✅ 运行全量测试验证

### 阶段 2: 架构改进（P1，预计 2-3 天）

5. 补全验证错误显示
6. 统一数据流（消除三模型并存）
7. 实现 Clinical/Management 模式区分
8. 集成 BR-002 离开决策弹窗

### 阶段 3: 完整重构（P2，预计 5-7 天）

9. Composite VM Parent 重写
10. XAML 绑定路径更新
11. 删除旧 Handler 文件
12. 全量验证与回归测试

---

## 十二、附录

### 附录 A: 医案状态流转图

```
Created → Active → Suspended → Active → Completed
                    ↓
                  Cancelled
```

| 状态 | 说明 | 可操作 |
|------|------|--------|
| Created | 新建 | 编辑/删除 |
| Active | 进行中 | 编辑/挂起/完成 |
| Suspended | 已挂起 | 恢复/取消 |
| Completed | 已完成 | 只读/打印 |
| Cancelled | 已取消 | 只读 |

### 附录 B: 编辑模式对比

| 维度 | Clinical 模式 | Management 模式 |
|------|---------------|-----------------|
| 入口 | 待诊队列/患者选择 | 医案列表 |
| 默认状态 | Editing | ReadOnly |
| 底部按钮 | [挂起] [打印] [完成] | [编辑] [打印] |
| 离开行为 | BR-002 (挂起/完成/取消) | BR-002 (保存/放弃/取消) |
| 数据校验 | BR-003 完成前校验 | 无强制校验 |

### 附录 C: 术语表

| 术语 | 含义 | 备注 |
|------|------|------|
| 医案 (MedicalCase) | 一次完整的诊疗记录 | DDD 聚合根 |
| 诊断 (Consultation) | 中医四诊信息 | 医案内部实体 |
| 处方 (Prescription) | 药材处方信息 | 医案内部实体 |
| 验方 (Formula) | 经验方模板 | 可复用的处方模板 |
| 待诊队列 (Pending Queue) | 等待看诊的患者列表 | 由挂号系统生成 |
| BR-001 | 医案碰撞处理规则 | 检查 Active/Suspended 医案 |
| BR-002 | 离开决策规则 | 未保存变更时的弹窗逻辑 |
| BR-003 | 完成校验规则 | 诊断+处方必填项校验 |

---

*文档版本: v1.0 | 创建日期: 2026-04-10 | 状态: 审计完成，待优化*
