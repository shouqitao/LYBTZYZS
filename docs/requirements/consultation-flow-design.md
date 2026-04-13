# 医生接诊 + 前台挂号 完整设计文档

> **版本**: v1.0
> **创建日期**: 2026-04-12
> **状态**: 待评审
> **范围**: 医生角色 (Clinical) + 前台角色 (Receptionist) 的完整业务流程设计
> **关联文档**:
> - `consultation-flow-architecture-alignment.md` (架构对齐分析)
> - `deviation-correction-plan.md` (偏差修正计划)
> - `medical-cases.md` (医案管理 PRD)
> - `registration.md` (挂号管理 PRD)

---

## 一、系统架构概述

### 1.1 角色职责定义

| 角色 | 主页 | 工作台 | 主要业务 |
|------|------|--------|----------|
| **医生 (Clinical)** | ClinicalHomeView: "开始接诊"入口 + 快捷导航 | PatientSelectionView → MedicalCaseWorkspaceView | 选择患者 → 编写医案 (诊断+处方) |
| **前台 (Receptionist)** | ReceptionistHomeView: 统计 + 快捷入口 | ReceptionistWorkspaceView: 挂号创建 + 队列管理 | 患者挂号 + 登记 + 队列管理 |

### 1.2 核心数据模型关系

```
Registration (挂号记录)                MedicalCase (医案记录)
┌─────────────────────────────┐       ┌─────────────────────────────┐
│ RegistrationId (PK)         │       │ MedicalCaseId (PK)          │
│ PatientId (FK)              │  ──┐  │ PatientId (FK)              │
│ DoctorId (FK)               │    │  │ RegistrationId (FK, nullable)│
│ Status: Waiting/InProgress/ │    └─→│ Status: Draft/Active/       │
│   Completed/Cancelled       │       │   Completed/Cancelled        │
│ Source: Receptionist/Doctor │       │                              │
│ RegistrationTime            │       │ Consultation + Prescription │
└─────────────────────────────┘       └─────────────────────────────┘

关系: 1条 Registration → 0或1条 MedicalCase
     1条 MedicalCase → 0或1条 Registration
```

### 1.3 两条数据流统一设计

**现状问题**: 当前医生待诊列表 (PendingQueue) 查询的是医案状态，而前台挂号创建的是挂号记录，两条数据流互不相通。

**统一方案**: 待诊列表合并两个数据源：

```
医生待诊列表 = {
  Registration 记录: Status = Waiting (前台挂号 或 医生直接创建)
  MedicalCase 记录: Status = Active/Suspended (已创建但未完成的医案)
}

展示规则:
- 优先显示 Registration Waiting 记录 (新挂号)
- 其次显示 MedicalCase Active/Suspended 记录 (进行中医案)
- 同一患者同时有两条记录时，只显示 Registration (避免重复)
```

---

## 二、完整业务流程

### 2.1 端到端流程图

