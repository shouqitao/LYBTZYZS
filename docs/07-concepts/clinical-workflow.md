---
type: concept
title: 端到端临床工作流
created: 2026-06-10
updated: 2026-06-10
tags: [clinical-workflow, medical-case, registration, prescription, business-rules]
related: [medical-case, consultation, prescription, patient, herb, formula, registration, user, print-log, dual-mode-architecture, ef-core-data-model, authentication, business-rules]
sources: ["docs/01-product/clinical-workflow.md"]
---

# 端到端临床工作流

端到端临床工作流是凌隐宝堂中医诊所管理系统的核心业务流程，定义了从患者到达诊所到诊疗完成离开的完整数字化路径。该流程串联了患者管理、挂号、医案创建、诊断、处方开具、打印和完成等多个模块，是系统业务逻辑的集中体现。

## 流程全景

系统支持两种启动诊疗的入口模式：
1.  **前台挂号模式**：由前台（Receptionist）通过读卡器或手动查询创建患者，并指派医生进行挂号，生成状态为`Waiting`的[[registration|Registration]]记录。
2.  **医生直接创建模式**：当没有前台时，医生（Doctor）可直接查询或创建患者，并发起诊疗。

两种模式最终都收敛于医生创建[[medical-case|MedicalCase]]，进入核心诊疗阶段。

## 核心阶段

1.  **创建医案**：医生选择患者后，系统执行**BR-001碰撞检查**，确保患者没有未完成的医案（Active或Suspended状态）。创建成功后，生成[[medical-case|MedicalCase]]（Active状态）和[[consultation|Consultation]]（1:1共享主键）。
2.  **填写诊断**：医生填写中医辨证（TcmDiagnosis）等诊断信息。中医辨证在医案完成时为必填项。
3.  **处方决策与开具**：根据`NeedsPrescription`标记决定是否需要处方。处方可通过三种方式获取：验方导入、历史处方复制或手工输入。处方数据存储在[[prescription|Prescription]]和[[prescription-item|PrescriptionItem]]中。
4.  **聚合保存**：采用[[medical-case|MedicalCase]]作为聚合根，将医案、诊断、处方及其明细进行原子性保存。保存时涉及**打印保护检查（MC-D15）**和**乐观锁（RowVersion）**并发控制。
5.  **打印**：打印是[[medical-case|MedicalCase]]聚合根的能力。打印后，`IsPrinted`标记为`true`，`PrintCount`递增，并生成[[medical-case-print-log|MedicalCasePrintLog]]记录。
6.  **完成医案**：执行**BR-003完成校验**，确保诊断、处方标记和处方内容完整。完成后，医案状态变为`Completed`，关联的[[registration|Registration]]状态也同步更新为`Completed`。

## 关键业务规则

- **BR-001 碰撞检查**：创建医案时，检查患者是否有Active或Suspended状态的医案，防止数据冲突。
- **BR-003 完成校验**：医案完成前必须满足诊断、处方标记和处方内容的完整性要求。
- **打印保护 (MC-D15)**：已打印的医案在修改诊断或处方时，必须提供修改原因（EditReason），保存后`IsPrinted`重置为`false`，`PrintVersion`递增，提示需要重新打印。
- **重复药材合并策略 (MC-D17)**：导入处方时，处理重复药材的策略（Max, Min, Accumulate, Skip, Replace）。

## 状态机与异常处理

[[medical-case|MedicalCase]]具有明确的状态机：`Active` ↔ `Suspended` → `Completed`。取消医案通过软删除（`IsDeleted=true`）实现。

文档详细定义了异常路径的处理流程，包括：
- **BR-001碰撞处理**：当发现碰撞时，提供“重开现有医案”、“关闭旧的后新建”或“取消操作”三种选择。
- **BR-002离开界面决策**：根据编辑模式（Clinical/Management）和是否有未保存变更，提供不同的离开选项（挂起、关闭、完成、保存、放弃等）。
- **并发冲突处理**：通过乐观锁（RowVersion）和3次重试机制处理多用户同时编辑的冲突。

## 跨模块联动

临床工作流体现了系统各模块间的紧密联动：
- **Registration联动**：医案创建、完成、取消会触发[[registration|Registration]]状态的相应变更。
- **药材禁用联动**：禁用[[herb|Herb]]会影响新建处方、验方导入和历史处方复制。
- **患者禁用联动**：禁用[[patient|Patient]]会阻止新医案的创建，并影响历史医案的查阅显示。

## 相关概念

- [[medical-case]]：作为核心聚合根，承载了临床工作流的主要数据和状态。
- [[business-rules]]：集中定义了BR-001、BR-003、MC-D15等业务规则。
- [[dual-mode-architecture]]：临床模式（Clinical）和管理模式（Management）决定了UI交互和离开界面时的行为。