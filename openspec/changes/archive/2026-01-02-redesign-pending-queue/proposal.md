# OpenSpec Proposal: redesign-pending-queue

## 元数据

| 字段 | 值 |
|------|-----|
| **Change ID** | redesign-pending-queue |
| **标题** | 待看诊队列全栈重新设计 |
| **状态** | Draft |
| **创建时间** | 2026-01-02 |
| **作者** | Claude Code |
| **影响范围** | Desktop/MedicalCase, Server/MedicalCase, Shared/Models |

---

## 1. 问题陈述

### 1.1 当前问题

待看诊队列（PendingQueue）存在以下设计缺陷：

| 问题 | 详情 |
|------|------|
| **状态判定硬编码** | Server端`GetPendingCasesAsync`将所有医案硬编码为`PendingCaseType.Suspended`，未实现真正的状态判定 |
| **切换逻辑复杂** | 三选项弹窗（编辑模式切换）+ 四选项弹窗（暂存患者），用户体验繁琐 |
| **信息缺失** | 未显示序号，患者识别困难 |
| **扩展性差** | 未预留挂号状态的接入能力 |
| **Handler职责不清** | `WorkspacePendingQueueHandler`混合了队列管理和导航逻辑 |

### 1.2 业务背景

- **当前阶段**：前台模块未开发，队列仅包含"暂存"和"正在看诊"两种状态
- **未来扩展**：需预留"挂号等待"状态，为前台模块接入做准备
- **多医生模式**：每个医生只能看到自己的待诊队列

---

## 2. 解决方案

### 2.1 设计目标

1. **正确的状态判定**：Server端根据医案实际状态返回正确的`PendingCaseType`
2. **简化切换逻辑**：编辑模式自动暂存，暂存患者双选项弹窗
3. **增加序号显示**：方便医生快速识别患者
4. **后台轮询**：集成`ApplicationTickService`实现队列自动刷新
5. **架构清晰**：分离队列管理和导航逻辑

### 2.2 状态定义

```csharp
public enum PendingCaseType
{
    /// <summary>已挂号等候（预留，当前阶段不实现）</summary>
    Registered = 0,
    
    /// <summary>正在看诊（当前医生的Active医案）</summary>
    InProgress = 1,
    
    /// <summary>暂存草稿（Draft状态的医案）</summary>
    Suspended = 2
}
```

**状态判定规则**：

| 医案状态 | PendingCaseType | 判定条件 |
|----------|-----------------|----------|
| Active | InProgress | MedicalCase.Status == Active |
| Draft | Suspended | MedicalCase.Status == Draft |
| Registered | Registered | 预留，当前返回空 |

### 2.3 切换逻辑简化

**变更前**：

```
编辑模式切换 → 三选项弹窗（暂存/取消/继续）
选择暂存患者 → 四选项弹窗（继续/关闭新建/仅关闭/取消）
```

**变更后**：

```
编辑模式切换 → 自动暂存，无需确认
选择暂存患者 → 双选项弹窗（继续看诊/新建医案）
```

| 场景 | 新行为 | 用户体验 |
|------|--------|----------|
| 编辑中点击其他患者 | 自动暂存当前医案 → 切换 | 无干扰，流畅切换 |
| 点击暂存患者 | 弹窗询问"继续/新建" | 简洁明确的选择 |
| 点击正在看诊患者 | 忽略（已是当前） | 无反应 |

### 2.4 信息显示增强

**列定义**：

| 列名 | 宽度 | 说明 |
|------|------|------|
| 序号 | 40px | 显示1, 2, 3...顺序号 |
| 姓名 | 70px | 患者姓名 |
| 电话 | 120px | 脱敏电话号码 |
| 状态 | 70px | 状态标签（颜色区分） |

**状态颜色**：

| 状态 | 颜色 | 含义 |
|------|------|------|
| InProgress | 绿色 | 正在看诊 |
| Suspended | 橙色 | 暂存草稿 |
| Registered | 灰色 | 等待就诊（预留） |