```
┌─────────────────────────────────────────────────────────────────┐
│                        前台 (Receptionist)                       │
│                                                                 │
│  ReceptionistHomeView (主页)                                     │
│  ┌─────────────┐ ┌─────────────┐ ┌──────────────┐              │
│  │  新建患者    │ │  新建挂号    │ │  今日统计     │              │
│  │  PatientMgmt│ │  RegCreate  │ │  (真实数据)   │              │
│  └─────────────┘ └─────────────┘ └──────────────┘              │
│                                                                 │
│  新建挂号流程:                                                   │
│  搜索/创建患者 → 选择医生 → 创建 Registration (Waiting)         │
│                      │                                          │
│                      ▼                                          │
│              发布 RegistrationCreatedEvent                      │
│              (通过 EventAggregator 通知全系统)                    │
└──────────────────────────┬──────────────────────────────────────┘
                           │ EventAggregator
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                        医生 (Clinical)                           │
│                                                                 │
│  ClinicalHomeView (主页)                                         │
│  ┌──────────────────────────────────────────┐                   │
│  │          "开始接诊" 主卡片                 │                   │
│  └──────────────────────────────────────────┘                   │
│              │                                                  │
│              ▼                                                  │
│  PatientSelectionView (患者选择)                                 │
│  ┌───────────────┬──────────────────┬──────────────┐           │
│  │ 待诊队列       │  患者搜索/选择    │  患者信息    │           │
│  │ (合并数据源)   │                  │              │           │
│  │               │                  │              │           │
│  │ 来源A:        │  来源B:          │              │           │
│  │ Registration  │  MedicalCase     │              │           │
│  │ Waiting记录   │  Active医案      │              │           │
│  │ (前台挂号)    │  (医生直接创建)   │              │           │
│  └───────────────┴──────────────────┴──────────────┘           │
│              │                                                  │
│              ▼                                                  │
│  点击"开始看诊" → 检查 BR-001 碰撞                                │
│  → 有 Waiting Registration → StartVisit(registrationId)        │
│  → 无 Registration → 静默创建 Registration (Source=Doctor)     │
│  → 创建/打开 MedicalCase                                       │
│              │                                                  │
│              ▼                                                  │
│  MedicalCaseWorkspaceView (医案工作台)                           │
│  ┌─────────────────┬──────────────────────────────┐            │
│  │ Consultation    │ Prescription                  │            │
│  │ 诊断填写         │ 处方开具                       │            │
│  └─────────────────┴──────────────────────────────┘            │
│  [挂起] [打印] [完成看诊]                                       │
│                                                                 │
│  完成看诊后:                                                     │
│  MedicalCase → Completed                                       │
│  Registration → Completed (如有关联)                             │
│  发布 MedicalCaseCompletedEvent                                 │
│  通知前台统计更新                                                │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 两条接诊路径详细说明

#### 路径 A: 医生直接模式 (Direct Consultation)

```
触发: ClinicalHomeView → "开始接诊" → PatientSelectionView → 选择患者 → "开始看诊"

步骤:
1. 用户选择患者 (通过搜索或待诊队列)
2. 调用 StartMedicalCaseAsync(patientId)
3. 检查患者是否已有 Waiting 状态的 Registration
   a. 有 → 调用 StartVisitAsync(registrationId)，将 Registration 转为 InProgress
   b. 无 → 静默创建 Registration (Source=Doctor, Status=InProgress)
4. 检查患者是否已有 Active/Suspended 医案 (BR-001 碰撞检测)
   a. 有 Active → 弹窗: 继续看诊 / 新建医案 / 取消
   b. 有 Suspended → 弹窗: 继续看诊 / 新建医案 / 取消
   c. 无 → 创建新医案 (Draft → Active)
5. 导航到 MedicalCaseWorkspaceView (Clinical 模式, Editing 状态)

数据流:
  Patient → Registration (Source=Doctor, InProgress) → MedicalCase (Active)
```

#### 路径 B: 挂号队列模式 (Registration Queue)

```
触发: ReceptionistHomeView → "新建挂号" → 创建 Registration (Waiting)

步骤:
1. 前台搜索/创建患者
2. 选择医生
3. 创建 Registration (Source=Receptionist, Status=Waiting)
4. 发布 RegistrationCreatedEvent (通知医生端刷新待诊列表)
5. 医生端待诊列表收到通知 → 自动刷新 → 显示新挂号患者
6. 医生点击"接诊" → 调用 StartVisitAsync(registrationId)
7. StartVisitAsync 内部:
   a. 更新 Registration 状态为 InProgress
   b. 创建 MedicalCase (Draft → Active)
8. 导航到 MedicalCaseWorkspaceView (Clinical 模式, Editing 状态)

数据流:
  Patient + Doctor → Registration (Waiting) → StartVisit → 
    Registration (InProgress) + MedicalCase (Active)
```

### 2.3 状态机定义

#### Registration 状态机

```
               ┌──────────┐
               │  Waiting  │ ← 前台挂号创建 / 医生直接接诊创建
               └────┬─────┘
                    │ StartVisit (医生接诊)
                    ▼
            ┌───────────────┐
            │  InProgress   │ ← 医生正在看诊
            └───────┬───────┘
                    │ CompleteVisit (完成看诊)
                    ▼
            ┌───────────────┐
            │   Completed   │ ← 终态
            └───────────────┘

         ┌──────────┐     Cancel
         │  Waiting  │ ───────────► ┌───────────┐
         └──────────┘              │ Cancelled │ ← 终态
                                    └───────────┘

