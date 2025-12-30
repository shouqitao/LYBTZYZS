# Design: 医案界面UI优化

**Change ID**: `optimize-medicalcase-ui`

---

## 1. 架构概述

本次优化不涉及架构变更，仅对现有UI布局和客户端状态逻辑进行优化。

---

## 2. UI布局设计

### 2.1 当前布局

```
+------------------+----------------------------------------+
|                  |  Header: 标题 + 患者信息(重复) + 按钮    |
|  PatientInfo     +----------------------------------------+
|  CardControl     |                                        |
|                  |  诊断区 (35%)                           |
|  +------------+  |  +----------------------------------+  |
|  | 患者信息   |  |  | 现病史 (MinHeight=80, 滚动条)     |  |
|  +------------+  |  | 舌诊+脉诊 (Height=100, 滚动条)    |  |
|                  |  | 中医诊断 (MinHeight=80, 滚动条)   |  |
|  PendingQueue    |  +----------------------------------+  |
|  Control         |                                        |
|                  |  处方区 (65%)                           |
|                  |  +----------------------------------+  |
|                  |  | 处方编辑面板                       |  |
|                  |  +----------------------------------+  |
+------------------+----------------------------------------+
      25%                          75%
```

### 2.2 优化后布局

```
+------------------+----------------------------------------+
|                  |  Header: 标题 + 操作按钮               |
|  PatientInfo     +----------------------------------------+
|  CardControl     |                                        |
|                  |  诊断区 (35%) - 2x2无滚动条网格         |
|  +------------+  |  +----------------+----------------+   |
|  | 患者信息   |  |  | 现病史         | 中医诊断*       |   |
|  | (完整)     |  |  | (Height=*)    | (Height=*)     |   |
|  +------------+  |  +----------------+----------------+   |
|                  |  | 舌诊           | 脉诊            |   |
|  PendingQueue    |  | (Height=Auto) | (Height=Auto)  |   |
|  Control         |  +----------------+----------------+   |
|  (三种状态)      |  | 处方选项                          |   |
|                  |  +----------------------------------+  |
|                  |                                        |
|                  |  处方区 (65%)                           |
|                  |  +----------------------------------+  |
|                  |  | 处方编辑面板                       |  |
|                  |  +----------------------------------+  |
+------------------+----------------------------------------+
      25%                          75%
```

### 2.3 布局变更要点

| 区域 | 变更前 | 变更后 |
|------|--------|--------|
| Header | 显示患者姓名+信息 | 仅显示标题+操作按钮 |
| 诊断区布局 | 垂直堆叠4行 | 2x2网格 |
| 诊断字段高度 | 固定MinHeight+滚动条 | 自适应填充无滚动条 |
| 待诊队列状态 | 硬编码"暂存" | 三种状态颜色区分 |

---

## 3. 待诊队列状态设计

### 3.1 状态枚举定义

```csharp
namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 待诊队列项类型
    /// </summary>
    public enum PendingCaseType
    {
        /// <summary>等待（已挂号，未开始看诊）</summary>
        Waiting = 0,

        /// <summary>看诊中（正在进行诊疗）</summary>
        InProgress = 1,

        /// <summary>挂起（暂停看诊，稍后继续）</summary>
        Suspended = 2
    }
}
```

### 3.2 状态判定逻辑

```
MedicalCase.Status = Draft 且 无Consultation记录
    → PendingCaseType.Waiting（刚挂号，未开始）

MedicalCase.Status = Draft 且 有Consultation记录
    → PendingCaseType.Suspended（已开始但暂停）

MedicalCase.Status = Active
    → PendingCaseType.InProgress（正在看诊）
```

### 3.3 状态流转图

```
[新建医案]
     ↓
[Waiting] ←→ [InProgress] → [Completed]
     ↓           ↑
     └→ [Suspended] ─┘
```

### 3.4 UI视觉规范

| 状态 | 背景色 | 文本色 | 显示文本 |
|------|--------|--------|----------|
| Waiting | #E0E0E0 (灰) | #666666 | 等待 |
| InProgress | #4CAF50 (绿) | #FFFFFF | 看诊中 |
| Suspended | #FF9800 (橙) | #FFFFFF | 挂起 |

---

## 4. 数据流设计

### 4.1 待诊队列查询流程

```
MedicalCaseRepository.GetPendingCasesAsync()
    ↓
1. 查询所有Draft/Active状态的MedicalCase
2. 左连接Consultation表判断是否有记录
3. 根据Status+HasConsultation计算PendingCaseType
4. 返回List<PendingMedicalCaseDto>
    ↓
MedicalCaseWorkspaceViewModel.PendingQueue
    ↓
PendingQueueControl (绑定显示)
```

### 4.2 DTO变更

```csharp
public class PendingMedicalCaseDto
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PhoneMasked { get; set; } = string.Empty;

    // 变更: string → PendingCaseType
    public PendingCaseType Type { get; set; }

    // 新增: 显示用属性
    public string TypeDisplay => Type switch
    {
        PendingCaseType.Waiting => "等待",
        PendingCaseType.InProgress => "看诊中",
        PendingCaseType.Suspended => "挂起",
        _ => "未知"
    };

    public Guid? MedicalCaseId { get; set; }
}
```

---

## 5. 兼容性考虑

### 5.1 向后兼容

- 无后端API变更，所有状态判定在客户端完成
- 现有PendingMedicalCaseDto的使用方不受影响（TypeDisplay提供向后兼容显示）

### 5.2 影响范围

| 模块 | 影响 |
|------|------|
| LYBT.Desktop.MedicalCase | 布局变更、Repository逻辑变更 |
| LYBT.Desktop.Infrastructure | PendingQueueControl UI变更 |
| LYBT.Shared.Models | 新增枚举、DTO变更 |

---

## 6. 测试策略

### 6.1 手动测试

- [ ] 界面布局在1920x1080分辨率正常显示
- [ ] 界面布局在1366x768分辨率正常显示（无滚动条）
- [ ] 待诊队列显示三种状态颜色
- [ ] 看诊流程功能完整（选患者→诊断→处方→完成）

### 6.2 编译验证

- [ ] `dotnet build LYBT.All.sln -c Release --no-restore` 成功
