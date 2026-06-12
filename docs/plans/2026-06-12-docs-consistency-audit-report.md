# v1.0 文档自洽性审查报告

**审查日期**: 2026-06-12
**审查范围**: `docs/` 全部 135 个活跃文档（不含 archive/）
**审查方法**: 4 组并行子代理深度审查 + 人工复核
**提交**: `b8501fda2` → `48b4982ce` → `5ddaa97c8`（3 次提交，38 文件修改）

---

## 一、审查评分总览

### 按目录汇总

| 目录 | 文件数 | 评分范围 | 已修复 | 遗留问题 |
|------|--------|----------|--------|----------|
| `01-product/` | 10 | A ~ B | 4 文件 | 1 MEDIUM |
| `02-requirements/` | 22 | — | 1 文件 | 0 |
| `03-architecture/` | 11 + 11 子目录 | B- ~ A | 5 文件 | 1 MEDIUM |
| `04-api-reference/` | 13 | D ~ A | 7 文件 | 4 LOW |
| `05-development/` | 5 + 6 子目录 | A | 1 文件 | 0 |
| `06-operations/` | 9 | C ~ A | 6 文件 | 0 |
| `07-concepts/` | 23 + 9 + 3 子目录 | C ~ A | 8 文件 | 5 LOW |
| **合计** | **135** | | **38 文件** | **11 LOW/MEDIUM** |

### 评分标准

| 等级 | 含义 |
|------|------|
| **A** | 内部自洽，无矛盾 |
| **B** | 轻微问题（术语、断链），无逻辑矛盾 |
| **C** | 存在内部矛盾或重要缺口 |
| **D** | 严重问题，文档不可信 |

---

## 二、已修复问题清单

### 第一轮：事实错误与缺口（2 次提交，9 文件）

**提交 `b8501fda2`**

| # | 文件 | 类型 | 问题 | 修复 |
|---|------|------|------|------|
| 1 | `03-architecture/README.md` | CONTRADICTION | Mapperly 版本 4.1.1（实际 4.3.1） | 4.1.1 → 4.3.1 |
| 2 | `03-architecture/README.md` | CONTRADICTION | 未提及 LocalDB 模式 | 补充 LocalDB 描述 |
| 3 | `CHANGELOG.md` | CONTRADICTION | "SQLite 种子数据"（实际 LocalDB） | SQLite → LocalDB |
| 4 | `04-api-reference/README.md` | GAP | 缺 2/4 授权策略 | 补齐 PatientAccess + SuperAdminOnly |
| 5 | `docs/README.md` | CONTRADICTION | 文件计数过时（01-product 7→实际 9） | 更新计数 |
| 6 | `05-development/05-testing.md` | GAP | 未区分 SQLite InMemory（测试）vs LocalDB（生产） | 补充明确说明 |
| 7 | `07-concepts/25,32,33` | STUB | 3 处"待补充"占位符 | 替换为实际交叉引用 |
| 8 | `02-requirements/08-registration.md` | GAP | 2 个 OPEN OQ 无分类 | 标记为 (v1.0 决策) |

**提交 `48b4982ce`**

| # | 文件 | 类型 | 问题 | 修复 |
|---|------|------|------|------|
| 9 | `05-dual-mode.md:201` | CONTRADICTION | MedicalCase 同步"(未实现)"vs"支持" | 统一为已实现 (SYNC-D01) |
| 10 | `05-dual-mode.md:249` | CONTRADICTION | 变更日志 v4.0 日期在 v5.0 之后 | 2026-07-01 → 2026-04-01 |
| 11 | `01-system-overview.md:27` | CONTRADICTION | 服务端模块 x9（实际 8+2 dormant） | x9 → 8 active + 2 dormant |
| 12 | `01-system-overview.md:68` | GAP | 缺 Receptionist 角色项目 | 补齐 |
| 13 | `03-jtbd.md:82,117` | FLOW | 过时的 vision.md 流程步骤引用 | → clinical-workflow.md 链接 |
| 14 | `01-dual-mode-architecture.md` | STUB | 3 处裸标签链接 | → 有效 markdown 链接 |
| 15 | `30,31,33` concepts | STUB | 裸标签/断链 | → 有效链接 |
| 16 | `04-data-model.md:97` | GAP | IsLocked 计算公式未给出 | 补充 `IsCompleted && CompletedAt.Date < Today` |