状态说明:
- Waiting: 已挂号但未接诊，显示在医生待诊列表
- InProgress: 医生正在看诊，不显示在待诊列表
- Completed: 看诊完成，归档
- Cancelled: 取消挂号 (仅 Waiting 状态可取消)
```

#### MedicalCase 状态机

```
                ┌───────┐
                │ Draft │ ← 创建时 (短暂状态)
                └───┬───┘
                    │ Auto-activate
                    ▼
              ┌──────────┐
              │  Active  │ ← 医生正在编辑
              └────┬─────┘
                   │
          ┌────────┼─────────┐
          │        │         │
          ▼        ▼         ▼
    ┌─────────┐ ┌────────┐ ┌──────────┐
    │Suspended│ │Completed│ │Cancelled │
    └────┬────┘ └────────┘ └──────────┘
         │
         │ Resume
         ▼
    ┌──────────┐
    │  Active  │

状态说明:
- Draft: 刚创建，尚未激活 (瞬间状态)
- Active: 医生正在编辑，允许修改
- Suspended: 挂起 (BR-001 碰撞时暂存)，可恢复
- Completed: 完成看诊，只读
- Cancelled: 取消，只读
```

---

## 三、UI 设计规格

### 3.1 医生主页 (ClinicalHomeView)

**布局**:
```
┌───────────────────────────────────────────────┐
│  凌隐宝堂 - 医生工作台                    [设置] │
├───────────────────────────────────────────────┤
│                                               │
│  欢迎，[医生姓名]                               │
│                                               │
│  ┌─────────────────────────────────────────┐  │
│  │                                         │  │
│  │           🩺 开始接诊                     │  │  ← 主卡片 (大尺寸，突出显示)
│  │        选择患者开始新的看诊                │  │
│  │                                         │  │
│  └─────────────────────────────────────────┘  │
│                                               │
│  ┌───────────────┐  ┌───────────────┐         │
│  │   患者管理     │  │   医案查询     │         │  ← 快捷入口
│  └───────────────┘  └───────────────┘         │
│  ┌───────────────┐  ┌───────────────┐         │
│  │   我的验方     │  │   个人资料     │         │
│  └───────────────┘  └───────────────┘         │
│                                               │
└───────────────────────────────────────────────┘
```

**功能卡片**:
| 卡片 | 导航目标 | 优先级 |
|------|----------|--------|
| 开始接诊 | PatientSelectionView | 主入口 |
| 患者管理 | PatientManagementView | 快捷 |
| 医案查询 | MedicalCaseManagementView | 快捷 |
| 我的验方 | FormulaManagementView | 快捷 |
| 个人资料 | AccountSettingsView | 快捷 |

**已移除的卡片**: 今日统计 (无后端接口，显示 0 比不显示更糟)、药材库、挂号队列、数据同步

### 3.2 患者选择页 (PatientSelectionView)

**布局**: 三栏式
```
┌─────────────────────────────────────────────────────────────────┐
│  患者选择                                    [返回首页]          │
├──────────┬──────────────────────────┬───────────────────────────┤
│ 读卡器   │ 患者搜索                  │ 患者信息                   │
│ [状态]   │ ┌──────────────────────┐ │ (选中后显示)               │
│          │ │  搜索框 [搜索]        │ │ ┌─────────────────────┐  │
│ ┌──────┐ │ │                      │ │ │ 姓名: 张三          │  │
│ │待诊队│ │ │ 搜索结果列表          │ │ │ 性别: 男            │  │
│ │列    │ │ │ ┌──────────────────┐ │ │ │ 年龄: 45岁          │  │
│ │      │ │ │ │ 李四 | 男 | 32岁  │ │ │ │ 电话: 138****1234  │  │
│ │ [刷新]│ │ │ │ 王五 | 女 | 28岁  │ │ │ │ 过敏史: 无          │  │
│ │      │ │ │ │ ...              │ │ │ │ 既往史: 高血压       │  │
│ │ 张三  │ │ │ └──────────────────┘ │ │ └─────────────────────┘  │
│ │ 李四  │ │                      │ │                           │
│ │ 王五  │ │ [加载更多]            │ │                           │
│ │      │ │                      │ │                           │
│ └──────┘ │                      │ │                           │
├──────────┴──────────────────────┴───────────────────────────┤
│  状态信息...                              [开始看诊]          │
└─────────────────────────────────────────────────────────────┘
```

**待诊队列数据源**: 合并 Registration Waiting + MedicalCase Active/Suspended
- 优先显示 Registration Waiting (新挂号)
- 其次显示 MedicalCase Active/Suspended (已创建医案)
- 同一患者去重

**读卡器区域**: 条件显示
- 检测到读卡器硬件 → 显示
- 未检测到 → 隐藏读卡器区域，待诊队列扩展至全宽

### 3.3 医案工作台 (MedicalCaseWorkspaceView)

**布局**: 双栏式
```
┌─────────────────────────────────────────────────────────────────┐
│  医案工作台 - 患者: 张三                     [挂起] [打印] [完成] │
├─────────────────────────────┬───────────────────────────────────┤
│ 诊断                        │ 处方                              │
│ ┌─────────────────────────┐ │ ┌───────────────────────────────┐ │
│ │ 四诊信息                  │ │ 中药处方                       │ │
│ │ 望诊:                    │ │ ┌───────────────────────────┐ │ │
│ │ 闻诊:                    │ │ │ 药材 | 剂量 | 用法 | 备注   │ │ │
│ │ 问诊:                    │ │ │ 黄芪 | 15g | 水煎服 |      │ │ │
│ │ 切诊:                    │ │ │ 党参 | 12g | 水煎服 |      │ │ │
│ │                         │ │ │ 白术 | 10g | 水煎服 |      │ │ │
│ │ 辨证结果:                │ │ └───────────────────────────┘ │ │
│ │ [输入框]                 │ │                               │ │
│ │                         │ │ [添加药材] [删除] [清空]       │ │
│ │ 诊断结论:                │ │                               │ │
│ │ [输入框]                 │ │ 煎法: [选择]                   │ │
│ │                         │ │ 服法: [选择]                   │ │
│ │                         │ │ 剂数: [输入]                   │ │
│ └─────────────────────────┘ │ └───────────────────────────────┘ │
└─────────────────────────────┴───────────────────────────────────┘
```

### 3.4 前台主页 (ReceptionistHomeView)

**布局**:
```
┌───────────────────────────────────────────────────────┐
│  凌隐宝堂 - 前台工作台                            [设置] │
├───────────────────────────────────────────────────────┤
│                                                       │
│  ┌────────────────────┐  ┌─────────────────────────┐  │
│  │ 搜索患者... [搜索]  │  │ 今日统计                 │  │
│  └────────────────────┘  │ 今日挂号: [数字]         │  │
│                          │ 待接诊: [数字]           │  │
│                          │ 已完成: [数字]           │  │
│                          └─────────────────────────┘  │
│                                                       │
│  ┌─────────────────────┐  ┌─────────────────────┐    │
│  │                     │  │                     │    │
│  │  📋 新建挂号         │  │  👤 新建患者         │    │
│  │  为患者创建挂号记录   │  │  录入新患者信息      │    │
│  │                     │  │                     │    │
│  └─────────────────────┘  └─────────────────────┘    │
│                                                       │
│  ┌───────────────┐  ┌───────────────┐                 │
│  │   挂号队列     │  │   患者管理     │                 │
│  └───────────────┘  └───────────────┘                 │
│                                                       │
└───────────────────────────────────────────────────────┘
```

### 3.5 前台工作台 (ReceptionistWorkspaceView) - 新增

**布局**: 一站式工作台
```
┌─────────────────────────────────────────────────────────────────┐
│  前台工作台                                   [刷新] [设置]      │
├─────────────────────────────────────────────────────────────────┤
│  选项卡: [挂号创建] | [挂号队列] | [患者管理]                     │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  [挂号创建 Tab]                                                  │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │ 步骤1: 选择患者                                              ││
│  │ 搜索患者: [__________] [搜索] 或 [新建患者]                   ││
│  │ 搜索结果: [列表]                                             ││
│  │                                                              ││
│  │ 步骤2: 选择医生                                              ││
│  │ 医生: [下拉选择]                                             ││
│  │                                                              ││
│  │ 步骤3: 确认挂号                                              ││
│  │ 患者: [张三] | 医生: [李医生]                                 ││
│  │ [确认挂号] [取消]                                            ││
│  └─────────────────────────────────────────────────────────────┘│
│                                                                 │
│  [挂号队列 Tab]                                                  │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │ 日期: [今天 ▼]  状态: [全部 ▼]  [刷新]                       ││
│  │ ┌─────────────────────────────────────────────────────────┐││
│  │ │ 时间  | 患者 | 医生 | 状态    | 操作                     │││
│  │ │ 09:00 | 张三 | 李医 | 待接诊   | [取消]                   │││
│  │ │ 09:15 | 王五 | 赵医 | 进行中   | [查看]                   │││
│  │ │ 09:30 | 李四 | 李医 | 已完成   | [打印]                   │││
│  │ └─────────────────────────────────────────────────────────┘││
│  └─────────────────────────────────────────────────────────────┘│
│                                                                 │
│  [患者管理 Tab]                                                  │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │ 搜索: [__________] [搜索]  [新建患者]                        ││
│  │ ┌─────────────────────────────────────────────────────────┐││
│  │ │ 姓名 | 性别 | 年龄 | 电话      | 操作                    │││
│  │ │ 张三  | 男  | 45  | 138****1234 | [编辑] [挂号]          │││
│  │ │ 李四  | 女  | 28  | 139****5678 | [编辑] [挂号]          │││
│  │ └─────────────────────────────────────────────────────────┘││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
```

---

## 四、事件通信设计

### 4.1 EventAggregator 事件定义

| 事件名称 | 发布方 | 订阅方 | 触发时机 | 携带数据 |
|----------|--------|--------|----------|----------|
| `RegistrationCreatedEvent` | ReceptionistHomeVM / Workspace | 所有端 | 新建挂号成功 | RegistrationId, PatientId, DoctorId |
| `RegistrationStatusChangedEvent` | RegistrationService | 所有端 | 挂号状态变更 | RegistrationId, OldStatus, NewStatus |
| `MedicalCaseCompletedEvent` | MedicalCaseWorkspaceVM | 所有端 | 完成看诊 | MedicalCaseId, PatientId, DoctorId |
| `MedicalCaseSuspendedEvent` | MedicalCaseWorkspaceVM | 所有端 | 挂起医案 | MedicalCaseId, PatientId, DoctorId |

### 4.2 事件处理流程

```
前台新建挂号成功
    │
    ├─→ 发布 RegistrationCreatedEvent
    │       │
    │       ├─→ 医生端 PendingQueueViewModel 订阅
    │       │       └─→ 触发 RefreshQueueAsync()
    │       │
    │       ├─→ 前台主页 ReceptionistHomeViewModel 订阅
    │       │       └─→ 刷新今日统计
    │       │
    │       └─→ 挂号队列 RegistrationListViewModel 订阅
    │               └─→ 刷新队列列表
    │
    └─→ UI 更新 (本地)