### 2.5 后台轮询机制

集成`IApplicationTickService`实现队列自动刷新：

```csharp
// 每30秒刷新一次待诊队列
private const int RefreshIntervalTicks = 30;

private void OnTick(ApplicationTickEventArgs e)
{
    if (e.TickCount % RefreshIntervalTicks == 0)
    {
        _ = RefreshPendingQueueAsync();
    }
}
```

---

## 3. 技术设计

### 3.1 控件设计模式

**设计决策**：采用"外部ViewModel驱动 + 轻度封装轮询"模式

**设计依据**：

| 因素 | 评估结果 | 设计影响 |
|------|----------|----------|
| 状态复杂度 | 简单（列表+选中） | 无需内部ViewModel |
| 与外部协调 | 需要（导航、暂存） | 外部ViewModel处理业务决策 |
| 交互频率 | 低（点击选择） | 无需内部状态管理 |
| 轮询刷新 | 通用行为 | 封装到控件内部 |

**控件职责划分**：

```
┌─────────────────────────────────────────────────────────────┐
│              PendingQueueControl（轻度封装）                  │
├─────────────────────────────────────────────────────────────┤
│  内部封装：                                                  │
│  - 订阅IApplicationTickService                              │
│  - 自动触发刷新回调                                          │
│  - 管理刷新状态（IsRefreshing）                              │
│                                                              │
│  DependencyProperty（外部绑定）：                            │
│  - DoctorId：医生ID（用于API调用）                           │
│  - PendingQueue：队列数据源                                  │
│  - SelectedItem：选中项                                      │
│  - RefreshCallback：刷新数据回调                             │
│  - AutoRefreshInterval：刷新间隔（默认30秒）                 │
│                                                              │
│  事件输出：                                                  │
│  - PatientSelected：患者选择事件                             │
│  - RefreshRequested：手动刷新请求                            │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│              外部ViewModel                                   │
├─────────────────────────────────────────────────────────────┤
│  - 提供RefreshCallback实现（调用API加载数据）               │
│  - 处理PatientSelected事件（导航、暂存确认等业务逻辑）      │
│  - 管理PendingQueue数据源                                   │
└─────────────────────────────────────────────────────────────┘
```

**与HerbListControl设计对比**：

| 特性 | HerbListControl | PendingQueueControl |
|------|-----------------|---------------------|
| 模式 | 内部ViewModel | 外部ViewModel + 轮询封装 |
| 原因 | 复杂编辑逻辑（增删改验证） | 简单只读列表 |
| 状态管理 | 控件内部完全自治 | 外部ViewModel管理数据 |
| 业务逻辑 | 封装在控件内 | 外部处理 |

### 3.2 架构变更

```
┌─────────────────────────────────────────────────────────────┐
│                        Desktop端                             │
├─────────────────────────────────────────────────────────────┤
│  PendingQueueControl.xaml                                   │
│    ├─ 新增序号列                                             │
│    ├─ 状态标签样式优化                                        │
│    └─ 内部集成ApplicationTickService轮询                     │
│                                                              │
│  WorkspacePendingQueueHandler (简化)                         │
│    └─ 仅保留导航和切换确认逻辑                                │
├─────────────────────────────────────────────────────────────┤
│                        Server端                              │
├─────────────────────────────────────────────────────────────┤
│  MedicalCasesController                                      │
│    └─ GetPendingCasesAsync (改进查询逻辑)                     │
│                                                              │
│  MedicalCaseRepository                                       │
│    └─ 实现正确的状态判定                                      │
├─────────────────────────────────────────────────────────────┤
│                        Shared                                │
├─────────────────────────────────────────────────────────────┤
│  PendingMedicalCaseDto                                       │
│    └─ 新增 QueueNumber (序号)                                │
└─────────────────────────────────────────────────────────────┘
```

### 3.3 API变更

**GET /api/v1/medicalcases/pending**

请求参数不变：
- `doctorId` (必需): 医生ID