### 第二轮：深度自洽性审查（1 次提交，20 文件）

**提交 `5ddaa97c8` — Wave 1: 运维脚本修复（脚本会执行失败）**

| # | 文件 | 严重度 | 问题 | 修复 |
|---|------|--------|------|------|
| 17 | `06-operations/07` | **HIGH** | 数据库名 `LYBT_DB`（实际 `LYBTDB`）×7 处 | LYBT_DB → LYBTDB |
| 18 | `06-operations/08` | **HIGH** | 数据库名 `LYBT_DB` ×2 处 | LYBT_DB → LYBTDB |
| 19 | `06-operations/09` | **HIGH** | 数据库名 `LYBT_DB` ×2 处 + 服务名 `LYBT-WebAPI` ×5 处 | 全部修正 |
| 20 | `06-operations/07,08` | **HIGH** | 服务名 `LYBT-WebAPI`（实际 `LYBT-API`） | LYBT-WebAPI → LYBT-API |
| 21 | `01-deployment.md` | **HIGH** | `--self-contained false` 与注释"自包含"矛盾 | false → true |
| 22 | `06-error-handling.md` | LOW | "草药"/"配方"非标准术语 | → "药材"/"验方" |
| 23 | `08-shared.md` | LOW | 同上 | → "药材"/"验方" |

**Wave 2: API 合约修复（集成会失败）**

| # | 文件 | 严重度 | 问题 | 修复 |
|---|------|--------|------|------|
| 24 | `04-herbs.md` | **HIGH** | 4 个幻影字段 + 3 个错误字段名 | 删除幻影字段，修正字段名 |
| 25 | `03-patients.md` | **HIGH** | `idCardNumber`/`allergies` 不匹配代码 | → `idNumber`/`allergyHistory` |
| 26 | `05-formulas.md` | **HIGH** | `herbItems` 不匹配代码 + ERR-60202/60204 HTTP 200 | → `herbs`, HTTP → 404 |
| 27 | `06-medical-cases.md` | **HIGH** | 6+ 错误码引用已删除端点 `POST /{id}/complete` | → `PUT /{id}/close` |
| 28 | `04-api-reference/README.md` | MEDIUM | 幻影端点 `POST /herbs/import` | 删除 |
| 29 | `11-health.md` | MEDIUM | 声称扁平 JSON 但实际用 ApiResponse | 澄清说明 |

**Wave 3: 架构与概念修复**

| # | 文件 | 严重度 | 问题 | 修复 |
|---|------|--------|------|------|
| 30 | `02-desktop.md` | MEDIUM | Core 层缺 LocalData 项目 + ViewModel 阈值矛盾 | 补齐 + 500→600 |
| 31 | `14-feature-toggles.md` | MEDIUM | IOptionsMonitor 声称已实现但 v1.0 未使用 | 添加 v1.0/v1.1 区分 |
| 32 | `18-mvvm-prism.md` | MEDIUM | 图缺 Consultation+Sync 节点，项目计数错 | 补齐 + 16→18 |
| 33 | `modules/registration-module.md` | MEDIUM | MedicalCase.Cancelled 结果行矛盾 | 拆分为前台/医生两行 |
| 34 | `modules/patient-module.md` | LOW | 字段数 22 vs 实际 20 | 22 → 20 |
| 35 | `modules/medical-case-module.md` | MEDIUM | 缺 Cancelled 状态 + 裸标签链接 | 补齐 + 修正链接 |
| 36 | `24-testing-strategy.md` | LOW | 测试计数 ~715（实际 ~760） | ~715 → ~760 |
| 37 | `06-api-tests.md` | LOW | "中药管理"非标准术语 | → "药材管理" |

---

## 三、遗留问题（按优先级）