医生完成看诊
    │
    ├─→ 发布 MedicalCaseCompletedEvent
    │       │
    │       ├─→ 前台主页订阅
    │       │       └─→ 刷新今日统计 (已完成+1)
    │       │
    │       └─→ 挂号队列订阅
    │               └─→ 更新状态显示
    │
    └─→ 导航回 ClinicalHomeView
```

---

## 五、后端 API 需求

### 5.1 新增/修改 API

| API | 方法 | 用途 | 优先级 |
|-----|------|------|--------|
| `GET /api/registrations/pending?doctorId={id}` | GET | 获取指定医生名下所有 Waiting 状态的挂号 | P0 |
| `POST /api/registrations/doctor-direct` | POST | 医生直接接诊时静默创建 Registration | P0 |
| `GET /api/registrations/patient/{patientId}/waiting` | GET | 检查患者是否有 Waiting 状态的挂号 | P0 |
| `GET /api/statistics/today?doctorId={id}` | GET | 获取医生今日统计数据 | P1 |
| `GET /api/statistics/receptionist/today` | GET | 获取前台今日统计数据 | P1 |

### 5.2 待诊列表查询 API 设计

```
GET /api/medical-cases/combined-pending?doctorId={id}

响应:
{
  "items": [
    {
      "type": "Registration",
      "registrationId": "guid",
      "patientId": "guid",
      "patientName": "张三",
      "phoneMasked": "138****1234",
      "status": "Waiting",
      "source": "Receptionist",
      "registrationTime": "2026-04-12T09:00:00Z",
      "doctorId": "guid"
    },
    {
      "type": "MedicalCase",
      "medicalCaseId": "guid",
      "patientId": "guid",
      "patientName": "李四",
      "phoneMasked": "139****5678",
      "status": "Active",
      "createdTime": "2026-04-12T09:30:00Z",
      "doctorId": "guid"
    }
  ],
  "totalCount": 2
}

