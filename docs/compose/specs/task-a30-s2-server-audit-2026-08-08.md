# 任务 A-30-S2：Server 层方法级深审

> 派发对象：Mimo Code（审查只读，不改代码）
> 版本：v1.0 | 日期：2026-08-08
> 依据：`docs/compose/plans/2026-08-08-method-audit-plan.md`（S2 阶段）+ S0 基线 + S1 Shared 结论
> 用户方针：先收敛再完善

## 任务

对 **Server 层 10 项目**进行方法级深度审查（A/B/C/D/E 分级），重点是横切机制方法重复 + 跨模块方法 + Service/Handler 方法边界，为 Server 项目合并方案提供依据。

## 范围

- ✅ `src/Server/` 全部：Core/LYBT.Infrastructure + 8 Modules（Auth/Users/Patients/Herbs/Formula/MedicalCase/Registration/Reports）+ Services/LYBT.WebAPI
- ✅ 消费方核实（Desktop/LocalWebAPI 引用 Server 的位置，只读）
- ❌ 排除：Shared（S1 已审）、Desktop 模块自身（S3）
- 仓库根：`D:\source\repos\LYBTZYZS`｜分支 `master`｜基线 `4a956a851` + S0/S1 产出

## 审查维度（方法级）

沿用 A/B/C/D/E 分级，重点：

### 1. 横切机制方法重复（C 级重点）
- **日志**：Server 侧日志方法（SerilogMSSqlServerExtensions/ApiLoggingFilter/CorrelationIdMiddleware 等）与 S1 日志专项方案的关联——哪些方法应迁移到 Shared.Logging 集中项目？
- **异常**：SystemExceptionHandler/BusinessExceptionHandler 方法（Infrastructure）与 S1 异常专项方案的关联——处理器方法是否应迁往 ExceptionHandling？
- **配置**：各模块 Options/配置读取方法是否重复？
- **错误码/常量**：ErrorCode/PolicyConstants 使用点是否一致？

### 2. 跨模块方法（C 级）
- ICrossModuleService 族方法：是否每模块重复实现相似查询（如"按 ID 取名称"）？
- 跨模块方法调用链是否清晰（P07 边界）？

### 3. Service/Handler 方法边界（A-28 后复核）
- 双轨规范化后：写操作是否全部走 Handler、读走 Service（蓝图 §2.2 规则是否真落地）？
- Service 中是否还有"写方法残留"（应迁 Handler 未迁）？
- Handler 中是否有"读逻辑"（应走 Service）？

### 4. 仓储方法（A-28 后复核）
- BaseRepository 继承者方法 vs 裸实现方法：CRUD 方法是否还有重复？
- 例外仓储（User/AuthSession/SecurityAudit/Registration/Report/SystemLog）方法是否有可收敛的？

### 5. 死方法（D 级）
- 0 调用方法（结合 S0 死方法候选符号级复核）

### 6. 合并候选识别（本次新增，供 S4 整合）
- **模块内目录**：同模块内 Application/Domain/Infrastructure/Interfaces/Services 方法分布——是否有目录可合并（如 Mappers 位置统一）？
- **跨模块合并候选**：哪些模块的方法集高度相似（如 Herbs/Formula 的 CRUD 方法）→ 模块合并可行性？
- **Infrastructure 职责**：Infrastructure 方法是否过载（跨模块基础设施 vs 该属模块）？

## 产出

1. 10 项目方法分级表（项目/类/方法/级/证据）
2. C 级重复清单（横切/跨模块/仓储）+ 收敛方向
3. D 级死方法清单（符号级确认）
4. **Server 合并候选分析**（哪些项目/目录可合并，方法级证据）
5. 与 S1 日志/异常专项的衔接（哪些 Server 方法应迁移）

## 硬性约束

1. **只读**：不修改任何 src 代码；只写报告
2. 每发现带证据：`文件:行号` + 调用链
3. **不要 commit**：报告由技术总监统一提交
4. 产出单一报告：`docs/compose/reports/method-audit-server-2026-08-08.md`

## 报告结构

```
# Server 层方法级深审报告（Mimo 独立分析）
## 0. 方法说明
## 1. 10 项目方法分级总表（A/B/C/D/E 计数）
## 2. 横切机制方法（日志/异常/配置/常量）与 S1 专项衔接
## 3. 跨模块方法清单
## 4. Service/Handler 边界复核（蓝图 §2.2）
## 5. 仓储方法复核（A-28 后）
## 6. C 级重复清单 + 收敛方向
## 7. D 级死方法清单
## 8. 合并候选分析（模块内目录 + 跨模块 + Infrastructure）
## 9. 统计汇总
```

## 明确不做（防发散）

- ❌ 不审 Shared（S1 已审）
- ❌ 不审 Desktop 模块（S3）
- ❌ 不修改任何代码
- ❌ 不执行合并（只出候选分析，S4 整合 + 执行另立批次）
