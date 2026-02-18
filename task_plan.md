# Task Plan

## Goal
全面分析全部 PRD 文档，验证功能和逻辑是否闭环，识别所有不一致和缺失，逐段确认后修复。

## Current Phase
Phase 1: BRAINSTORM -> complete (全四段已完成)

---

## Phases

### Phase 1: BRAINSTORM - 全量 PRD 闭环分析
- Status: complete
- Tasks:
  - [x] 读取全部 20 个文档 (4 product + 16 requirements)
  - [x] 系统性分析：FR编号/数量/跨引用/角色/数据模型/错误码/业务规则
  - [x] 第一段呈现并修复 (功能缺失 -- 2 个问题)
  - [x] 第二段呈现并修复 (数据模型缺陷 -- 6 个问题)
  - [x] 第三段呈现并修复 (错误码体系 + 双模式覆盖 -- 3 个问题)
    - [x] 问题 9: 错误码全量分配 90 个 (决策 A, 已执行)
    - [x] 问题 10: FR-AUTH-007 本地模式修复 (已执行)
    - [x] 问题 11: 降级为非问题
  - [x] 第四段呈现并修复 (边界条件 + 其他 -- 5 个问题)
    - [x] 问题 12: 打印保护策略 (MC-D15, IsPrinted 提升到 MedicalCase)
    - [x] 问题 13: 患者禁用规则 (FR-PAT-013, MC-D16, 角色脱敏)
    - [x] 问题 14: 缓存失效策略 (nfr.md 完整重写 + 客户端配置)
    - [x] 问题 15: 分页参数统一 (NFR-API-001)
    - [x] 问题 16: 身份证脱敏示例 (降级为非问题)
  - [x] 用户最终确认 (2026-02-18)

---

## Decisions Made

### 第一段修复 (v1.7)
- MC-D13: 历史处方复制价格策略 = 实时获取 (与 FR-MC-016 一致)
- MC-D14: 总价计算公式 = SingleDosePrice x DosageCount x Discount
- 新增 FR-MC-018 复制历史处方 (10 条业务规则)
- FR-MC-004 补充总价计算公式

### 第二段修复 (v1.8)
- Prescription 补充 LastPrintedAt 字段
- DecocteMethod 新增完整枚举定义 (7 种煎法)
- Dosage/UnitPrice/Amount 补充单位语义
- ReferencedFormulas 改为 JSON 数组格式
- DoctorName/PatientName 标注创建时快照语义
- 错误消息"病案"全部修正为"医案"

### 第三段修复
- 问题 9: 错误码全量分配 90 个编号到 6 个文件 (MCCEE 体系)
- 问题 10: FR-AUTH-007 本地模式 "保持登录" 明确为重置计时器
- 问题 11: sync.md 矛盾降级为非问题

## Errors Encountered
(无)

---

## 待呈现的分析结果 (第四段)

### 第四段: 边界条件 + 其他
已分析完成，核心发现:

**高严重度:**
- 打印与编辑并发策略缺失 (PrintVersion 递增规则不清; 聚合保存是否绕过打印保护)
- 患者禁用规则缺失 (无 FR-PAT-006; 禁用后能否创建医案未定义)

**中严重度:**
- 缓存失效策略不完整 (写操作后何时清除缓存未列表化)
- 分页参数统一性 (各文档默认值一致但验证规则未统一)
- 身份证脱敏示例错误 (nfr.md 示例为 22 位，应为 18 位)

**下一步**: 呈现第四段详细分析，逐个问题决策并修复。