排序规则:
1. Registration Waiting 优先 (新挂号)
2. MedicalCase Active 其次 (进行中)
3. MedicalCase Suspended 最后 (挂起)
4. 同一类型按时间升序
```

---

## 六、开发计划

### Phase 1: 快速修复 (4-6 小时)

| 需求 | 变更文件 | 变更类型 |
|------|----------|----------|
| REQ-002: 修复 PatientDetail 为 null | `PendingQueueViewModel.cs` | Bug 修复 |
| REQ-003: 隐藏统计卡片 | `ClinicalHomeView.xaml`, `ClinicalHomeViewModel.cs` | UI 简化 |
| REQ-005: 统一按钮文案 | `PatientSelectionView.xaml` | 文案修改 |
| REQ-008: 统一错误守卫 | `PatientSelectionViewModel.cs`, `PendingQueueViewModel.cs` | 代码质量 |

### Phase 2: 功能补齐 (5-7 小时)

| 需求 | 变更文件 | 变更类型 |
|------|----------|----------|
| REQ-006: 清理多余命令 | `ClinicalHomeView.xaml`, `ClinicalHomeViewModel.cs` | 代码清理 |
| REQ-004: 三选一弹窗 | `PatientSelectionViewModel.cs`, `PendingQueueViewModel.cs` | 功能补齐 |
| REQ-007: 自动刷新 | `PatientSelectionViewModel.cs` | 功能补齐 |
| REQ-001: 统一医案创建 | `PatientSelectionViewModel.cs`, 后端 API | 功能修复 |

### Phase 3: 体验优化 (5-6 小时)

| 需求 | 变更文件 | 变更类型 |
|------|----------|----------|
| REQ-009: 患者搜索分页 | `PatientSelectionViewModel.cs`, `PatientSelectionView.xaml` | 体验优化 |
| REQ-010: 读卡器条件显示 | `PatientSelectionView.xaml` | 体验优化 |
| REQ-011: 空状态优化 | `PatientSelectionView.xaml` | 体验优化 |

### Phase 4: 前台工作台 (6-8 小时)

| 需求 | 变更文件 | 变更类型 |
|------|----------|----------|
| ARCH-003: 新建挂号直接打开 Dialog | `ReceptionistHomeViewModel.cs` | 功能修复 |
| ARCH-004: 修复前台统计 | `ReceptionistHomeViewModel.cs` | 功能修复 |
| ARCH-005: 实现 RefreshDataCommand | `ReceptionistHomeViewModel.cs` | 功能补齐 |
| ARCH-006: 创建 ReceptionistWorkspaceView | 新文件 | 新功能 |
| ARCH-007: EventAggregator 通知 | 全系统 | 架构改进 |

---

## 七、术语表

| 术语 | 含义 |
|------|------|
| 开始接诊 | 医生首页的主操作入口，点击后进入患者选择页 |
| 开始看诊 | 患者选择页的确认操作，点击后创建/打开医案并进入工作台 |
| 医案 (MedicalCase) | 一次完整的诊疗记录，DDD 聚合根 |
| 挂号 (Registration) | 患者就诊的排队记录 |
| 待诊队列 | 合并 Registration Waiting + MedicalCase Active/Suspended 的患者列表 |
| BR-001 | 医案碰撞处理规则：创建医案前检查患者是否有 Active/Suspended 医案 |
| BR-002 | 离开决策规则：有未保存变更时的弹窗逻辑 |
| BR-003 | 完成校验规则：诊断+处方必填项校验 |
| Clinical 模式 | 临床看诊模式，从患者选择/待诊队列进入，默认 Editing |
| Management 模式 | 医案管理模式，从医案列表进入，默认 ReadOnly |

---

*文档版本: v1.0 | 创建日期: 2026-04-12 | 状态: 待评审*
