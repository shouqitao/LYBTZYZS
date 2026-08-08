# 任务 A-22：设计蓝图逐项目验证（每个 class 有设计依据）

> 依据：产品负责人要求「深入审查每个 project，确保每个 project 或每个 class 的设计都是有依据的」
> 前置：蓝图 v1.0 `14-structure-design-blueprint.md`（34 项目全量设计依据）
> 目标：**逐 project 逐 class 核对**——每个类都能在设计蓝图/ADR/架构文档中追溯到设计依据；无法追溯的类 = 孤儿类（无依据设计），需要标注处置

## 方法

对 34 个项目的**每个 .cs 文件/类**，判定其「设计依据」：

| 依据等级 | 判定 | 处置 |
|---------|------|------|
| A 有明确依据 | 类职责对应蓝图 §1-§4 定义 / ADR / 架构文档 / US 需求 | ✅ 无需处理 |
| B 有隐含依据 | 类是支撑性实现（如 DTO 助手、验证器、扩展方法），从属于有依据的父类/模块 | ✅ 记录归属 |
| C 依据存疑 | 类存在但难以对应任何设计决策（僵尸/冗余/私有实现细节） | ⚠️ 列清单待决策 |
| D 无依据 | 孤儿类（零引用/仅测试用/被绕过）| ❌ 建议删除或合并 |

## 审计范围（34 项目）

### Shared 5
LYBT.Entities / LYBT.Shared.Models / LYBT.Shared.Configuration / LYBT.Shared.ExceptionHandling / LYBT.Shared.Logging

### Server 10
LYBT.Infrastructure / 8 Modules / LYBT.WebAPI

### Desktop 16
Core 6（Contracts/Foundation/Infrastructure/Controls/Printing/LocalWebAPI）+ Modules 7 + Roles 2 + Shell

### Tests 3
Architecture / Server / Desktop

## 产出格式

```
## Project: LYBT.Module.Patients（23 文件）
| 类 | 依据等级 | 设计依据（蓝图§/ADR/文档）| 处置 |
|----|---------|--------------------------|------|
| Patient | A | 蓝图§2.2 Patients 模块 / Entities 实体源 | 保留 |
| PatientService | A | 蓝图§2.4 Service 层 / 03-server §Service 规范 | 保留 |
| ... | C | ... | 待决策 |
| XxxHelper | D | 无对应设计决策 | 建议删除 |

汇总：A=xx B=xx C=xx D=xx；孤儿类清单：[...]
```

## 重点核查项（基于已有审计发现）

1. **Server 模块内**：Application/Domain/Infrastructure 每层类是否都有职责依据（尤其 CQRS Handler 是否对应 US）
2. **Desktop 模块内**：ViewModel/Service/Repository 是否遵循 MVVM 分层（A-21 M5 后是否还有遗漏越层）
3. **Infrastructure 14 类职责**：逐类标注归属（Http 应属 Foundation、CardReader 待独立、LocalData 已废弃——验证是否清理干净）
4. **Shared.Models 120 文件**：八目录（Contracts/Enums/Primitives/Utilities/Validators/Attributes/DTOs/Extensions）每类归属
5. **查找 C/D 级类**：无法追溯到设计决策的类，重点排查

## 硬性约束

1. 只读审计，禁改代码
2. 每 class 判定必须可复核（依据等级 + 证据）
3. 产出报告：`docs/03-architecture/structure-audit-perclass-2026-08-08.md`（技术总监）或 `-mimo-2026-08-08.md`（Mimo）
4. 不 commit（技术总监统一提交）
5. 用 serena/codebase-memory 符号级验证，grep 只做初筛

## 不做

- ❌ 不修改任何 class（审计先行）
- ❌ 不重写蓝图（蓝图 v1.0 是基线，验证结果只记录偏差）
- ❌ 不评估业务正确性/性能（另立专项）