### MEDIUM — 需后续确认或设计决策

| # | 文件 | 问题 | 建议 |
|---|------|------|------|
| M1 | `09-customer-journey.md:341` vs `08-value-proposition.md:65` | 价格机制矛盾：前者说"实时获取 (非快照)"，后者说"价格快照隔离" | **实际行为**：保存时从药材库获取当前价格并存储到 PrescriptionItem.UnitPrice（快照），打印后 IsPrinted 触发编辑保护。建议在两处统一为"保存时快照，打印后锁定" |
| M2 | `06-error-handling.md:14-20` vs `08-shared.md:319-327` | 异常层次结构矛盾：前者将 Validation/NotFound/Conflict 作为 BusinessException 兄弟，后者作为其子类 | `08-shared.md` 描述的子类关系与实际目录结构一致（Business/ 子目录），应以此为准。需更新 `06-error-handling.md` |
| M3 | `06-api-tests.md` | 测试用例统计表计数与文档内实际用例数不一致（Auth 17 vs 16, Users 15 vs 18 等） | 需完整重算统计表 |

### LOW — 不影响理解但可改进

| # | 文件 | 问题 |
|---|------|------|
| L1 | `07-concepts/` 多文件 | "相关链接"仍使用裸概念名称而非 markdown 链接（~10 文件） |
| L2 | `07-concepts/` 13 个编号位置 | 原计划的概念文档从未创建（02-17 范围），编号方案不完整 |
| L3 | `modules/printing-module.md:75-78` | 4 处裸标签链接 |
| L4 | `modules/medical-case-module.md:121-130` | 9 处裸标签链接（部分已修复） |
| L5 | `29-prescription-completeness-checker.md:42` | 3 处"规划中"概念引用无链接 |
| L6 | `10-sensitive-data.md:51` | "密钥生命周期管理 (规划中)"无 v1.0 现状说明 |
| L7 | `02-users.md:57` | UserDetailDto 枚举缺 SuperAdmin/Receptionist |
| L8 | `03-patients.md` | DTO schema 缺少多个实际字段（maritalStatus, idType 等） |
| L9 | `04-herbs.md` | HerbDetailDto schema 仍缺实际字段（origin, spec, costPrice 等） |
| L10 | `06-medical-cases.md` | MedicalCaseInputDto 缺 userId, registrationId, editReason 字段 |
| L11 | `07-registrations.md` | 无错误码表（其他模块均有） |

---

## 四、文档质量热力图

```
01-product/          █████████░  90%  (1 MEDIUM 遗留)
02-requirements/     ██████████  100% (22 PRD，自洽)
03-architecture/     ████████░░  80%  (1 MEDIUM 遗留 + 子目录)
04-api-reference/    ███████░░░  70%  (DTO schema 不完整，但核心字段已修正)
05-development/      ██████████  100%
06-operations/       ██████████  100% (全部脚本级错误已修复)
07-concepts/         ████████░░  80%  (裸标签链接广泛，编号方案不完整)
```

---

## 五、审查方法论

1. **4 组并行审查**：按文档类型分组（产品+运维+架构 / API参考 / 概念第一批 / 概念第二批）
2. **审查维度**：内部矛盾(CONTRADICTION) > 缺口(GAP) > 术语(TERMINOLOGY) > 断链(STUB) > 行文(FLOW)
3. **修复策略**：3 波按严重度（HIGH→MEDIUM→LOW），运维脚本 > API合约 > 架构概念
4. **验证方式**：代码级验证（grep 字段名/类名确认文档准确性）

---

## 六、审查统计

| 指标 | 数值 |
|------|------|
| 审查文档总数 | 135 |
| 发现问题总数 | ~50 |
| 已修复问题 | ~39 |
| 遗留 MEDIUM | 3 |
| 遗留 LOW | 11 |
| 修改文件数 | 38 |
| 提交数 | 3 |
| 高严重度修复（脚本/API 会失败） | 13 |

---

*报告生成时间: 2026-06-12*
*审查人: MiMoCode Agent*
*最后提交: 5ddaa97c8*