响应变更：

```json
[
  {
    "patientId": "guid",
    "patientName": "张三",
    "phoneNumber": "13800138000",
    "phoneMasked": "138****8000",
    "type": 1,          // 1=InProgress, 2=Suspended
    "medicalCaseId": "guid",
    "createdAt": "2026-01-02T09:00:00",
    "queueNumber": 1    // 新增：序号
  }
]
```

### 3.4 数据模型变更

**PendingMedicalCaseDto**：

```csharp
public class PendingMedicalCaseDto
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PhoneMasked { get; set; } = string.Empty;
    public PendingCaseType Type { get; set; }
    public Guid? MedicalCaseId { get; set; }
    public DateTime CreatedAt { get; set; }
    
    /// <summary>队列序号（新增）</summary>
    public int QueueNumber { get; set; }
}
```

---

## 4. 实施计划

### Phase 1: Server端状态判定修复

- 修改`MedicalCaseRepository.GetPendingCasesAsync`
- 实现正确的状态判定逻辑（Active→InProgress, Draft→Suspended）
- 添加QueueNumber计算

### Phase 2: Desktop端控件重构

- PendingQueueControl新增序号列
- 优化状态标签样式
- 控件内部集成`IApplicationTickService`轮询
- 新增`RefreshCallback`、`AutoRefreshInterval`属性
- 新增`PatientSelected`事件（替代Command模式）

### Phase 3: 切换逻辑简化

- 重构`WorkspacePendingQueueHandler`
- 实现自动暂存机制
- 简化暂存患者弹窗为双选项（继续/新建）
- 删除三选项弹窗逻辑

### Phase 4: 集成与测试

- 更新WorkspaceViewModel适配新控件接口
- 单元测试覆盖
- 集成测试
- 更新API文档

---

## 5. 关联变更点（备注）

本提案简化了待诊队列的切换逻辑（四选项→双选项）。以下关联模块使用相同的"未完成医案四选项弹窗"逻辑，建议后续统一简化：

| 模块 | 文件 | 当前行为 | 建议变更 |
|------|------|----------|----------|
| 患者列表选择 | `PatientSelectionViewModel` | 四选项弹窗 | 统一为双选项 |
| 医案启动协调器 | `MedicalCaseStartCoordinator` | 支持四种结果 | 移除CloseOnly |
| 未完成医案处理器 | `UnfinishedCaseHandler` | CloseOnly方法 | 保留但不推荐 |
| 对话框枚举 | `UnfinishedCaseChoice` | 四个枚举值 | 可简化为三个 |

**不在本提案范围**：上述关联变更需要单独的OpenSpec提案处理，以确保：
1. 患者列表模块的用户体验与待诊队列一致
2. 代码维护的统一性
3. 测试覆盖的完整性

**建议后续提案**：`unify-unfinished-case-dialog` - 统一未完成医案处理弹窗逻辑

---

## 6. 风险评估

| 风险 | 等级 | 缓解措施 |
|------|------|----------|
| 状态判定逻辑变更影响现有功能 | 中 | 充分测试，确保向后兼容 |
| 自动暂存可能导致意外数据保存 | 低 | 暂存是无损操作，可随时恢复 |
| 后台轮询增加服务器压力 | 低 | 30秒间隔，影响可控 |
| 待诊队列与患者列表行为不一致 | 中 | 本提案备注关联变更点，后续统一处理 |

---

## 7. 成功标准

1. Server端正确返回InProgress/Suspended状态
2. 切换患者时无需手动确认暂存
3. 队列显示序号，便于识别
4. 队列每30秒自动刷新
5. 所有测试通过

---

## 8. 相关资源

- 当前实现：`src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/PendingQueueControl.xaml`
- Handler：`src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/Components/WorkspacePendingQueueHandler.cs`
- Server端：`src/Server/Modules/LYBT.Module.MedicalCase/Repositories/MedicalCaseRepository.cs`
- 轮询服务：`src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/ApplicationTickService.cs`
